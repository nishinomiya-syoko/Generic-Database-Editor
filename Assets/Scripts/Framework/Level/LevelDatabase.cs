using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

    // 关卡数据库

    public class LevelDatabase 
    {
        public List<LevelData> levels = new List<LevelData>();

        public LevelData GetLevelData(int levelId)
        {
            return levels.Find(l => l.levelId == levelId);
        }

        public List<LevelData> GetLevelsByType(LevelType type)
        {
            return levels.FindAll(l => l.levelType == type);
        }
    }
