# 通用数据编辑器 (General Editor)

## 概述

通用数据编辑器是一个 Unity Editor 工具，用于可视化编辑和管理游戏配置数据。支持数据的增删改查、Excel 导入导出等功能。

## 如何创建可编辑数据类

### 1. 添加 `[EditableData]` 特性

数据类需要添加 `[EditableData]` 特性，编辑器会自动扫描并识别这些类：

```csharp
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Top
{
    [EditableData]  // 必须添加此特性
    public class AchievementData
    {
        [Header("基本信息")]
        public string id;
        public string achievementName;
        public string description;
        public Sprite icon;
        public int points;

        [Header("解锁条件")]
        public QuestObjectiveType unlockType;
        public string targetId;
        public int requiredAmount;

        [Header("奖励")]
        public QuestReward rewards;
        public string unlockTitle;
        public string unlockBadge;
    }
}
```

### 2. 特性说明

- `[EditableData]`：标记该类可被通用编辑器识别
  - 可选参数 `DisplayName`：在编辑器中显示的名称
  
  ```csharp
  [EditableData("成就数据")]
  public class AchievementData { }
  ```

### 3. 支持的字段类型

编辑器支持以下类型的字段编辑：

- **基础类型**：`int`, `float`, `string`, `bool`, `double`, `long` 等
- **Unity 对象引用**：`Sprite`, `GameObject`, `Texture` 等
- **枚举类型**：自动识别并以下拉框显示
- **集合类型**：`Array`, `List<T>`, `Dictionary<K, V>`
- **自定义类/结构体**：需标记 `[Serializable]`，支持递归编辑，未实现序列化对象的转换，建议使用string，int 等字段存储

## 数据路径

### JSON 数据存储路径
导出文件命名格式：`{数据类型}.xlsx`

例如：
- `AchievementData.xlsx`
- `SkillData.xlsx`
- `LevelData.xlsx`

## 使用方式

### 打开编辑器

在 Unity 菜单栏中打开编辑器窗口（具体菜单路径请查看项目配置）。

### 基本操作

1. **选择数据类型**：在左侧面板选择要编辑的数据类型
2. **实例管理**：
   - 新建实例：输入实例名称，点击新建
   - 选择实例：从列表中选择已有实例
   - 删除实例：选中后点击删除
   - 搜索实例：使用搜索框过滤
3. **编辑数据**：在右侧面板编辑字段值
4. **保存数据**：点击保存按钮，数据将保存为 JSON 文件

### Excel 导入导出

- **导出到 Excel**：将当前数据类型的所有实例导出为一个 Excel 文件
- **从 Excel 导入**：从 Excel 文件导入数据，覆盖现有实例

#### Excel 文件格式

导出的 Excel 文件格式如下：

| 行号 | 内容 |
|------|------|
| 第1行 | 实例名 \| 字段1名 \| 字段2名 \| 字段3名 ... |
| 第2行 | string \| 字段1类型 \| 字段2类型 \| ... |
| 第3行 | 注释 \| 字段1注释 \| 字段2注释 \| ... |
| 第4行起 | 实例1数据 \| 值1 \| 值2 \| ... |

## 注意事项

1. 数据类必须添加 `[EditableData]` 特性才能被编辑器识别
2. 数据类需要放在编辑器可扫描的程序集中
3. Excel 导出会覆盖同名的已有文件，请注意备份
4. 复杂嵌套结构支持最大 8 层深度
5. 修改数据后记得点击保存，避免数据丢失