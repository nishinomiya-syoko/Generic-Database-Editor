using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 建筑数据
    [CreateAssetMenu(fileName = "BuildingData", menuName = "Top/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("基本信息")]
        public string id;
        public string buildingName;
        public string description;
        public BuildingType buildingType;
        public Vector2Int size = new Vector2Int(2, 2);
        public Sprite icon;
        public GameObject prefab;

        [Header("建造信息")]
        public int buildTime = 60; // 秒
        public ResourceCost[] buildCosts;

        [Header("升级信息")]
        public int maxLevel = 10;
        public BuildingLevelData[] levelData;

        [Header("特殊属性")]
        public bool canBeAttacked = true;
        public int hitPoints = 100;
        public int armor = 0;
        public float range = 5f;

        public bool isResourceProducer = false;
        public ResourceType producedResource;
        public int[] productionRates;

        [Header("建筑技能")]
        public string[] buildingSkills;         // 建筑技能
        public bool hasAuraEffect = false;      // 是否有光环效果
        public float auraRadius = 5f;           // 光环范围
        public string auraSkillId;              // 光环技能ID

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }

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