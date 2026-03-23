// ============================================================
// 自动生成的数据表类 - BuildingData
// 生成时间：2026-03-23 22:37:33
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// BuildingData 数据行
    /// </summary>
    public class BuildingData
    {
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string IconPath { get; set; }

        public Top.BuildingType buildingType { get; set; }

        public Vector2Int size { get; set; }

        /// <summary>
        /// 建造信息
        /// </summary>
        public int buildTime { get; set; }

        public Dictionary<string, int> costs { get; set; }

        /// <summary>
        /// 升级信息
        /// </summary>
        public int levelIdl { get; set; }

        /// <summary>
        /// 特殊属性
        /// </summary>
        public int hitPoints { get; set; }

        public int armor { get; set; }

        public float range { get; set; }

        public bool isResourceProducer { get; set; }

        public Top.ResourceType producedResource { get; set; }

        /// <summary>
        /// 建筑技能
        /// </summary>
        public string[] buildingSkills { get; set; }

        /// <summary>
        /// 构造函数（初始化默认值）
        /// </summary>
        public BuildingData()
        {
            Id = 0;
            DisplayName = string.Empty;
            Description = string.Empty;
            IconPath = string.Empty;
            buildingType = default;
            size = default;
            buildTime = 0;
            costs = default;
            levelIdl = 0;
            hitPoints = 0;
            armor = 0;
            range = 0f;
            isResourceProducer = false;
            producedResource = default;
            buildingSkills = default;
        }
    }
}
