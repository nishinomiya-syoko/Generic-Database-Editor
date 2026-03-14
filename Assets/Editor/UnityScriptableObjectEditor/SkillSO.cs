using System;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        Active,
        Passive,
        Buff,
        Debuff,
        Ultimate
    }
    
    /// <summary>
    /// 伤害类型枚举
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical,
        True,
        Fire,
        Ice,
        Lightning,
        Poison
    }
    
    /// <summary>
    /// 技能 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "SO Editor/Skill")]
    public class SkillSO : ScriptableObjectBase
    {
        [Header("技能属性")]
        [SerializeField] private SkillType _skillType = SkillType.Active;
        [SerializeField] private DamageType _damageType = DamageType.Physical;
        [SerializeField] private int _manaCost;
        [SerializeField] private float _cooldown = 1f;
        [SerializeField] private float _castTime;
        [SerializeField] private float _duration = 1f;
        [SerializeField] private float _range = 5f;
        [SerializeField] private float _areaOfEffect;
        
        [Header("伤害/治疗")]
        [SerializeField] private float _baseDamage;
        [SerializeField] private float _damageMultiplier = 1f;
        [SerializeField] private float _healingAmount;
        [SerializeField] private bool _isHealingSkill;
        
        [Header("效果")]
        [SerializeField] private bool _hasKnockback;
        [SerializeField] private float _knockbackForce;
        [SerializeField] private bool _hasStun;
        [SerializeField] private float _stunDuration;
        [SerializeField] private bool _hasDot; // Damage over time
        [SerializeField] private float _dotDamage;
        [SerializeField] private float _dotDuration;
        
        [Header("需求")]
        [SerializeField] private int _requiredLevel = 1;
        [SerializeField] private string _requiredSkillId;
        
        // 属性访问器
        public SkillType SkillType => _skillType;
        public DamageType DamageType => _damageType;
        public int ManaCost => _manaCost;
        public float Cooldown => _cooldown;
        public float CastTime => _castTime;
        public float Duration => _duration;
        public float Range => _range;
        public float AreaOfEffect => _areaOfEffect;
        public float BaseDamage => _baseDamage;
        public float DamageMultiplier => _damageMultiplier;
        public float HealingAmount => _healingAmount;
        public bool IsHealingSkill => _isHealingSkill;
        public bool HasKnockback => _hasKnockback;
        public float KnockbackForce => _knockbackForce;
        public bool HasStun => _hasStun;
        public float StunDuration => _stunDuration;
        public bool HasDot => _hasDot;
        public float DotDamage => _dotDamage;
        public float DotDuration => _dotDuration;
        public int RequiredLevel => _requiredLevel;
        public string RequiredSkillId => _requiredSkillId;
        
        public override string GetSOType() => "Skill";
        
        public override object GetSerializableData()
        {
            return new SkillData
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                IconPath = Icon != null ? Icon.name : "",
                SkillType = _skillType,
                DamageType = _damageType,
                ManaCost = _manaCost,
                Cooldown = _cooldown,
                CastTime = _castTime,
                Duration = _duration,
                Range = _range,
                AreaOfEffect = _areaOfEffect,
                BaseDamage = _baseDamage,
                DamageMultiplier = _damageMultiplier,
                HealingAmount = _healingAmount,
                IsHealingSkill = _isHealingSkill,
                HasKnockback = _hasKnockback,
                KnockbackForce = _knockbackForce,
                HasStun = _hasStun,
                StunDuration = _stunDuration,
                HasDot = _hasDot,
                DotDamage = _dotDamage,
                DotDuration = _dotDuration,
                RequiredLevel = _requiredLevel,
                RequiredSkillId = _requiredSkillId
            };
        }
        
        public override void LoadFromSerializableData(object data)
        {
            if (data is SkillData skillData)
            {
                SetId(skillData.Id);
                SetDisplayName(skillData.DisplayName);
                SetDescription(skillData.Description);
                _skillType = skillData.SkillType;
                _damageType = skillData.DamageType;
                _manaCost = skillData.ManaCost;
                _cooldown = skillData.Cooldown;
                _castTime = skillData.CastTime;
                _duration = skillData.Duration;
                _range = skillData.Range;
                _areaOfEffect = skillData.AreaOfEffect;
                _baseDamage = skillData.BaseDamage;
                _damageMultiplier = skillData.DamageMultiplier;
                _healingAmount = skillData.HealingAmount;
                _isHealingSkill = skillData.IsHealingSkill;
                _hasKnockback = skillData.HasKnockback;
                _knockbackForce = skillData.KnockbackForce;
                _hasStun = skillData.HasStun;
                _stunDuration = skillData.StunDuration;
                _hasDot = skillData.HasDot;
                _dotDamage = skillData.DotDamage;
                _dotDuration = skillData.DotDuration;
                _requiredLevel = skillData.RequiredLevel;
                _requiredSkillId = skillData.RequiredSkillId;
            }
        }
        
        // 设置方法
        public void SetSkillType(SkillType type) => _skillType = type;
        public void SetDamageType(DamageType type) => _damageType = type;
        public void SetManaCost(int cost) => _manaCost = cost;
        public void SetCooldown(float cd) => _cooldown = cd;
        public void SetCastTime(float time) => _castTime = time;
        public void SetDuration(float duration) => _duration = duration;
        public void SetRange(float range) => _range = range;
        public void SetAreaOfEffect(float aoe) => _areaOfEffect = aoe;
        public void SetBaseDamage(float damage) => _baseDamage = damage;
        public void SetDamageMultiplier(float mult) => _damageMultiplier = mult;
        public void SetHealingAmount(float heal) => _healingAmount = heal;
        public void SetIsHealingSkill(bool isHeal) => _isHealingSkill = isHeal;
        public void SetHasKnockback(bool has) => _hasKnockback = has;
        public void SetKnockbackForce(float force) => _knockbackForce = force;
        public void SetHasStun(bool has) => _hasStun = has;
        public void SetStunDuration(float duration) => _stunDuration = duration;
        public void SetHasDot(bool has) => _hasDot = has;
        public void SetDotDamage(float damage) => _dotDamage = damage;
        public void SetDotDuration(float duration) => _dotDuration = duration;
        public void SetRequiredLevel(int level) => _requiredLevel = level;
        public void SetRequiredSkillId(string id) => _requiredSkillId = id;
    }
    
    /// <summary>
    /// 技能序列化数据
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public SkillType SkillType;
        public DamageType DamageType;
        public int ManaCost;
        public float Cooldown;
        public float CastTime;
        public float Duration;
        public float Range;
        public float AreaOfEffect;
        public float BaseDamage;
        public float DamageMultiplier;
        public float HealingAmount;
        public bool IsHealingSkill;
        public bool HasKnockback;
        public float KnockbackForce;
        public bool HasStun;
        public float StunDuration;
        public bool HasDot;
        public float DotDamage;
        public float DotDuration;
        public int RequiredLevel;
        public string RequiredSkillId;
    }
}
