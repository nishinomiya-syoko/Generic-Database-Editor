using System;
using System.Collections.Generic;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// 任务类型枚举
    /// </summary>
    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Weekly,
        Event,
        Tutorial
    }
    
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum QuestState
    {
        NotStarted,
        InProgress,
        Completed,
        TurnedIn
    }
    
    /// <summary>
    /// 任务目标类型
    /// </summary>
    public enum ObjectiveType
    {
        Kill,
        Collect,
        Talk,
        ReachLocation,
        DefeatBoss,
        Escort,
        Defend
    }
    
    /// <summary>
    /// 任务目标
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public string ObjectiveId;
        public ObjectiveType Type;
        public string TargetId; // 目标ID（怪物ID、物品ID等）
        public string Description;
        public int RequiredAmount = 1;
        public int CurrentAmount;
        public bool IsOptional;
        public bool IsCompleted;
    }
    
    /// <summary>
    /// 任务奖励
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        public string ItemId;
        public int ItemAmount;
        public int Experience;
        public int Gold;
        public string SkillId;
    }
    
    /// <summary>
    /// 任务 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "SO Editor/Quest")]
    public class QuestSO : ScriptableObjectBase
    {
        [Header("任务属性")]
        [SerializeField] private QuestType _questType = QuestType.Side;
        [SerializeField] private int _requiredLevel = 1;
        [SerializeField] private string _prerequisiteQuestId;
        [SerializeField] private bool _isRepeatable;
        [SerializeField] private int _repeatCooldownHours;
        
        [Header("目标")]
        [SerializeField] private List<QuestObjective> _objectives = new List<QuestObjective>();
        
        [Header("奖励")]
        [SerializeField] private QuestReward _reward = new QuestReward();
        
        [Header("NPC")]
        [SerializeField] private string _startNpcId;
        [SerializeField] private string _completeNpcId;
        
        [Header("时间限制")]
        [SerializeField] private bool _hasTimeLimit;
        [SerializeField] private float _timeLimitMinutes;
        
        // 属性访问器
        public QuestType QuestType => _questType;
        public int RequiredLevel => _requiredLevel;
        public string PrerequisiteQuestId => _prerequisiteQuestId;
        public bool IsRepeatable => _isRepeatable;
        public int RepeatCooldownHours => _repeatCooldownHours;
        public List<QuestObjective> Objectives => _objectives;
        public QuestReward Reward => _reward;
        public string StartNpcId => _startNpcId;
        public string CompleteNpcId => _completeNpcId;
        public bool HasTimeLimit => _hasTimeLimit;
        public float TimeLimitMinutes => _timeLimitMinutes;
        
        public override string GetSOType() => "Quest";
        
        public override object GetSerializableData()
        {
            return new QuestData
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                IconPath = Icon != null ? Icon.name : "",
                QuestType = _questType,
                RequiredLevel = _requiredLevel,
                PrerequisiteQuestId = _prerequisiteQuestId,
                IsRepeatable = _isRepeatable,
                RepeatCooldownHours = _repeatCooldownHours,
                Objectives = _objectives,
                Reward = _reward,
                StartNpcId = _startNpcId,
                CompleteNpcId = _completeNpcId,
                HasTimeLimit = _hasTimeLimit,
                TimeLimitMinutes = _timeLimitMinutes
            };
        }
        
        public override void LoadFromSerializableData(object data)
        {
            if (data is QuestData questData)
            {
                SetId(questData.Id);
                SetDisplayName(questData.DisplayName);
                SetDescription(questData.Description);
                _questType = questData.QuestType;
                _requiredLevel = questData.RequiredLevel;
                _prerequisiteQuestId = questData.PrerequisiteQuestId;
                _isRepeatable = questData.IsRepeatable;
                _repeatCooldownHours = questData.RepeatCooldownHours;
                _objectives = questData.Objectives ?? new List<QuestObjective>();
                _reward = questData.Reward ?? new QuestReward();
                _startNpcId = questData.StartNpcId;
                _completeNpcId = questData.CompleteNpcId;
                _hasTimeLimit = questData.HasTimeLimit;
                _timeLimitMinutes = questData.TimeLimitMinutes;
            }
        }
        
        // 设置方法
        public void SetQuestType(QuestType type) => _questType = type;
        public void SetRequiredLevel(int level) => _requiredLevel = level;
        public void SetPrerequisiteQuestId(string id) => _prerequisiteQuestId = id;
        public void SetIsRepeatable(bool repeatable) => _isRepeatable = repeatable;
        public void SetRepeatCooldownHours(int hours) => _repeatCooldownHours = hours;
        public void SetObjectives(List<QuestObjective> objectives) => _objectives = objectives;
        public void SetReward(QuestReward reward) => _reward = reward;
        public void SetStartNpcId(string id) => _startNpcId = id;
        public void SetCompleteNpcId(string id) => _completeNpcId = id;
        public void SetHasTimeLimit(bool has) => _hasTimeLimit = has;
        public void SetTimeLimitMinutes(float minutes) => _timeLimitMinutes = minutes;
        
        /// <summary>
        /// 添加任务目标
        /// </summary>
        public void AddObjective(QuestObjective objective)
        {
            if (_objectives == null)
                _objectives = new List<QuestObjective>();
            _objectives.Add(objective);
        }
        
        /// <summary>
        /// 移除任务目标
        /// </summary>
        public void RemoveObjective(string objectiveId)
        {
            _objectives?.RemoveAll(o => o.ObjectiveId == objectiveId);
        }
    }
    
    /// <summary>
    /// 任务序列化数据
    /// </summary>
    [Serializable]
    public class QuestData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public QuestType QuestType;
        public int RequiredLevel;
        public string PrerequisiteQuestId;
        public bool IsRepeatable;
        public int RepeatCooldownHours;
        public List<QuestObjective> Objectives;
        public QuestReward Reward;
        public string StartNpcId;
        public string CompleteNpcId;
        public bool HasTimeLimit;
        public float TimeLimitMinutes;
    }
}
