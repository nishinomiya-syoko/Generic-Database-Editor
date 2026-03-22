可以。
如果你想用 **UniTask** 优化，这里比较适合做的不是把“单次搜索”强行异步化，而是把 **高频更新与批量搜索调度** 做成 **分帧 / 定时 / 可取消** 的异步流程，从而减少主线程尖峰。

因为：

* **坐标检索本身** 是纯内存计算，单次通常很快
* 真正容易卡的是：

  * 大量单位每帧都更新网格
  * 大量单位同一帧同时索敌
  * 搜索结果后续还要做排序、过滤、决策

所以 UniTask 最合适的优化方向是：

1. **单位位置更新改为低频异步轮询**
2. **索敌改为可取消的异步循环**
3. **大批量搜索分批执行，避免同帧尖峰**
4. **尽量复用 List，减少 GC**

下面给你一版适合 **Unity + UniTask + RTS 平面地图** 的实现。

---

# 方案说明

这版会包含：

* `ITargetable`：目标接口
* `TargetEntity`：目标组件
* `TargetSearchSystem`：网格索敌核心
* `AsyncTargetTracker`：用 UniTask 低频更新坐标格子
* `AsyncSeeker`：用 UniTask 定时索敌，而不是每帧硬查

---

# 1）ITargetable.cs

```csharp
using UnityEngine;

namespace RTS.TargetSearch
{
    public interface ITargetable
    {
        int Id { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        int TeamId { get; }
        GameObject Owner { get; }
    }
}
```

---

# 2）TargetEntity.cs

```csharp
using UnityEngine;

namespace RTS.TargetSearch
{
    public class TargetEntity : MonoBehaviour, ITargetable
    {
        [SerializeField] private int id;
        [SerializeField] private int teamId;
        [SerializeField] private bool isAlive = true;

        public int Id => id;
        public Vector3 Position => transform.position;
        public bool IsAlive => isAlive;
        public int TeamId => teamId;
        public GameObject Owner => gameObject;

        public void SetAlive(bool value)
        {
            isAlive = value;
        }

        private void OnEnable()
        {
            TargetSearchSystem.Register(this);
        }

        private void OnDisable()
        {
            TargetSearchSystem.Unregister(this);
        }
    }
}
```

---

# 3）TargetSearchSystem.cs

