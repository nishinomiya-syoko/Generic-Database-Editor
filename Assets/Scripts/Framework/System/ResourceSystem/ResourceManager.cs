using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 资源类型枚举
    public enum ResourceType
    {
        Gold,
        Elixir,
        DarkElixir,
        Gems
    }

    // 资源成本结构
    [System.Serializable]
    public struct ResourceCost
    {
        public ResourceType resourceType;
        public int amount;

        public ResourceCost(ResourceType type, int cost)
        {
            resourceType = type;
            amount = cost;
        }
    }

    // 资源数据
    [System.Serializable]
    public class ResourceData
    {
        public int gold = 1000;
        public int elixir = 1000;
        public int darkElixir = 0;
        public int gems = 100;
        public int maxGold = 5000;
        public int maxElixir = 5000;
        public int maxDarkElixir = 1000;

        public int GetResource(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    return gold;
                case ResourceType.Elixir:
                    return elixir;
                case ResourceType.DarkElixir:
                    return darkElixir;
                case ResourceType.Gems:
                    return gems;
                default:
                    return 0;
            }
        }

        public int GetMaxResource(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    return maxGold;
                case ResourceType.Elixir:
                    return maxElixir;
                case ResourceType.DarkElixir:
                    return maxDarkElixir;
                default:
                    return int.MaxValue;
            }
        }

        public void SetResource(ResourceType type, int value)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    gold = Mathf.Clamp(value, 0, maxGold);
                    break;
                case ResourceType.Elixir:
                    elixir = Mathf.Clamp(value, 0, maxElixir);
                    break;
                case ResourceType.DarkElixir:
                    darkElixir = Mathf.Clamp(value, 0, maxDarkElixir);
                    break;
                case ResourceType.Gems:
                    gems = Mathf.Max(value, 0);
                    break;
            }
        }

        public void SetMaxResource(ResourceType type, int value)
        {
            switch (type)
            {
                case ResourceType.Gold:
                    maxGold = Mathf.Max(value, 0);
                    break;
                case ResourceType.Elixir:
                    maxElixir = Mathf.Max(value, 0);
                    break;
                case ResourceType.DarkElixir:
                    maxDarkElixir = Mathf.Max(value, 0);
                    break;
            }
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

        public bool CanAfford(ResourceCost[] costs)
        {
            foreach (var cost in costs)
            {
                if (!HasEnoughResource(cost.resourceType, cost.amount))
                    return false;
            }
            return true;
        }

        public bool SpendResources(ResourceCost[] costs)
        {
            if (!CanAfford(costs))
                return false;

            foreach (var cost in costs)
            {
                SpendResource(cost.resourceType, cost.amount);
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
            resources.gold = initialGold;
            resources.elixir = initialElixir;
            resources.darkElixir = initialDarkElixir;
            resources.gems = initialGems;
            resources.maxGold = initialMaxGold;
            resources.maxElixir = initialMaxElixir;
            resources.maxDarkElixir = initialMaxDarkElixir;
        }

        #endregion

        #region 调试功能

        [ContextMenu("Add Test Resources")]
        public void AddTestResources()
        {
            AddResource(ResourceType.Gold, 1000);
            AddResource(ResourceType.Elixir, 1000);
            AddResource(ResourceType.DarkElixir, 100);
            AddResource(ResourceType.Gems, 50);
        }

        [ContextMenu("Reset All Resources")]
        public void DebugResetResources()
        {
            ResetResources();
            OnResourceChanged?.Invoke(ResourceType.Gold, resources.gold);
            OnResourceChanged?.Invoke(ResourceType.Elixir, resources.elixir);
            OnResourceChanged?.Invoke(ResourceType.DarkElixir, resources.darkElixir);
            OnResourceChanged?.Invoke(ResourceType.Gems, resources.gems);
        }

        #endregion
    }
}