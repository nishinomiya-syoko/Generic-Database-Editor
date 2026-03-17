// ============================================================
// 自动生成的数据表类 - BuildingData
// 生成时间：2026-03-17 22:42:27
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
    public class BuildingDataRow
    {
        public string Id;

        public string DisplayName;

        public string Description;

        public string IconPath;

        public string prefabPath;

        /// <summary>
        /// 建造信息
        /// </summary>
        public int buildTime;

        public Dictionary<string, int> costs;

        /// <summary>
        /// 升级信息
        /// </summary>
        public int maxLevel;

        /// <summary>
        /// 特殊属性
        /// </summary>
        public int hitPoints;

        public int armor;

        public float range;

        public bool isResourceProducer;

        /// <summary>
        /// 建筑技能
        /// </summary>
        public string[] buildingSkills;

    }

    /// <summary>
    /// BuildingData 数据表容器
    /// </summary>
    public class BuildingDataTable : DataTableBase<BuildingDataRow>
    {
        private static BuildingDataTable _instance;
        public static BuildingDataTable Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BuildingDataTable();
                return _instance;
            }
        }

        public void Load(bool useBinary = false) => LoadTable("BuildingData", useBinary);
    }
}
