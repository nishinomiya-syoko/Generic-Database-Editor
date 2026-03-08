// SavableAttribute.cs
using System;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SavableAttribute : Attribute 
{
    public string Key { get; set; }        // 自定义存档键名（可选）
    public object DefaultValue { get; set; } // 默认值（可选）
    public bool Encrypt { get; set; }      // 是否加密存储
}

// 标记类需要生成存档代码
[AttributeUsage(AttributeTargets.Class)]
public class AutoArchiveAttribute : Attribute 
{
    public int Version { get; set; } = 1;  // 存档版本号，用于迁移
}