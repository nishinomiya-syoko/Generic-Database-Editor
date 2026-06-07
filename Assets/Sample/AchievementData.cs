using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 成就数据
    // [CreateAssetMenu(fileName = "AchievementData", menuName = "Top/Achievement Data")]
    [EditableData]
    public class AchievementData
    {
        [Header("基本信息")]
        public string id;
        public string achievementName;
        public string description;
        public Sprite icon;
        public int points;                  // 成就点数

        [Header("解锁条件")]
        public QuestObjectiveType unlockType;
        public string targetId;
        public int requiredAmount;

        [Header("奖励")]
        public QuestReward rewards;
        public string unlockTitle;          // 解锁称号
        public string unlockBadge;          // 解锁徽章

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }
    }
}