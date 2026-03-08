using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using Top;
// ==================== 技能管理器 ====================

public class SkillManager : MonoBehaviour
{
    [Header("配置")]
    public List<SkillDataSO> allSkills = new List<SkillDataSO>();           // 所有可用技能池
    public List<SkillDataSO> starterSkills = new List<SkillDataSO>();       // 初始可选技能

    [Header("运行时")]
    private List<SkillInstance> acquiredSkills = new List<SkillInstance>();    // 已获得的技能
    private Dictionary<TriggerType, List<SkillInstance>> triggerMap = new Dictionary<TriggerType, List<SkillInstance>>();

    public static SkillManager Instance { get; private set; }

    public event Action<SkillInstance> OnSkillAcquired;
    public event Action<SkillInstance> OnSkillLeveledUp;
    public event Action<SkillInstance> OnSkillEvolved;

    private void Awake()
    {
        Instance = this;
        InitializeTriggerMap();
    }

    private void InitializeTriggerMap()
    {
        foreach (TriggerType type in System.Enum.GetValues(typeof(TriggerType)))
        {
            triggerMap[type] = new List<SkillInstance>();
        }
    }

    /// <summary>
    /// 获取随机技能选择（用于升级奖励）
    /// </summary>
    public List<SkillDataSO> GetRandomSkillChoices(int count, SkillRarity minRarity)
    {
        var available = new List<SkillDataSO>();

        // 1. 优先提供已有技能的升级
        foreach (var skill in acquiredSkills)
        {
            if (skill.CurrentLevel < skill.Data.maxLevel)
            {
                available.Add(skill.Data);
            }
        }

        // 2. 提供新技能
        var ownedIds = new HashSet<string>(acquiredSkills.Select(s => s.Data.skillId));
        var newSkills = allSkills
            .Where(s => !ownedIds.Contains(s.skillId) && s.rarity >= minRarity)
            .OrderBy(_ => Random.value)
            .Take(count - available.Count);

        available.AddRange(newSkills);

        // 3. 检查进化选项
        var evolutions = new List<SkillDataSO>();
        foreach (var skill in acquiredSkills)
        {
            if (skill.CanEvolve(acquiredSkills))
            {
                evolutions.Add(skill.Data.evolutions[0].evolvedSkill);
            }
        }

        // 混合并随机排序
        var result = available.Concat(evolutions)
            .OrderBy(_ => Random.value)
            .Take(count)
            .ToList();

        return result;
    }

    /// <summary>
    /// 学习新技能
    /// </summary>
    public SkillInstance AcquireSkill(SkillDataSO data)
    {
        // 检查是否已有此技能
        var existing = acquiredSkills.Find(s => s.Data.skillId == data.skillId);
        if (existing != null)
        {
            existing.LevelUp();
            OnSkillLeveledUp?.Invoke(existing);
            return existing;
        }

        // 创建新实例
        var instance = new SkillInstance(data);
        acquiredSkills.Add(instance);
        triggerMap[data.triggerType].Add(instance);

        OnSkillAcquired?.Invoke(instance);
        return instance;
    }

    /// <summary>
    /// 触发技能效果
    /// </summary>
    public void TriggerSkills(TriggerType type, DamageContext context)
    {
        if (!triggerMap.ContainsKey(type)) return;

        foreach (var skill in triggerMap[type])
        {
            if (skill.CurrentCooldown <= 0)
            {
                ExecuteSkill(skill, context);
            }
        }
    }

    /// <summary>
    /// 执行技能
    /// </summary>
    private void ExecuteSkill(SkillInstance skill, DamageContext context)
    {
        StartCoroutine(SkillCoroutine(skill, context));
    }

    private System.Collections.IEnumerator SkillCoroutine(SkillInstance skill, DamageContext context)
    {
        var data = skill.Data;

        // 应用所有效果
        foreach (var effect in data.effects)
        {
            ApplyEffect(effect, skill, context);
            yield return new WaitForSeconds(effect.tickInterval);
        }

        // 进入冷却
        // skill.CurrentCooldown = data.cooldown;
        skill.ResetCD();
    }

    /// <summary>
    /// 应用具体效果
    /// </summary>
    private void ApplyEffect(SkillEffectData effect, SkillInstance skill, DamageContext context)
    {
        switch (effect.type)
        {
            case EffectType.Damage:
                ApplyDamageEffect(effect, skill, context);
                break;
            case EffectType.Heal:
                // ApplyHealEffect(effect, skill, context);
                break;
            case EffectType.Buff:
                // ApplyBuffEffect(effect, skill, context);
                break;
            case EffectType.Projectile:
                SpawnProjectile(effect, skill, context);
                break;
            case EffectType.Area:
                SpawnAreaEffect(effect, skill, context);
                break;
        }
    }

    private void ApplyDamageEffect(SkillEffectData effect, SkillInstance skill, DamageContext context)
    {
        float damage = skill.CalculateDamage(context);

        // 这里调用你的伤害系统
        // DamageSystem.Instance.DealDamage(context.target, damage, context.attacker);

        // 播放特效
        if (effect.visualEffect != null)
        {
            Instantiate(effect.visualEffect, context.target.transform.position, Quaternion.identity);
        }
    }

    private void SpawnProjectile(SkillEffectData effect, SkillInstance skill, DamageContext context)
    {
        var projectile = Instantiate(effect.visualEffect, context.attacker.transform.position, Quaternion.identity);
        var proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(skill, context);
        }
    }

    private void SpawnAreaEffect(SkillEffectData effect, SkillInstance skill, DamageContext context)
    {
        var area = Instantiate(effect.visualEffect, context.target.transform.position, Quaternion.identity);
        var aoe = area.GetComponent<AreaEffect>();
        if (aoe != null)
        {
            aoe.Initialize(skill, effect, context);
        }
    }

    private void Update()
    {
        // 更新冷却
        foreach (var skill in acquiredSkills)
        {
            if (skill.CurrentCooldown > 0)
            {
                skill.CurrentCooldown -= Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 获取当前所有技能（用于UI显示）
    /// </summary>
    public List<SkillInstance> GetAcquiredSkills() => acquiredSkills;
}