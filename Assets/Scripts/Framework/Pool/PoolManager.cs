using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic
{
    /// <summary>
    /// 放到场景中的池管理器（可选单例）。
    /// 在 Inspector 中配置不同 id 对应的 prefab，然后代码中通过 id Spawn/Despawn。
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        [EditableData]
        [Serializable]
        public class PoolConfig
        {
            [Tooltip("池的唯一标识（不填则默认使用 prefab 名称）")]
            public string id;

            [Tooltip("要池化的预制件名称")]
            public string name;

            [Tooltip("要池化的预制件（需要挂有实现 IReference 的脚本，如 EntityBase 的子类）")]
            public GameObject prefab;
            // public string prefabPath;

            [Tooltip("初始预创建数量")]
            public int initialSize = 0;

            [Tooltip("池空时是否允许继续实例化")]
            public bool expandIfEmpty = true;

            [Tooltip("最大实例数量（<=0 表示不限）")]
            public int maxSize = 0;

            [HideInInspector] public Transform container;
        }

        [SerializeField]
        private List<PoolConfig> pools = new List<PoolConfig>();

        // 运行时池字典
        private readonly Dictionary<string, IPoolWrapper> runtimePools = new Dictionary<string, IPoolWrapper>();

        // 单例（可选）
        public static PoolManager Instance { get; private set; }
        private bool preWarmed = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        public void PreWarm()
        {
            if (preWarmed)
                return;
            preWarmed = true;
            LoadPoolConfigs();
            InitializePools();
        }
        private void LoadPoolConfigs()
        {
            var p = GlobalManager.Instance.DataTableManager.GetAllData<DataCenter.PoolConfig>();
            foreach (var item in p)
            {
                 var q = GlobalManager.Instance.DataTableManager.GetDataById<DataCenter.AssetPath>(item.Id);

                var poolConfig = new PoolConfig()
                {
                    id = item.Id.ToString(),
                    name = item.DisplayName,
                    prefab = GlobalManager.Instance.AssetLoader.LoadAsset<GameObject>(q.Path),
                    initialSize = item.InitialSize,
                    expandIfEmpty = item.expandIfEmpty,
                    maxSize = item.maxSize
                };
                pools.Add(poolConfig);
            }
        }

        /// <summary>
        /// 根据 Inspector 配置创建各个 ObjectPool。
        /// </summary>
        private void InitializePools()
        {
            foreach (var cfg in pools)
            {
                if (cfg.prefab == null)
                {
                    DebugInfo.LogWarning("[PoolManager] Prefab is null in config, skipped.");
                    continue;
                }

                if (string.IsNullOrEmpty(cfg.id))
                {
                    cfg.id = cfg.prefab.name;
                }

                if (runtimePools.ContainsKey(cfg.id))
                {
                    DebugInfo.LogWarning($"[PoolManager] Duplicate pool id: {cfg.id}, skipped.");
                    continue;
                }

                // 创建容器节点
                GameObject containerGo = new GameObject($"Pool:{cfg.id}");
                containerGo.transform.SetParent(transform, false);
                cfg.container = containerGo.transform;

                // 从 prefab 上找到第一个实现 IReference 的脚本，用作 T
                var monoBehaviours = cfg.prefab.GetComponents<MonoBehaviour>();
                Component templateComponent = null;

                foreach (var mb in monoBehaviours)
                {
                    if (mb is IReference)
                    {
                        templateComponent = mb;
                        break;
                    }
                }

                if (templateComponent == null)
                {
                    DebugInfo.LogWarning(
                        $"[PoolManager] Prefab '{cfg.prefab.name}' has no component implementing IReference. Pool '{cfg.id}' skipped.");
                    continue;
                }

                var componentType = templateComponent.GetType();
                var genericType = typeof(ObjectPool<>).MakeGenericType(componentType);

                // 构造函数签名：(T prefab, int initialSize, Transform parent, bool expandIfEmpty, int maxSize)
                var poolInstance = Activator.CreateInstance(
                    genericType,
                    new object[] { templateComponent, cfg.initialSize, cfg.container, cfg.expandIfEmpty, cfg.maxSize }
                ) as IPoolWrapper;

                if (poolInstance == null)
                {
                    DebugInfo.LogError($"[PoolManager] Failed to create ObjectPool<{componentType.Name}> for id '{cfg.id}'.");
                    continue;
                }

                runtimePools[cfg.id] = poolInstance;
            }
        }

        /// <summary>
        /// 从指定池中生成一个实例。
        /// </summary>
        public GameObject Spawn(string id, Vector3 position, Quaternion rotation)
        {
            if (!runtimePools.TryGetValue(id, out var pool))
            {
                DebugInfo.LogWarning($"[PoolManager] Pool id '{id}' not found.");
                return null;
            }

            return pool.Spawn(position, rotation);
        }

        /// <summary>
        /// 重载：只传位置，旋转使用 identity。
        /// </summary>
        public GameObject Spawn(string id, Vector3 position)
        {
            if (id == null)
                return null;
            return Spawn(id, position, Quaternion.identity);
        }

        /// <summary>
        /// 重载：使用默认位置和旋转。
        /// </summary>
        public GameObject Spawn(string id)
        {
            return Spawn(id, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 将对象回收到指定 id 对应的池。
        /// </summary>
        public bool Despawn(string id, GameObject instance)
        {
            if (!instance) return false;

            if (!runtimePools.TryGetValue(id, out var pool))
            {
                DebugInfo.LogWarning($"[PoolManager] Pool id '{id}' not found.");
                return false;
            }

            return pool.Despawn(instance);
        }

        /// <summary>
        /// 判断某个池是否存在。
        /// </summary>
        public bool HasPool(string id) => runtimePools.ContainsKey(id);

        /// <summary>
        /// 清空所有池。
        /// </summary>
        public void ClearAll()
        {
            foreach (var kv in runtimePools)
            {
                kv.Value.Clear();
            }
            runtimePools.Clear();
        }
    }
}
