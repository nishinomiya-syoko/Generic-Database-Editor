// ============================================================
// 自动生成的数据表类 - GameConfig
// 生成时间：2026-03-17 22:42:27
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
    public class GameConfigRow
    {
        public float musicVolume;

        public float sfxVolume;

        public int maxPlayerCount;

        public Vector2 screenResolution;

    }

    /// <summary>
    /// GameConfig 数据表容器
    /// </summary>
    public class GameConfigTable : DataTableBase<GameConfigRow>
    {
        private static GameConfigTable _instance;
        public static GameConfigTable Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameConfigTable();
                return _instance;
            }
        }

        public void Load(bool useBinary = false) => LoadTable("GameConfig", useBinary);
    }
}
