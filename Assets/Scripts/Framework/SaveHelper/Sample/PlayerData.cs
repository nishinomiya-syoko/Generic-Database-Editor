// PlayerData.cs
using UnityEngine;

namespace GameData
{
    [AutoArchive(Version = 2)]  // 标记为自动存档，当前版本2
    public partial class PlayerData
    {
        [Savable] 
        public int Level;
        
        [Savable(Key = "exp", DefaultValue = 0)] 
        private int _experience;
        
        [Savable(Encrypt = true)]  // 敏感数据加密
        public int GemCount;
        
        [Savable]
        public Vector3 LastPosition;
        
        // 非存档字段（不标记）
        public float tempCalculationValue;
        
        // 版本迁移逻辑（手动实现partial方法）
        // partial void MigrateFromOldVersion(ArchiveData data)
        // {
        //     if (data._version == 1)
        //     {
        //         // 从旧版本升级：比如重命名字段
        //         // data.NewField = data.OldField;
        //     }
        // }
    }
}