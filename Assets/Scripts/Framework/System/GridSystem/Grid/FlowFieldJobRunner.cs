using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System;

/// <summary>
/// FlowField 的 Jobs + Burst 实现器（同步执行，simple API）。
/// 把 grid 信息（cost/blocked）传入，指定目标格或世界位置，执行后会把 integrationCost 和 bestDirection 写回。
///
/// 使用示例：
///   FlowFieldJobRunner runner = new FlowFieldJobRunner();
///   runner.BuildFlowField(goalWorldPos, gridManager);
///   // 运行完成后 GridManager 上可以读取 node.integrationCost & bestDirection （此脚本会写回）
///
/// 注意：调用后本脚本会在内部分配 NativeArray 并负责 Dispose（同步完成）。
/// 若你想异步 schedule，请把 job.Schedule() 保持并在 Complete 后读取输出（需要修改）。
/// </summary>
public class FlowFieldJobRunner
{
    // 主入口：根据目标世界位置生成 flow field 并写回 GridManager
    public static void BuildFlowFieldWithJob(Vector2Int goal, GridManager gm, bool allowDiagonal = false)
    {
        if (gm == null) throw new ArgumentNullException(nameof(gm));
        int width = gm.width;
        int height = gm.height;
        int gridSize = width * height;

        int goalX, goalY;
        var goalNode = gm.GetNode(goal.x, goal.y);
        if (goalNode == null) return;
        goalX = goalNode.x; goalY = goalNode.y;
        int goalIndex = goalY * width + goalX;

        // 填充输入数组：costs 与 blocked
        var costs = new NativeArray<int>(gridSize, Allocator.TempJob);
        var blocked = new NativeArray<byte>(gridSize, Allocator.TempJob); // 0/1
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var n = gm.GetNode(x, y);
                int idx = y * width + x;
                costs[idx] = Mathf.Max(1, n.cost);
                blocked[idx] = n.blocked ? (byte)1 : (byte)0;
            }

        // 输出数组
        var integrationCosts = new NativeArray<int>(gridSize, Allocator.TempJob);
        var bestDirX = new NativeArray<float>(gridSize, Allocator.TempJob);
        var bestDirY = new NativeArray<float>(gridSize, Allocator.TempJob);

        // Job 参数
        var job = new FlowFieldJob
        {
            width = width,
            height = height,
            allowDiagonal = allowDiagonal ? 1 : 0,
            costs = costs,
            blocked = blocked,
            goalIndex = goalIndex,
            integrationCosts = integrationCosts,
            bestDirX = bestDirX,
            bestDirY = bestDirY
        };

        // schedule & complete (同步)
        var handle = job.Schedule();
        handle.Complete();

        // 将结果写回 GridManager 的每个 GridNode
        for (int i = 0; i < gridSize; i++)
        {
            int x = i % width;
            int y = i / width;
            // var n = gm.GetNode(x, y);
            var n = gm.GetNode(x, y);
            n.integrationCost = integrationCosts[i];
            n.bestDirection = new Vector2(bestDirX[i], bestDirY[i]);
        }

        // var goal = gm.GetNodeFromWorldPos(goalWorld);
        var goalGrid = new Vector2Int(goal.x, goal.y);
        FlowFieldMap map = new FlowFieldMap(width, height, goalGrid);
        // 将结果写回 GridManager 的每个 GridNode
        for (int i = 0; i < gridSize; i++)
        {
            int x = i % width;
            int y = i / width;
            // var n = gm.GetNode(x, y);
            var n = map.GetNode(x, y);
            n.integrationCost = integrationCosts[i];
            n.bestDirection = new Vector2(bestDirX[i], bestDirY[i]);
        }
        PathPool.AddFlowFieldMap(map);

