// using System;
// using System.Collections.Generic;

// /// <summary>
// /// 高性能二叉堆（最小堆）——泛型键为 int（节点 id），优先级为 int。
// /// 适用于 A* 等单线程场景（managed）。
// /// 线程不安全，不要在 Jobs 中使用此类（Jobs 版本见 FlowFieldJob）。
// /// </summary>
// public class BinaryHeapPriorityQueue
// {
//     private int[] heapKeys;      // 存 node ids
//     private int[] heapPrior;     // 存对应优先级
//     private int count;
//     private Dictionary<int, int> positions; // nodeId -> heap index (便于 UpdatePriority / Contains)

//     public int Count => count;

//     public BinaryHeapPriorityQueue(int capacity = 1024)
//     {
//         heapKeys = new int[capacity];
//         heapPrior = new int[capacity];
//         positions = new Dictionary<int, int>(capacity);
//         count = 0;
//     }

//     public bool Contains(int nodeId) => positions.ContainsKey(nodeId);

//     public void EnsureCapacity(int cap)
//     {
//         if (heapKeys.Length >= cap) return;
//         int newCap = Math.Max(cap, heapKeys.Length * 2);
//         Array.Resize(ref heapKeys, newCap);
//         Array.Resize(ref heapPrior, newCap);
//     }

//     public void Clear()
//     {
//         positions.Clear();
//         count = 0;
//     }

//     public void Enqueue(int nodeId, int priority)
//     {
//         if (positions.ContainsKey(nodeId))
//         {
//             UpdatePriority(nodeId, priority);
//             return;
//         }

//         if (count >= heapKeys.Length) EnsureCapacity(count + 1);
//         heapKeys[count] = nodeId;
//         heapPrior[count] = priority;
//         positions[nodeId] = count;
//         SiftUp(count++);
//     }

//     public int Dequeue()
//     {
//         if (count == 0) throw new InvalidOperationException("Heap empty");
//         int result = heapKeys[0];
//         positions.Remove(result);
//         count--;
//         if (count > 0)
//         {
//             heapKeys[0] = heapKeys[count];
//             heapPrior[0] = heapPrior[count];
//             positions[heapKeys[0]] = 0;
//             SiftDown(0);
//         }
//         return result;
//     }

//     public void UpdatePriority(int nodeId, int newPriority)
//     {
//         if (!positions.TryGetValue(nodeId, out int idx)) return;
//         int old = heapPrior[idx];
//         heapPrior[idx] = newPriority;
//         if (newPriority < old) SiftUp(idx);
//         else if (newPriority > old) SiftDown(idx);
//     }

//     private void SiftUp(int i)
//     {
//         while (i > 0)
//         {
//             int p = (i - 1) >> 1;
//             if (heapPrior[i] < heapPrior[p])
//             {
//                 Swap(i, p);
//                 i = p;
//             }
//             else break;
//         }
//     }

//     private void SiftDown(int i)
//     {
//         while (true)
//         {
//             int l = (i << 1) + 1;
//             int r = l + 1;
//             int smallest = i;
//             if (l < count && heapPrior[l] < heapPrior[smallest]) smallest = l;
//             if (r < count && heapPrior[r] < heapPrior[smallest]) smallest = r;
//             if (smallest != i)
//             {
//                 Swap(i, smallest);
//                 i = smallest;
//             }
//             else break;
//         }
//     }

//     private void Swap(int a, int b)
//     {
//         int ka = heapKeys[a], kb = heapKeys[b];
//         int pa = heapPrior[a], pb = heapPrior[b];
//         heapKeys[a] = kb; heapPrior[a] = pb;
//         heapKeys[b] = ka; heapPrior[b] = pa;
//         positions[heapKeys[a]] = a;
//         positions[heapKeys[b]] = b;
//     }
// }
