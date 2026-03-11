using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using SOEditor;

namespace Top
{
    // 建筑数据
    [CreateAssetMenu(fileName = "BuildingData", menuName = "Top/Building Data")]
    public class BuildingDataSO : ScriptableObjectBase
    {
        public BuildingType buildingType;
        public Vector2Int size = new Vector2Int(2, 2);
        public string prefabPath;

        [Header("建造信息")]
        public int buildTime = 60; // 秒
        public ResourceCost[] buildCosts;

        [Header("升级信息")]
        public int maxLevel = 10;
        public BuildingLevelData[] levelData;

        [Header("特殊属性")]
        public int hitPoints = 100;
        public int armor = 0;
        public float range = 5f;

        public bool isResourceProducer = false;
        public ResourceType producedResource;
        public int[] productionRates;

        [Header("建筑技能")]
        public string[] buildingSkills;         // 建筑技能
       
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(Id))
            GenerateId();
        }
        
        public override string GetSOType()
        {
            return "BuildingData";
        }
        public override object GetSerializableData()
        {
            return new BuildingData
            {
                buildingType = buildingType,
                size = size,
                // prefab = prefab,
                prefabPath = prefabPath,
                buildTime = buildTime,
                buildCosts = buildCosts,
                maxLevel = maxLevel,
                levelData = levelData,
                hitPoints = hitPoints,
                armor = armor,
                range = range,
                isResourceProducer = isResourceProducer,
                producedResource = producedResource,
                productionRates = productionRates,
                buildingSkills = buildingSkills
            };
        }
        public override void LoadFromSerializableData(object data)
        {
            if (data is BuildingData buildingData)
            {
                SetId(buildingData.Id);
                SetDisplayName(buildingData.DisplayName);
                SetDescription(buildingData.Description);
                // buildingType = buildingData.buildingType;
                size = buildingData.size;
                // prefab = buildingData.prefab;
                prefabPath = buildingData.prefabPath;
                buildTime = buildingData.buildTime;
                // buildCosts = buildingData.buildCosts;
                maxLevel = buildingData.maxLevel;
                levelData = buildingData.levelData;
                hitPoints = buildingData.hitPoints;
                armor = buildingData.armor;
                range = buildingData.range;
                isResourceProducer = buildingData.isResourceProducer;
                producedResource = buildingData.producedResource;
                productionRates = buildingData.productionRates;
                buildingSkills = buildingData.buildingSkills;
            }
        }
    }
    [System.Serializable]
    public class BuildingData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public BuildingType buildingType;
        public Vector2Int size = new Vector2Int(2, 2);
        // public GameObject prefab;
        public string prefabPath;

        [Header("建造信息")]
        public int buildTime = 60; // 秒
        public ResourceCost[] buildCosts;

        [Header("升级信息")]
        public int maxLevel = 10;
        public BuildingLevelData[] levelData;

        [Header("特殊属性")]
        public int hitPoints = 100;
        public int armor = 0;
        public float range = 5f;

        public bool isResourceProducer = false;
        public ResourceType producedResource;
        public int[] productionRates;

        [Header("建筑技能")]
        public string[] buildingSkills;
        public BuildingLevelData GetLevelData(int level)
        {
            if (levelData != null && level > 0 && level <= levelData.Length)
            {
                return levelData[level - 1];
            }
            return null;
        }

        public int GetProductionRate(int level)
        {
            if (isResourceProducer && productionRates != null && level > 0 && level <= productionRates.Length)
            {
                return productionRates[level - 1];
            }
            return 0;
        }
    }
    // 建筑等级数据
    [System.Serializable]
    public class BuildingLevelData
    {
        public int level;
        public int hitPoints;
        public ResourceCost[] upgradeCosts;
        public int upgradeTime;
        public BuildingStats stats;
    }

    // 建筑属性
    [System.Serializable]
    public class BuildingStats
    {
        public int damage = 0;
        public float attackSpeed = 1f;
        public float attackRange = 5f;
        public int productionRate = 0;
        public int storageCapacity = 0;
        public int troopCapacity = 0;
        public float buildSpeed = 1f;
        public float researchSpeed = 1f;
    }
}