using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Logic;
using DataCenter;
using System.Linq;

namespace Top
{
    // 建筑管理器
    public class BuildingManager : MonoBehaviour
    {
        [Header("建筑数据库")]
        // public BuildingDatabase buildingDatabase;
        // public BuildingDataTable buildingDatabase;

        [Header("建造设置")]
        public LayerMask groundLayer = 1;
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;
        public GameObject placementGhost;

        private List<Building> placedBuildings = new List<Building>();

        public List<Building> PlacedBuildings
        {
            get { return placedBuildings; }
        }
        private BuildingData selectedBuildingData;
        private GameObject currentGhost;
        private bool isPlacingBuilding = false;

        // 事件
        public event Action<Building> OnBuildingPlaced;
        public event Action<Building> OnBuildingUpgraded;
        public event Action<Building> OnBuildingDestroyed;

        void Start()
        {
            // GlobalManager.Instance.EventManager.AddListener<Building>(EventType.BuildingPlaced, OnBuildingPlaced);
            GlobalManager.Instance.EventManager.OnBuildingPlaced += OnBuildingPlaced;
            GlobalManager.Instance.EventManager.OnBuildingUpgraded += OnBuildingUpgraded;
            GlobalManager.Instance.EventManager.OnBuildingDestroyed += OnBuildingDestroyed;
        }
       
        void Update()
        {
            HandleBuildingPlacement();
        }

        #region 建筑放置

        public void StartBuildingPlacement(int buildingId)
        {
            var p = GlobalManager.Instance.DataTableManager.GetDataById<DataCenter.BuildingData>(buildingId);
            if (p == null)
            {
                DebugInfo.LogWarning("Invalid building id!");
                return;
            }
            BuildingData buildingData = new BuildingData()
            {
                Id = p.Id.ToString(),
                DisplayName = p.DisplayName,
                Description = p.Description,
                IconPath = p.IconPath,
                // prefabPath = p.prefabPath,
                buildingTags = p.unitTags,
                size = p.size,
            };
            List<Top.BuildingLevelData> levelTemp = new List<Top.BuildingLevelData>();
            foreach (var id in p.levelId)
            {
                var i = GlobalManager.Instance.DataTableManager.GetDataById<DataCenter.BuildingLevelData>(id);
                levelTemp.Add(new Top.BuildingLevelData()
                {
                    levelId = i.Id,
                    poolId = i.PoolId,
                    upgradeCosts = new ResourceCost(i.block, i.screw, i.crystal, i.plastic, i.gold),
                    upgradeTime = i.upgradeTime,
                    stats = new BuildingStats()
                    {
                        attackRange = i.attackRange,
                        attackSpeed = i.attackSpeed,
                    }
                }
                );
            }
            buildingData.levelData = levelTemp.ToArray();
            StartBuildingPlacement(buildingData);
        }

        public void StartBuildingPlacement(BuildingData buildingData)
        {
            var cost = buildingData.levelData?[0].upgradeCosts;
            if (!GlobalManager.Instance?.ResourceManager?.CanAfford(cost) == true)
            {
                DebugInfo.LogWarning("Not enough resources to build!");
                return;
            }

            selectedBuildingData = buildingData;
            DebugInfo.LogError("[BuildingManager] Building: " + buildingData.DisplayName);
            isPlacingBuilding = true;

            // 创建放置预览
            CreatePlacementGhost();
        }

