using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Logic;
using ExcelDataReader.Log;

public class EntityManager : MonoBehaviour
{
    private float m_IntervalTime;
    private float m_TickTime;
    private float m_AccumulatedTime;
    private float m_AccumulatedTickTime;
    private readonly LinkedList<EntityBase> m_Entities = new LinkedList<EntityBase>();
    // private readonly Dictionary<string, EntityBase> _entityDict = new Dictionary<string, EntityBase>();
    private LinkedListNode<EntityBase> m_CachedNode;

    private PoolManager poolManager;

    private void Awake()
    {
        m_IntervalTime = Constant.DEFAULT_DELTA_TIME;
        m_TickTime = Constant.LONG_DELTA_TIME;
    }
    void Start()
    {
        poolManager = PoolManager.Instance;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        m_AccumulatedTickTime += dt;
        m_AccumulatedTime += dt;

        if (m_AccumulatedTime >= m_IntervalTime)
        { 
            OnUpdate();
            m_AccumulatedTime = 0;
        }
        if (m_AccumulatedTickTime >= m_TickTime)
        {
            OnTick();
            m_AccumulatedTickTime = 0;
        }
    }
    /// <summary>
    /// 每0.1秒调用一次 OnUpdate，适合处理一些更新的逻辑，比如攻击、技能效果等。
    /// </summary>
    private void OnUpdate()
    {
        LinkedListNode<EntityBase> current = m_Entities.First;
        while (current != null)
        {
            m_CachedNode = current.Next;
            current.Value.OnUpdate(m_IntervalTime);
            current = m_CachedNode;
            m_CachedNode = null;
        }
    }
    /// <summary>
    /// 每 0.5 秒调用一次 OnTick，适合处理一些不需要每帧更新的逻辑，比如状态效果、技能持续伤害等。  
    /// </summary>
    private void OnTick()
    {
        LinkedListNode<EntityBase> current = m_Entities.First;
        while (current != null)
        {
            m_CachedNode = current.Next;
            current.Value.OnTick(m_TickTime);
            current = m_CachedNode;
            m_CachedNode = null;
        }
    }   
   

    public GameObject ShowEntity(string id)
    {
        return ShowEntity(id, Vector3.zero, Quaternion.identity);
    }
    public GameObject ShowEntity(string id, Vector3 position)
    {
        return ShowEntity(id,position, Quaternion.identity);
    }
    public GameObject ShowEntity(string id, Vector3 position, Quaternion rotation)
    {
        GameObject p = poolManager.Spawn(id, position, rotation);
        EntityBase logic = p.GetComponent<EntityBase>();
        // var logics = p.GetComponents<EntityBase>();
        if(logic != null)  
        {
            logic.OnSpawn();
            m_Entities.AddLast(logic);
        }
       return p;
    }
    public void HideEntity(EntityBase entity)
    {
        if (m_CachedNode != null && m_CachedNode.Value == entity)
        {
            m_CachedNode = m_CachedNode.Next;
        }
        m_Entities.Remove(entity);
        poolManager.Despawn(entity.id, entity.gameObject);
    }
}