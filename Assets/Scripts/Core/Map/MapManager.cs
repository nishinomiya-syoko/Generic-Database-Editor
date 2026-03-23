using UnityEngine;
using System.Collections.Generic;

namespace Top
{
    // 地图管理器
    public class MapManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridWidth = 100;
        public int gridHeight = 100;
        public float cellSize = 2f;
        public Vector3 gridOrigin = Vector3.zero;

        [Header("Scenes")]
        public SceneData[] availableScenes;
        public int currentSceneIndex = 0;

        [Header("References")]
        public CameraHandler cameraController;
        public GameObject groundPlane;

        private GridManager gridSystem;
        private GameObject currentGround;

        void Start()
        {
            InitializeGrid();
            LoadScene(currentSceneIndex);
        }

        void InitializeGrid()
        {
            // gridSystem = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin);
            // gridSystem.DebugDrawGrid();
            gridSystem = GridManager.Instance;
        }

        public void LoadScene(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= availableScenes.Length)
                return;

            currentSceneIndex = sceneIndex;
            SceneData sceneData = availableScenes[sceneIndex];

            // 更新地面材质
            if (groundPlane != null && sceneData.groundMaterial != null)
            {
                groundPlane.GetComponent<Renderer>().material = sceneData.groundMaterial;
            }

            // 更新环境光照
            RenderSettings.ambientLight = sceneData.ambientColor;

            // 播放背景音乐
            if (GlobalManager.Instance?.AudioManager != null && sceneData.backgroundMusic != null)
            {
                GlobalManager.Instance.AudioManager.PlayMusic(sceneData.backgroundMusic.name);
            }

            // 触发场景切换事件
            GlobalManager.Instance?.EventManager?.OnSceneChanged?.Invoke(sceneIndex);
        }

        public void SwitchScene(int sceneIndex)
        {
            if (sceneIndex == currentSceneIndex)
                return;

            // 检查解锁条件
            if (sceneIndex > 0 && availableScenes[sceneIndex].unlockLevel > GlobalManager.Instance.LevelManager.playerLevel)
            {
                Debug.LogWarning("Scene not unlocked yet!");
                return;
            }

            LoadScene(sceneIndex);
        }

        public bool CanPlaceBuilding( BuildingData buildingData,Vector3 position)
        {
            // return gridSystem.CanPlaceBuilding(position, buildingData.size);
            var pos = gridSystem.GetNodeFromWorldPos(position);
            return gridSystem.CheckRegionPlaceable(pos.x,pos.y, buildingData.size.x, buildingData.size.y);
        }

        public void PlaceBuilding(BuildingData building, Vector3 position)
        {
            // gridSystem.OccupyCells(position, building.data.size, building);
            var pos = gridSystem.GetNodeFromWorldPos(position);

            gridSystem.SetRegionPlaced(pos.x,pos.y, building.size.x, building.size.y, true);
            // building.transform.position = position;
        }

        public void RemoveBuilding(BuildingData building,Vector3 position)
        {
             var pos = gridSystem.GetNodeFromWorldPos(position);
            gridSystem.SetRegionPlaced(pos.x,pos.y, building.size.x, building.size.y, false);
        }

        public Vector3 GetNearestGridPosition(Vector3 worldPosition)
        {
            var node = gridSystem.GetNodeFromWorldPos(worldPosition);
            return gridSystem.GetWorldPosition(node.x, node.y);
        }

        public GridManager GetGridSystem()
        {
            return gridSystem;
        }

        public SceneData GetCurrentScene()
        {
            return availableScenes[currentSceneIndex];
        }

        public SceneData[] GetAvailableScenes()
        {
            return availableScenes;
        }
    }

    // 场景数据
    [System.Serializable]
    public class SceneData
    {
        public string sceneName;
        public Sprite backgroundImage;
        public Material groundMaterial;
        /// <summary>
        /// 环境光照
        /// </summary>
        public Color ambientColor;
        public AudioClip backgroundMusic;
        public int unlockLevel;
    }
}