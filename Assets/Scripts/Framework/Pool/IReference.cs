using UnityEngine;
public interface IReference
{
    /// <summary>
    /// 回收
    /// </summary>
    void OnRecycle();
    /// <summary>
    /// 创建
    /// </summary>
    void OnSpawn();
}