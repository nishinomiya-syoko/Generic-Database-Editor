using System;

/// <summary>
/// 标记该特性的普通类可被通用编辑器识别并编辑
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class EditableDataAttribute : Attribute
{
    /// <summary>
    /// 编辑器中显示的名称（可选）
    /// </summary>
    public string DisplayName { get; set; }

    public EditableDataAttribute(string displayName = "")
    {
        DisplayName = string.IsNullOrEmpty(displayName) ? "" : displayName;
    }
}