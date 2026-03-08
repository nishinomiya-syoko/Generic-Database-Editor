using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Top
{
    // 单位类型枚举
    public enum UnitType
    {
        Infantry,
        Cavalry,
        Ranged,
        Siege,
        Flying,
        Hero
    }

    // 单位状态枚举
    public enum UnitState
    {
        Idle,
        Moving,
        Attacking,
        Dead,
        Training
    }

    // 单位数据
    [CreateAssetMenu(fileName = "UnitData", menuName = "EmpireClash/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("基本信息")]
        public string id;
        public string unitName;
        public string description;
        public UnitType unitType;
        public Sprite icon;
        public GameObject prefab;

        [Header("训练信息")]
        public int trainingTime = 30; // 秒
        // public ResourceCost[] trainingCosts;
        public int housingSpace = 1;

        [Header("战斗属性")]
        public int hitPoints = 100;
        public int damage = 25;
        public float attackSpeed = 1f;
        public float attackRange = 2f;
        public float movementSpeed = 3f;
        public float attackDelay = 0.5f;

        [Header("特殊属性")]
        public bool canFly = false;
        public bool canAttackAir = false;
        public bool canAttackGround = true;
        public bool isRanged = false;
        public GameObject projectilePrefab;

        [Header("AI属性")]
        public float searchRange = 10f;
        public float preferredTargetDistance = 2f;
        public TargetType preferredTarget = TargetType.Any;

        [Header("技能系统")]
        public string[] innateSkills;           // 天生技能
        public int maxSkillSlots = 3;           // 最大技能槽位
        public string ultimateSkill;            // 终极技能
        public float skillChargeRate = 1f;      // 技能充能速度

        [Header("装备系统")]
        public bool canEquipItems = false;      // 是否可以装备物品
        // public EquipmentType[] equipableTypes;  // 可装备类型

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }
    }

    // 目标类型
    public enum TargetType
    {
        Any,
        Building,
        Unit,
        Resource
    }


    // 可伤害对象接口
    public interface IDamageable
    {
        void TakeDamage(int damage);
        bool IsAlive { get; }
    }
}