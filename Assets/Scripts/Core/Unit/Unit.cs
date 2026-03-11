    using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Top
{
    // 单位基类
    public partial class Unit : LiveEntity,IDamageable
    {
        [Header("单位信息")]
        public UnitDataSO data;
        // public UnitState currentState = UnitState.Idle;
        public int currentLevel = 1;

        [Header("组件引用")]
        public Animator animator;
        public Renderer unitRenderer;
        // public HealthBar healthBar;
        public ParticleSystem deathEffect;
        public ParticleSystem attackEffect;
        
        // 战斗相关
        private LiveEntity currentTarget;
        private LiveEntity currentBuildingTarget;
        private float lastAttackTime;
        private UnitAI aiController;

        // 事件
        public event Action<Unit> OnUnitSpawned;
        public event Action<Unit> OnUnitDied;
        public event Action<Unit, IDamageable> OnUnitAttacked;

        public override void OnSpawn()
        {
            base.OnSpawn();
            InitializeUnit();
        }

        public override void OnStart()
        {
            aiController = GetComponent<UnitAI>();

            if (aiController != null)
            {
                aiController.speed = data.movementSpeed;
                aiController.stoppingDistance = data.attackRange * 0.8f;
            }
        }

        public override void OnTick(float deltaTime)
        {
            if (!IsAlive || IsTraining)
                return;

            UpdateUnitState();
        }

        #region 初始化

        private void InitializeUnit()
        {
            if (data != null)
            {
                MaxHitPoints = data.hitPoints;
                CurrentHitPoints = MaxHitPoints;
            }
        }

        #endregion

        #region 单位状态更新

        private void UpdateUnitState()
        {
            switch (currentState)
            {
                case UnitState.Idle:
                    UpdateIdleState();
                    break;
                case UnitState.Moving:
                    UpdateMovingState();
                    break;
                case UnitState.Attacking:
                    UpdateAttackingState();
                    break;
            }
        }

        private void UpdateIdleState()
        {
            // AI决策
            if (aiController != null)
            {
                IDamageable target = aiController.FindTarget();
                if (target != null)
                {
                    MoveToTarget(target);
                }
            }
        }

        private void UpdateMovingState()
        {
            // 检查是否到达目标
            if (aiController != null && aiController.remainingDistance <= aiController.stoppingDistance)
            {
                if (currentTarget != null && currentTarget.IsAlive)
                {
                    StartAttacking(currentTarget);
                }
                else if (currentBuildingTarget != null)
                {
                    StartAttacking(currentBuildingTarget);
                }
                else
                {
                    currentState = UnitState.Idle;
                }
            }
        }

        private void UpdateAttackingState()
        {
            if (currentTarget != null && !currentTarget.IsAlive)
            {
                currentTarget = null;
                currentState = UnitState.Idle;
                return;
            }

            // 检查攻击冷却
            if (Time.time - lastAttackTime >= data.attackSpeed)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }

        #endregion

        #region 移动和攻击

        public void MoveToPosition(Vector3 position)
        {
            if (aiController != null)
            {
                aiController.SetDestination(position);
                currentState = UnitState.Moving;

                if (animator != null)
                {
                    animator.SetBool("IsMoving", true);
                }
            }
        }

        public void MoveToTarget(IDamageable target)
        {
            if (target == null)
                return;

            Vector3 targetPosition = (target as MonoBehaviour)?.transform.position ?? transform.position;

            if (target is Unit unit)
            {
                currentTarget = unit;
                currentBuildingTarget = null;
            }
            else if (target is Building building)
            {
                currentBuildingTarget = building;
                currentTarget = null;
            }

            MoveToPosition(targetPosition);
        }

        public void StartAttacking(LiveEntity target)
        {
            if (target == null || !target.IsAlive)
                return;

            currentTarget = target;
            currentBuildingTarget = null;
            currentState = UnitState.Attacking;

            // 停止移动
            if (aiController != null)
                aiController.ResetPath();

            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetTrigger("Attack");
            }
        }

        public void StartAttacking(Building target)
        {
            if (target == null)
                return;

            currentBuildingTarget = target as LiveEntity;
            currentTarget = null;
            currentState = UnitState.Attacking;

            // 停止移动
            if (aiController != null)
                aiController.ResetPath();

            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetTrigger("Attack");
            }
        }

        private void PerformAttack()
        {
            IDamageable target = currentTarget as IDamageable ?? currentBuildingTarget as IDamageable;
            if (target == null)
            {
                currentState = UnitState.Idle;
                return;
            }

            // 播放攻击特效
            if (attackEffect != null)
                attackEffect.Play();

            // 计算伤害
            int damage = CalculateDamage();
            target.TakeDamage(damage);

            OnUnitAttacked?.Invoke(this, target);

            // 远程攻击发射投射物
            if (data.isRanged && data.projectilePrefab != null)
            {
                LaunchProjectile(target);
            }
        }

        private int CalculateDamage()
        {
            // 基础伤害
            int damage = data.damage;

            // 等级加成
            damage = Mathf.RoundToInt(damage * (1 + (currentLevel - 1) * 0.1f));

            // 科技加成
            // var techManager = GameManager.Instance?.TechManager;
            // if (techManager != null)
            // {
            //     damage = Mathf.RoundToInt(damage * (1 + techManager.GetTechEffect(TechEffectType.UnitDamage)));
            // }

            return damage;
        }

        private void LaunchProjectile(IDamageable target)
        {
            if (data.projectilePrefab != null)
            {
                GameObject projectile = Instantiate(data.projectilePrefab, transform.position, Quaternion.identity);
                Projectile projectileScript = projectile.GetComponent<Projectile>();

                if (projectileScript != null)
                {
                    projectileScript.SetTarget(target, data.damage);
                }
            }
        }

        #endregion

        #region 伤害和死亡

        public void TakeDamage(int damage)
        {
            if (!IsAlive)
                return;

            CurrentHitPoints -= damage;

            // if (healthBar != null)
            // {
            //     healthBar.SetHealth(CurrentHitPoints);
            // }

            if (CurrentHitPoints <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            currentState = UnitState.Dead;

            // 播放死亡特效
            if (deathEffect != null)
                deathEffect.Play();

            // 播放死亡动画
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            OnUnitDied?.Invoke(this);
            GlobalManager.Instance?.EventManager?.OnUnitDied?.Invoke(this);

            // 延迟销毁
            OnRecycle();
        }

        #endregion

        #region 辅助方法

        public bool CanAttackTarget(IDamageable target)
        {
            if (target is Unit unit)
            {
                return unit.data.canFly ? data.canAttackAir : data.canAttackGround;
            }
            else if (target is Building)
            {
                return data.canAttackGround;
            }
            return false;
        }

        public float GetDistanceToTarget(IDamageable target)
        {
            if (target == null)
                return float.MaxValue;

            Vector3 targetPosition = (target as MonoBehaviour)?.transform.position ?? transform.position;
            return Vector3.Distance(transform.position, targetPosition);
        }

        #endregion
    }
}