using UnityEngine;

namespace Top
{
    public class LiveEntity : EntityBase
    {
        public UnitState currentState = UnitState.Idle;

        // 属性
        public int CurrentHitPoints { get; protected set; }
        public int MaxHitPoints { get; protected set; }

        public bool IsAlive => currentState != UnitState.Dead;
        public bool IsTraining => currentState == UnitState.Training;
        //
        public bool IsBuilding => currentState == UnitState.Building;
        public bool IsWorking => currentState == UnitState.Working;

        public UnitType UnitType { get; protected set; }
        public UnitType PreferredTarget { get; protected set; }

    }
}