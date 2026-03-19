using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Linq;

public static class TxtTableParser
{
    // 缓存项目中所有枚举类型
    private static List<Type> _allEnumTypes;
    private static List<Type> AllEnumTypes
    {
        get
        {
            if (_allEnumTypes == null)
                _allEnumTypes = CSharpTypeToString.GetAllEnumTypes();
            return _allEnumTypes;
        }
    }

    // 原有ParseTableHeader、ParseTableData方法保留，修改以下核心方法：

    /// <summary>
    /// 增强版类型转换：支持基础类型/Unity类型/枚举
    /// </summary>
    private static object ConvertValueToType(string value, string targetType)
{
    if (string.IsNullOrEmpty(value))
        return GetDefaultValue(targetType);

    if (value.StartsWith("//") || value.StartsWith("#"))
        throw new Exception($"注释行：{value}");

    try
    {
        // 先去掉空格
        targetType = targetType?.Trim();
        value = value?.Trim();

        // 1. 处理枚举类型
        Type enumType = AllEnumTypes.Find(t =>
            string.Equals(t.Name, targetType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.FullName, targetType, StringComparison.OrdinalIgnoreCase));

        if (enumType != null && enumType.IsEnum)
        {
            if (int.TryParse(value, out int enumInt))
                return Enum.ToObject(enumType, enumInt);

            return Enum.Parse(enumType, value, true);
        }

            // 2. Unity类型
            switch (targetType.ToLower())
            {
                case "vector2":
                    {
                        string clean = value.Trim().Trim('(', ')');
                        string[] v2 = clean.Split(',');
                        return new Vector2(float.Parse(v2[0]), float.Parse(v2[1]));
                    }
                case "vector2int":
                    {
                        string clean = value.Trim().Trim('(', ')');
                        string[] v2 = clean.Split(',');
                        return new Vector2Int(int.Parse(v2[0]), int.Parse(v2[1]));
                    }
                case "vector3":
                    {
                        string clean = value.Trim().Trim('(', ')');
                        string[] v3 = clean.Split(',');
                        return new Vector3(float.Parse(v3[0]), float.Parse(v3[1]), float.Parse(v3[2]));
                    }
                case "vector3int":
                    {
                        string clean = value.Trim().Trim('(', ')');
                        string[] v3 = clean.Split(',');
                        return new Vector3Int(int.Parse(v3[0]), int.Parse(v3[1]), int.Parse(v3[2]));
                    }
                case "vector4":
                    {
                        string clean = value.Trim().Trim('(', ')');
                        string[] v4 = clean.Split(',');
                        return new Vector4(float.Parse(v4[0]), float.Parse(v4[1]), float.Parse(v4[2]), float.Parse(v4[3]));
                    }
            }

        // 3. 基础类型
        switch (targetType.ToLower())
        {
            case "int": return int.Parse(value);
            case "long": return long.Parse(value);
            case "float": return float.Parse(value);
            case "double": return double.Parse(value);
            case "bool": return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
            case "string": return value;
            default: return value;
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"类型转换失败：值={value}，目标类型={targetType}，错误：{e.Message}");
        return GetDefaultValue(targetType);
    }
}
    // private static object ConvertValueToType(string value, string targetType)
    // {
    //     if (string.IsNullOrEmpty(value))
    //         return GetDefaultValue(targetType);
    //     if (value.StartsWith("//") || value.StartsWith("#"))
    //     {
    //         throw new Exception($"注释行：{value}");
    //     }
    //     if (value == string.Empty)
    //         return GetDefaultValue(targetType);

    //     try
    //     {
    //         // 1. 处理枚举类型
    //         if (CSharpTypeToString.IsEnumType(targetType, AllEnumTypes))
    //         {
    //             Type enumType = AllEnumTypes.Find(t => t.Name.Equals(targetType, StringComparison.OrdinalIgnoreCase));
    //             // 支持数值转枚举（如"0"）或字面量转枚举（如"Warrior"）
    //             if (int.TryParse(value, out int enumInt))
    //                 return Enum.ToObject(enumType, enumInt);
    //             else
    //                 return Enum.Parse(enumType, value, true); // true=忽略大小写
    //         }

