// ============================================================
// 自动生成的数据表类 - UnitLevelData
// 生成时间：2026-03-26 20:49:29
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// UnitLevelData 数据行
    /// </summary>
    public class UnitLevelData
    {
        /// <summary>
        /// 资源id
        /// </summary>
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public int PoolId { get; set; }

        public int block { get; set; }

        public int screw { get; set; }

        public int crystal { get; set; }

        public int plastic { get; set; }

        public int gold { get; set; }

        public int upgradeTime { get; set; }

        public int trainingTime { get; set; }

        public int damage { get; set; }

        public int attackSpeed { get; set; }

        public int attackRange { get; set; }

        public int hp { get; set; }

        /// <summary>
        /// 构造函数（初始化默认值）
        /// </summary>
        public UnitLevelData()
        {
            Id = 0;
            DisplayName = string.Empty;
            PoolId = 0;
            block = 0;
            screw = 0;
            crystal = 0;
            plastic = 0;
            gold = 0;
            upgradeTime = 0;
            trainingTime = 0;
            damage = 0;
            attackSpeed = 0;
            attackRange = 0;
            hp = 0;
        }
    }
}
