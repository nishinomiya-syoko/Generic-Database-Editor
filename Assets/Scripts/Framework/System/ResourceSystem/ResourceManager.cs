using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
// using Sirenix.OdinInspector;

namespace Top
{
    // 资源类型枚举
    public enum ResourceType
    {
        Block,
        Screw,
        Crystal,
        Plastic,
        Gold,
        Exploit,    //
        AtkPoint,
    }

    // 资源成本结构
    [System.Serializable]
    public class ResourceCost
    {
        public Dictionary<ResourceType, int> cost;
        public ResourceCost(Dictionary<ResourceType, int> cost)
        {
            this.cost = cost;
        }
        public ResourceCost(int block = 0, int screw = 0, int crystal = 0, int plastic = 0, int gold = 0)
        {
            this.cost = new Dictionary<ResourceType, int>();
            cost.Add(ResourceType.Block, block);
            cost.Add(ResourceType.Screw, screw);
            cost.Add(ResourceType.Crystal, crystal);
            cost.Add(ResourceType.Plastic, plastic);
        }
        public int GetAmount(ResourceType resourceType)
        {
            return cost[resourceType];
        }
    }

    // 资源数据
    [System.Serializable]
    public class ResourceData
    {
        public int gold => customResources[ResourceType.Gold];
        
        public Dictionary<ResourceType, int> customResources = new Dictionary<ResourceType, int>();
        public Dictionary<ResourceType, int> maxInventory = new Dictionary<ResourceType, int>();


        public int GetResource(ResourceType type)
        {
            return customResources.ContainsKey(type) ? customResources[type] : 0;
        }

        public int GetMaxResource(ResourceType type)
        {
           return maxInventory.ContainsKey(type) ? maxInventory[type] : 0;
        }

        public void SetResource(ResourceType type, int value)
        {
            customResources[type] = value;
        }

        public void SetMaxResource(ResourceType type, int value)
        {
            maxInventory[type] = value;
        }
    }

    // 资源生产建筑接口
    public interface IResourceProducer
    {
        ResourceType ProductionType { get; }
        int ProductionRate { get; }
        int ProductionCapacity { get; }
        int CurrentProduction { get; }
        bool IsProductionReady { get; }
        float ProductionProgress { get; }
        
        void StartProduction();
        void StopProduction();
        int CollectProduction();
    }

    // 资源管理器
    public class ResourceManager : MonoBehaviour
    {
        [Header("Initial Resources")]
        public int initialGold = 1000;
        public int initialElixir = 1000;
        public int initialDarkElixir = 0;
        public int initialGems = 100;

        [Header("Initial Max Resources")]
        public int initialMaxGold = 5000;
        public int initialMaxElixir = 5000;
        public int initialMaxDarkElixir = 1000;

        private ResourceData resources;
        private List<IResourceProducer> resourceProducers = new List<IResourceProducer>();
        private Coroutine productionCoroutine;

        // 事件
        public event Action<ResourceType, int> OnResourceChanged;
        public event Action<ResourceType, int> OnResourceProduced;
        public event Action<ResourceType, int> OnResourceCollected;

        void Awake()
        {
            resources = new ResourceData();
            ResetResources();
        }

        void Start()
        {
            StartResourceProduction();
        }

        void OnDestroy()
        {
            StopResourceProduction();
        }

        #region 资源操作

        public bool SpendResource(ResourceType type, int amount)
        {
            if (amount <= 0)
                return false;

            if (resources.GetResource(type) >= amount)
            {
                int newAmount = resources.GetResource(type) - amount;
                resources.SetResource(type, newAmount);
                
                OnResourceChanged?.Invoke(type, newAmount);
                return true;
            }
            
            return false;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0)
                return;

            int currentAmount = resources.GetResource(type);
            int maxAmount = resources.GetMaxResource(type);
            int addAmount = Mathf.Min(amount, maxAmount - currentAmount);

            resources.SetResource(type, currentAmount + addAmount);
            OnResourceChanged?.Invoke(type, resources.GetResource(type));
        }
        public void ProduceResource(ResourceType type, int amount)
        {
            OnResourceProduced?.Invoke(type,amount);
        }

