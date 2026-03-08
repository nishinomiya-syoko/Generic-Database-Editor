using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 成就数据库
    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "Top/Achievement Database")]
    public class AchievementSO : ScriptableObject
    {
        public List<AchievementData> achievements = new List<AchievementData>();

        public AchievementData GetAchievementData(string achievementId)
        {
            return achievements.Find(a => a.id == achievementId);
        }
    }
}