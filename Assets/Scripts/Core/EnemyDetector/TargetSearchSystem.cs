using System.Collections.Generic;
using UnityEngine;

namespace RTS.TargetSearch
{   
    /// <summary>
    /// 搜索系统 核心仍然保持 **同步搜索**，因为这部分最好做到最轻量。
    ///异步只用于“调度”。
    /// </summary>
    public static class TargetSearchSystem
    {
        public static float CellSize = 5f;

        private static readonly Dictionary<Vector2Int, HashSet<ITargetable>> Grid =
            new Dictionary<Vector2Int, HashSet<ITargetable>>();

        private static readonly Dictionary<ITargetable, Vector2Int> EntityCellMap =
            new Dictionary<ITargetable, Vector2Int>();

        public static void Register(ITargetable entity)
        {
            if (entity == null || EntityCellMap.ContainsKey(entity))
                return;

            Vector2Int cell = WorldToCell(entity.Position);
            EntityCellMap[entity] = cell;

            if (!Grid.TryGetValue(cell, out var bucket))
            {
                bucket = new HashSet<ITargetable>();
                Grid[cell] = bucket;
            }

            bucket.Add(entity);
        }

        public static void Unregister(ITargetable entity)
        {
            if (entity == null)
                return;

            if (!EntityCellMap.TryGetValue(entity, out var cell))
                return;

            if (Grid.TryGetValue(cell, out var bucket))
            {
                bucket.Remove(entity);
                if (bucket.Count == 0)
                    Grid.Remove(cell);
            }

            EntityCellMap.Remove(entity);
        }

        public static void UpdateEntityCell(ITargetable entity)
        {
            if (entity == null)
                return;

            if (!EntityCellMap.TryGetValue(entity, out var oldCell))
            {
                Register(entity);
                return;
            }

            Vector2Int newCell = WorldToCell(entity.Position);
            if (newCell == oldCell)
                return;

            if (Grid.TryGetValue(oldCell, out var oldBucket))
            {
                oldBucket.Remove(entity);
                if (oldBucket.Count == 0)
                    Grid.Remove(oldCell);
            }

            if (!Grid.TryGetValue(newCell, out var newBucket))
            {
                newBucket = new HashSet<ITargetable>();
                Grid[newCell] = newBucket;
            }

            newBucket.Add(entity);
            EntityCellMap[entity] = newCell;
        }
        /// <summary>
        /// 搜索指定半径内的目标
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="results"></param>
        /// <param name="excludeTeamId"></param>
        public static void SearchInRangeNonAlloc(
            Vector3 center,
            float radius,
            List<ITargetable> results,
            int? excludeTeamId = null)
        {
            results.Clear();

            float radiusSqr = radius * radius;
            GetSearchCellBounds(center, radius, out int minX, out int maxX, out int minY, out int maxY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!Grid.TryGetValue(cell, out var bucket))
                        continue;

                    foreach (var entity in bucket)
                    {
                        if (entity == null || !entity.IsAlive)
                            continue;

                        if (excludeTeamId.HasValue && entity.TeamId == excludeTeamId.Value)
                            continue;

                        Vector3 pos = entity.Position;
                        float dx = pos.x - center.x;
                        float dz = pos.z - center.z;
                        float distSqr = dx * dx + dz * dz;

                        if (distSqr <= radiusSqr)
                        {
                            results.Add(entity);
                        }
                    }
                }
            }
        }

        public static ITargetable FindNearest(
            Vector3 center,
            float radius,
            int? excludeTeamId = null)
        {
            float radiusSqr = radius * radius;
            float minDistSqr = float.MaxValue;
            ITargetable nearest = null;

            GetSearchCellBounds(center, radius, out int minX, out int maxX, out int minY, out int maxY);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!Grid.TryGetValue(cell, out var bucket))
                        continue;

                    foreach (var entity in bucket)
                    {
                        if (entity == null || !entity.IsAlive)
                            continue;

                        if (excludeTeamId.HasValue && entity.TeamId == excludeTeamId.Value)
                            continue;

                        Vector3 pos = entity.Position;
                        float dx = pos.x - center.x;
                        float dz = pos.z - center.z;
                        float distSqr = dx * dx + dz * dz;

                        if (distSqr <= radiusSqr && distSqr < minDistSqr)
                        {
                            minDistSqr = distSqr;
                            nearest = entity;
                        }
                    }
                }
            }

            return nearest;
        }

        private static Vector2Int WorldToCell(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / CellSize);
            int y = Mathf.FloorToInt(position.z / CellSize);
            return new Vector2Int(x, y);
        }

        private static void GetSearchCellBounds(
            Vector3 center,
            float radius,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY)
        {
            Vector2Int minCell = WorldToCell(new Vector3(center.x - radius, 0f, center.z - radius));
            Vector2Int maxCell = WorldToCell(new Vector3(center.x + radius, 0f, center.z + radius));

            minX = minCell.x;
            maxX = maxCell.x;
            minY = minCell.y;
            maxY = maxCell.y;
        }

        public static void Clear()
        {
            Grid.Clear();
            EntityCellMap.Clear();
        }
    }
}