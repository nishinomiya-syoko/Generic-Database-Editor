using System.Collections.Generic;
using UnityEngine;
// ==================== 技能修饰器接口 ====================

/// <summary>
/// 技能修饰器接口（用于实现技能协同效果）
/// </summary>
public interface ISkillModifier
{
    float ModifyDamage(float baseDamage, DamageContext context);
    float ModifyCooldown(float baseCooldown);
    void OnSkillCast(SkillInstance skill);
}

/// <summary>
/// 元素协同示例：火+冰=蒸汽
/// </summary>
public class ElementalSynergy : ISkillModifier
{
    private string element1;
    private string element2;
    private float bonusMultiplier;
    
    public ElementalSynergy(string e1, string e2, float multiplier)
    {
        element1 = e1;
        element2 = e2;
        bonusMultiplier = multiplier;
    }
    
    public float ModifyDamage(float baseDamage, DamageContext context)
    {
        if (context.activeTags.Contains(element1) && context.activeTags.Contains(element2))
        {
            return baseDamage * bonusMultiplier;
        }
        return baseDamage;
    }
    
    public float ModifyCooldown(float baseCooldown) => baseCooldown;
    public void OnSkillCast(SkillInstance skill) { }
}