        public bool HasEnoughResource(ResourceType type, int amount)
        {
            return resources.GetResource(type) >= amount;
        }

        public int GetResource(ResourceType type)
        {
            return resources.GetResource(type);
        }

        public int GetMaxResource(ResourceType type)
        {
            return resources.GetMaxResource(type);
        }

        public float GetResourcePercentage(ResourceType type)
        {
            return (float)resources.GetResource(type) / resources.GetMaxResource(type);
        }

        public void IncreaseMaxResource(ResourceType type, int amount)
        {
            int currentMax = resources.GetMaxResource(type);
            resources.SetMaxResource(type, currentMax + amount);
            OnResourceChanged?.Invoke(type, resources.GetResource(type));
        }

        public bool CanAfford(ResourceCost costs)
        {
            foreach (var cost in costs.cost)
            {
                if (!HasEnoughResource(cost.Key,cost.Value))
                    return false;
            }
            return true;
        }

        public bool SpendResources(ResourceCost costs)
        {
            if (!CanAfford(costs))
                return false;

            foreach (var cost in costs.cost)
            {
                SpendResource(cost.Key, cost.Value);
            }
            return true;
        }

        #endregion

        #region 资源生产

        public void RegisterResourceProducer(IResourceProducer producer)
        {
            if (!resourceProducers.Contains(producer))
            {
                resourceProducers.Add(producer);
                producer.StartProduction();
            }
        }

        public void UnregisterResourceProducer(IResourceProducer producer)
        {
            if (resourceProducers.Contains(producer))
            {
                producer.StopProduction();
                resourceProducers.Remove(producer);
            }
        }

        private void StartResourceProduction()
        {
            if (productionCoroutine == null)
            {
                productionCoroutine = StartCoroutine(ResourceProductionLoop());
            }
        }

        private void StopResourceProduction()
        {
            if (productionCoroutine != null)
            {
                StopCoroutine(productionCoroutine);
                productionCoroutine = null;
            }
        }

        private IEnumerator ResourceProductionLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                UpdateResourceProduction();
            }
        }

        private void UpdateResourceProduction()
        {
            for (int i = resourceProducers.Count - 1; i >= 0; i--)
            {
                var producer = resourceProducers[i];
                if (producer == null)
                {
                    resourceProducers.RemoveAt(i);
                    continue;
                }

                // 检查是否可以收集
                if (producer.IsProductionReady)
                {
                    int collected = producer.CollectProduction();
                    if (collected > 0)
                    {
                        AddResource(producer.ProductionType, collected);
                        OnResourceCollected?.Invoke(producer.ProductionType, collected);
                    }
                }
            }
        }

        #endregion

        #region 数据持久化

        public void SaveData()
        {
            string json = JsonUtility.ToJson(resources);
            PlayerPrefs.SetString("ResourceData", json);
            PlayerPrefs.Save();
        }

        public void LoadData()
        {
            if (PlayerPrefs.HasKey("ResourceData"))
            {
                string json = PlayerPrefs.GetString("ResourceData");
                resources = JsonUtility.FromJson<ResourceData>(json);
            }
            else
            {
                ResetResources();
            }
        }

        public void ResetResources()
        {
            resources = new ResourceData();
        }

        #endregion

        #region 调试功能

        [Sirenix.OdinInspector.Button("Add Test Resources")]
        public void AddTestResources()
        {
            AddResource(ResourceType.Gold, 1000);
            AddResource(ResourceType.Block, 1000);
            AddResource(ResourceType.Crystal, 1000);
            AddResource(ResourceType.Plastic, 1000);
            AddResource(ResourceType.Screw, 1000);
        }

        [Sirenix.OdinInspector.Button("Reset All Resources")]
        public void DebugResetResources()
        {
            ResetResources();
            OnResourceChanged?.Invoke(ResourceType.Gold, resources.GetResource(ResourceType.Gold));
        }

        #endregion
    }
}