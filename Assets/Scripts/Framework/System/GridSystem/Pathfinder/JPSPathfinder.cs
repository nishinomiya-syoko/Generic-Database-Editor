using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// JPS（Jump Point Search）寻路算法实现，基于GridManager网格系统
/// 优化点：通过跳跃点剪枝，减少搜索节点数量，效率远高于BFS
/// </summary>
public static class JPSPathfinder
{
    /// <summary>
    /// JPS寻路主方法
    /// </summary>
    /// <param name="startWorldPos">起点世界坐标</param>
    /// <param name="endWorldPos">终点世界坐标</param>
    /// <param name="allowDiagonal">是否允许对角线移动（八连通）</param>
    /// <returns>路径的世界坐标列表，找不到路径返回null</returns>
    public static List<Vector3> FindPath(Vector3 startWorldPos, Vector3 endWorldPos, bool allowDiagonal = false)
    {
        // 1. 转换坐标并验证起点终点有效性
        GridNode startNode = GridManager.Instance.GetNodeFromWorldPos(startWorldPos);
        GridNode endNode = GridManager.Instance.GetNodeFromWorldPos(endWorldPos);

        if (!ValidateNodes(startNode, endNode))
            return null;

        // 起点即终点
        if (startNode == endNode)
            return new List<Vector3> { GridManager.Instance.GetWorldPosition(startNode.x, startNode.y) };

        // 2. 初始化JPS所需数据结构（优先队列按距离排序，确保最短路径）
        // PriorityQueue<GridNode, int> openSet = new PriorityQueue<GridNode, int>();
        BinaryHeapPriorityQueue<GridNode> openSet = new BinaryHeapPriorityQueue<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        Dictionary<GridNode, GridNode> parentMap = new Dictionary<GridNode, GridNode>();
        Dictionary<GridNode, int> gCostMap = new Dictionary<GridNode, int>(); // 起点到当前节点的实际代价

        // 3. 初始化起点
        openSet.Enqueue(startNode, 0);
        gCostMap[startNode] = 0;
        parentMap[startNode] = null;

        // 4. 方向数组（四连通/八连通）
        Vector2Int[] directions = allowDiagonal ? 
            new[] { new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(-1,0),
                    new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1) } :
            new[] { new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(-1,0) };

        // 5. JPS主循环
        while (openSet.Count > 0)
        {
            // 取出当前代价最小的节点
            GridNode current = openSet.Dequeue();

            // 已找到终点，重建路径
            if (current == endNode)
                return ReconstructPath(parentMap, startNode, endNode);

            // 跳过已处理的节点
            if (closedSet.Contains(current))
                continue;
            closedSet.Add(current);

            // 探索所有方向的跳跃点
            foreach (Vector2Int dir in directions)
            {
                // 沿当前方向搜索跳跃点
                GridNode jumpPoint = FindJumpPoint(current, dir, endNode, allowDiagonal);
                if (jumpPoint != null && !closedSet.Contains(jumpPoint))
                {
                    // 计算起点到跳跃点的代价（累计移动成本）
                    int newGCost = gCostMap[current] + CalculateCost(current, jumpPoint, allowDiagonal);

                    // 如果跳跃点未被访问，或找到更优路径
                    if (!gCostMap.ContainsKey(jumpPoint) || newGCost < gCostMap[jumpPoint])
                    {
                        gCostMap[jumpPoint] = newGCost;
                        // 优先级 = 实际代价 + 启发式代价（曼哈顿/切比雪夫距离）
                        int priority = newGCost + Heuristic(jumpPoint, endNode, allowDiagonal);
                        openSet.Enqueue(jumpPoint, priority);
                        parentMap[jumpPoint] = current;
                    }
                }
            }
        }