核心仍然保持 **同步搜索**，因为这部分最好做到最轻量。
异步只用于“调度”。

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace RTS.TargetSearch
{
    public static class TargetSearchSystem
    {
        public static float CellSize = 5f;

        private static readonly Dictionary<Vector2Int, HashSet<ITargetable>> Grid =
            new Dictionary<Vector2Int, HashSet<ITargetable>>();

        private static readonly Dictionary<ITargetable, Vector2Int> EntityCellMap =
            new Dictionary<ITargetable, Vector2Int>();

        public static void Register(ITargetable entity)
        {
            if (entity == null || EntityCellMap.ContainsKey(entity))
                return;

            Vector2Int cell = WorldToCell(entity.Position);
            EntityCellMap[entity] = cell;

            if (!Grid.TryGetValue(cell, out var bucket))
            {
                bucket = new HashSet<ITargetable>();
                Grid[cell] = bucket;
            }

            bucket.Add(entity);
        }

        public static void Unregister(ITargetable entity)
        {
            if (entity == null)
                return;

            if (!EntityCellMap.TryGetValue(entity, out var cell))
                return;

            if (Grid.TryGetValue(cell, out var bucket))
            {
                bucket.Remove(entity);
                if (bucket.Count == 0)
                    Grid.Remove(cell);
            }

            EntityCellMap.Remove(entity);
        }

        public static void UpdateEntityCell(ITargetable entity)
        {
            if (entity == null)
                return;

            if (!EntityCellMap.TryGetValue(entity, out var oldCell))
            {
                Register(entity);
                return;
            }

            Vector2Int newCell = WorldToCell(entity.Position);
            if (newCell == oldCell)
                return;

            if (Grid.TryGetValue(oldCell, out var oldBucket))
            {
                oldBucket.Remove(entity);
                if (oldBucket.Count == 0)
                    Grid.Remove(oldCell);
            }

            if (!Grid.TryGetValue(newCell, out var newBucket))
            {
                newBucket = new HashSet<ITargetable>();
                Grid[newCell] = newBucket;
            }

            newBucket.Add(entity);
            EntityCellMap[entity] = newCell;
        }

        public static void SearchInRangeNonAlloc(
            Vector3 center,
            float radius,
            List<ITargetable> results,
            int? excludeTeamId = null)
        {
            results.Clear();

            float radiusSqr = radius * radius;
            GetSearchCellBounds(center, radius, out int minX, out int maxX, out int minY, out int maxY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!Grid.TryGetValue(cell, out var bucket))
                        continue;

                    foreach (var entity in bucket)
                    {
                        if (entity == null || !entity.IsAlive)
                            continue;

                        if (excludeTeamId.HasValue && entity.TeamId == excludeTeamId.Value)
                            continue;

                        Vector3 pos = entity.Position;
                        float dx = pos.x - center.x;
                        float dz = pos.z - center.z;
                        float distSqr = dx * dx + dz * dz;

                        if (distSqr <= radiusSqr)
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }

        public static ITargetable FindNearest(
            Vector3 center,
            float radius,
            int? excludeTeamId = null)
        {
            float radiusSqr = radius * radius;
            float minDistSqr = float.MaxValue;
            ITargetable nearest = null;

            GetSearchCellBounds(center, radius, out int minX, out int maxX, out int minY, out int maxY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!Grid.TryGetValue(cell, out var bucket))
                        continue;

                    foreach (var entity in bucket)
                    {
                        if (entity == null || !entity.IsAlive)
                            continue;

                        if (excludeTeamId.HasValue && entity.TeamId == excludeTeamId.Value)
                            continue;

                        Vector3 pos = entity.Position;
                        float dx = pos.x - center.x;
                        float dz = pos.z - center.z;
                        float distSqr = dx * dx + dz * dz;

                        if (distSqr <= radiusSqr && distSqr < minDistSqr)
                        {
                            minDistSqr = distSqr;
                            nearest = entity;
                        }
                    }
                }
            }

            return nearest;
        }

        private static Vector2Int WorldToCell(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / CellSize);
            int y = Mathf.FloorToInt(position.z / CellSize);
            return new Vector2Int(x, y);
        }

        private static void GetSearchCellBounds(
            Vector3 center,
            float radius,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY)
        {
            Vector2Int minCell = WorldToCell(new Vector3(center.x - radius, 0f, center.z - radius));
            Vector2Int maxCell = WorldToCell(new Vector3(center.x + radius, 0f, center.z + radius));

            minX = minCell.x;
            maxX = maxCell.x;
            minY = minCell.y;
            maxY = maxCell.y;
        }

        public static void Clear()
        {
            Grid.Clear();
            EntityCellMap.Clear();
        }
    }
}
```

---

# 4）AsyncTargetTracker.cs

这个组件用于 **异步低频更新单位所在网格**。
比每帧 `UpdateEntityCell()` 更平滑。

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RTS.TargetSearch
{
    [DisallowMultipleComponent]
    public class AsyncTargetTracker : MonoBehaviour
    {
        [SerializeField] private int updateIntervalMs = 100;

        private TargetEntity targetEntity;
        private CancellationTokenSource cts;
        private Vector3 lastPosition;

        private void Awake()
        {
            targetEntity = GetComponent<TargetEntity>();
            lastPosition = transform.position;
        }

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            TrackLoopAsync(cts.Token).Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private async UniTaskVoid TrackLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Vector3 current = transform.position;

                if ((current - lastPosition).sqrMagnitude > 0.0001f)
                {
                    lastPosition = current;
                    if (targetEntity != null)
                    {
                        TargetSearchSystem.UpdateEntityCell(targetEntity);
                    }
                }

                await UniTask.Delay(updateIntervalMs, cancellationToken: token);
            }
        }
    }
}
```

---

# 5）AsyncSeeker.cs

这个组件用于 **异步定时索敌**。
RTS 里一般不需要每帧索敌，100ms~300ms 通常就够了。

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RTS.TargetSearch
{
    [DisallowMultipleComponent]
    public class AsyncSeeker : MonoBehaviour
    {
        [Header("Search")]
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private int searchIntervalMs = 200;
        [SerializeField] private bool enemyOnly = true;
        [SerializeField] private int selfTeamId = 0;

        private readonly List<ITargetable> results = new List<ITargetable>(32);
        private CancellationTokenSource cts;

        public Action<List<ITargetable>> OnSearchCompleted;
        public Action<ITargetable> OnNearestFound;

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            SearchLoopAsync(cts.Token).Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private async UniTaskVoid SearchLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                int? excludeTeamId = enemyOnly ? selfTeamId : null;

                TargetSearchSystem.SearchInRangeNonAlloc(
                    transform.position,
                    searchRadius,
                    results,
                    excludeTeamId);

                OnSearchCompleted?.Invoke(results);

                ITargetable nearest = null;
                float minDist = float.MaxValue;

                for (int i = 0; i < results.Count; i++)
                {
                    var target = results[i];
                    Vector3 pos = target.Position;
                    float dx = pos.x - transform.position.x;
                    float dz = pos.z - transform.position.z;
                    float dist = dx * dx + dz * dz;

                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = target;
                    }
                }

                if (nearest != null)
                {
                    OnNearestFound?.Invoke(nearest);
                }

                await UniTask.Delay(searchIntervalMs, cancellationToken: token);
            }
        }
    }
}
```

---

# 6）分帧批量索敌管理器（UniTask 优化重点）

如果你地图上有很多 AI 单位，每个单位一个异步循环虽然可用，但更优雅的方式是：
**统一调度所有 seeker，分批处理。**

下面这版更适合大量单位。

---

## BatchedSearchManager.cs

```csharp
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RTS.TargetSearch
{
    public interface IBatchedSeeker
    {
        void PerformSearch();
        bool IsValid { get; }
    }

    public class BatchedSearchManager : MonoBehaviour
    {
        public static BatchedSearchManager Instance { get; private set; }

        [SerializeField] private int batchSizePerFrame = 20;
        [SerializeField] private int loopIntervalMs = 50;

        private readonly List<IBatchedSeeker> seekers = new List<IBatchedSeeker>();
        private CancellationTokenSource cts;
        private int currentIndex;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            LoopAsync(cts.Token).Forget();
        }

        private void OnDisable()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            if (Instance == this)
                Instance = null;
        }

        public void Register(IBatchedSeeker seeker)
        {
            if (seeker == null || seekers.Contains(seeker))
                return;

            seekers.Add(seeker);
        }

        public void Unregister(IBatchedSeeker seeker)
        {
            seekers.Remove(seeker);
        }

        private async UniTaskVoid LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (seekers.Count == 0)
                {
                    await UniTask.Delay(loopIntervalMs, cancellationToken: token);
                    continue;
                }

                int processed = 0;
                int safeCount = seekers.Count;

                while (processed < batchSizePerFrame && safeCount > 0 && seekers.Count > 0)
                {
                    if (currentIndex >= seekers.Count)
                        currentIndex = 0;

                    var seeker = seekers[currentIndex];

                    if (seeker == null || !seeker.IsValid)
                    {
                        seekers.RemoveAt(currentIndex);
                        continue;
                    }

                    seeker.PerformSearch();
                    currentIndex++;
                    processed++;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}
