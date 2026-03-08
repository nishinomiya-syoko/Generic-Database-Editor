using System.Collections.Generic;
using UnityEngine;
// ==================== 运行时层 ====================

/// <summary>
/// 运行时技能实例
/// </summary>
public class SkillInstance
{
    public SkillData Data { get; private set; }
    public int CurrentLevel { get; private set; }
    public float CurrentCooldown { get; set; }
    public bool IsEvolved { get; private set; }

    // 运行时修饰器（来自装备、Buff等）
    private List<ISkillModifier> modifiers = new List<ISkillModifier>();
    public void ResetCD()
    {
        CurrentCooldown = Data.cooldown;
    }
    public SkillInstance(SkillData data)
    {
        Data = data;
        CurrentLevel = 1;
    }

    /// <summary>
    /// 升级技能
    /// </summary>
    public bool LevelUp()
    {
        if (CurrentLevel >= Data.maxLevel) return false;
        CurrentLevel++;
        return true;
    }

    /// <summary>
    /// 计算最终伤害
    /// </summary>
    public float CalculateDamage(DamageContext context)
    {
        float baseValue = Data.baseDamage + Data.damagePerLevel * (CurrentLevel - 1);

        // 应用所有修饰器
        foreach (var modifier in modifiers)
        {
            baseValue = modifier.ModifyDamage(baseValue, context);
        }

        return baseValue;
    }

    /// <summary>
    /// 添加修饰器
    /// </summary>
    public void AddModifier(ISkillModifier modifier)
    {
        modifiers.Add(modifier);
    }

    /// <summary>
    /// 检查是否可以进化
    /// </summary>
    public bool CanEvolve(List<SkillInstance> ownedSkills)
    {
        foreach (var evo in Data.evolutions)
        {
            var requiredSkill = ownedSkills.Find(s => s.Data.skillId == evo.requiredSkillId);
            if (requiredSkill != null && requiredSkill.CurrentLevel >= evo.requiredLevel)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 执行进化
    /// </summary>
    public SkillInstance Evolve()
    {
        var evolution = Data.evolutions[0]; // 简化处理，实际可能需要选择
        return new SkillInstance(evolution.evolvedSkill);
    }
}

/// <summary>
/// 伤害计算上下文
/// </summary>
public class DamageContext
{
    public GameObject attacker;
    public GameObject target;
    public bool isCrit;
    public bool isKill;
    public List<string> activeTags = new List<string>();
}