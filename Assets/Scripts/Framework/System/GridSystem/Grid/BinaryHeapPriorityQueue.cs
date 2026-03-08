using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 高性能二叉堆优先队列（最小堆），用于寻路算法（A* / FlowField）
/// 替换原演示级 SimplePriorityQueue，解决 Dequeue 操作 O(n) 的性能瓶颈
/// 接口完全兼容，无需修改 AStarPathfinder 和 FlowFieldPathfinder 的调用逻辑
/// </summary>
/// <typeparam name="T">队列元素类型（如 GridNode）</typeparam>
public class BinaryHeapPriorityQueue<T>
{
    // 堆的核心存储：每个元素包含「实际数据」和「优先级」
    private List<KeyValuePair<T, int>> _heap;
    // 快速索引：记录元素在堆中的位置，实现 O(1) Contains 和 O(log n) UpdatePriority
    private Dictionary<T, int> _elementIndices;
    // 元素相等比较器（默认使用系统比较器）
    private IEqualityComparer<T> _comparer;

    /// <summary>
    /// 队列当前元素数量
    /// </summary>
    public int Count => _heap.Count;

    /// <summary>
    /// 初始化优先队列
    /// </summary>
    public BinaryHeapPriorityQueue()
    {
        _heap = new List<KeyValuePair<T, int>>();
        _elementIndices = new Dictionary<T, int>();
        _comparer = EqualityComparer<T>.Default;
    }

    /// <summary>
    /// 初始化优先队列（指定初始容量，减少扩容开销）
    /// </summary>
    /// <param name="initialCapacity">初始容量</param>
    public BinaryHeapPriorityQueue(int initialCapacity)
    {
        _heap = new List<KeyValuePair<T, int>>(initialCapacity);
        _elementIndices = new Dictionary<T, int>(initialCapacity);
        _comparer = EqualityComparer<T>.Default;
    }

    /// <summary>
    /// 入队：添加元素到堆，并调整堆结构（上浮）
    /// </summary>
    /// <param name="item">元素</param>
    /// <param name="priority">优先级（值越小越优先）</param>
    public void Enqueue(T item, int priority)
    {
        // 检查元素是否已存在（避免重复入队）
        if (_elementIndices.ContainsKey(item))
        {
            Debug.LogWarning($"元素 {item} 已在队列中，无需重复入队");
            return;
        }

        // 1. 把元素添加到堆的末尾
        int newIndex = _heap.Count;
        _heap.Add(new KeyValuePair<T, int>(item, priority));
        _elementIndices[item] = newIndex;

        // 2. 上浮调整：与父节点比较，优先级更小则交换，直到满足最小堆规则
        BubbleUp(newIndex);
    }

    /// <summary>
    /// 出队：取出优先级最高的元素（堆顶），并调整堆结构（下沉）
    /// </summary>
    /// <returns>优先级最高的元素</returns>
    public T Dequeue()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("队列已空，无法执行 Dequeue 操作");

        // 1. 取出堆顶元素（优先级最高）
        T topItem = _heap[0].Key;
        // 2. 移除元素的索引记录
        _elementIndices.Remove(topItem);

