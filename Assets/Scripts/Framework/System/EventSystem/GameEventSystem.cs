using UnityEngine;
using System;
using System.Collections;

namespace Top
{
    // 游戏事件系统
    public class EventManager
    {
        // 资源事件
        public Action<ResourceType, int> OnResourceChanged;
        public Action<ResourceType, int> OnResourceProduced;
        public Action<ResourceType, int> OnResourceCollected;

        // 建筑事件
        public Action<Building> OnBuildingPlaced;
        public Action<Building> OnBuildingUpgraded;
        public Action<Building> OnBuildingDestroyed;

        // 单位事件
        public Action<Unit> OnUnitTrained;
        public Action<Unit> OnUnitDeployed;
        public Action<Unit> OnUnitDied;

        public Action<EquipmentData> OnEquipmentGet;

        // 科技事件
        public Action<string, int> OnTechResearched;

        // 关卡事件
        public Action<int> OnLevelStarted;
        public Action<int> OnLevelCompleted;
        public Action<int> OnLevelFailed;
        public Action<int> OnPlayerLeveledUp;

        // 场景事件
        public Action<int> OnSceneChanged;

        // 系统事件
        public Action OnGameLoaded;
        public Action OnGameSaved;

        //skill
        public Action<Unit,SkillData> OnSkillUsed;
    }
}