using UnityEngine;
using System;

namespace Top
{
    /// <summary>
    /// 技能触发条件
    /// </summary>
    // [Serializable]
    [CreateAssetMenu(fileName = "New Condition", menuName = "Equip/Condition")]
    public class ConditionData : ScriptableObject
    {
        public string id;
        public string conditionName;
        public string description;
        [Tooltip("运算条件 = 判断条件一 相对于 判断条件二 的运算符")]
        [Header("判断条件1")]
        public EnumProperty propertyType_1;
        public SkillTargetType targetType_1;

        [Header("条件运算符")]
        public EnumMathematicalJudgmentType mathematicalJudgmentType;

        [Header("条件值")]
        public float triggerValue;
        [Header("百分比")]
        public bool isPercentage;
        [Header("判断条件2")]
        public EnumProperty propertyType_2;
        public SkillTargetType targetType_2;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }
        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        /// <returns></returns>
        public bool IsConditionMet(float value1, float value2)
        {
            switch (mathematicalJudgmentType)
            {
                case EnumMathematicalJudgmentType.GreaterThan:
                    return value1 > value2;
                case EnumMathematicalJudgmentType.LessThan:
                    return value1 < value2;
                case EnumMathematicalJudgmentType.Equal:
                    return Math.Abs(value1 - value2) < 0.001f;
                case EnumMathematicalJudgmentType.NotEqual:
                    return Math.Abs(value1 - value2) >= 0.001f;
                case EnumMathematicalJudgmentType.GreaterThanOrEqual:
                    return value1 >= value2;
                case EnumMathematicalJudgmentType.LessThanOrEqual:
                    return value1 <= value2;
                default:
                    return false;
            }
        }
    }

    public enum EnumProperty
    {
        /// <summary>无属性</summary>
        None = -1,

        #region 基础成长属性
        /// <summary>经验值</summary>
        Exp,
        /// <summary>等级</summary>
        Level,
        /// <summary>升级所需经验值</summary>
        UpgradeExp,
        /// <summary>经验值加成</summary>
        ExpAdditionalAdd,
        /// <summary>经验获取</summary>
        ExpGet,
        #endregion

        #region 生命与魔力属性
        /// <summary>生命值</summary>
        HP,
        /// <summary>最大生命值</summary>
        MaxHp,
        /// <summary>每秒回复生命值</summary>
        RegenerateHPEverySecond,
        /// <summary>每秒生命值回复（等价于RegenerateHPEverySecond）</summary>
        HpRegenPerSecond,

        /// <summary>魔力值</summary>
        MP,
        /// <summary>最大魔力值</summary>
        MaxMP,
        /// <summary>每秒回复魔力值</summary>
        RegenerateMPEverySecond,
        /// <summary>每秒魔力值回复（等价于RegenerateMPEverySecond）</summary>
        MpRegenPerSecond,
        /// <summary>普通攻击时获得的魔力值</summary>
        NormalAttackGetMP,
        #endregion

        #region 基础能力属性
        /// <summary>攻击力（综合）</summary>
        Attack,
        /// <summary>物理攻击力</summary>
        PhysicalAttack,
        /// <summary>魔法力（综合）</summary>
        Magic,
        /// <summary>法术攻击力</summary>
        MagicAttack,
        /// <summary>防御力</summary>
        Defense,

        /// <summary>力量</summary>
        Strength,
        /// <summary>敏捷</summary>
        Agility,
        /// <summary>智力</summary>
        Intelligence,
        #endregion

        #region 战斗机制属性
        /// <summary>行动速度</summary>
        SpeedOfAction,
        /// <summary>攻击速度</summary>
        AttackSpeed,
        /// <summary>移动速度</summary>
        MoveSpeed,

        /// <summary>暴击几率（综合）</summary>
        CritChance,
        /// <summary>物理暴击几率</summary>
        PhysicalCritChance,
        /// <summary>魔法暴击几率</summary>
        MagicCritChance,

        /// <summary>暴击额外伤害（综合）</summary>
        CritDamage,
        /// <summary>物理暴击伤害</summary>
        PhysicalCritDamage,
        /// <summary>魔法暴击伤害</summary>
        MagicCritDamage,

        /// <summary>闪避</summary>
        Evasion,
        /// <summary>反击几率（受到普通攻击时触发）</summary>
        CounterattackProbability,
        #endregion

        #region 伤害与减免属性
        /// <summary>火焰伤害</summary>
        FireDamage,
        /// <summary>冰霜伤害</summary>
        IceDamage,
        /// <summary>闪电伤害</summary>
        LightningDamage,
        /// <summary>毒素伤害</summary>
        PoisonDamage,
        /// <summary>神圣伤害</summary>
        HolyDamage,

        /// <summary>伤害减免</summary>
        DamageReduction,
        /// <summary>伤害额外加深</summary>
        IncreasedDamage,
        /// <summary>技能伤害</summary>
        SkillDamage,
        /// <summary>伤害扩散</summary>
        AmplifyDamage,
        #endregion

        #region 资源获取属性
        /// <summary>战斗内金币</summary>
        FightGold,
        /// <summary>金币获取量</summary>
        GoldGet,
        /// <summary>生命值获取（命中时）</summary>
        LifeOnHit,
        /// <summary>法术吸血（命中时获取魔力）</summary>
        ManaOnHit,
        #endregion

        #region 特殊效果属性
        /// <summary>幸运</summary>
        Luck,
        /// <summary>护盾值</summary>
        Shield,
        /// <summary>物理吸血</summary>
        PhysicalVamp,
        /// <summary>法术吸血（等价于ManaOnHit，按上下文区分）</summary>
        MagicVamp,
        /// <summary>荆棘（反弹伤害）</summary>
        Thorns,
        #endregion

        #region 技能与投射物属性
        /// <summary>投射物数量</summary>
        ProjectilesNumber,
        /// <summary>投射物穿透次数</summary>
        ProjectilesPenetrationCount,
        /// <summary>技能加速冷却</summary>
        SkillCooldownFaster,
        #endregion

        /// <summary>枚举结束标记</summary>
        End
    }


    /// <summary>
    /// 数学符号枚举
    /// </summary>
    public enum EnumMathematicalJudgmentType
    {

        /// <summary>等于（= 或 ==）：判断两数是否相等</summary>
        Equal,
        /// <summary>不等于（≠ 或 !=）：判断两数是否不相等</summary>
        NotEqual,
        /// <summary>大于（>）：判断前者是否大于后者</summary>
        GreaterThan,
        /// <summary>小于（<）：判断前者是否小于后者</summary>
        LessThan,
        /// <summary>大于等于（≥ 或 >=）：判断前者是否大于或等于后者</summary>
        GreaterThanOrEqual,
        /// <summary>小于等于（≤ 或 <=）：判断前者是否小于或等于后者</summary>
        LessThanOrEqual,

    }
}