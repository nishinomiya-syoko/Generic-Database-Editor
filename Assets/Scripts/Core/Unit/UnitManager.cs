using UnityEngine;
using System;
using System.Collections.Generic;
using Logic;
using Cysharp.Threading.Tasks;
using RTS.TargetSearch;

namespace Top
{
    public class BattleIndicator
    {
        public Transform m_Transform;
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;
        public enum IndicatorType
        {
            //矩形
            Rectangle,

            //圆形  
            Circle,
        }
        private IndicatorType m_IndicatorType;
        public BattleIndicator(Transform transform,IndicatorType indicatorType,float range,Vector3 posiiton)
        {
            m_Transform = transform;
            m_IndicatorType = indicatorType;
            SetRange(range);
            SetPosition(posiiton);
        }
        public void SetIndicator(IndicatorType type)
        {
            m_IndicatorType = type;
        }
        public void SetSize(Vector2 size)
        {
            if (m_IndicatorType == IndicatorType.Rectangle)
            {
                m_Transform.localScale = size;
            }
        }
        public void SetRange(float range)
        {
            if (m_IndicatorType == IndicatorType.Circle)
            {
                m_Transform.localScale = new Vector3(range, range, 1);
            }
        }
        public void SetPosition(Vector3 position)
        {
            m_Transform.position = position;
        }
        public void SetMaterial(bool isValid)
        {
            if (isValid)
            {
                m_Transform.GetComponent<Renderer>().material = validPlacementMaterial;
            }
            else
            {
                m_Transform.GetComponent<Renderer>().material = invalidPlacementMaterial;
            }
        }
        public void Show()
        {
            m_Transform.gameObject.SetActive(true);
        }
        public void Hide()
        {
            m_Transform.gameObject.SetActive(false);

        }
    }
    public class UnitManager : MonoBehaviour
    {
        public LayerMask groundLayer = 1;
        public BattleIndicator indicatorPrefab;
        public Transform indicatorPrefabInstance;
        private List<Unit> placedUnits = new List<Unit>();
        public List<Unit> PlacedUnits
        {
            get { return placedUnits; }
        }
        // private List<UnitData> preparedUnits = new List<UnitData>();
        private int[] preparedUnitsId = new int[0];
        private bool isPlacing = false;
        Vector3 m_mousePosition;


        void Start()
        {

        }
        void Update()
        {
            HandlePlacement();
        }
        public void PrepareUnit(int[] unitIds)
        {
            preparedUnitsId = unitIds;
            foreach (var unitId in unitIds)
            {
                var unitData = GlobalManager.Instance.DataTableManager.GetDataById<DataCenter.UnitData>(unitId);
                // preparedUnits.Add(unitData);
            }
            StartUnitPlacement();
        }
        private void StartUnitPlacement()
        {
            isPlacing = true;
            indicatorPrefab = new BattleIndicator(indicatorPrefabInstance.transform, BattleIndicator.IndicatorType.Rectangle, 1, Vector3.zero);
            indicatorPrefab.Show();
            // TryPlaceUnit();
        }
        private void CancelBuildingPlacement()
        {
            isPlacing = false;
            indicatorPrefab.Hide();
        }
        private bool TryPlaceUnit()
        {
            var mousePosition = GetMousePosition();
            if (!GlobalManager.Instance.MapManager.CanPlaceUnit(mousePosition, 1))
                return false;
            // foreach (var id in preparedUnitsId)
            // {
            //     Vector2 randomPosInRange = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
            //     var p = GM.EntityManager.ShowEntity(id.ToString(), randomPosInRange);
            //     placedUnits.Add(p.GetOrAddComponent<Unit>());
            // }
            AsyncPlaceUnit().Forget();
            return true;
        }
        private async UniTask AsyncPlaceUnit()
        {
            foreach (var id in preparedUnitsId)
            {
                Vector2 randomPosInRange = new Vector2(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f));
                var p = GM.EntityManager.ShowEntity(id.ToString(), randomPosInRange);
                placedUnits.Add(p.GetOrAddComponent<Unit>());
                p.GetOrAddComponent<TargetEntity>();
                p.GetOrAddComponent<AsyncTargetTracker>();
            }
        }
        private void HandlePlacement()
        {
            if (!isPlacing) return;

            UpdateIndicator();

            if (Input.GetMouseButtonDown(0))
            {
                bool valid = TryPlaceUnit();
                SetIndicatorValid(valid);
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBuildingPlacement();
            }
        }
        private void UpdateIndicator()
        {
            // indicatorPrefab.SetSize(new Vector2Int(1, 1));
            indicatorPrefab.SetPosition(GetMousePosition());
        }
        private void SetIndicatorValid(bool isValid)
        {
            indicatorPrefab.SetMaterial(isValid);
        }

        // Vector3 m_mousePosition;
        private Vector3 GetMousePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                // bool canPlace = GlobalManager.Instance.MapManager.CanPlaceUnit(hit.point, 1);
                m_mousePosition = hit.point;
            }
            return m_mousePosition;
        }
    }
}
