// ============================================================
// 自动生成的数据表类 - UnitData
// 生成时间：2026-03-26 20:49:29
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// UnitData 数据行
    /// </summary>
    public class UnitData
    {
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string IconPath { get; set; }

        public Top.UnitType[] unitTags { get; set; }

        public Vector2Int size { get; set; }

        /// <summary>
        /// 升级信息
        /// </summary>
        public int[] levelId { get; set; }

        public bool isHero { get; set; }

        /// <summary>
        /// 建筑技能
        /// </summary>
        public string[] buildingSkills { get; set; }

        /// <summary>
        /// 构造函数（初始化默认值）
        /// </summary>
        public UnitData()
        {
            Id = 0;
            DisplayName = string.Empty;
            Description = string.Empty;
            IconPath = string.Empty;
            unitTags = default;
            size = default;
            levelId = default;
            isHero = false;
            buildingSkills = default;
        }
    }
}
