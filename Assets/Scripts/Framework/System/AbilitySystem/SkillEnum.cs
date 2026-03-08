using System.Collections.Generic;
using UnityEngine;
// ==================== 数据层 ====================

/// <summary>
/// 技能稀有度
/// </summary>
public enum SkillRarity
{
    Common,     // 普通
    Rare,       // 稀有
    Epic,       // 史诗
    Legendary   // 传说
}

/// <summary>
/// 技能触发时机
/// </summary>
public enum TriggerType
{
    OnAttack,       // 攻击时
    OnHit,          // 命中时
    OnKill,         // 击杀时
    OnDamaged,      // 受伤时
    OnDodge,        // 闪避时
    OnCrit,         // 暴击时
    OnTimer,        // 定时触发
    OnSkillCast,    // 释放技能时
    Passive         // 被动常驻
}

/// <summary>
/// 效果类型
/// </summary>
public enum EffectType
{
    Damage,         // 伤害
    Heal,           // 治疗
    Buff,           // 增益
    Debuff,         // 减益
    Summon,         // 召唤
    Transform,      // 变形
    Teleport,       // 传送
    Projectile,     // 投射物
    Area,           // 范围效果
    Chain           // 连锁
}

/// <summary>
/// 技能数据配置（ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Roguelike/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("基础信息")]
    public string skillId;
    public string skillName;
    public string description;
    public Sprite icon;
    public SkillRarity rarity;
    
    [Header("技能参数")]
    public int maxLevel = 5;
    public float baseDamage;
    public float damagePerLevel;
    public float cooldown;
    public float manaCost;
    public float range;
    
    [Header("效果配置")]
    public TriggerType triggerType;
    public EffectType effectType;
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
}

/// <summary>
/// 技能效果数据
/// </summary>
[System.Serializable]
public class SkillEffectData
{
    public EffectType type;
    public float value;
    public float duration;
    public float tickInterval;      // 持续效果的间隔
    public int maxStacks;           // 最大叠加层数
    public GameObject visualEffect; // 特效预制体
    public AudioClip soundEffect;
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
    public string evolutionCondition;   // 进化条件描述
}