        // 没有找到路径
        Debug.LogWarning("JPS：未找到从起点到终点的路径");
        return null;
    }

    /// <summary>
    /// 沿指定方向搜索跳跃点
    /// </summary>
    /// <param name="current">当前起始节点</param>
    /// <param name="dir">搜索方向</param>
    /// <param name="endNode">终点节点</param>
    /// <param name="allowDiagonal">是否允许对角线</param>
    /// <returns>找到的跳跃点，无则返回null</returns>
    private static GridNode FindJumpPoint(GridNode current, Vector2Int dir, GridNode endNode, bool allowDiagonal)
    {
        int x = current.x + dir.x;
        int y = current.y + dir.y;
        GridNode nextNode = GridManager.Instance.GetNode(x, y);

        // 1. 边界检查或障碍物，直接返回null
        if (nextNode == null || nextNode.blocked)
            return null;

        // 2. 到达终点，终点是跳跃点
        if (nextNode == endNode)
            return nextNode;

        // 3. 检查是否有强制邻居（触发跳跃点的关键条件）
        if (HasForcedNeighbor(nextNode, dir, allowDiagonal))
            return nextNode;

        // 4. 对角线方向：需要先检查正交方向是否有跳跃点（避免遗漏路径）
        if (allowDiagonal && (dir.x != 0 && dir.y != 0))
        {
            // 检查x轴方向是否有跳跃点
            if (FindJumpPoint(nextNode, new Vector2Int(dir.x, 0), endNode, allowDiagonal) != null)
                return nextNode;
            // 检查y轴方向是否有跳跃点
            if (FindJumpPoint(nextNode, new Vector2Int(0, dir.y), endNode, allowDiagonal) != null)
                return nextNode;
        }

        // 5. 继续沿当前方向递归搜索
        return FindJumpPoint(nextNode, dir, endNode, allowDiagonal);
    }

    /// <summary>
    /// 检查节点是否有强制邻居（跳跃点判定条件）
    /// 强制邻居：当前路径旁有障碍物，必须在此节点转弯才能通过的邻居
    /// </summary>
    private static bool HasForcedNeighbor(GridNode node, Vector2Int dir, bool allowDiagonal)
    {
        int x = node.x;
        int y = node.y;
        int dx = dir.x;
        int dy = dir.y;

        // 四连通方向（水平/垂直）
        if (dx != 0 && dy == 0) // 水平方向（左右）
        {
            // 检查上下两个斜向邻居是否为障碍物
            if (IsBlocked(x, y + 1) && !IsBlocked(x + dx, y + 1))
                return true;
            if (IsBlocked(x, y - 1) && !IsBlocked(x + dx, y - 1))
                return true;
        }
        else if (dx == 0 && dy != 0) // 垂直方向（上下）
        {
            // 检查左右两个斜向邻居是否为障碍物
            if (IsBlocked(x + 1, y) && !IsBlocked(x + 1, y + dy))
                return true;
            if (IsBlocked(x - 1, y) && !IsBlocked(x - 1, y + dy))
                return true;
        }
        // 八连通方向（对角线）
        else if (allowDiagonal)
        {
            // 检查对角移动时的"切角"障碍物（避免穿墙）
            if (IsBlocked(x - dx, y) && !IsBlocked(x - dx, y + dy))
                return true;
            if (IsBlocked(x, y - dy) && !IsBlocked(x + dx, y - dy))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 计算两个节点之间的移动成本（考虑通行成本和移动距离）
    /// </summary>
    private static int CalculateCost(GridNode from, GridNode to, bool allowDiagonal)
    {
        int cost = 0;
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);

        // 四连通：曼哈顿距离 × 节点平均成本
        if (!allowDiagonal)
        {
            int steps = dx + dy;
            // 取路径上所有节点的成本平均值（简化计算，也可逐节点累加）
            cost = steps * (from.cost + to.cost) / 2;
        }
        // 八连通：切比雪夫距离（对角线14成本，正交10成本，贴合网格移动逻辑）
        else
        {
            int diagonalSteps = Mathf.Min(dx, dy);
            int straightSteps = Mathf.Abs(dx - dy);
            // 对角线成本 = 14 × 对角线步数，正交成本 = 10 × 正交步数（乘以平均节点成本）
            cost = (diagonalSteps * 14 + straightSteps * 10) * (from.cost + to.cost) / 20;
        }

        return Mathf.Max(1, cost); // 确保成本至少为1
    }

    /// <summary>
    /// 启发式函数：估算节点到终点的代价（影响寻路效率，不影响路径正确性）
    /// </summary>
    private static int Heuristic(GridNode node, GridNode endNode, bool allowDiagonal)
    {
        int dx = Mathf.Abs(node.x - endNode.x);
        int dy = Mathf.Abs(node.y - endNode.y);

        // 四连通：曼哈顿距离
        if (!allowDiagonal)
            return (dx + dy) * 10; // ×10 与成本计算单位统一
        // 八连通：切比雪夫距离（对角线和正交移动代价相同）
        else
            return Mathf.Max(dx, dy) * 10;
    }

    /// <summary>
    /// 检查节点是否被阻挡（简化边界和阻挡判断）
    /// </summary>
    private static bool IsBlocked(int x, int y)
    {
        GridNode node = GridManager.Instance.GetNode(x, y);
        return node == null || node.blocked;
    }

    /// <summary>
    /// 验证起点和终点节点有效性
    /// </summary>
    private static bool ValidateNodes(GridNode start, GridNode end)
    {
        if (start == null || end == null)
        {
            Debug.LogError("JPS：起点或终点超出网格范围");
            return false;
        }

        if (start.blocked || end.blocked)
        {
            Debug.LogError("JPS：起点或终点被阻挡");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 从终点回溯到起点，重建路径（与BFS逻辑一致，但节点更少）
    /// </summary>
    private static List<Vector3> ReconstructPath(Dictionary<GridNode, GridNode> parentMap, GridNode start, GridNode end)
    {
        List<GridNode> pathNodes = new List<GridNode>();
        GridNode current = end;

        // 回溯父节点
        while (current != null)
        {
            pathNodes.Add(current);
            current = parentMap[current];
        }

        // 反转路径（起点→终点）并转换为世界坐标
        pathNodes.Reverse();
        List<Vector3> worldPath = new List<Vector3>();
        foreach (GridNode node in pathNodes)
        {
            worldPath.Add(GridManager.Instance.GetWorldPosition(node.x, node.y));
        }

        return worldPath;
    }

    /// <summary>
    /// 调试用：可视化路径和跳跃点
    /// </summary>
    public static void DrawPath(List<Vector3> path, Color pathColor = default, Color jumpPointColor = default, float duration = 5f)
    {
        if (path == null || path.Count < 2)
            return;

        // 默认颜色
        if (pathColor == default) pathColor = Color.cyan;
        if (jumpPointColor == default) jumpPointColor = Color.yellow;

        // 绘制路径线
        for (int i = 0; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }

        // 绘制跳跃点（路径上的节点）
        foreach (Vector3 point in path)
        {
            Gizmos.DrawSphere(point, 0.2f);
        }
    }
}
