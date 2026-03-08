using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public static class PathPool
{
    private const int CACHE_SIZE = 10;
    private const int OUT_WIDTH = 2;
    private static readonly Dictionary<int, FlowFieldMap> _flowFieldCache = new Dictionary<int, FlowFieldMap>();
    private static readonly Dictionary<SearchParam, List<Vector3>> _pathCache = new Dictionary<SearchParam, List<Vector3>>();
    
    private static int HashCell(int x, int y) => x * 1000000 + y;
    public static void AddFlowFieldMap(FlowFieldMap map)
    {
        if (_flowFieldCache.Count > CACHE_SIZE)
        {
            _flowFieldCache.Clear();
        }
        if (map == null) return;
        int key = HashCell(map.destination.x, map.destination.y);
        _flowFieldCache[key] = map;
    }
    public static FlowFieldMap GetFlowFieldMap(int x, int y)
    {
        int key = HashCell(x, y);
        if (_flowFieldCache.TryGetValue(key, out FlowFieldMap map)) return map;
        else
        {
            var outCells = OutHashCells(x, y, OUT_WIDTH);
            foreach (var k in outCells)
            {
                if (_flowFieldCache.TryGetValue(k, out map)) return map;
            }
        }
        FlowFieldJobRunner.BuildFlowFieldWithJob(new Vector2Int(x, y), GridManager.Instance, true);
        return null;
    }
    private static List<int> OutHashCells(int x, int y, int range = 1)
    {
        List<int> cells = new List<int>();
        int minX = x - range;
        int maxX = x + range;
        int minY = y - range;
        int maxY = y + range;
        for (int j = minY; j <= maxY; j++)
            for (int i = minX; i <= maxX; i++)
                cells.Add(HashCell(i, j));

        return cells;
    }
    public static void AddPath(Vector2Int fromPos, Vector2Int toPos, List<Vector3> path)
    {
        if(_pathCache.ContainsKey(new SearchParam(fromPos, toPos)))
        return;
        if (_pathCache.Count > CACHE_SIZE)
        {
            _pathCache.Clear();
        }
        
        _pathCache.Add(new SearchParam(fromPos, toPos), path);
    }
    public static List<Vector3> GetPath(Vector2Int fromPos, Vector2Int toPos)
    {
        return _pathCache.TryGetValue(new SearchParam(fromPos, toPos), out var path) ? path : null;
    }
    public static int pathCount { get { return _pathCache.Count; } }
    public static int flowFieldMapCount { get { return _flowFieldCache.Count; } }
}
public class SearchParam
{
    public Vector2Int fromPos,toPos;
    public const int SEARCH_RANGE = 3;
    public SearchParam(Vector2Int fromPos, Vector2Int toPos)
    {
        this.fromPos = fromPos;
        this.toPos = toPos;
    }
    
    public override bool Equals(object obj)
    {
        SearchParam other = obj as SearchParam;
        if(fromPos == other.fromPos && toPos == other.toPos)
            return true;
        if (Vector2.Distance(fromPos,other.fromPos) < SEARCH_RANGE && Vector2.Distance(toPos,other.toPos) < SEARCH_RANGE)
            return true;
            return false;
    }
    public override int GetHashCode()
    {
        return 1;
    }
}