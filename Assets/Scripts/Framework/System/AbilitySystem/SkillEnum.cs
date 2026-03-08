
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
