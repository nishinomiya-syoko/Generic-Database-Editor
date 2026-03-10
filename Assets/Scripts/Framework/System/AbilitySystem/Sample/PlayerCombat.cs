using System.Collections.Generic;
using UnityEngine;

// 示例：创建火球术技能
[CreateAssetMenu]
public class FireballSkill : SkillDataSO
{
    private void OnEnable()
    {
        Id = "fireball";
        skillName = "火球术";
        description = "发射火球造成 {damage} 点火焰伤害";
        rarity = SkillRarity.Common;
        triggerType = TriggerType.OnSkillCast;
        effectType = EffectType.Projectile;
        baseDamage = 50;
        damagePerLevel = 25;
        cooldown = 3f;
        tags.Add("Fire");
    }
}

// 示例：玩家攻击时触发技能
public class PlayerCombat : MonoBehaviour
{
    private void OnAttackHit(GameObject target)
    {
        var context = new DamageContext
        {
            attacker = gameObject,
            target = target,
            isCrit = Random.value < 0.2f
        };
        
        // 触发所有"命中时"技能
        SkillManager.Instance.TriggerSkills(TriggerType.OnHit, context);
    }
}