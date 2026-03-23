// ============================================================
// 自动生成的数据表类 - PoolConfig
// 生成时间：2026-03-23 22:43:49
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
    public class PoolConfig
    {
        /// <summary>
        /// 资源id
        /// </summary>
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public string EntityType { get; set; }

        public int InitialSize { get; set; }

        public bool expandIfEmpty { get; set; }

        public int expandRate { get; set; }

        public int maxSize { get; set; }

        /// <summary>
        /// 构造函数（初始化默认值）
        /// </summary>
        public PoolConfig()
        {
            Id = 0;
            DisplayName = string.Empty;
            EntityType = string.Empty;
            InitialSize = 0;
            expandIfEmpty = false;
            expandRate = 0;
            maxSize = 0;
        }
    }
}