        // 3. 把堆尾元素移到堆顶，然后下沉调整
        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);

        // 4. 如果堆不为空，执行下沉调整
        if (_heap.Count > 0)
        {
            _elementIndices[_heap[0].Key] = 0; // 更新堆顶元素的索引
            BubbleDown(0);
        }

        return topItem;
    }

    /// <summary>
    /// 检查元素是否在队列中
    /// </summary>
    /// <param name="item">要检查的元素</param>
    /// <returns>存在返回 true，否则 false</returns>
    public bool Contains(T item)
    {
        // 利用 Dictionary 实现 O(1) 查找
        return _elementIndices.ContainsKey(item);
    }

    /// <summary>
    /// 更新元素的优先级，并重新调整堆结构
    /// </summary>
    /// <param name="item">要更新的元素</param>
    /// <param name="newPriority">新的优先级</param>
    public void UpdatePriority(T item, int newPriority)
    {
        if (!_elementIndices.TryGetValue(item, out int index))
        {
            Debug.LogWarning($"元素 {item} 不在队列中，无法更新优先级");
            return;
        }

        // 1. 更新元素的优先级
        _heap[index] = new KeyValuePair<T, int>(item, newPriority);

        // 2. 判断需要上浮还是下沉：
        // - 新优先级 < 父节点优先级 → 上浮
        // - 新优先级 > 子节点优先级 → 下沉
        int parentIndex = GetParentIndex(index);
        if (parentIndex >= 0 && _heap[index].Value < _heap[parentIndex].Value)
        {
            BubbleUp(index);
        }
        else
        {
            BubbleDown(index);
        }
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        _heap.Clear();
        _elementIndices.Clear();
    }
    /// <summary>
    /// 查看堆顶元素但不移除（Peek）。
    /// </summary>
    /// <returns>堆顶元素</returns>
    public T Peek()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("队列已空，无法执行 Peek 操作");
        return _heap[0].Key;
    }

    /// <summary>
    /// 查看堆顶元素的优先级（不移除）。
    /// </summary>
    /// <returns>堆顶元素的优先级值</returns>
    public int PeekPriority()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("队列已空，无法执行 PeekPriority 操作");
        return _heap[0].Value;
    }

    #region 私有辅助方法：堆结构调整
    /// <summary>
    /// 上浮调整：从指定索引向上调整堆，确保父节点优先级 ≤ 子节点
    /// </summary>
    private void BubbleUp(int index)
    {
        KeyValuePair<T, int> currentItem = _heap[index];
        int currentPriority = currentItem.Value;

        // 循环：直到到达堆顶，或父节点优先级 ≤ 当前节点优先级
        while (index > 0)
        {
            int parentIndex = GetParentIndex(index);
            KeyValuePair<T, int> parentItem = _heap[parentIndex];

            // 如果父节点优先级 ≤ 当前节点，无需继续上浮
            if (parentItem.Value <= currentPriority)
                break;

            // 交换父节点和当前节点
            _heap[index] = parentItem;
            _elementIndices[parentItem.Key] = index; // 更新父节点的索引

            // 移动到父节点位置，继续循环
            index = parentIndex;
        }

        // 把当前元素放到最终位置，并更新索引
        _heap[index] = currentItem;
        _elementIndices[currentItem.Key] = index;
    }

    /// <summary>
    /// 下沉调整：从指定索引向下调整堆，确保父节点优先级 ≤ 子节点
    /// </summary>
    private void BubbleDown(int index)
    {
        int lastIndex = _heap.Count - 1;
        KeyValuePair<T, int> currentItem = _heap[index];
        int currentPriority = currentItem.Value;

        // 循环：直到没有子节点，或当前节点优先级 ≤ 所有子节点
        while (true)
        {
            // 获取左右子节点的索引
            int leftChildIndex = GetLeftChildIndex(index);
            int rightChildIndex = GetRightChildIndex(index);
            int smallestChildIndex = index; // 记录优先级最小的子节点索引

            // 比较左子节点：如果左子节点存在且优先级更小，更新最小索引
            if (leftChildIndex <= lastIndex && _heap[leftChildIndex].Value < _heap[smallestChildIndex].Value)
            {
                smallestChildIndex = leftChildIndex;
            }

            // 比较右子节点：如果右子节点存在且优先级更小，更新最小索引
            if (rightChildIndex <= lastIndex && _heap[rightChildIndex].Value < _heap[smallestChildIndex].Value)
            {
                smallestChildIndex = rightChildIndex;
            }

            // 如果当前节点就是优先级最小的，无需继续下沉
            if (smallestChildIndex == index)
                break;

            // 交换当前节点和最小子节点
            KeyValuePair<T, int> smallestChildItem = _heap[smallestChildIndex];
            _heap[index] = smallestChildItem;
            _elementIndices[smallestChildItem.Key] = index; // 更新子节点的索引

            // 移动到最小子节点位置，继续循环
            index = smallestChildIndex;
        }

        // 把当前元素放到最终位置，并更新索引
        _heap[index] = currentItem;
        _elementIndices[currentItem.Key] = index;
    }

    /// <summary>
    /// 获取父节点索引
    /// </summary>
    private int GetParentIndex(int childIndex) => (childIndex - 1) / 2;

    /// <summary>
    /// 获取左子节点索引
    /// </summary>
    private int GetLeftChildIndex(int parentIndex) => parentIndex * 2 + 1;

    /// <summary>
    /// 获取右子节点索引
    /// </summary>
    private int GetRightChildIndex(int parentIndex) => parentIndex * 2 + 2;
    #endregion
}