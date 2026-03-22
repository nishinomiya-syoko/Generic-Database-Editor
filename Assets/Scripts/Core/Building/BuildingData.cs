using System.Collections.Generic;
using UnityEngine;

namespace Top
{
    [EditableData]
    public class BuildingData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public BuildingType buildingType;
        public Vector2Int size = new Vector2Int(2, 2);
        public GameObject prefab;
        public string prefabPath;

        
        public int maxLevel => levelData.Length;
        public BuildingLevelData[] levelData = new BuildingLevelData[0];
        public string LevelId;

        [Header("特殊属性")]
        public int hitPoints = 100;
        public int armor = 0;
        public float range = 5f;

        public bool isResourceProducer = false;
        public ResourceType producedResource;
        // public int[] productionRates;

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
            if (isResourceProducer && levelData != null && level > 0 && level <= levelData.Length)
            {
                return levelData[level - 1].productionRate;
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
        public int productionRate; // 资源产量（如果是生产建筑）
        public BuildingLevelData()
        {
            stats = new BuildingStats();
        }
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