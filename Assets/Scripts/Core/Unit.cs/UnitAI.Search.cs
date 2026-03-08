using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Top
{
    // 单位AI控制器
    public partial class UnitAI : MonoBehaviour
    {
        [Header("AI设置")]
        public float updateInterval = 0.5f;
        public LayerMask enemyLayerMask = -1;

        private Unit unit;
        private float lastUpdateTime;

        void Start()
        {
            unit = GetComponent<Unit>();
        }

        public IDamageable FindTarget()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return null;

            lastUpdateTime = Time.time;

            // 在搜索范围内寻找目标
            Collider[] colliders = Physics.OverlapSphere(transform.position, unit.data.searchRange, enemyLayerMask);

            List<IDamageable> potentialTargets = new List<IDamageable>();

            foreach (var collider in colliders)
            {
                IDamageable target = collider.GetComponent<IDamageable>();
                if (target != null && target.IsAlive && unit.CanAttackTarget(target))
                {
                    potentialTargets.Add(target);
                }
            }

            // 根据偏好选择目标
            return SelectBestTarget(potentialTargets);
        }

        private IDamageable SelectBestTarget(List<IDamageable> targets)
        {
            if (targets.Count == 0)
                return null;

            // 优先选择偏好目标
            foreach (var target in targets)
            {
                if (IsPreferredTarget(target))
                {
                    return target;
                }
            }

            // 选择最近的敌人
            IDamageable closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var target in targets)
            {
                float distance = unit.GetDistanceToTarget(target);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        private bool IsPreferredTarget(IDamageable target)
        {
            switch (unit.data.preferredTarget)
            {
                case TargetType.Building:
                    return target is Building;
                case TargetType.Unit:
                    return target is Unit;
                case TargetType.Resource:
                    return target is IResourceProducer;
                default:
                    return true;
            }
        }

        public Vector3 GetOptimalPosition(Vector3 targetPosition)
        {
            // 计算最佳攻击位置
            float distance = unit.data.attackRange * 0.8f;
            Vector3 direction = (transform.position - targetPosition).normalized;
            return targetPosition + direction * distance;
        }
    }
}