    //         // 2. 处理Unity常用结构体
    //         switch (targetType.ToLower())
    //         {
    //             case "vector2":
    //                 string[] v2 = value.Split(',');
    //                 return new Vector2(float.Parse(v2[0]), float.Parse(v2[1]));
    //             case "vector3":
    //                 string[] v3 = value.Split(',');
    //                 return new Vector3(float.Parse(v3[0]), float.Parse(v3[1]), float.Parse(v3[2]));
    //             case "vector4":
    //                 string[] v4 = value.Split(',');
    //                 return new Vector4(float.Parse(v4[0]), float.Parse(v4[1]), float.Parse(v4[2]), float.Parse(v4[3]));
    //             case "color":
    //                 string[] color = value.Split(',');
    //                 // 支持RGBA（0-255）或0-1浮点数
    //                 float r = float.Parse(color[0]) > 1 ? float.Parse(color[0]) / 255f : float.Parse(color[0]);
    //                 float g = float.Parse(color[1]) > 1 ? float.Parse(color[1]) / 255f : float.Parse(color[1]);
    //                 float b = float.Parse(color[2]) > 1 ? float.Parse(color[2]) / 255f : float.Parse(color[2]);
    //                 float a = color.Length >= 4 ? (float.Parse(color[3]) > 1 ? float.Parse(color[3]) / 255f : float.Parse(color[3])) : 1f;
    //                 return new Color(r, g, b, a);
    //             case "rect":
    //                 string[] rect = value.Split(',');
    //                 return new Rect(float.Parse(rect[0]), float.Parse(rect[1]), float.Parse(rect[2]), float.Parse(rect[3]));
    //             case "quaternion":
    //                 string[] quat = value.Split(',');
    //                 return new Quaternion(float.Parse(quat[0]), float.Parse(quat[1]), float.Parse(quat[2]), float.Parse(quat[3]));
    //         }

    //         // 3. 处理基础数值类型
    //         switch (targetType.ToLower())
    //         {
    //             case "int": return int.Parse(value);
    //             case "long": return long.Parse(value);
    //             case "float": return float.Parse(value);
    //             case "double": return double.Parse(value);
    //             case "bool":
    //                 {
    //                     if (value.ToLower() == "true" || value == "1")
    //                         return true;
    //                     return false;
    //                 }
    //             case "string": return value;
    //             default: return value; // 自定义类型先返回字符串，由上层处理
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogError($"类型转换失败：值={value}，目标类型={targetType}，错误：{e.Message}");
    //         return GetDefaultValue(targetType);
    //     }
    // }

    /// <summary>
    /// 增强版默认值获取：支持枚举/Unity类型
    /// </summary>
    private static object GetDefaultValue(string type)
    {
        if (string.IsNullOrWhiteSpace(type) || type.StartsWith("//") || type.StartsWith("#"))
            return null;

        type = type.Trim();

        Type enumType = AllEnumTypes.Find(t =>
            string.Equals(t.Name, type, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.FullName, type, StringComparison.OrdinalIgnoreCase));

        if (enumType != null && enumType.IsEnum)
        {
            Array values = Enum.GetValues(enumType);
            return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(enumType);
        }

        switch (type.ToLower())
        {
            case "vector2": return Vector2.zero;
            case "vector3": return Vector3.zero;
            case "vector4": return Vector4.zero;
            case "color": return Color.clear;
            case "rect": return Rect.zero;
            case "quaternion": return Quaternion.identity;
        }

        switch (type.ToLower())
        {
            case "int": return 0;
            case "long": return 0L;
            case "float": return 0f;
            case "double": return 0d;
            case "bool": return false;
            case "string": return string.Empty;
            default: return null;
        }
    }
    // private static object GetDefaultValue(string type)
    // {

    //     if (type.StartsWith("//") || type.StartsWith("#") || type == string.Empty)
    //         return null;
    //     // 处理枚举
    //     if (CSharpTypeToString.IsEnumType(type, AllEnumTypes))
    //     {
    //         Type enumType = AllEnumTypes.Find(t => t.Name.Equals(type, StringComparison.OrdinalIgnoreCase));
    //         return Enum.GetValues(enumType).GetValue(0); // 返回第一个枚举值
    //     }

    //     // 处理Unity类型
    //     switch (type.ToLower())
    //     {
    //         case "vector2": return Vector2.zero;
    //         case "vector3": return Vector3.zero;
    //         case "vector4": return Vector4.zero;
    //         case "color": return Color.clear;
    //         case "rect": return Rect.zero;
    //         case "quaternion": return Quaternion.identity;
    //     }

