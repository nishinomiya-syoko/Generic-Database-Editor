using System.Collections.Generic;
using UnityEngine;
using System;
/// <summary>
/// 技能数据配置（ScriptableObject）
/// </summary>
[EditableData]
public class SkillData
{
    [Header("基础信息")]
    public string Id;
    public string skillName;
    public string description;
    public Sprite icon;
    // public SkillRarity rarity;
    
    [Header("技能参数")]
    public int maxLevel = 5;
    public float baseDamage;
    public float damagePerLevel;
    public float cooldown;
    public float manaCost;
    public float range;
    
    [Header("效果配置")]
    // public TriggerType triggerType;
    // public EffectType effectType;
    public List<SkillEffectData> effects = new List<SkillEffectData>();
    
    [Header("进化路线")]
    public List<SkillEvolution> evolutions = new List<SkillEvolution>();
    
    [Header("标签")]
    public List<string> tags = new List<string>(); // Fire, Ice, Lightning等

    public string GetDescription(int level)
    {
        float currentDamage = baseDamage + damagePerLevel * (level - 1);
        return description.Replace("{damage}", currentDamage.ToString("F0"))
                         .Replace("{level}", level.ToString());
    }
    private void OnValidate()
        {
            if (string.IsNullOrEmpty(Id))
                Id = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
}

/// <summary>
/// 技能效果数据
/// </summary>
[System.Serializable]
public class SkillEffectData
{
    // public EffectType type;
    public float value;
    public float duration;
    public float tickInterval;      // 持续效果的间隔
    public int maxStacks;           // 最大叠加层数
    public GameObject visualEffect; // 特效预制体
    public string visualEffectPath;

    public AudioClip soundEffect;
    public string soundEffectPath;
}

/// <summary>
/// 技能进化条件
/// </summary>
[System.Serializable]
public class SkillEvolution
{
    public string requiredSkillId;      // 需要的另一个技能ID
    public int requiredLevel;
    public SkillData evolvedSkill;      // 进化后的技能
    public string evolvedSkillId;
    public string evolutionCondition;   // 进化条件描述
}