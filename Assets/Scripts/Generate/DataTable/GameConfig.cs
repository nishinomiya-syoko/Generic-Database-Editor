// ============================================================
// 自动生成的数据表类 - GameConfig
// 生成时间：2026-03-18 21:53:57
// 请勿手动修改，修改会被覆盖
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// GameConfig 数据行
    /// </summary>
    public class GameConfig
    {
      public float musicVolume { get; set; }

      public float sfxVolume { get; set; }

      public int maxPlayerCount { get; set; }

      public Vector2 screenResolution { get; set; }

      /// <summary>
      /// 构造函数（初始化默认值）
      /// </summary>
      public GameConfig()
      {
        musicVolume = 0f;
        sfxVolume = 0f;
        maxPlayerCount = 0;
        screenResolution = Vector2.zero;
      }
    }
}
