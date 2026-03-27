// ============================================================
// 自动生成的数据表类 - BuildingData
// 生成时间：2026-03-27 22:27:09
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

        public string[] unitTags { get; set; }

        public Vector2Int size { get; set; }

        /// <summary>
        /// 升级信息
        /// </summary>
        public int[] levelId { get; set; }

        public bool isResourceProducer { get; set; }

        public Top.ResourceType producedResource { get; set; }

        /// <summary>
        /// 建筑技能
        /// </summary>
        public string[] buildingSkills { get; set; }

        public bool isDecoration { get; set; }

        /// <summary>
        /// 构造函数（初始化默认值）
        /// </summary>
        public BuildingData()
        {
            Id = 0;
            DisplayName = string.Empty;
            Description = string.Empty;
            IconPath = string.Empty;
            unitTags = default;
            size = default;
            levelId = default;
            isResourceProducer = false;
            producedResource = default;
            buildingSkills = default;
            isDecoration = false;
        }
    }
}
