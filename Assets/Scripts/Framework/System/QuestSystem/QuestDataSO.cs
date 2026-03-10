using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 任务数据库
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "Top/Quest Database")]
    public class QuestSO : ScriptableObject
    {
        public List<QuestDataSO> quests = new List<QuestDataSO>();

        public QuestDataSO GetQuestData(string questId)
        {
            return quests.Find(q => q.id == questId);
        }

        public List<QuestDataSO> GetQuestsByType(QuestType type)
        {
            return quests.FindAll(q => q.questType == type);
        }

        public List<QuestDataSO> GetAvailableQuests(int playerLevel, HashSet<string> completedQuests)
        {
            List<QuestDataSO> available = new List<QuestDataSO>();
            foreach (var quest in quests)
            {
                if (quest.CanAccept(playerLevel, completedQuests))
                {
                    available.Add(quest);
                }
            }
            return available;
        }
    }
}