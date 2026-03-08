using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    #region 资源生产建筑

    public class ResourceProducer : MonoBehaviour, IResourceProducer
    {
        [Header("Production Settings")]
        public ResourceType productionType = ResourceType.Gold;
        public int productionRate = 100;
        public int productionCapacity = 1000;
        public float productionInterval = 60f; // 秒

        private int currentProduction = 0;
        private float productionTimer = 0f;
        private bool isProducing = false;

        public ResourceType ProductionType => productionType;
        public int ProductionRate => productionRate;
        public int ProductionCapacity => productionCapacity;
        public int CurrentProduction => currentProduction;
        public bool IsProductionReady => currentProduction > 0;
        public float ProductionProgress => productionTimer / productionInterval;

        void Start()
        {
            GlobalManager.Instance?.ResourceManager?.RegisterResourceProducer(this);
        }

        void OnDestroy()
        {
            GlobalManager.Instance?.ResourceManager?.UnregisterResourceProducer(this);
        }

        void Update()
        {
            if (isProducing && currentProduction < productionCapacity)
            {
                productionTimer += Time.deltaTime;

                if (productionTimer >= productionInterval)
                {
                    ProduceResource();
                    productionTimer = 0f;
                }
            }
        }

        private void ProduceResource()
        {
            currentProduction = Mathf.Min(currentProduction + productionRate, productionCapacity);

            // 触发生产事件
            // GlobalManager.Instance?.ResourceManager?.OnResourceProduced?.Invoke(productionType, productionRate);
            GlobalManager.Instance?.ResourceManager?.ProduceResource(productionType, productionRate);
        }

        public void StartProduction()
        {
            isProducing = true;
        }

        public void StopProduction()
        {
            isProducing = false;
        }

        public int CollectProduction()
        {
            int amount = currentProduction;
            currentProduction = 0;
            return amount;
        }
    }

    #endregion
}