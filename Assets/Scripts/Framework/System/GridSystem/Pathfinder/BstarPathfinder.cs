using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 双向 A*（Bidirectional A*）实现，适用于网格寻路。
/// API 与 AStarPathfinder 类似：返回世界坐标点列表或 null（无路径）。
/// 特点：同时从起点和目标向中间扩展，通常比单向 A* 探索更少节点（特别是大地图长路径）。
/// </summary>
public static class BStarPathfinder
{
    private const int COST_STRAIGHT = 10;
    private const int COST_DIAGONAL = 14;
    private const int INF = int.MaxValue / 4;

    public static List<Vector3> FindPath(Vector3 fromWorld, Vector3 toWorld, bool allowDiagonal = false, bool allowThroughPassable = false)
    {
        var gm = GridManager.Instance;
        if (gm == null) return null;
        var start = gm.GetNodeFromWorldPos(fromWorld);
        var goal = gm.GetNodeFromWorldPos(toWorld);
        if (start == null || goal == null) return null;
        if (start == goal) return new List<Vector3> { gm.GetWorldPosition(start.x, start.y) };

        int approx = Mathf.Max(16, (gm.width * gm.height) / 8);
        var openF = new BinaryHeapPriorityQueue<GridNode>(approx);
        var openB = new BinaryHeapPriorityQueue<GridNode>(approx);
        var closedF = new HashSet<GridNode>(approx);
        var closedB = new HashSet<GridNode>(approx);

        var gF = new Dictionary<GridNode, int>(approx);
        var gB = new Dictionary<GridNode, int>(approx);
        var parentF = new Dictionary<GridNode, GridNode>(approx);
        var parentB = new Dictionary<GridNode, GridNode>(approx);

        // init
        gF[start] = 0;
        gB[goal] = 0;
        openF.Enqueue(start, Heuristic(start, goal, allowDiagonal));
        openB.Enqueue(goal, Heuristic(goal, start, allowDiagonal));

        GridNode meetingNode = null;

        while (openF.Count > 0 && openB.Count > 0)
        {
            // Expand the frontier with smaller top f
            var nextF = openF.PeekPriority();
            var nextB = openB.PeekPriority();
            bool expandForward = nextF <= nextB;

            if (expandForward)
            {
                var cur = openF.Dequeue();
                closedF.Add(cur);

                // if this node is seen by backward search, we met
                if (closedB.Contains(cur)) { meetingNode = cur; break; }

                foreach (var nb in gm.GetNeighbors(cur, allowDiagonal))
                {
                    if (closedF.Contains(nb)) continue;
                    if (nb.blocked && (!allowThroughPassable)) continue;
                    int moveCost = gF[cur] + nb.cost + (IsDiagonal(cur, nb) ? COST_DIAGONAL : COST_STRAIGHT);
                    if (!gF.TryGetValue(nb, out int prev) || moveCost < prev)
                    {
                        gF[nb] = moveCost;
                        parentF[nb] = cur;
                        int fScore = moveCost + Heuristic(nb, goal, allowDiagonal);
                        if (!openF.Contains(nb)) openF.Enqueue(nb, fScore);
                        else openF.UpdatePriority(nb, fScore);
                    }
                }
            }
            else
            {
                var cur = openB.Dequeue();
                closedB.Add(cur);

                if (closedF.Contains(cur)) { meetingNode = cur; break; }

                foreach (var nb in gm.GetNeighbors(cur, allowDiagonal))
                {
                    if (closedB.Contains(nb)) continue;
                    if (nb.blocked && (!allowThroughPassable)) continue;

                    int moveCost = gB[cur] + nb.cost + (IsDiagonal(cur, nb) ? COST_DIAGONAL : COST_STRAIGHT);
                    if (!gB.TryGetValue(nb, out int prev) || moveCost < prev)
                    {
                        gB[nb] = moveCost;
                        parentB[nb] = cur;
                        int fScore = moveCost + Heuristic(nb, start, allowDiagonal);
                        if (!openB.Contains(nb)) openB.Enqueue(nb, fScore);
                        else openB.UpdatePriority(nb, fScore);
                    }
                }
            }
        }

        if (meetingNode == null)
        {
            // No meeting point found
            return null;
        }

        // Reconstruct path from start -> meetingNode using parentF
        var pathF = new List<GridNode>();
        var curF = meetingNode;
        while (curF != null)
        {
            pathF.Add(curF);
            if (!parentF.TryGetValue(curF, out curF)) break;
        }
        pathF.Reverse(); // now from start .. meeting

        // Reconstruct path from meetingNode -> goal using parentB
        var pathB = new List<GridNode>();
        var curB = meetingNode;
        while (curB != null)
        {
            pathB.Add(curB);
            if (!parentB.TryGetValue(curB, out curB)) break;
        }
        // pathB currently meeting -> ... -> goal

        // Combine: pathF (start..meeting) + pathB (meeting.next .. goal)
        var full = new List<Vector3>();
        foreach (var n in pathF)
            full.Add(gm.GetWorldPosition(n.x, n.y));

        // skip first element of pathB to avoid duplicating meetingNode
        for (int i = 1; i < pathB.Count; i++)
            full.Add(gm.GetWorldPosition(pathB[i].x, pathB[i].y));

        return full;
    }



    private static bool IsDiagonal(GridNode a, GridNode b) => a.x != b.x && a.y != b.y;

    private static int Heuristic(GridNode a, GridNode b, bool allowDiagonal)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        if (!allowDiagonal) return COST_STRAIGHT * (dx + dy);
        int diag = Mathf.Min(dx, dy);
        int straight = dx + dy - 2 * diag;
        return COST_DIAGONAL * diag + COST_STRAIGHT * straight;
    }
}
