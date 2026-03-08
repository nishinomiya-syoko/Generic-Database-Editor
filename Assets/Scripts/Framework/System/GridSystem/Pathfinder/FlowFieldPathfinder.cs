using UnityEngine;
using System.Collections.Generic;

public static class FlowFieldPathfinder
{
    public static List<Vector3> GetPath(Vector3 fromWorld,Vector3 toWorld, int maxSteps = 1024)
    {
        var path = new List<Vector2Int>();
        var gm = GridManager.Instance;
        if (gm == null) return null;

        var goalNode = gm.GetNodeFromWorldPos(toWorld);
        var map = PathPool.GetFlowFieldMap(goalNode.x, goalNode.y);
        // Debug.Log("GetFlowFieldMap: " + map.destination);
        if (map == null) return null;

        var startNode = gm.GetNodeFromWorldPos(fromWorld);
        var pos = new Vector2Int(startNode.x, startNode.y);

        for (int i = 0; i < maxSteps; i++)
        {

            var node = map.GetNode(pos.x, pos.y);
            if (node == null) break;
            // add node center
            path.Add(new Vector2Int(node.x, node.y));

            // reached goal?
            if (node.x == map.destination.x && node.y == map.destination.y) break;

            // get direction
            if (node.bestDirection == Vector2.zero) break;
            
            // step to next cell center
            // curWorld = gm.GetWorldPosition(node.x, node.y) + dir * gm.cellSize * 0.6f; // move slightly into neighbor cell
            Vector2Int next = new Vector2Int();
            if (node.bestDirection.x > 0) next.x = 1;
            if (node.bestDirection.y > 0) next.y = 1;
            if (node.bestDirection.x < 0) next.x = -1;
            if (node.bestDirection.y < 0) next.y = -1;
            pos += next;
        }
        var worldPath = new List<Vector3>();
        foreach (var p in path)
        {
            worldPath.Add(gm.GetWorldPosition(p.x, p.y));
        }

        return worldPath;
    }
}