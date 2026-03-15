using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 任务数据库
    [CreateAssetMenu(fileName = "QuestDatabase", menuName = "Top/Quest Database")]
    public class QuestDatabase : ScriptableObject
    {
        public List<QuestData> quests = new List<QuestData>();

        public QuestData GetQuestData(string questId)
        {
            return quests.Find(q => q.id == questId);
        }

        public List<QuestData> GetQuestsByType(QuestType type)
        {
            return quests.FindAll(q => q.questType == type);
        }

        public List<QuestData> GetAvailableQuests(int playerLevel, HashSet<string> completedQuests)
        {
            List<QuestData> available = new List<QuestData>();
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