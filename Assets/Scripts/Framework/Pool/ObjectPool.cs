using System.Collections.Generic;
using UnityEngine;

namespace Logic
{

    /// <summary>
    /// 泛型对象池：
    /// - T 必须是 MonoBehaviour 且实现 IReference（建议继承 EntityBase）
    /// - 支持预热（initialSize）、最大数量（maxSize）和是否允许扩张（expandIfEmpty）
    /// </summary>
    public interface IPoolWrapper
    {
        GameObject Spawn(Vector3 position, Quaternion rotation);
        bool Despawn(GameObject instance);
        void Clear();
    }

    public class ObjectPool<T> : IPoolWrapper where T : MonoBehaviour, IReference
    {
        private readonly Queue<T> _pool = new Queue<T>();

        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly bool _expandIfEmpty;
        private readonly int _maxSize;          // <=0 表示不限制
        private int _totalCount;               // 已实例化总数（包括在用和在池里的）

        public ObjectPool(T prefab, int initialSize, Transform parent, bool expandIfEmpty, int maxSize)
        {
            _prefab = prefab.gameObject;
            _parent = parent;
            _expandIfEmpty = expandIfEmpty;
            _maxSize = maxSize;

            Prewarm(initialSize);
        }

        /// <summary>
        /// 预创建一定数量的对象（放入池中并禁用）。
        /// </summary>
        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(_prefab, _parent).GetComponent<T>();
                if (!instance) continue;

                instance.gameObject.SetActive(false);
                _pool.Enqueue(instance);
                _totalCount++;
            }
        }

        /// <summary>
        /// 从池中取出对象（带位置/旋转）。
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T entity = null;

            if (_pool.Count > 0)
            {
                entity = _pool.Dequeue();
            }
            else
            {
                // 池空了，看是否允许创建新实例
                if (!_expandIfEmpty)
                    return null;

                if (_maxSize > 0 && _totalCount >= _maxSize)
                    return null;

                entity = Object.Instantiate(_prefab, _parent).GetComponent<T>();
                if (entity != null)
                    _totalCount++;
            }

            if (!entity) return null;

            var tr = entity.transform;
            tr.position = position;
            tr.rotation = rotation;

            entity.gameObject.SetActive(true);
            entity.OnSpawn();

            return entity;
        }

        /// <summary>
        /// 无参重载，使用默认位置与旋转。
        /// </summary>
        public T Get()
        {
            return Get(Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// 回收对象。
        /// </summary>
        public void Release(T entity)
        {
            if (!entity) return;

            entity.OnRecycle();
            entity.gameObject.SetActive(false);
            _pool.Enqueue(entity);
        }

        /// <summary>
        /// 清空池中所有对象并销毁。
        /// </summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var e = _pool.Dequeue();
                if (e)
                    Object.Destroy(e.gameObject);
            }
        }

        // ----------------- IPoolWrapper 显式实现 -----------------

        GameObject IPoolWrapper.Spawn(Vector3 position, Quaternion rotation)
        {
            var e = Get(position, rotation);
            return e ? e.gameObject : null;
        }

        bool IPoolWrapper.Despawn(GameObject instance)
        {
            if (!instance) return false;

            var comp = instance.GetComponent<T>();
            if (!comp) return false;

            Release(comp);
            return true;
        }

        void IPoolWrapper.Clear()
        {
            Clear();
        }
    }
}