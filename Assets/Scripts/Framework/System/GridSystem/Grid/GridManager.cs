using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理网格节点（整数格），支持标记阻挡 / 成本，提供坐标转换
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 64;
    public int height = 64;
    public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;
    [Header("Show Grid")]
    public bool showGrid = false;
    public bool showHeatmap = false;

    // 内部数据：一维数组存储以减少多维索引开销
    private GridNode[] nodes;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;
        InitGrid();
    }

    public void InitGrid()
    {
        nodes = new GridNode[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                nodes[y * width + x] = new GridNode(x, y);
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    public GridNode GetNode(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return nodes[y * width + x];
    }

    public GridNode GetNodeFromWorldPos(Vector3 worldPos)
    {
        Vector3 local = worldPos - origin;
        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.z / cellSize);
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);
        return GetNode(x, y);
    }

    public Vector3 GetWorldPosition(int x, int y)
    {
        return origin + new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);
    }

    public bool CheckRegionPlaceable(int startX, int startY, int w, int h)
    {
        // 计算区域右上角坐标
        int endX = startX + w - 1;
        int endY = startY + h - 1;

        // 检查区域是否完全在网格范围内
        if (!InBounds(startX, startY) || !InBounds(endX, endY))
            return false;

        // 检查区域内所有节点是否未被阻挡
        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                GridNode node = GetNode(x, y);
                if (node == null || node.blocked || node.placed)
                    return false; // 存在阻挡节点，不可放置
            }
        }

        return true; // 区域可放置
    }
    /// <summary>
    /// 将一块矩形区域标记为被建筑占用或释放
    /// </summary>
    public bool SetRegionPlaced(int startX, int startY, int w, int h, bool placed, bool allowPartial = false)
    {
        // 检查边界，如果不允许部分放置则全部必须在范围内
        if (!allowPartial)
        {
            if (!InBounds(startX, startY) || !InBounds(startX + w - 1, startY + h - 1))
                return false;
        }

        int minX = Mathf.Clamp(startX, 0, width - 1);
        int minY = Mathf.Clamp(startY, 0, height - 1);
        int maxX = Mathf.Clamp(startX + w - 1, 0, width - 1);
        int maxY = Mathf.Clamp(startY + h - 1, 0, height - 1);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                GetNode(x, y).placed = placed;

        return true;
    }
    /// <summary>
    /// inner grid
    /// </summary>
    /// <param name="startX"></param>
    /// <param name="startY"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="placed"></param>
    /// <returns></returns>
    public bool SetRegionBlocked(int startX, int startY, int w, int h,bool blocked,int walkableWidth = 0)
    {
        if (!InBounds(startX, startY) || !InBounds(startX + w - 1, startY + h - 1))
            return false;
                
        int minX = Mathf.Clamp(startX + walkableWidth, 0, width - 1);
        int minY = Mathf.Clamp(startY + walkableWidth, 0, height - 1);
        int maxX = Mathf.Clamp(startX + w - 1 - walkableWidth, 0, width - 1);
        int maxY = Mathf.Clamp(startY + h - 1 - walkableWidth, 0, height - 1);

        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                {GetNode(x, y).blocked = true;GetNode(x, y).cost = blocked? Constant.PLACED_PATH_COST:0;}

        return true;
    }
    

    /// <summary>
    /// 设置单格的通行成本（1 = default）
    /// </summary>
    public void SetNodeCost(int x, int y, int cost)
    {
        var n = GetNode(x, y);
        if (n != null) n.cost = Mathf.Max(1, cost);
    }

    /// <summary>
    /// 获取节点周围的四连通或八连通邻居
    /// </summary>
    public IEnumerable<GridNode> GetNeighbors(GridNode node, bool allowDiagonal = false)
    {
        int x = node.x, y = node.y;
        // 四方向
        if (InBounds(x, y + 1)) yield return GetNode(x, y + 1);
        if (InBounds(x + 1, y)) yield return GetNode(x + 1, y);
        if (InBounds(x, y - 1)) yield return GetNode(x, y - 1);
        if (InBounds(x - 1, y)) yield return GetNode(x - 1, y);

        if (allowDiagonal)
        {
            if (InBounds(x + 1, y + 1)) yield return GetNode(x + 1, y + 1);
            if (InBounds(x + 1, y - 1)) yield return GetNode(x + 1, y - 1);
            if (InBounds(x - 1, y + 1)) yield return GetNode(x - 1, y + 1);
            if (InBounds(x - 1, y - 1)) yield return GetNode(x - 1, y - 1);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGrid)
            return;
        float halfSize = cellSize * 0.5f;
        Vector3 offset = new Vector3(halfSize, 0, halfSize);
         // 绘制水平线（X轴方向）
        for (int z = 0; z <= height; z++)
        {
            Gizmos.DrawLine(GetWorldPosition(0, z) - offset, GetWorldPosition(height, z)- offset);
        }

        // 绘制垂直线（Z轴方向）
        for (int x = 0; x <= width; x++)
        {
            Gizmos.DrawLine(GetWorldPosition(x, 0)- offset, GetWorldPosition(x, height)- offset);
        }
        if (nodes == null)
        {
            InitGrid();
        }
        Gizmos.matrix = Matrix4x4.identity;
        if(showHeatmap)
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var n = GetNode(x, y);
                    Vector3 pos = GetWorldPosition(x, y);
                    // Gizmos.color = n.blocked ? Color.green : Color.red;
                    
                        if (n.blocked)
                           Gizmos.color =  Color.red;
                        else if (n.placed)
                            Gizmos.color = Color.yellow;
                        else  Gizmos.color =  Color.green;
                    
                
                Gizmos.DrawCube(pos, new Vector3(cellSize * 0.95f, 0.01f, cellSize * 0.95f));
            }
    }
#endif
}

/// <summary>
/// 网格节点
/// </summary>
public class GridNode
{
    public int x, y;
    public bool placed = false;
    public bool blocked = false; // 是否完全阻挡
    public int cost = 1; // 行走成本，>1 意味着优先绕行
    // 为寻路算法存临时变量（A* 或 Flow Field）
    public int g; // A* g 距离起点的代价
    public int f; // A* f 综合代价
    public GridNode parent;
    public int integrationCost; // FlowField：到目标的累计代价
    public Vector2 bestDirection; // FlowField 用: 从此格到邻接最优格的方向（世界/格子坐标）
    public GridNode(int x, int y) { this.x = x; this.y = y; }
}
