// ============================================================
// 自动生成的数据表类 - PoolConfig
// 生成时间：2026-03-17 22:42:27
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// PoolConfig 数据行
    /// </summary>
    public class PoolConfigRow
    {
        /// <summary>
        /// 序号
        /// </summary>
        public string id;

        public string name;

        /// <summary>
        /// 预制体路径
        /// </summary>
        public string prefabPath;

        public int initialSize;

        public bool expandIfEmpty;

        public int maxSize;

    }

    /// <summary>
    /// PoolConfig 数据表容器
    /// </summary>
    public class PoolConfigTable : DataTableBase<PoolConfigRow>
    {
        private static PoolConfigTable _instance;
        public static PoolConfigTable Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PoolConfigTable();
                return _instance;
            }
        }

        public void Load(bool useBinary = false) => LoadTable("PoolConfig", useBinary);
    }
}
