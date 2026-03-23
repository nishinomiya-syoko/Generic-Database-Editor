// ============================================================
// 自动生成的数据表类 - AssetPath
// 生成时间：2026-03-23 20:30:31
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// AssetPath 数据行
    /// </summary>
    public class AssetPath
    {
      /// <summary>
      /// 资源id
      /// </summary>
      public int Id { get; set; }

      public string DisplayName { get; set; }

      /// <summary>
      /// 资源路径Assets/开头
      /// </summary>
      public string Path { get; set; }

      /// <summary>
      /// 构造函数（初始化默认值）
      /// </summary>
      public AssetPath()
      {
        Id = 0;
        DisplayName = string.Empty;
        Path = string.Empty;
      }
    }
}
