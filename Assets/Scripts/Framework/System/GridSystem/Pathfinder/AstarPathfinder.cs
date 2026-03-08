using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 传统 A*（启发式曼哈顿/欧式）用于单体路径查询
/// 注意：对大量单位频繁调用会成为瓶颈
/// </summary>
public static class AStarPathfinder
{
    private const int COST_STRAIGHT = 10;
    private const int COST_DIAGONAL = 14;
    public static List<Vector3> FindPath(Vector3 fromWorld, Vector3 toWorld, bool allowDiagonal = false, bool allowThroughPassable = false)
    {
        var gm = GridManager.Instance;
        if (gm == null) return null;
        var start = gm.GetNodeFromWorldPos(fromWorld);
        var goal = gm.GetNodeFromWorldPos(toWorld);
        if (start == null || goal == null) return null;

        var open = new BinaryHeapPriorityQueue<GridNode>();
        var closed = new HashSet<GridNode>();

        // reset used fields
        ResetNodes(gm);

        start.g = 0;
        start.f = Heuristic(start, goal);
        open.Enqueue(start, start.f);

        while (open.Count > 0)
        {
            var cur = open.Dequeue();
            if (cur == goal)
                return ReconstructPath(cur, gm);

            closed.Add(cur);
            foreach (var nb in gm.GetNeighbors(cur, allowDiagonal))
            {
                if (closed.Contains(nb)) continue;
                if (nb.blocked && (!allowThroughPassable)) continue; // 如果是完全阻挡且不可穿越，跳过
                int moveCost = cur.g + nb.cost + (IsDiagonal(cur, nb) ? COST_DIAGONAL : COST_STRAIGHT); // 10/14 案例
                if (!open.Contains(nb) || moveCost < nb.g)
                {
                    nb.g = moveCost;
                    nb.parent = cur;
                    nb.f = nb.g + Heuristic(nb, goal);
                    if (!open.Contains(nb))
                        open.Enqueue(nb, nb.f);
                    else
                        open.UpdatePriority(nb, nb.f);
                }
            }
        }
        return null; // 无路径
    }

    private static bool IsDiagonal(GridNode a, GridNode b) => a.x != b.x && a.y != b.y;

    private static int Heuristic(GridNode a, GridNode b)
    {
        // 使用曼哈顿或欧式的 scaled version
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 10 * (dx + dy);
    }

    private static List<Vector3> ReconstructPath(GridNode goal, GridManager gm)
    {
        List<Vector3> path = new List<Vector3>();
        var cur = goal;
        while (cur != null)
        {
            path.Add(gm.GetWorldPosition(cur.x, cur.y));
            cur = cur.parent;
        }
        path.Reverse();
        return path;
    }

    private static void ResetNodes(GridManager gm)
    {
        for (int y = 0; y < gm.height; y++)
            for (int x = 0; x < gm.width; x++)
            {
                var n = gm.GetNode(x, y);
                n.g = int.MaxValue / 4;
                n.f = int.MaxValue / 4;
                n.parent = null;
            }
    }
}
