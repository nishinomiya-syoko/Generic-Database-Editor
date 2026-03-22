using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RTS.TargetSearch
{
    /// <summary>
    /// 少量异步搜索，搜索指定范围内的目标 这个组件用于 **异步定时索敌**。
    /// RTS 里一般不需要每帧索敌，100ms~300ms 通常就够了。
    /// </summary>
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