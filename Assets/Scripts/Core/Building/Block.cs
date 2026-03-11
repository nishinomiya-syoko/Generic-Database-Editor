using UnityEngine;
using System;

namespace Top
{
    /// <summary>
    /// 建筑和装饰物
    /// </summary>
    public class Block : MonoBehaviour
    {
        public BuildingData buildingData;
        public Vector2Int gridPosition; // 网格坐标
        public Vector3 worldPosition; // 世界坐标

        private GameObject SizeIndicatorPrefab; // 大小指示器预制体

        void Start()
        {
            SizeIndicatorPrefab = GlobalManager.Instance.poolManager.Spawn("Indicator", transform.position);
            SizeIndicatorPrefab.transform.SetParent(transform);
            SizeIndicatorPrefab.SetActive(false);
        }

        public void SetMoving(bool isMoving)
        {
            if (SizeIndicatorPrefab != null)
            {
                SizeIndicatorPrefab.SetActive(isMoving);
            }
        }
    }
}