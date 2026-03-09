# Unity ScriptableObject 编辑器

一个功能完整的 Unity 编辑器工具，用于创建、修改、删除不同类型的 ScriptableObject，并支持 JSON 文本保存和运行时加载。

## 功能特性

- ✅ **可视化编辑器窗口** - 直观的 GUI 界面管理所有 ScriptableObject
- ✅ **多类型支持** - 内置 Item、Character、Skill、Quest 四种类型，可轻松扩展
- ✅ **JSON 序列化** - 所有数据保存为 JSON 文本格式，易于版本控制和手动编辑
- ✅ **运行时加载** - 游戏启动时自动加载所有数据到内存
- ✅ **批量编辑** - 支持批量修改多个对象的属性
- ✅ **导入/导出** - 支持数据库的导入和导出
- ✅ **搜索过滤** - 支持按名称、描述搜索

## 文件结构

```
Assets/
├── Scripts/
│   ├── SOEditor/
│   │   ├── ScriptableObjectBase.cs      # SO 基类
│   │   ├── ItemSO.cs                    # 物品 SO
│   │   ├── CharacterSO.cs               # 角色 SO
│   │   ├── SkillSO.cs                   # 技能 SO
│   │   ├── QuestSO.cs                   # 任务 SO
│   │   ├── SODatabase.cs                # JSON 数据库系统
│   │   ├── SORuntimeManager.cs          # 运行时管理器
│   │   └── SOExampleUsage.cs            # 使用示例
│   └── Editor/
│       ├── SOEditorWindow.cs            # 主编辑器窗口
│       └── SOBulkEditor.cs              # 批量编辑器
└── SO_Database/                         # JSON 数据文件夹（自动生成）
    ├── Item/
    ├── Character/
    ├── Skill/
    └── Quest/
```

## 安装步骤

1. 将所有脚本文件复制到 Unity 项目的 `Assets/Scripts` 文件夹
2. 编辑器脚本放在 `Assets/Scripts/Editor` 文件夹
3. 在 Unity 菜单中会出现 `Tools/SO Editor` 选项

## 使用方法

### 1. 打开编辑器

- 点击菜单 `Tools/SO Editor` 或按快捷键 `Ctrl+Shift+E`

### 2. 创建新的 ScriptableObject

1. 在编辑器左侧选择类型（Item/Character/Skill/Quest）
2. 点击 "+ 新建" 按钮
3. 输入名称并点击 "创建"
4. 在列表中找到新创建的对象，点击 "编辑"
5. 修改属性后点击 "保存"

### 3. 编辑 ScriptableObject

1. 在列表中找到要编辑的对象
2. 点击 "编辑" 按钮
3. 修改属性
4. 点击 "保存" 按钮

### 4. 删除 ScriptableObject

1. 在列表中找到要删除的对象
2. 点击 "删除" 按钮
3. 确认删除

### 5. 批量编辑

1. 点击菜单 `Tools/SO Editor/Bulk Editor`
2. 选择要编辑的类型
3. 勾选要修改的项目
4. 选择要修改的字段并输入新值
5. 点击 "应用批量修改"

### 6. 导入/导出数据库

- **导出**: 在主编辑器窗口点击 "导出" 按钮，选择保存文件夹
- **导入**: 在主编辑器窗口点击 "导入" 按钮，选择数据库文件夹

## 运行时加载

### 1. 自动加载

在场景中创建一个空物体，添加 `SORuntimeManager` 组件：

```csharp
// 设置
Load On Start: true      // 启动时自动加载
Dont Destroy On Load: true // 切换场景时不销毁
```

### 2. 手动加载

```csharp
// 获取管理器实例
SORuntimeManager manager = SORuntimeManager.Instance;

// 加载所有数据库
manager.LoadAllDatabases();

// 或加载指定类型
manager.LoadItemDatabase();
manager.LoadCharacterDatabase();
```

### 3. 获取数据

```csharp
// 获取物品
ItemSO item = SORuntimeManager.Instance.GetItem("ITEM001");

// 获取角色
CharacterSO character = SORuntimeManager.Instance.GetCharacter("CHAR001");

// 获取技能
SkillSO skill = SORuntimeManager.Instance.GetSkill("SKILL001");

// 获取任务
QuestSO quest = SORuntimeManager.Instance.GetQuest("QUEST001");
```

