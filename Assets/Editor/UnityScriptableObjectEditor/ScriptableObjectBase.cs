using System;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// ScriptableObject 基类，所有可管理的 SO 都需要继承此类
    /// </summary>
    public abstract class ScriptableObjectBase : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] [TextArea(3, 10)] private string _description;
        [SerializeField] private Sprite _icon;
        
        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        
        /// <summary>
        /// 获取 SO 类型名称
        /// </summary>
        public abstract string GetSOType();
        
        /// <summary>
        /// 生成唯一 ID
        /// </summary>
        public void GenerateId()
        {
            _id = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
        
        public void SetId(string id) => _id = id;
        public void SetDisplayName(string name) => _displayName = name;
        public void SetDescription(string desc) => _description = desc;
        public void SetIcon(Sprite icon) => _icon = icon;
        public void SetIcon(string iconPath)
        {
            // _icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }
        
        /// <summary>
        /// 获取可序列化的数据对象
        /// </summary>
        public abstract object GetSerializableData();
        
        /// <summary>
        /// 从序列化数据加载
        /// </summary>
        public abstract void LoadFromSerializableData(object data);
    }
}
