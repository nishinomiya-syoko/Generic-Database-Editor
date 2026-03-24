using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks;

namespace Top
{
    // 建筑基类
    public partial class Building : LiveEntity,IDamageable
    {
        [Header("建筑信息")]
        public BuildingData data;
        public EntityStat stats;
        public int currentLevel = 1;

        [Header("组件引用")]
        public Renderer buildingRenderer;
        public ParticleSystem buildEffect;
        public ParticleSystem upgradeEffect;

        // 事件
        

        public override void OnSpawn()
        {
            base.OnSpawn();
            InitializeBuilding();
        }

        public override void OnStart()
        {
            base.OnStart();
            UpdateVisuals();
        }

        public override void OnTick(float deltaTime)
        {
            base.OnTick(deltaTime);
            UpdateUnitState();
        }

        #region 初始化

        private void InitializeBuilding()
        {
            if (data != null)
            {
                MaxHitPoints = GetMaxHP();
                CurrentHitPoints = MaxHitPoints;

            }
        }

        private void UpdateVisuals()
        {
            if (buildingRenderer != null && data != null)
            {
                // 根据等级更新材质或颜色
                Material[] materials = buildingRenderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // 可以在这里根据等级调整材质属性
                    Color baseColor = materials[i].color;
                    float levelBrightness = 0.8f + (currentLevel * 0.02f);
                    materials[i].color = baseColor * levelBrightness;
                }
                buildingRenderer.materials = materials;
            }
        }

        #endregion

        #region 建造和升级

        public void StartBuilding()
        {
            if (currentState != UnitState.Idle)
                return;

            currentState = UnitState.Building;

            // 播放建造特效
            if (buildEffect != null)
                buildEffect.Play();

            BuildingProcess().Forget();
        }

        private async UniTask BuildingProcess()
        {
            float buildTime = data.GetLevelData(currentLevel).upgradeTime;
            float elapsedTime = 0f;

            // 建造动画
            while (elapsedTime < buildTime)
            {
                elapsedTime += 0.1f;
                float progress = elapsedTime / buildTime;

                // 建造进度动画
                if (buildingRenderer != null)
                {
                    Vector3 scale = Vector3.one * Mathf.Lerp(0.1f, 1f, progress);
                    buildingRenderer.transform.localScale = scale;
                }

                await UniTask.WaitForSeconds(0.1f);
            }

            // 建造完成
            buildingRenderer.transform.localScale = Vector3.one;
            currentState = UnitState.Working;

            // 停止建造特效
            if (buildEffect != null)
                buildEffect.Stop();

            GM.EventManager?.OnBuildingPlaced?.Invoke(this);
        }

        public void StartUpgrade()
        {
            if (currentState != UnitState.Working || currentLevel >= data.maxLevel)
                return;

            currentState = UnitState.Building;

            // 播放升级特效
            if (upgradeEffect != null)
                upgradeEffect.Play();

            UpgradeProcess().Forget();
        }

        private async UniTask UpgradeProcess()
        {
            var levelData = data.GetLevelData(currentLevel + 1);
            if (levelData == null)
                //终止 unitask
                return;

            float upgradeTime = levelData.upgradeTime;
            float elapsedTime = 0f;

            // 升级动画
            while (elapsedTime < upgradeTime)
            {
                elapsedTime += 0.1f;
                float progress = elapsedTime / upgradeTime;

                // 升级进度动画
                if (buildingRenderer != null)
                {
                    Color color = buildingRenderer.material.color;
                    color.a = Mathf.Lerp(1f, 0.5f, Mathf.PingPong(progress * 2, 1));
                    buildingRenderer.material.color = color;
                }

                await UniTask.WaitForSeconds(0.1f);
            }

            // 升级完成
            currentLevel++;
            MaxHitPoints = GetMaxHP();
            CurrentHitPoints = MaxHitPoints;


            currentState = UnitState.Working;
            UpdateVisuals();

            // 停止升级特效
            if (upgradeEffect != null)
                upgradeEffect.Stop();

            // OnBuildingUpgraded?.Invoke(this);
            GM.EventManager?.OnBuildingUpgraded?.Invoke(this);
        }

        #endregion

        #region 战斗相关

        public void TakeDamage(int damage)
        {
            if (currentState == UnitState.Dead )
                return;

            int actualDamage = Mathf.Max(1, damage - stats.GetStat("armor"));
            CurrentHitPoints -= actualDamage;


            // OnBuildingDamaged?.Invoke(this, actualDamage);
            GM.EventManager?.OnBuildingDamaged?.Invoke(this, actualDamage);

            if (CurrentHitPoints <= 0)
            {
                DestroyBuilding();
            }
        }

        private void DestroyBuilding()
        {
            currentState = UnitState.Dead;

            // 播放摧毁特效
            if (buildEffect != null)
                buildEffect.Play();

            // 隐藏建筑
            if (buildingRenderer != null)
                buildingRenderer.gameObject.SetActive(false);

            // OnBuildingDestroyed?.Invoke(this);
            GM.EventManager?.OnBuildingDestroyed?.Invoke(this);

            // 延迟销毁对象
            // Destroy(gameObject, 2f);
            Hide();
        }

        public void Repair(int repairAmount)
        {
            if (currentState == UnitState.Dead)
                return;

            CurrentHitPoints = Mathf.Min(CurrentHitPoints + repairAmount, MaxHitPoints);

        }

        #endregion

        #region 辅助方法

        private int GetMaxHP()
        {
            var levelData = data.GetLevelData(currentLevel);
            return levelData != null ? levelData.stats.hp : 1;
        }

        private void UpdateUnitState()
        {
            // 根据建筑类型更新状态
            switch (data.buildingType)
            {
                case BuildingType.Resource:
                    UpdateResourceBuilding();
                    break;
                case BuildingType.Defense:
                    UpdateDefenseBuilding();
                    break;
                case BuildingType.Military:
                    UpdateMilitaryBuilding();
                    break;
            }
        }

        private void UpdateResourceBuilding()
        {
            if (currentState == UnitState.Working && data.isResourceProducer)
            {
                // 资源建筑自动工作
            }
        }

        private void UpdateDefenseBuilding()
        {
            if (currentState == UnitState.Working)
            {
                // 防御建筑检查攻击范围
            }
        }

        private void UpdateMilitaryBuilding()
        {
            if (currentState == UnitState.Working)
            {
                // 军事建筑更新训练状态
            }
        }

        public bool CanUpgrade()
        {
            return currentState == UnitState.Working &&
                   currentLevel < data.maxLevel &&
                   GM.ResourceManager?.CanAfford(data.GetLevelData(currentLevel + 1)?.upgradeCosts) == true;
        }

        public ResourceCost GetUpgradeCosts() 
        {
            var levelData = data.GetLevelData(currentLevel + 1);
            return levelData?.upgradeCosts;
        }

        #endregion
    }
}