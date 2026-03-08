using UnityEngine;

public class EntityRoot : MonoBehaviour
{
    /// <summary>
    /// 
    /// </summary>
    public Transform root;
    [Tooltip("头部")]
    public Transform head;
    [Tooltip("头部子节点")]
    public Transform head_vertical;
    [Tooltip("炮管")]
    public Transform barrel;
    [Tooltip("弹口")]
    public Transform muzzle;

}