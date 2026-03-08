using UnityEngine;

/// <summary>
/// 负责在格子上放置占格建筑（可在编辑器或运行时调用）
/// 演示如何用 GridManager.SetRegionBlocked 标记
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
    public GameObject buildingPrefab; // 视觉预制体
    public Vector2Int size = new Vector2Int(2, 2); // 占格大小
    public int walkableWidth = 1;
    public bool allowOverlapPartial = false;
    public bool buildingMode = false;
    public KeyCode keyCode;

    /// <summary>
    /// 在网格坐标放置建筑 (x,y 为左下角格子)
    /// 返回实例（null 表示放置失败）
    /// </summary>
    public GameObject PlaceBuilding(int gridX, int gridY)
    {
        if (!GridManager.Instance) return null;
       
        if(!GridManager.Instance.CheckRegionPlaceable(gridX, gridY, size.x, size.y))
        return null;
        // 标记格子为阻挡
        GridManager.Instance.SetRegionPlaced(gridX, gridY, size.x, size.y, true, allowOverlapPartial);
        GridManager.Instance.SetRegionBlocked(gridX, gridY, size.x, size.y,true, walkableWidth);

        // 实例化视觉对象（中心对齐）
        Vector3 worldPos = GridManager.Instance.GetWorldPosition(gridX + size.x / 2, gridY + size.y / 2);
        var go = Instantiate(buildingPrefab, worldPos, Quaternion.identity);
        var b = go.AddComponent<Tower>(); // 添加辅助组件用于后续移除
        b.gridX = gridX; b.gridY = gridY; b.size = size;
        return go;
    }

    public void RemoveBuilding(Tower b)
    {
        if (b == null) return;
        GridManager.Instance.SetRegionPlaced(b.gridX, b.gridY, b.size.x, b.size.y, false);
        GridManager.Instance.SetRegionBlocked(b.gridX, b.gridY, b.size.x, b.size.y, false);
        Destroy(b.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(keyCode))
        {
            buildingMode = !buildingMode;
        }
        if (!buildingMode)
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            LayerMask layerMask = 1 << 6;
            if (Physics.Raycast(ray,out hit,int.MaxValue,layerMask))
            {
                var p = GridManager.Instance.GetNodeFromWorldPos(hit.point);
                PlaceBuilding(p.x, p.y);
            }
        }
    }
}
