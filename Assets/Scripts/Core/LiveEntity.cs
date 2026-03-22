using RTS.TargetSearch;
using UnityEngine;

namespace Top
{
    public class LiveEntity : EntityBase,ITargetable
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

        // 方法
        public int Id{get;}
        public Vector3 Position{ get; }
        public int TeamId{ get; }
        // public GameObject Owner { get; }

    }
}