### 4. 搜索数据

```csharp
// 搜索物品
List<ItemSO> items = SORuntimeManager.Instance.SearchItems("剑");

// 按类型获取物品
List<ItemSO> weapons = SORuntimeManager.Instance.GetItemsByType(ItemType.Weapon);

// 获取所有物品
List<ItemSO> allItems = SORuntimeManager.Instance.GetAllItems();
```

## 扩展新类型

### 1. 创建新的 SO 类

```csharp
using System;
using UnityEngine;

namespace SOEditor
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "SO Editor/Enemy")]
    public class EnemySO : ScriptableObjectBase
    {
        [Header("敌人属性")]
        [SerializeField] private int _health;
        [SerializeField] private int _damage;
        
        public int Health => _health;
        public int Damage => _damage;
        
        public override string GetSOType() => "Enemy";
        
        public override object GetSerializableData()
        {
            return new EnemyData
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                Health = _health,
                Damage = _damage
            };
        }
        
        public override void LoadFromSerializableData(object data)
        {
            if (data is EnemyData enemyData)
            {
                SetId(enemyData.Id);
                SetDisplayName(enemyData.DisplayName);
                SetDescription(enemyData.Description);
                _health = enemyData.Health;
                _damage = enemyData.Damage;
            }
        }
    }
    
    [Serializable]
    public class EnemyData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public int Health;
        public int Damage;
    }
}
```

### 2. 更新编辑器窗口

在 `SOEditorWindow.cs` 中：

```csharp
// 添加新类型到数组
private readonly string[] _soTypes = new[] { "Item", "Character", "Skill", "Quest", "Enemy" };

// 添加到类型映射
private readonly Dictionary<string, Type> _soTypeMap = new Dictionary<string, Type>
{
    { "Item", typeof(ItemSO) },
    { "Character", typeof(CharacterSO) },
    { "Skill", typeof(SkillSO) },
    { "Quest", typeof(QuestSO) },
    { "Enemy", typeof(EnemySO) }
};
```

### 3. 更新运行时管理器

在 `SORuntimeManager.cs` 中添加：

```csharp
private Dictionary<string, EnemySO> _enemies = new Dictionary<string, EnemySO>();

public void LoadEnemyDatabase()
{
    _enemies.Clear();
    List<EnemyData> enemyDataList = SODatabase.LoadAllData<EnemyData>("Enemy");
    
    foreach (EnemyData data in enemyDataList)
    {
        EnemySO enemy = ScriptableObject.CreateInstance<EnemySO>();
        enemy.LoadFromSerializableData(data);
        _enemies[enemy.Id] = enemy;
    }
}

public EnemySO GetEnemy(string id)
{
    _enemies.TryGetValue(id, out EnemySO enemy);
    return enemy;
}
```

## 数据文件位置

### 编辑器模式
```
项目根目录/SO_Database/
├── Item/
├── Character/
├── Skill/
└── Quest/
```

### 运行时模式
```
Application.persistentDataPath/SO_Database/
```

## JSON 文件格式示例

### Item 示例
```json
{
    "Id": "A1B2C3D4",
    "DisplayName": "铁剑",
    "Description": "一把普通的铁剑",
    "IconPath": "IronSword",
    "ItemType": 0,
    "MaxStackSize": 1,
    "BuyPrice": 100,
    "SellPrice": 50,
    "IsUsable": false,
    "IsEquipable": true,
    "Rarity": 1,
    "AttackBonus": 10,
    "DefenseBonus": 0,
    "HealthBonus": 0,
    "ManaBonus": 0
}
```

## 快捷键

- `Ctrl+Shift+E` - 打开 SO Editor

## 注意事项

1. **ID 生成**: 每个 SO 都有唯一的 8 位 ID，由系统自动生成
2. **图标引用**: JSON 中只保存图标名称，运行时需要在 Resources 文件夹中放置对应名称的 Sprite
3. **数据备份**: 建议定期导出数据库进行备份
4. **版本控制**: JSON 文件可以直接加入版本控制

## 许可证

MIT License - 可自由使用和修改