```

---

## BatchedSeeker.cs

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.TargetSearch
{
    public class BatchedSeeker : MonoBehaviour, IBatchedSeeker
    {
        [SerializeField] private float searchRadius = 10f;
        [SerializeField] private bool enemyOnly = true;
        [SerializeField] private int selfTeamId = 0;

        private readonly List<ITargetable> results = new List<ITargetable>(32);

        public bool IsValid => this != null && gameObject.activeInHierarchy;

        public Action<List<ITargetable>> OnSearchCompleted;

        private void OnEnable()
        {
            BatchedSearchManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            BatchedSearchManager.Instance?.Unregister(this);
        }

        public void PerformSearch()
        {
            int? excludeTeamId = enemyOnly ? selfTeamId : null;

            TargetSearchSystem.SearchInRangeNonAlloc(
                transform.position,
                searchRadius,
                results,
                excludeTeamId);

            OnSearchCompleted?.Invoke(results);
        }
    }
}
```

---

# 为什么这才是 UniTask 的正确优化方式

很多人会写成这样：

```csharp
await UniTask.RunOnThreadPool(() => SearchInRange(...));
```

但这在 Unity RTS 里通常 **不是最优解**，原因是：

1. 你的目标数据来自 Unity 对象，很多字段不适合跨线程直接碰
2. 搜索本身是轻计算，切线程的收益不一定比开销大
3. 真正问题往往是“同时做太多次”，不是“单次太慢”

所以更好的思路是：

* **主线程做轻量搜索**
* **UniTask 做时间调度**
* **把 100 个单位的搜索拆到多帧**
* **把每帧位置更新改成 50~100ms 一次**

---

# 推荐配置

如果是普通 RTS：

* `CellSize = 4 ~ 8`
* `AsyncTargetTracker.updateIntervalMs = 50 ~ 100`
* `AsyncSeeker.searchIntervalMs = 100 ~ 300`

如果单位非常多：

* 优先使用 `BatchedSearchManager`
* `batchSizePerFrame = 10 ~ 30`
* 每帧分批做，而不是每单位自己开循环

---

# 极简使用方式

## 单位身上挂：

* `TargetEntity`
* `AsyncTargetTracker`

## 需要主动索敌的单位再挂：

* 少量单位：`AsyncSeeker`
* 大量单位：`BatchedSeeker` + 场景里放一个 `BatchedSearchManager`

---

# 再给你一个更实用的小优化

避免每次回调把内部 `results` 暴露出去被外部修改。
可以改成复制到缓存，或者只回调最近目标。

例如：

```csharp
public Action<ITargetable> OnNearestFound;
```

这样比把整个 `List` 到处传更稳。

---

# 最后给结论

用 UniTask 优化这类索敌，核心不是“把搜索函数异步化”，而是：

* **定时搜索替代每帧搜索**
* **异步轮询替代每帧更新**
* **批量分帧处理替代同帧爆发**
* **复用容器减少 GC**

这才是 RTS 场景下更有效的优化。

如果你愿意，我下一条可以直接继续给你一版：

1. **完整 Unity Package 风格目录**
2. **支持敌我过滤 / 最近目标 / 扇形搜索**
3. **支持 `CancellationToken` 与单位销毁自动取消**
4. **再补一个 `UniTaskAsyncEnumerable` 版事件流接口**