        // Dispose
        costs.Dispose();
        blocked.Dispose();
        integrationCosts.Dispose();
        bestDirX.Dispose();
        bestDirY.Dispose();
    }

    // 内部 Job：单线程逻辑但以 Burst 编译，使用 Native 二叉堆（固定容量）
    [BurstCompile]
    private struct FlowFieldJob : IJob
    {
        public int width;
        public int height;
        public int allowDiagonal; // 0/1
        public int goalIndex;

        [ReadOnly] public NativeArray<int> costs;
        [ReadOnly] public NativeArray<byte> blocked;

        // outputs
        public NativeArray<int> integrationCosts;
        public NativeArray<float> bestDirX;
        public NativeArray<float> bestDirY;

        // 临时堆（最小堆），capacity = width*height
        // 为了避免额外分配，我们在 Job 内创建临时 NativeArrays（会自动被 Job 管理的 stack 分配不可行）
        // 但 IJob 中可以 new NativeArray，需要最后 Dispose（这里在 Job 内创建并在结束前 Dispose）
        public void Execute()
        {
            var self = this;
            int gridSize = width * height;
            // 初始化 outputs
            for (int i = 0; i < gridSize; i++)
            {
                integrationCosts[i] = int.MaxValue / 4;
                bestDirX[i] = 0f;
                bestDirY[i] = 0f;
            }

            // 创建用于堆的 NativeArray
            var heapNodes = new NativeArray<int>(gridSize, Allocator.Temp);
            var heapPrior = new NativeArray<int>(gridSize, Allocator.Temp);
            int heapCount = 0;

            // Helper: heap ops
            void HeapPush(int nodeIdx, int priority)
            {
                int i = heapCount++;
                heapNodes[i] = nodeIdx;
                heapPrior[i] = priority;
                // sift up
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (heapPrior[i] < heapPrior[p])
                    {
                        // swap
                        int tn = heapNodes[i]; heapNodes[i] = heapNodes[p]; heapNodes[p] = tn;
                        int tp = heapPrior[i]; heapPrior[i] = heapPrior[p]; heapPrior[p] = tp;
                        i = p;
                    }
                    else break;
                }
            }

            int HeapPop()
            {
                int result = heapNodes[0];
                heapCount--;
                if (heapCount > 0)
                {
                    heapNodes[0] = heapNodes[heapCount];
                    heapPrior[0] = heapPrior[heapCount];
                    // sift down
                    int i = 0;
                    while (true)
                    {
                        int l = (i << 1) + 1;
                        int r = l + 1;
                        int smallest = i;
                        if (l < heapCount && heapPrior[l] < heapPrior[smallest]) smallest = l;
                        if (r < heapCount && heapPrior[r] < heapPrior[smallest]) smallest = r;
                        if (smallest != i)
                        {
                            int tn = heapNodes[i]; heapNodes[i] = heapNodes[smallest]; heapNodes[smallest] = tn;
                            int tp = heapPrior[i]; heapPrior[i] = heapPrior[smallest]; heapPrior[smallest] = tp;
                            i = smallest;
                        }
                        else break;
                    }
                }
                return result;
            }

            bool HeapIsEmpty() => heapCount == 0;

            // Dijkstra: 从目标开始
            integrationCosts[goalIndex] = 0;
            HeapPush(goalIndex, 0);

            while (!HeapIsEmpty())
            {
                int cur = HeapPop();
                int curCost = integrationCosts[cur];

                int cx = cur % width;
                int cy = cur / width;

                // Iterate neighbors
                // 4 directions
                TryRelaxNeighbor(cx, cy + 1, cur, curCost);
                TryRelaxNeighbor(cx + 1, cy, cur, curCost);
                TryRelaxNeighbor(cx, cy - 1, cur, curCost);
                TryRelaxNeighbor(cx - 1, cy, cur, curCost);

                if (allowDiagonal == 1)
                {
                    TryRelaxNeighbor(cx + 1, cy + 1, cur, curCost, true);
                    TryRelaxNeighbor(cx + 1, cy - 1, cur, curCost, true);
                    TryRelaxNeighbor(cx - 1, cy + 1, cur, curCost, true);
                    TryRelaxNeighbor(cx - 1, cy - 1, cur, curCost, true);
                }
            }

            // 生成 bestDirection：对每个格子选一个 neighbor cost 更小的方向
            for (int i = 0; i < gridSize; i++)
            {
                int ix = i % width;
                int iy = i / width;
                int selfCost = integrationCosts[i];
                if (selfCost >= int.MaxValue / 8) continue; // unreachable

                int bestIdx = -1;
                int bestC = selfCost;
                // neighbors
                TryPickNeighbor(ix, iy + 1, ref bestIdx, ref bestC);
                TryPickNeighbor(ix + 1, iy, ref bestIdx, ref bestC);
                TryPickNeighbor(ix, iy - 1, ref bestIdx, ref bestC);
                TryPickNeighbor(ix - 1, iy, ref bestIdx, ref bestC);
                if (allowDiagonal == 1)
                {
                    TryPickNeighbor(ix + 1, iy + 1, ref bestIdx, ref bestC);
                    TryPickNeighbor(ix + 1, iy - 1, ref bestIdx, ref bestC);
                    TryPickNeighbor(ix - 1, iy + 1, ref bestIdx, ref bestC);
                    TryPickNeighbor(ix - 1, iy - 1, ref bestIdx, ref bestC);
                }

                if (bestIdx >= 0)
                {
                    int bx = bestIdx % width;
                    int by = bestIdx / width;
                    float wx = (bx - ix); // direction in grid units, normalized later
                    float wy = (by - iy);
                    float len = math.sqrt(wx * wx + wy * wy);
                    if (len > 0f)
                    {
                        bestDirX[i] = wx / len;
                        bestDirY[i] = wy / len;
                    }
                }
            }

            // Dispose temporaries
            heapNodes.Dispose();
            heapPrior.Dispose();

            // Local helper functions inside Job (can't capture outer variables except by value)
            void TryRelaxNeighbor(int nx, int ny, int curIdx, int curCost, bool diagonal = false)
            {
                if (nx < 0 || ny < 0 || nx >= self.width || ny >= self.height) return;
                int nIdx = ny * self.width + nx;
                if (self.blocked[nIdx] == 1) return;
                int moveExtra = diagonal ? 14 : 10;
                int moveCost = self.costs[nIdx] + moveExtra; // 修复访问方式
                int newCost = curCost + moveCost;
                if (newCost < self.integrationCosts[nIdx]) // 修复访问方式
                {
                    self.integrationCosts[nIdx] = newCost; // 修复访问方式
                    HeapPush(nIdx, newCost);
                }
            }

            void TryPickNeighbor(int nx, int ny, ref int outBestIdx, ref int outBestCost)
            {
                if (nx < 0 || ny < 0 || nx >= self.width || ny >= self.height) return;
                int nIdx = ny * self.width + nx;
                int c = self.integrationCosts[nIdx]; // 修复访问方式
                if (c < outBestCost)
                {
                    outBestCost = c;
                    outBestIdx = nIdx;
                }
            }
        }
    }
}
