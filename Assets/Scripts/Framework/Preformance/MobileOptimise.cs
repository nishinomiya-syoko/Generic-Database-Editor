using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace EmpireClash
{
    // 性能管理器
    public class PerformanceManager : MonoBehaviour
    {
        [Header("性能设置")]
        public PerformanceLevel currentLevel = PerformanceLevel.Medium;
        public bool autoDetectPerformance = true;
        public float targetFrameRate = 30f;

        [Header("质量设置")]
        public int[] textureQualityLevels = { 0, 1, 2 }; // 低、中、高质量
        public int[] shadowQualityLevels = { 0, 1, 2 };
        public int[] antiAliasingLevels = { 0, 2, 4 };

        private float frameTime;
        private int frameCount;
        private float fpsCheckInterval = 1f;
        private float lastFpsCheckTime;
        private List<float> frameTimes = new List<float>();

        public enum PerformanceLevel
        {
            Low,      // 低端设备
            Medium,   // 中端设备
            High      // 高端设备
        }

        void Start()
        {
            if (autoDetectPerformance)
            {
                DetectDevicePerformance();
            }
            
            ApplyPerformanceSettings();
            StartCoroutine(FPSMonitor());
        }

        #region 性能检测

        private void DetectDevicePerformance()
        {
            // 根据设备规格检测性能等级
            int processorCount = SystemInfo.processorCount;
            int systemMemorySize = SystemInfo.systemMemorySize;
            string graphicsDeviceName = SystemInfo.graphicsDeviceName.ToLower();

            // 基础检测逻辑
            if (systemMemorySize < 2048 || processorCount < 4)
            {
                currentLevel = PerformanceLevel.Low;
            }
            else if (systemMemorySize < 4096 || processorCount < 6)
            {
                currentLevel = PerformanceLevel.Medium;
            }
            else
            {
                currentLevel = PerformanceLevel.High;
            }

            // 特殊设备检测
            if (graphicsDeviceName.Contains("mali-t") || graphicsDeviceName.Contains("adreno 3"))
            {
                currentLevel = PerformanceLevel.Low;
            }
            else if (graphicsDeviceName.Contains("adreno 4") || graphicsDeviceName.Contains("mali-g"))
            {
                currentLevel = PerformanceLevel.Medium;
            }
            else if (graphicsDeviceName.Contains("adreno 5") || graphicsDeviceName.Contains("adreno 6"))
            {
                currentLevel = PerformanceLevel.High;
            }

            Debug.Log($"Detected performance level: {currentLevel}");
        }

        private IEnumerator FPSMonitor()
        {
            while (true)
            {
                yield return new WaitForSeconds(fpsCheckInterval);
                
                float averageFPS = frameCount / fpsCheckInterval;
                frameCount = 0;
                
                // 动态调整性能设置
                if (autoDetectPerformance && averageFPS < targetFrameRate * 0.8f)
                {
                    LowerQuality();
                }
                else if (averageFPS > targetFrameRate * 1.2f && currentLevel != PerformanceLevel.High)
                {
                    // 可以考虑提升质量
                }
            }
        }

        void Update()
        {
            frameCount++;
            frameTime += Time.unscaledDeltaTime;
            
            // 记录帧时间用于分析
            frameTimes.Add(Time.unscaledDeltaTime);
            if (frameTimes.Count > 60)
            {
                frameTimes.RemoveAt(0);
            }
        }

        #endregion

        #region 性能设置应用

        public void ApplyPerformanceSettings()
        {
            int qualityLevel = (int)currentLevel;

            // 设置质量等级
            QualitySettings.SetQualityLevel(qualityLevel, true);

            // 应用特定设置
            switch (currentLevel)
            {
                case PerformanceLevel.Low:
                    ApplyLowQualitySettings();
                    break;
                case PerformanceLevel.Medium:
                    ApplyMediumQualitySettings();
                    break;
                case PerformanceLevel.High:
                    ApplyHighQualitySettings();
                    break;
            }

            // 设置目标帧率
            Application.targetFrameRate = (int)targetFrameRate;
        }

        private void ApplyLowQualitySettings()
        {
            // 纹理质量
            QualitySettings.globalTextureMipmapLimit = 2; // 降低纹理分辨率
            
            // 阴影设置
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            
            // 抗锯齿
            QualitySettings.antiAliasing = 0;
            
            // 其他优化
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = false;
            
            // 减少渲染距离
            if (Camera.main != null)
            {
                Camera.main.farClipPlane = 100f;
            }
        }

        private void ApplyMediumQualitySettings()
        {
            QualitySettings.globalTextureMipmapLimit = 1;
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.antiAliasing = 2;
            QualitySettings.softParticles = false;
        }

        private void ApplyHighQualitySettings()
        {
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.antiAliasing = 4;
            QualitySettings.softParticles = true;
        }

        public void LowerQuality()
        {
            if (currentLevel > PerformanceLevel.Low)
            {
                currentLevel--;
                ApplyPerformanceSettings();
                Debug.Log($"Lowered quality to: {currentLevel}");
            }
        }

        public void HigherQuality()
        {
            if (currentLevel < PerformanceLevel.High)
            {
                currentLevel++;
                ApplyPerformanceSettings();
                Debug.Log($"Highered quality to: {currentLevel}");
            }
        }

        #endregion

        #region 内存管理

        public void TriggerGarbageCollection()
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }

        #endregion
    }

    // 对象池管理器
    public class ObjectPoolManager : MonoBehaviour
    {
        private static ObjectPoolManager instance;
        public static ObjectPoolManager Instance => instance;

        private Dictionary<string, Queue<GameObject>> objectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void CreatePool(string poolKey, GameObject prefab, int initialSize = 10)
        {
            if (!objectPools.ContainsKey(poolKey))
            {
                objectPools[poolKey] = new Queue<GameObject>();
                prefabMap[poolKey] = prefab;

                // 预创建对象
                for (int i = 0; i < initialSize; i++)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    objectPools[poolKey].Enqueue(obj);
                }
            }
        }

        public GameObject GetObject(string poolKey)
        {
            if (objectPools.ContainsKey(poolKey))
            {
                if (objectPools[poolKey].Count > 0)
                {
                    GameObject obj = objectPools[poolKey].Dequeue();
                    obj.SetActive(true);
                    return obj;
                }
                else
                {
                    // 创建新对象
                    GameObject newObj = Instantiate(prefabMap[poolKey]);
                    return newObj;
                }
            }
            return null;
        }

        public void ReturnObject(string poolKey, GameObject obj)
        {
            if (objectPools.ContainsKey(poolKey))
            {
                obj.SetActive(false);
                objectPools[poolKey].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        public void ClearPool(string poolKey)
        {
            if (objectPools.ContainsKey(poolKey))
            {
                while (objectPools[poolKey].Count > 0)
                {
                    GameObject obj = objectPools[poolKey].Dequeue();
                    Destroy(obj);
                }
                objectPools.Remove(poolKey);
                prefabMap.Remove(poolKey);
            }
        }
    }

    // LOD系统
    public class LODSystem : MonoBehaviour
    {
        [Header("LOD设置")]
        public LODGroup lodGroup;
        public LOD[] lods;

        void Start()
        {
            if (lodGroup == null)
                lodGroup = GetComponent<LODGroup>();

            if (lodGroup != null && lods.Length > 0)
            {
                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();
            }
        }
    }

    // 动态批处理
    public class DynamicBatching : MonoBehaviour
    {
        [Header("批处理设置")]
        public bool enableDynamicBatching = true;
        public Material sharedMaterial;

        private List<Renderer> batchedRenderers = new List<Renderer>();

        void Start()
        {
            if (enableDynamicBatching && sharedMaterial != null)
            {
                SetupDynamicBatching();
            }
        }

        private void SetupDynamicBatching()
        {
            // 获取所有子渲染器
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer is MeshRenderer)
                {
                    renderer.material = sharedMaterial;
                    batchedRenderers.Add(renderer);
                }
            }
        }

        void OnDestroy()
        {
            // 清理材质引用
            foreach (var renderer in batchedRenderers)
            {
                if (renderer != null)
                {
                    renderer.material = null;
                }
            }
        }
    }

    // 纹理压缩管理器
    public class TextureCompressionManager : MonoBehaviour
    {
        [Header("压缩设置")]
        public bool autoCompressTextures = true;
        public int maxTextureSize = 512;

        void Start()
        {
            if (autoCompressTextures)
            {
                CompressAllTextures();
            }
        }

        private void CompressAllTextures()
        {
            // 获取所有纹理
            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            
            foreach (var texture in textures)
            {
                if (texture.width > maxTextureSize || texture.height > maxTextureSize)
                {
                    // 压缩纹理
                    CompressTexture(texture);
                }
            }
        }

        private void CompressTexture(Texture2D texture)
        {
            // 根据平台设置压缩格式
            string platform = Application.platform.ToString();
            
            if (platform.Contains("Android"))
            {
                texture.Compress(true); // ETC2 compression
            }
            else if (platform.Contains("iOS"))
            {
                texture.Compress(true); // ASTC compression
            }
        }
    }

    // 渲染优化管理器
    public class RenderingOptimizer : MonoBehaviour
    {
        [Header("渲染优化")]
        public bool optimizeShadows = true;
        public bool optimizeLighting = true;
        public float shadowDistance = 50f;
        public int pixelLightCount = 1;

        void Start()
        {
            ApplyRenderingOptimizations();
        }

        private void ApplyRenderingOptimizations()
        {
            // 阴影优化
            if (optimizeShadows)
            {
                QualitySettings.shadowDistance = shadowDistance;
                QualitySettings.shadowCascades = 2;
            }

            // 光照优化
            if (optimizeLighting)
            {
                QualitySettings.pixelLightCount = pixelLightCount;
                QualitySettings.maxQueuedFrames = 2;
            }

            // 其他渲染优化
            QualitySettings.vSyncCount = 0; // 禁用VSync以提高性能
            Application.targetFrameRate = 30;
        }
    }

    // 内存监控器
    public class MemoryMonitor : MonoBehaviour
    {
        [Header("监控设置")]
        public bool enableMemoryTracking = true;
        public float memoryCheckInterval = 5f;
        public long memoryWarningThreshold = 512 * 1024 * 1024; // 512MB

        private long lastMemoryUsage;

        void Start()
        {
            if (enableMemoryTracking)
            {
                InvokeRepeating(nameof(CheckMemory), memoryCheckInterval, memoryCheckInterval);
            }
        }

        private void CheckMemory()
        {
            long totalMemory = System.GC.GetTotalMemory(false);
            
            if (totalMemory > memoryWarningThreshold)
            {
                Debug.LogWarning($"High memory usage detected: {totalMemory / (1024 * 1024)}MB");
                
                // 触发内存清理
                TriggerMemoryCleanup();
            }

            lastMemoryUsage = totalMemory;
        }

        private void TriggerMemoryCleanup()
        {
            // 清理未使用资源
            Resources.UnloadUnusedAssets();
            
            // 垃圾回收
            System.GC.Collect();
            
            Debug.Log("Memory cleanup triggered");
        }

        void OnGUI()
        {
            if (enableMemoryTracking && Debug.isDebugBuild)
            {
                long totalMemory = System.GC.GetTotalMemory(false);
                GUI.Label(new Rect(10, 10, 200, 20), $"Memory: {totalMemory / (1024 * 1024)}MB");
            }
        }
    }

    // 移动端输入优化
    public class MobileInputOptimizer : MonoBehaviour
    {
        [Header("输入优化")]
        public bool optimizeTouchInput = true;
        public float touchDeadzone = 5f;

        private Vector2 lastTouchPosition;
        private bool isDragging = false;

        void Update()
        {
            if (optimizeTouchInput)
            {
                OptimizeTouchInput();
            }
        }

        private void OptimizeTouchInput()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        lastTouchPosition = touch.position;
                        isDragging = false;
                        break;
                        
                    case TouchPhase.Moved:
                        float distance = Vector2.Distance(touch.position, lastTouchPosition);
                        if (distance > touchDeadzone)
                        {
                            isDragging = true;
                            lastTouchPosition = touch.position;
                        }
                        break;
                        
                    case TouchPhase.Ended:
                        isDragging = false;
                        break;
                }
            }
        }

        public bool IsDragging()
        {
            return isDragging;
        }
    }
}