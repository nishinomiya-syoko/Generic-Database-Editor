using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

    // 关卡数据库

    public class LevelDatabase 
    {
        public List<LevelDataSO> levels = new List<LevelDataSO>();

        public LevelDataSO GetLevelData(int levelId)
        {
            return levels.Find(l => l.levelId == levelId);
        }

        public List<LevelDataSO> GetLevelsByType(LevelType type)
        {
            return levels.FindAll(l => l.levelType == type);
        }
    }
