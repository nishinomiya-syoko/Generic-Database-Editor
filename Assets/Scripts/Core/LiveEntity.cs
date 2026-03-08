using UnityEngine;

namespace Top
{
    public class LiveEntity : EntityBase
    {
        [Header("单位信息")]
        public UnitData data;
        public UnitState currentState = UnitState.Idle;
        public int currentLevel = 1;

        [Header("组件引用")]
        public Animator animator;
        public Renderer unitRenderer;
        // public HealthBar healthBar;
        public ParticleSystem deathEffect;
        public ParticleSystem attackEffect;

        // 属性
        public int CurrentHitPoints { get; protected set; }
        public int MaxHitPoints { get; protected set; }
        public bool IsAlive => currentState != UnitState.Dead;
        public bool IsTraining => currentState == UnitState.Training;

    }
}