        public void CancelBuildingPlacement()
        {
            isPlacingBuilding = false;
            selectedBuildingData = null;

            // 销毁放置预览
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                currentGhost = null;
            }
        }
        /// <summary>
        /// 预览
        /// </summary>
        private void HandleBuildingPlacement()
        {
            if (!isPlacingBuilding || selectedBuildingData == null)
                return;

            // 更新放置预览位置
            UpdatePlacementGhost();

            // 处理放置确认
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceBuilding();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBuildingPlacement();
            }
        }

        private void CreatePlacementGhost()
        {
            if (currentGhost != null)
                Destroy(currentGhost);

            if (selectedBuildingData != null)
            {
                currentGhost = PoolManager.Instance.Spawn(selectedBuildingData.levelData[0].poolId.ToString(), transform.position);

                currentGhost.name = "PlacementGhost";
            }
        }

        private void UpdatePlacementGhost()
        {
            if (currentGhost == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 placementPosition = GlobalManager.Instance?.MapManager?.GetNearestGridPosition(hit.point) ?? hit.point;
                currentGhost.transform.position = placementPosition;

                // 检查是否可以放置
                bool canPlace = GlobalManager.Instance?.MapManager?.CanPlaceBuilding(selectedBuildingData,placementPosition) == true;

                // 更新预览颜色
                Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    foreach (var material in materials)
                    {
                        Color color = material.color;
                        color = canPlace ? Color.green : Color.red;
                        color.a = 0.5f;
                        material.color = color;
                    }
                }
            }
        }

        private void TryPlaceBuilding()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 placementPosition = GlobalManager.Instance?.MapManager?.GetNearestGridPosition(hit.point) ?? hit.point;

                if (GlobalManager.Instance?.MapManager?.CanPlaceBuilding(selectedBuildingData,placementPosition) == true)
                {
                    PlaceBuilding(selectedBuildingData, placementPosition);
                }
            }
        }

        #endregion

        #region 建筑操作

        public Building PlaceBuilding(BuildingData buildingData, Vector3 position)
        {
            if (!GlobalManager.Instance?.ResourceManager?.SpendResources(buildingData.levelData?[0].upgradeCosts) == true)
            {
                Debug.LogWarning("Failed to spend resources for building!");
                return null;
            }

            // GameObject buildingObj = PoolManager.Instance.Spawn(buildingData.levelData[0].poolId.ToString(),position);
            GameObject buildingObj = GlobalManager.Instance.EntityManager.ShowEntity(buildingData.levelData[0].poolId.ToString(),position);
            Building building = buildingObj.GetComponent<Building>();

            if (building == null)
            {
                building = buildingObj.AddComponent<Building>();
            }

            // 设置建筑数据
            building.data = buildingData;
            building.currentLevel = 1;
            building.currentState = UnitState.Building;

            // 注册到地图系统
            GlobalManager.Instance?.MapManager?.PlaceBuilding(buildingData, position);

            // 添加到建筑列表
            placedBuildings.Add(building);

            // 开始建造
            building.StartBuilding();
            GlobalManager.Instance?.EventManager?.OnBuildingPlaced?.Invoke(building);

            // 注册事件
            // building.OnBuildingPlaced += OnBuildingPlacedHandler;
            // building.OnBuildingUpgraded += OnBuildingUpgradedHandler;
            // building.OnBuildingDestroyed += OnBuildingDestroyedHandler;

            // 取消放置模式
            CancelBuildingPlacement();

            return building;
        }

        public void UpgradeBuilding(Building building)
        {
            if (!building.CanUpgrade())
                return;

            var upgradeCosts = building.GetUpgradeCosts();
            if (GlobalManager.Instance?.ResourceManager?.SpendResources(upgradeCosts) == true)
            {
                building.StartUpgrade();
            }
        }

        public void RemoveBuilding(Building building)
        {
            if (building == null)
                return;

            // 从地图系统中移除
            GlobalManager.Instance?.MapManager?.RemoveBuilding(building.data,building.transform.position);

            // 从建筑列表中移除
            placedBuildings.Remove(building);

            // 销毁建筑对象
            // Destroy(building.gameObject);
            PoolManager.Instance?.Despawn(building.data.Id,building.gameObject);
        }

        public List<Building> GetBuildingsOfType(UnitType type)
        {
            List<Building> buildingsOfType = new List<Building>();
            foreach (var building in placedBuildings)
            {
                if (building.data.buildingTags.Contains(type))
                {
                    buildingsOfType.Add(building);
                }
            }
            return buildingsOfType;
        }

        #endregion

        #region 事件处理

        // private void OnBuildingPlacedHandler(Building building)
        // {
        //     OnBuildingPlaced?.Invoke(building);
        // }

        // private void OnBuildingUpgradedHandler(Building building)
        // {
        //     OnBuildingUpgraded?.Invoke(building);
        // }

        // private void OnBuildingDestroyedHandler(Building building)
        // {
        //     OnBuildingDestroyed?.Invoke(building);
        //     placedBuildings.Remove(building);
        // }

        #endregion

        #region 辅助方法

        public Building GetBuildingAt(Vector3 position)
        {
            foreach (var building in placedBuildings)
            {
                if (Vector3.Distance(building.transform.position, position) < 1f)
                {
                    return building;
                }
            }
            return null;
        }

        public bool HasBuildingType(UnitType type)
        {
            foreach (var building in placedBuildings)
            {
                // if (building.data.buildingType == type)
                if (building.data.buildingTags.Contains(type))
                {
                    return true;
                }
            }
            return false;
        }

        public int GetBuildingCount(UnitType type)
        {
            int count = 0;
            foreach (var building in placedBuildings)
            {
                // if (building.data.buildingType == type)
                if (building.data.buildingTags.Contains(type))
                {
                    count++;
                }
            }
            return count;
        }

        #endregion
    }
}