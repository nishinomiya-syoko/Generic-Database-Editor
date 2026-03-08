using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BFS寻路算法实现，基于GridManager的网格系统
/// 广度优先搜索（BFS）是一种用于遍历或搜索树或图的算法。
/// 它从根节点开始，逐层向下遍历节点，直到找到所需的节点或遍历完所有节点。
/// BFS算法的核心思想是先访问离根节点最近的节点，因此它通常用于找到从起点到终点的最短路径问题。
/// </summary>
public static class BFSPathfinder
{
    /// <summary>
    /// 使用BFS算法寻找从起点到终点的最短路径
    /// </summary>
    /// <param name="startWorldPos">起点世界坐标</param>
    /// <param name="endWorldPos">终点世界坐标</param>
    /// <param name="allowDiagonal">是否允许对角线移动</param>
    /// <param name="preventCutCorners">是否防止角切入</param>
    /// <returns>路径的世界坐标列表，如果找不到路径则返回null</returns>
    public static List<Vector3> FindPath(Vector3 startWorldPos, Vector3 endWorldPos, bool allowDiagonal = false, bool preventCutCorners = false)
    {
        var gm = GridManager.Instance;
        if (gm == null)
        {
            Debug.LogError("BFSPathfinding: GridManager.Instance is null");
            return null;
        }

        // 将世界坐标转换为网格节点
        GridNode startNode = gm.GetNodeFromWorldPos(startWorldPos);
        GridNode endNode = gm.GetNodeFromWorldPos(endWorldPos);

        // 检查起点和终点是否有效
        if (startNode == null || endNode == null)
        {
            Debug.LogError("起点或终点超出网格范围");
            return null;
        }

        if (startNode.blocked || endNode.blocked)
        {
            Debug.LogError("起点或终点被阻挡");
            return null;
        }

        // 如果起点就是终点，直接返回空路径
        if (startNode.x == endNode.x && startNode.y == endNode.y)
        {
            return new List<Vector3> { GridManager.Instance.GetWorldPosition(startNode.x, startNode.y) };
        }

        // BFS所需的数据结构
        int approx = Mathf.Max(16, (gm.width * gm.height) / 8);
        // 队列
        Queue<GridNode> queue = new Queue<GridNode>(approx);
        // 集合
        HashSet<GridNode> visited = new HashSet<GridNode>(approx);
        // 优先级队列
        Dictionary<GridNode, GridNode> parentMap = new Dictionary<GridNode, GridNode>(approx);

        // 初始化队列，添加起点
        queue.Enqueue(startNode);
        visited.Add(startNode);

        // 执行BFS
        while (queue.Count > 0)
        {
            GridNode currentNode = queue.Dequeue();

            // 检查是否到达终点
            if (currentNode == endNode)
            {
                // 回溯构建路径
                return ReconstructPath(parentMap, startNode, endNode);
            }

            // 探索所有邻居
            foreach (GridNode neighbor in gm.GetNeighbors(currentNode, allowDiagonal))
            {
                if (neighbor == null) continue;
                // 可选：禁止通过“角落切入”（即从两个正交被阻挡格子之间斜向穿过）
                if (preventCutCorners && IsDiagonal(currentNode, neighbor))
                {
                    // 检查两个正交邻居是否其中之一被阻挡
                    int dx = neighbor.x - currentNode.x;
                    int dy = neighbor.y - currentNode.y;
                    var n1 = gm.GetNode(currentNode.x + dx, currentNode.y);
                    var n2 = gm.GetNode(currentNode.x, currentNode.y + dy);
                    if ((n1 != null && n1.blocked) || (n2 != null && n2.blocked))
                    continue; // 禁止穿过角落
                }

                // 检查邻居是否未被访问且未被阻挡
                if (!neighbor.blocked && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parentMap[neighbor] = currentNode;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // 如果队列为空仍未找到终点，则没有路径
        Debug.LogWarning("找不到从起点到终点的路径");
        return null;
    }

    /// <summary>
    /// 从终点回溯到起点，重建路径
    /// </summary>
    private static List<Vector3> ReconstructPath(Dictionary<GridNode, GridNode> parentMap,
                                                GridNode startNode, GridNode endNode)
    {
        List<GridNode> pathNodes = new List<GridNode>();
        GridNode current = endNode;

        // 从终点回溯到起点
        while (current != startNode)
        {
            pathNodes.Add(current);
            if (!parentMap.TryGetValue(current, out current))
            {
                Debug.LogError("BFSPathfinding: ReconstructPath failed - missing parent mapping.");
                return null;
            }
        }

        // 添加起点
        pathNodes.Add(startNode);

        // 反转路径，使其从起点到终点
        pathNodes.Reverse();

        // 转换为世界坐标
        List<Vector3> worldPath = new List<Vector3>();
        foreach (GridNode node in pathNodes)
        {
            worldPath.Add(GridManager.Instance.GetWorldPosition(node.x, node.y));
        }

        return worldPath;
    }
    private static bool IsDiagonal(GridNode a, GridNode b) => a.x != b.x && a.y != b.y;
    /// <summary>
    /// 调试用：可视化路径
    /// </summary>
    public static void DrawPath(List<Vector3> path, Color color, float duration = 5f)
    {
        if (path == null || path.Count < 2) return;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawLine(path[i], path[i + 1], color, duration);
        }
    }
}