    //     // 基础类型
    //     switch (type.ToLower())
    //     {
    //         case "int": return 0;
    //         case "long": return 0L;
    //         case "float": return 0f;
    //         case "double": return 0d;
    //         case "bool": return false;
    //         default: return string.Empty;
    //     }
    // }
    

    // 原有SplitLineByTab方法保留
    private static List<string> SplitLineByTab(string line)
    {
        List<string> parts = new List<string>();
        string[] splitParts = line.Split(new[] { '\t' }, StringSplitOptions.None);
        parts.AddRange(splitParts);
        for (int i = 0; i < parts.Count; i++)
        {
            if(parts[i].StartsWith("//") || parts[i].StartsWith("#"))
            parts[i] = string.Empty;
        }
        return parts;
    }

    // 原有ParseTableHeader、ParseTableData方法保留（无需修改）
    public static bool ParseTableHeader(string txtPath, out List<string> fieldNames, out List<string> fieldTypes, out List<string> fieldComments)
    {
        fieldNames = new List<string>();
        fieldTypes = new List<string>();
        fieldComments = new List<string>();

        if (!File.Exists(txtPath))
        {
            Debug.LogError($"TXT文件不存在：{txtPath}");
            return false;
        }

        try
        {
            string[] allLines = File.ReadAllLines(txtPath, Encoding.UTF8);
            
            if (allLines.Length < 3)
            {
                Debug.LogError("TXT数据表表头不完整（至少需要3行：变量名、类型、注释）");
                return false;
            }

            fieldNames = SplitLineByTab(allLines[0]);
            fieldTypes = SplitLineByTab(allLines[1]);
            fieldComments = SplitLineByTab(allLines[2]);

            // fieldNames.RemoveAll(s => s == string.Empty || s.StartsWith("//") || s.StartsWith("#"));
            // fieldTypes.RemoveAll(s => s == string.Empty || s.StartsWith("//") || s.StartsWith("#"));
            // fieldComments.RemoveAll(s => s == string.Empty || s.StartsWith("//") || s.StartsWith("#"));

            if (fieldNames.Count != fieldTypes.Count || fieldNames.Count != fieldComments.Count)
            {
                Debug.LogError("表头行的列数不一致，请检查TXT文件");
                return false;
            }

            if (!fieldNames.Contains("ID"))
            {
                Debug.LogWarning("数据表未包含ID字段，建议添加ID作为唯一标识");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"解析表头失败：{e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public static List<Dictionary<string, object>> ParseTableData(string txtPath, List<string> fieldNames, List<string> fieldTypes)
    {
        List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();

        if (!File.Exists(txtPath))
        {
            Debug.LogError($"TXT文件不存在：{txtPath}");
            return dataList;
        }

        try
        {
            string[] allLines = File.ReadAllLines(txtPath, Encoding.UTF8);
            
            for (int i = 3; i < allLines.Length; i++)
            {
                string line = allLines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                List<string> cellValues = SplitLineByTab(line);
                Dictionary<string, object> dataDict = new Dictionary<string, object>();
                
                // 逐列转换数据类型
                for (int j = 0; j < fieldNames.Count; j++)
                {
                    string fieldName = fieldNames[j];
                    string fieldType = fieldTypes[j];

                    if (fieldName.StartsWith("//") || fieldName.StartsWith("#") || fieldName == string.Empty)
                        continue;
                    if (fieldType.StartsWith("//") || fieldType.StartsWith("#") || fieldType == string.Empty)
                        continue;

                    string cellValue = j < cellValues.Count ? cellValues[j].Trim() : string.Empty;
                    if (fieldName == string.Empty || fieldType == string.Empty)
                        continue;

                    object value = ConvertValueToType(cellValue, fieldType);
                    Debug.Log($"字段名：{fieldName} 字段类型：{fieldType} 值：{value}");
                    dataDict.Add(fieldName, value);
                }

                dataList.Add(dataDict);
            }

            Debug.Log($"解析数据完成，共加载{dataList.Count}行数据");
            return dataList;
        }
        catch (Exception e)
        {
            Debug.LogError($"解析数据行失败：{e.Message}\n{e.StackTrace}");
            return dataList;
        }
    }
}