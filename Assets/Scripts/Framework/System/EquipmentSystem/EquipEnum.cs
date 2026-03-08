using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 技能类型枚举
    public enum SkillType
    {
        Active,         // 主动技能
        Passive,        // 被动技能
        Ultimate        // 终极技能
    }

    // 技能目标类型
    public enum SkillTargetType
    {
        Self,           // 自身
        Single,         // 单体
        Area,           // 范围
        Line,           // 直线
        Sector,          // 扇形
    }

    // 技能效果类型
    public enum SkillEffectType
    {
        Damage,         // 造成伤害
        Heal,           // 治疗
        Buff,           // 增益效果
        Debuff,         // 减益效果
        Summon,         // 召唤
        Teleport,       // 传送
        Shield,         // 护盾
        Stun,           // 眩晕
        Slow,           // 减速
        DOT,            // 持续伤害
        HOT             // 持续治疗
    }

    // // 技能数据
    // [CreateAssetMenu(fileName = "SkillData", menuName = "Top/Skill Data")]
    // public class SkillData : ScriptableObject
    // {
    //     [Header("基本信息")]
    //     public string id;
    //     public string skillName;
    //     public string description;
    //     public SkillType skillType;
    //     public SkillTargetType targetType;
    //     public Sprite icon;
    //     public GameObject effectPrefab;     // 技能特效预制体

    //     [Header("技能参数")]
    //     public float cooldown;              // 冷却时间
    //     public float castTime;              // 施法时间
    //     public float range;                 // 技能范围
    //     public int manaCost;                // 魔法消耗
    //     public int energyCost;              // 能量消耗

    //     [Header("技能效果")]
    //     public SkillEffectData[] effects;

    //     [Header("升级参数")]
    //     public int maxLevel = 5;
    //     public float[] damagePerLevel;      // 每级伤害
    //     public float[] rangePerLevel;       // 每级范围
    //     public float[] cooldownPerLevel;    // 每级冷却

    //     private void OnValidate()
    //     {
    //         if (string.IsNullOrEmpty(id))
    //             id = Guid.NewGuid().ToString();
    //     }

    //     public float GetDamage(int level)
    //     {
    //         if (damagePerLevel != null && level > 0 && level <= damagePerLevel.Length)
    //             return damagePerLevel[level - 1];
    //         return 0f;
    //     }

    //     public float GetRange(int level)
    //     {
    //         if (rangePerLevel != null && level > 0 && level <= rangePerLevel.Length)
    //             return rangePerLevel[level - 1];
    //         return range;
    //     }

    //     public float GetCooldown(int level)
    //     {
    //         if (cooldownPerLevel != null && level > 0 && level <= cooldownPerLevel.Length)
    //             return cooldownPerLevel[level - 1];
    //         return cooldown;
    //     }
    // }

    // BUFF类型
    public enum BuffType
    {
        DamageIncrease,     // 伤害增加
        DamageReduction,    // 伤害减免
        AttackSpeed,        // 攻击速度
        MovementSpeed,      // 移动速度
        CriticalChance,     // 暴击率
        CriticalDamage,     // 暴击伤害
        LifeSteal,          // 生命偷取
        SpellPower,         // 法术强度
        ArmorPenetration,   // 护甲穿透
        MagicPenetration,   // 魔法穿透
        CooldownReduction   // 冷却缩减
    }

    // 装备品质
    public enum EquipmentQuality
    {
        Common,     // 普通
        Rare,       // 稀有
        Epic,       // 史诗
        Legendary,  // 传说
        Mythic      // 神话
    }

    // 装备类型
    public enum EquipmentType
    {
        Weapon,     // 武器
        Armor,      // 护甲
        Helmet,     // 头盔
        Boots,      // 靴子
        Accessory,  // 饰品
        Special     // 特殊装备
    }
}