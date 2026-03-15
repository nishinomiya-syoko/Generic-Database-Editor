using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 任务类型枚举
    public enum QuestType
    {
        MainStory,      // 主线任务
        Daily,          // 每日任务
        Weekly,         // 每周任务
        Achievement,    // 成就任务
        Event           // 活动任务
    }

    // 任务状态枚举
    public enum QuestState
    {
        Locked,         // 未解锁
        Available,      // 可接受
        InProgress,     // 进行中
        Completed,      // 已完成
        Claimed         // 已领取奖励
    }

    // 任务目标类型
    public enum QuestObjectiveType
    {
        CollectResource,    // 收集资源
        BuildBuilding,      // 建造建筑
        UpgradeBuilding,    // 升级建筑
        TrainUnit,          // 训练单位
        WinBattle,          // 赢得战斗
        CompleteLevel,      // 完成关卡
        ResearchTech,       // 研究科技
        DestroyEnemy,       // 摧毁敌人
        UseSkill,           // 使用技能
        CollectEquipment,   // 收集装备
        ReachLevel          // 达到等级
    }

    // 任务目标
    [System.Serializable]
    public class QuestObjective
    {
        public QuestObjectiveType type;
        public string targetId;         // 目标ID
        public int requiredAmount;      // 需要数量
        public int currentAmount;       // 当前数量
        public string description;      // 目标描述

        public bool IsCompleted => currentAmount >= requiredAmount;

        public void UpdateProgress(int amount = 1)
        {
            currentAmount = Mathf.Min(currentAmount + amount, requiredAmount);
        }

        public string GetProgressText()
        {
            return $"{description}: {currentAmount}/{requiredAmount}";
        }
    }

    // 任务奖励
    [System.Serializable]
    public class QuestReward
    {
        public ResourceCost[] resources;    // 资源奖励
        public string[] unlockContent;      // 解锁内容
        public int experience;              // 经验奖励
        public string[] equipmentIds;       // 装备奖励
        public int gemReward;               // 宝石奖励
    }

    // 任务数据
    [EditableData]
    public class QuestData 
    {
        [Header("基本信息")]
        public string id;
        public string questName;
        public string description;
        public QuestType questType;
        public Sprite icon;
        public int priority;                // 任务优先级

        [Header("任务条件")]
        public string[] prerequisiteQuests; // 前置任务
        public int requiredPlayerLevel;     // 需要玩家等级
        public bool isRepeatable;           // 是否可重复
        public int repeatInterval;          // 重复间隔(小时)

        [Header("任务目标")]
        public QuestObjective[] objectives;

        [Header("任务奖励")]
        public QuestReward rewards;

        [Header("UI设置")]
        public Color questColor = Color.white;
        public bool showProgress = true;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }

        public bool CanAccept(int playerLevel, HashSet<string> completedQuests)
        {
            // 检查玩家等级
            if (playerLevel < requiredPlayerLevel)
                return false;

            // 检查前置任务
            foreach (var prereq in prerequisiteQuests)
            {
                if (!completedQuests.Contains(prereq))
                    return false;
            }

            return true;
        }

        public float GetProgress()
        {
            if (objectives == null || objectives.Length == 0)
                return 1f;

            int completedCount = 0;
            foreach (var obj in objectives)
            {
                if (obj.IsCompleted)
                    completedCount++;
            }

            return (float)completedCount / objectives.Length;
        }

        public bool IsAllObjectivesCompleted()
        {
            foreach (var obj in objectives)
            {
                if (!obj.IsCompleted)
                    return false;
            }
            return true;
        }
    }
    /// <summary>
    /// 玩家任务数据
    /// </summary>
    [System.Serializable]
    public class PlayerQuestData
    {
        public string questId;
        public QuestState state;
        public QuestObjective[] objectives;
        public DateTime acceptTime;
        public DateTime completeTime;
        public int completionCount;         // 完成次数

        public PlayerQuestData(QuestData questData)
        {
            questId = questData.id;
            state = QuestState.Available;
            acceptTime = DateTime.MinValue;
            completeTime = DateTime.MinValue;
            completionCount = 0;

            // 复制目标
            objectives = new QuestObjective[questData.objectives.Length];
            for (int i = 0; i < questData.objectives.Length; i++)
            {
                objectives[i] = new QuestObjective
                {
                    type = questData.objectives[i].type,
                    targetId = questData.objectives[i].targetId,
                    requiredAmount = questData.objectives[i].requiredAmount,
                    description = questData.objectives[i].description,
                    currentAmount = 0
                };
            }
        }

        public void UpdateObjective(QuestObjectiveType type, string targetId, int amount = 1)
        {
            foreach (var obj in objectives)
            {
                if (obj.type == type && (string.IsNullOrEmpty(targetId) || obj.targetId == targetId))
                {
                    obj.UpdateProgress(amount);
                }
            }
        }

        public bool IsCompleted()
        {
            foreach (var obj in objectives)
            {
                if (!obj.IsCompleted)
                    return false;
            }
            return true;
        }
    }

}   