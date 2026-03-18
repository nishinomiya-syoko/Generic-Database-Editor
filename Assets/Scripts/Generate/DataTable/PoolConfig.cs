// ============================================================
// 自动生成的数据表类 - PoolConfig
// 生成时间：2026-03-18 21:53:57
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
      /// 序号
      /// </summary>
      public string id { get; set; }

      public string name { get; set; }

      /// <summary>
      /// 预制体路径
      /// </summary>
      public string prefabPath { get; set; }

      public int initialSize { get; set; }

      public bool expandIfEmpty { get; set; }

      public int maxSize { get; set; }

      /// <summary>
      /// 构造函数（初始化默认值）
      /// </summary>
      public PoolConfig()
      {
        id = string.Empty;
        name = string.Empty;
        prefabPath = string.Empty;
        initialSize = 0;
        expandIfEmpty = false;
        maxSize = 0;
      }
    }
}
