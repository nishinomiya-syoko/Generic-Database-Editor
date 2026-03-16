// ============================================================
// 自动生成的数据表类 - PoolDataTest
// 生成时间：2026-03-16 21:09:09
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
namespace DataCenter
{
    /// <summary>
    /// PoolDataTest 数据行
    /// </summary>
    [Serializable]
    public class PoolDataTestRow
    {
        public string id;
        public string name;
        public string prefabPath;
        public int initialSize;
        public bool expandIfEmpty;
        public int maxSize;
        public List<string> tags;
        public int[] tagPriorities;
        public Dictionary<string, int> tagPriorityDict;
    }

    /// <summary>
    /// PoolDataTest 数据表容器
    /// </summary>
    public class PoolDataTestTable : DataTableBase<PoolDataTestRow>
    {
        private static PoolDataTestTable _instance;
        public static PoolDataTestTable Instance
        {
            get
            {
                if (_instance == null) _instance = new PoolDataTestTable();
                return _instance;
            }
        }

        public void Load(bool useBinary = false) => LoadTable("PoolDataTest", useBinary);
    }
}
