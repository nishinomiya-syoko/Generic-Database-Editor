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
        // public BuildingType buildingType;
        // public UnitType[] buildingTags;
        public string[] buildingTags;
        public BuildingFunction buildingFunction;
        public Vector2Int size = new Vector2Int(2, 2);
        // public GameObject prefab;
        public string prefabPath;

        public int curLevel = 1;
        public int maxLevel => levelData.Length;
        public BuildingLevelData[] levelData;
        public string LevelId;

        [Header("建筑技能")]
        public string[] buildingSkills;
        
        [Header("特殊属性")]
        public bool isResourceProducer = false;
        public ResourceType producedResource;

        
        public BuildingLevelData GetLevelData(int level)
        {
            if (levelData != null && level > 0 && level <= levelData.Length)
            {
                return levelData[level - 1];
            }
            return null;
        }
    }
    // 建筑等级数据
    [System.Serializable]
    public class BuildingLevelData
    {
        public int levelId;
        public int poolId;
        public ResourceCost upgradeCosts;
        public int upgradeTime;
        public BuildingStats stats;
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
        public int hp;
        public int productionRate = 0;
        /// <summary>
        /// 存储容量
        /// </summary>
        public int storageCapacity = 0;
        /// <summary>
        /// 科技产量
        /// </summary>
        public int troopCapacity = 0;
        public float buildSpeed = 1f;
        public float researchSpeed = 1f;
    }
}