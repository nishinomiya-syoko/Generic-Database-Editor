using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Logic;

namespace Top
{
    // 建筑管理器
    public class BuildingManager : MonoBehaviour
    {
        [Header("建筑数据库")]
        public BuildingDatabase buildingDatabase;

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
            if (buildingDatabase == null)
            {
                buildingDatabase = Resources.Load<BuildingDatabase>("BuildingDatabase");
            }
        }

        void Update()
        {
            HandleBuildingPlacement();
        }

        #region 建筑放置

        public void StartBuildingPlacement(string buildingId)
        {
            BuildingData buildingData = buildingDatabase.GetBuildingData(buildingId);
            if (buildingData != null)
            {
                StartBuildingPlacement(buildingData);
            }
        }

        public void StartBuildingPlacement(BuildingData buildingData)
        {
            if (!GlobalManager.Instance?.ResourceManager?.CanAfford(buildingData.buildCosts) == true)
            {
                Debug.LogWarning("Not enough resources to build!");
                return;
            }

            selectedBuildingData = buildingData;
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

            if (selectedBuildingData.prefab != null)
            {
                currentGhost = Instantiate(selectedBuildingData.prefab);
                currentGhost.name = "PlacementGhost";

                // 设置半透明材质
                Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Color color = materials[i].color;
                        color.a = 0.5f;
                        materials[i].color = color;
                        materials[i].SetFloat("_Mode", 2);
                        materials[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        materials[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        materials[i].SetInt("_ZWrite", 0);
                        materials[i].DisableKeyword("_ALPHATEST_ON");
                        materials[i].EnableKeyword("_ALPHABLEND_ON");
                        materials[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        materials[i].renderQueue = 3000;
                    }
                    renderer.materials = materials;
                }
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
            if (!GlobalManager.Instance?.ResourceManager?.SpendResources(buildingData.buildCosts) == true)
            {
                Debug.LogWarning("Failed to spend resources for building!");
                return null;
            }

            // 实例化建筑
            // GameObject buildingObj = Instantiate(buildingData.prefab, position, Quaternion.identity);
            GameObject buildingObj = PoolManager.Instance.Spawn(buildingData.id,position);
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

            // 注册事件
            building.OnBuildingPlaced += OnBuildingPlacedHandler;
            building.OnBuildingUpgraded += OnBuildingUpgradedHandler;
            building.OnBuildingDestroyed += OnBuildingDestroyedHandler;

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

        // public void DestroyBuilding(Building building)
        // {
        //     if (building == null || building.IsDestroyed)
        //         return;

        //     // 从地图系统中移除
        //     GlobalManager.Instance?.MapManager?.RemoveBuilding(building.data,building);

        //     // 从建筑列表中移除
        //     placedBuildings.Remove(building);

        //     // 销毁建筑对象
        //     Destroy(building.gameObject);
        // }

        public List<Building> GetBuildingsOfType(BuildingType type)
        {
            List<Building> buildingsOfType = new List<Building>();
            foreach (var building in placedBuildings)
            {
                if (building.data.buildingType == type)
                {
                    buildingsOfType.Add(building);
                }
            }
            return buildingsOfType;
        }

        #endregion

        #region 事件处理

        private void OnBuildingPlacedHandler(Building building)
        {
            OnBuildingPlaced?.Invoke(building);
        }

        private void OnBuildingUpgradedHandler(Building building)
        {
            OnBuildingUpgraded?.Invoke(building);
        }

        private void OnBuildingDestroyedHandler(Building building)
        {
            OnBuildingDestroyed?.Invoke(building);
            placedBuildings.Remove(building);
        }

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

        public bool HasBuildingType(BuildingType type)
        {
            foreach (var building in placedBuildings)
            {
                if (building.data.buildingType == type)
                {
                    return true;
                }
            }
            return false;
        }

        public int GetBuildingCount(BuildingType type)
        {
            int count = 0;
            foreach (var building in placedBuildings)
            {
                if (building.data.buildingType == type)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion
    }
}