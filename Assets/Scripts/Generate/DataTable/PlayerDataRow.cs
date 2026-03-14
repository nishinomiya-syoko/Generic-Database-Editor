// ============================================================
// 自动生成的数据表类 - PlayerData
// 生成时间：2026-03-14 18:42:30
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
namespace DataCenter
{
    /// <summary>
    /// PlayerData 数据行
    /// </summary>
    [Serializable]
    public class PlayerDataRow
    {
        /// <summary>
        /// 实例名称
        /// </summary>
        public string 实例名;
        /// <summary>
        /// 基础属性
        /// </summary>
        public string playerName;
        public int level;
        public float maxHP;
        public bool isVIP;
        /// <summary>
        /// 位置/颜色
        /// </summary>
        public Vector3 spawnPos;
        public Color playerColor;
    }

    /// <summary>
    /// PlayerData 数据表容器
    /// </summary>
    public class PlayerDataTable : DataTableBase<PlayerDataRow>
    {
        private static PlayerDataTable _instance;
        public static PlayerDataTable Instance
        {
            get
            {
                if (_instance == null) _instance = new PlayerDataTable();
                return _instance;
            }
        }

        public void Load(bool useBinary = false) => LoadTable("PlayerData", useBinary);
    }
}
