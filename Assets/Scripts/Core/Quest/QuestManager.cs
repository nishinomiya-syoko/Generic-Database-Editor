using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 任务管理器
    public class QuestManager : MonoBehaviour
    {
        [Header("任务设置")]
        public QuestDatabase questDatabase;
        public AchievementDatabase achievementDatabase;
        public int maxActiveQuests = 10;

        private Dictionary<string, PlayerQuestData> activeQuests = new Dictionary<string, PlayerQuestData>();
        private HashSet<string> completedQuests = new HashSet<string>();
        private Dictionary<string, int> achievementProgress = new Dictionary<string, int>();

        // 事件
        public event Action<QuestData> OnQuestAccepted;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData, QuestReward> OnQuestRewardClaimed;
        public event Action<AchievementData> OnAchievementUnlocked;

        void Start()
        {
            LoadQuestData();
            InitializeDailyQuests();
        }

        void Update()
        {
            UpdateDailyQuests();
        }

        #region 任务管理

        public bool AcceptQuest(string questId)
        {
            if (activeQuests.Count >= maxActiveQuests)
                return false;

            var questData = questDatabase?.GetQuestData(questId);
            if (questData == null)
                return false;

            // 检查是否可以接受任务
            int playerLevel = GlobalManager.Instance?.LevelManager?.playerLevel ?? 1;
            if (!questData.CanAccept(playerLevel, completedQuests))
                return false;

            // 检查是否已接受
            if (activeQuests.ContainsKey(questId))
                return false;

            // 创建玩家任务数据
            var playerQuest = new PlayerQuestData(questData);
            playerQuest.state = QuestState.InProgress;
            playerQuest.acceptTime = DateTime.Now;
            activeQuests[questId] = playerQuest;

            OnQuestAccepted?.Invoke(questData);
            GlobalManager.Instance?.UIManager?.ShowNotification($"任务已接受: {questData.questName}");

            return true;
        }

        public bool CompleteQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var playerQuest))
                return false;

            if (playerQuest.state != QuestState.InProgress)
                return false;

            if (!playerQuest.IsCompleted())
                return false;

            // 标记任务完成
            playerQuest.state = QuestState.Completed;
            playerQuest.completeTime = DateTime.Now;
            playerQuest.completionCount++;

            var questData = questDatabase.GetQuestData(questId);
            OnQuestCompleted?.Invoke(questData);
            GlobalManager.Instance?.UIManager?.ShowNotification($"任务完成: {questData.questName}");

            return true;
        }
        // 任务奖励
        public bool ClaimQuestReward(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out var playerQuest))
                return false;

            if (playerQuest.state != QuestState.Completed)
                return false;

            var questData = questDatabase.GetQuestData(questId);
            if (questData == null)
                return false;

            // 发放奖励
            GrantQuestReward(questData.rewards);

            // 标记奖励已领取
            playerQuest.state = QuestState.Claimed;
            completedQuests.Add(questId);
            activeQuests.Remove(questId);

            OnQuestRewardClaimed?.Invoke(questData, questData.rewards);

            return true;
        }
        // 任务奖励发放
        private void GrantQuestReward(QuestReward reward)
        {
            if (reward == null)
                return;

            var resourceManager = GlobalManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                // 发放资源奖励
                foreach (var resource in reward.resources.cost)
                {
                    resourceManager.AddResource(resource.Key, resource.Value);
                }

                // 发放宝石奖励
                if (reward.gemReward > 0)
                {
                    resourceManager.AddResource(ResourceType.Gold, reward.gemReward);
                }
            }

            // 增加经验
            if (reward.experience > 0)
            {
                // GlobalManager.Instance?.LevelManager?.AddExperience(reward.experience);
            }

            // 解锁内容
            if (reward.unlockContent != null)
            {
                foreach (var contentId in reward.unlockContent)
                {
                    // 解锁对应内容
                    UnlockContent(contentId);
                }
            }

            // 发放装备奖励
            if (reward.equipmentIds != null)
            {
                foreach (var equipmentId in reward.equipmentIds)
                {
                    // 添加到玩家装备
                    AddEquipmentToPlayer(equipmentId);
                }
            }
        }

        private void UnlockContent(string contentId)
        {
            // 解锁关卡、建筑、单位等
            var levelManager = GlobalManager.Instance?.LevelManager;
            if (levelManager != null)
            {
                var level = levelManager.levelDatabase?.GetLevelData(int.Parse(contentId));
                if (level != null)
                {
                    level.isUnlocked = true;
                }
            }
        }

        private void AddEquipmentToPlayer(string equipmentId)
        {
            // 添加到玩家装备库
            var equipmentManager = GlobalManager.Instance?.GetComponent<EquipmentManager>();
            if (equipmentManager != null)
            {
                equipmentManager.AddEquipment(equipmentId);
            }
        }

        #endregion

        #region 任务进度更新

        public void UpdateQuestProgress(QuestObjectiveType type, string targetId = null, int amount = 1)
        {
            foreach (var quest in activeQuests.Values)
            {
                if (quest.state == QuestState.InProgress)
                {
                    quest.UpdateObjective(type, targetId, amount);

                    // 检查任务是否完成
                    if (quest.IsCompleted())
                    {
                        CompleteQuest(quest.questId);
                    }
                }
            }

            // 更新成就进度
            UpdateAchievementProgress(type, targetId, amount);
        }

        private void UpdateAchievementProgress(QuestObjectiveType type, string targetId, int amount)
        {
            foreach (var achievement in achievementDatabase?.achievements)
            {
                if (achievement.unlockType == type)
                {
                    string key = $"{type}_{achievement.targetId}";
                    if (!achievementProgress.ContainsKey(key))
                        achievementProgress[key] = 0;

                    achievementProgress[key] += amount;

                    // 检查成就是否解锁
                    if (achievementProgress[key] >= achievement.requiredAmount)
                    {
                        UnlockAchievement(achievement.id);
                    }
                }
            }
        }

        #endregion

        #region 成就系统

        public void UnlockAchievement(string achievementId)
        {
            var achievement = achievementDatabase?.GetAchievementData(achievementId);
            if (achievement == null)
                return;

            // 发放成就奖励
            GrantQuestReward(achievement.rewards);

            OnAchievementUnlocked?.Invoke(achievement);
            GlobalManager.Instance?.UIManager?.ShowNotification($"成就解锁: {achievement.achievementName}");
        }

        #endregion

        #region 每日任务

        private void InitializeDailyQuests()
        {
            // 检查是否需要重置每日任务
            var lastResetTime = GetLastDailyResetTime();
            if (DateTime.Now.Date > lastResetTime.Date)
            {
                ResetDailyQuests();
            }
        }

        private void UpdateDailyQuests()
        {
            // 每小时检查一次
            if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
            {
                var lastResetTime = GetLastDailyResetTime();
                if (DateTime.Now.Date > lastResetTime.Date)
                {
                    ResetDailyQuests();
                }
            }
        }

        private void ResetDailyQuests()
        {
            // 移除已完成的每日任务
            var keysToRemove = new List<string>();
            foreach (var quest in activeQuests)
            {
                var questData = questDatabase.GetQuestData(quest.Key);
                if (questData != null && questData.questType == QuestType.Daily)
                {
                    keysToRemove.Add(quest.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                activeQuests.Remove(key);
            }

            // 生成新的每日任务
            GenerateDailyQuests();

            SetLastDailyResetTime(DateTime.Now);
        }

        private void GenerateDailyQuests()
        {
            // 从数据库中选择每日任务
            var dailyQuests = questDatabase?.GetQuestsByType(QuestType.Daily);
            if (dailyQuests != null)
            {
                // 随机选择几个每日任务
                int questCount = Mathf.Min(3, dailyQuests.Count);
                for (int i = 0; i < questCount; i++)
                {
                    var quest = dailyQuests[i];
                    if (quest.CanAccept(GlobalManager.Instance?.LevelManager?.playerLevel ?? 1, completedQuests))
                    {
                        AcceptQuest(quest.id);
                    }
                }
            }
        }

        #endregion

        #region 数据持久化

        public void SaveQuestData()
        {
            var saveData = new QuestSaveData
            {
                activeQuests = new List<PlayerQuestData>(activeQuests.Values),
                completedQuests = new List<string>(completedQuests),
                achievementProgress = achievementProgress,
                lastDailyResetTime = GetLastDailyResetTime()
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("QuestData", json);
        }

        public void LoadQuestData()
        {
            if (PlayerPrefs.HasKey("QuestData"))
            {
                string json = PlayerPrefs.GetString("QuestData");
                var saveData = JsonUtility.FromJson<QuestSaveData>(json);

                if (saveData != null)
                {
                    // 加载活跃任务
                    foreach (var quest in saveData.activeQuests)
                    {
                        activeQuests[quest.questId] = quest;
                    }

                    // 加载已完成任务
                    completedQuests = new HashSet<string>(saveData.completedQuests);

                    // 加载成就进度
                    achievementProgress = saveData.achievementProgress ?? new Dictionary<string, int>();

                    // 设置最后重置时间
                    if (saveData.lastDailyResetTime != default)
                    {
                        SetLastDailyResetTime(saveData.lastDailyResetTime);
                    }
                }
            }
        }

        private DateTime GetLastDailyResetTime()
        {
            if (PlayerPrefs.HasKey("LastDailyReset"))
            {
                long ticks = long.Parse(PlayerPrefs.GetString("LastDailyReset"));
                return new DateTime(ticks);
            }
            return DateTime.MinValue;
        }

        private void SetLastDailyResetTime(DateTime time)
        {
            PlayerPrefs.SetString("LastDailyReset", time.Ticks.ToString());
        }

        #endregion

        #region 查询方法

        public List<PlayerQuestData> GetActiveQuests()
        {
            return new List<PlayerQuestData>(activeQuests.Values);
        }

        public List<PlayerQuestData> GetActiveQuestsByType(QuestType type)
        {
            var result = new List<PlayerQuestData>();
            foreach (var quest in activeQuests.Values)
            {
                var questData = questDatabase.GetQuestData(quest.questId);
                if (questData != null && questData.questType == type)
                {
                    result.Add(quest);
                }
            }
            return result;
        }

        public PlayerQuestData GetQuestData(string questId)
        {
            activeQuests.TryGetValue(questId, out var quest);
            return quest;
        }

        public bool IsQuestCompleted(string questId)
        {
            return completedQuests.Contains(questId);
        }
        public List<QuestData> GetAvailableQuests()
        {
            return questDatabase.quests;
        }

        #endregion

        [System.Serializable]
        private class QuestSaveData
        {
            public List<PlayerQuestData> activeQuests;
            public List<string> completedQuests;
            public Dictionary<string, int> achievementProgress;
            public DateTime lastDailyResetTime;
        }
    }
}