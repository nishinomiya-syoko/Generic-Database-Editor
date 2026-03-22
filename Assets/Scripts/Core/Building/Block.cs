using UnityEngine;
using System;
using DG.Tweening;

namespace Top
{
    /// <summary>
    /// 建筑和装饰物
    /// </summary>
    public class Block : MonoBehaviour
    {
        public string Id;
        public BuildingData buildingData;
        public Vector2Int gridPosition; // 网格坐标
        public Vector3 worldPosition; // 世界坐标

        private GameObject m_sizeIndicatorPrefab; // 大小指示器预制体
        private GameObject m_rangeIndicatorPrefab;
        private GameObject modelInstance; // 视觉模型实例
        public void GenerateId()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        void Start()
        {
            modelInstance = gameObject;
            m_sizeIndicatorPrefab = GlobalManager.Instance.PoolManager.Spawn("Indicator", transform.position);
            m_sizeIndicatorPrefab.transform.SetParent(transform);
            m_sizeIndicatorPrefab.SetActive(false);
        }

        public void SetMoving(bool isMoving)
        {
            if (isMoving)
            {
                ShowRangeIndicator();
                ShowSizeIndicator();
            }
            else
            {
                HideIndicator();
            }

        }
        public void ShowRangeIndicator()
        {
            if (m_rangeIndicatorPrefab == null)
                return;
            m_rangeIndicatorPrefab.transform.localScale *= .5f;
            m_rangeIndicatorPrefab.transform.DOScale(1f, 0.5f);
        }
        public void ShowSizeIndicator()
        {
            m_sizeIndicatorPrefab?.SetActive(true);
        }
        public void HideIndicator()
        {
            m_rangeIndicatorPrefab?.SetActive(false);
            m_sizeIndicatorPrefab?.SetActive(false);
            
        }
        public void PreviewMode()
        {
            // 设置半透明材质
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
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
        public void PlayBuildAnimation()
        {

        }
    }
}