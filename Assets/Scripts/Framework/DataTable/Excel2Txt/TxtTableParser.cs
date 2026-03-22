using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Collections;

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

    #region 新增：集合解析分隔符常量（统一维护）
    /// <summary>集合元素分隔符</summary>
    private const string ELEMENT_SEPARATOR = "|";
    /// <summary>字典键值对分隔符</summary>
    private const string KEY_VALUE_SEPARATOR = "=";
    #endregion

    /// <summary>
    /// 增强版类型转换：支持基础类型/Unity类型/枚举/Dictionary/Array/List
    /// </summary>
    private static object ConvertValueToType(string value, string targetType)
    {
        if (string.IsNullOrEmpty(value))
            return GetDefaultValue(targetType);
        if (value.Trim().StartsWith("//") || value.Trim().StartsWith("#"))
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

            // 2. 处理Unity类型
            switch (targetType.ToLower())
            {
                case "vector2": return ParseVector2(value);
                case "vector2int": return ParseVector2Int(value);
                case "vector3": return ParseVector3(value);
                case "vector3int": return ParseVector3Int(value);
                case "vector4": return ParseVector4(value);
            }

            // 3. 新增：处理字典类型 Dictionary<,>
            if (targetType.StartsWith("Dictionary<", StringComparison.OrdinalIgnoreCase) && targetType.EndsWith(">"))
            {
                return ParseDictionary(value, targetType);
            }

            // 4. 新增：处理数组类型（如string[]/int[]）
            if (targetType.EndsWith("[]", StringComparison.OrdinalIgnoreCase))
            {
                return ParseArray(value, targetType);
            }

            // 5. 新增：处理List类型 List<*>
            if (targetType.StartsWith("List<", StringComparison.OrdinalIgnoreCase) && targetType.EndsWith(">"))
            {
                return ParseList(value, targetType);
            }

            // 6. 基础类型
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

    #region 新增：集合解析私有方法（Dictionary/Array/List）
    /// <summary>解析Dictionary类型</summary>
    private static object ParseDictionary(string value, string targetTypeStr)
    {
        // 通过CSharpTypeToString获取真实的Dictionary类型
        Type dictType = CSharpTypeToString.GetCSharpType(targetTypeStr);
        if (!dictType.IsGenericType || dictType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return GetDefaultValue(targetTypeStr);

        // 获取字典的键、值类型
        Type keyType = dictType.GetGenericArguments()[0];
        Type valueType = dictType.GetGenericArguments()[1];
        // 创建空字典实例
        IDictionary dict = (IDictionary)Activator.CreateInstance(dictType);
        // 分割键值对
        string[] keyValuePairs = value.Split(new[] { ELEMENT_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in keyValuePairs)
        {
            string[] kv = pair.Split(new[] { KEY_VALUE_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2)
            {
                Debug.LogWarning($"字典键值对格式错误：{pair}，需符合 键=值 格式");
                continue;
            }
            // 转换键、值为目标类型
            object key = Convert.ChangeType(kv[0].Trim(), keyType);
            object val = Convert.ChangeType(kv[1].Trim(), valueType);
            dict.Add(key, val);
        }
        return dict;
    }

    /// <summary>解析数组类型</summary>
    private static object ParseArray(string value, string targetTypeStr)
    {
        // 获取数组的元素类型
        Type elementType = CSharpTypeToString.GetCSharpType(targetTypeStr.Substring(0, targetTypeStr.Length - 2));
        // 分割元素
        string[] elements = value.Split(new[] { ELEMENT_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
        // 创建数组并赋值
        Array array = Array.CreateInstance(elementType, elements.Length);
        for (int i = 0; i < elements.Length; i++)
        {
            object elem = Convert.ChangeType(elements[i].Trim(), elementType);
            array.SetValue(elem, i);
        }
        return array;
    }

    /// <summary>解析List类型（复用数组解析逻辑）</summary>
    private static object ParseList(string value, string targetTypeStr)
    {
        // 获取List的元素类型
        Type listType = CSharpTypeToString.GetCSharpType(targetTypeStr);
        Type elementType = listType.GetGenericArguments()[0];
        // 先解析为数组，再转换为List
        Array array = ParseArray(value, $"{elementType.Name}[]") as Array;
        if (array == null) return Activator.CreateInstance(listType);
        // 通过反射调用List.AddRange方法添加元素
        IList list = (IList)Activator.CreateInstance(listType);
        foreach (var item in array)
        {
            list.Add(item);
        }
        return list;
    }
    #endregion

    #region 抽离：Unity向量解析方法（简化代码）
    private static Vector2 ParseVector2(string value)
    {
        string clean = value.Trim().Trim('(', ')');
        string[] v2 = clean.Split(',');
        return new Vector2(float.Parse(v2[0]), float.Parse(v2[1]));
    }

    private static Vector2Int ParseVector2Int(string value)
    {
        string clean = value.Trim().Trim('(', ')');
        string[] v2 = clean.Split(',');
        return new Vector2Int(int.Parse(v2[0]), int.Parse(v2[1]));
    }

    private static Vector3 ParseVector3(string value)
    {
        string clean = value.Trim().Trim('(', ')');
        string[] v3 = clean.Split(',');
        return new Vector3(float.Parse(v3[0]), float.Parse(v3[1]), float.Parse(v3[2]));
    }

    private static Vector3Int ParseVector3Int(string value)
    {
        string clean = value.Trim().Trim('(', ')');
        string[] v3 = clean.Split(',');
        return new Vector3Int(int.Parse(v3[0]), int.Parse(v3[1]), int.Parse(v3[2]));
    }

    private static Vector4 ParseVector4(string value)
    {
        string clean = value.Trim().Trim('(', ')');
        string[] v4 = clean.Split(',');
        return new Vector4(float.Parse(v4[0]), float.Parse(v4[1]), float.Parse(v4[2]), float.Parse(v4[3]));
    }
    #endregion

    /// <summary>
    /// 增强版默认值获取：支持枚举/Unity类型/Dictionary/Array/List
    /// </summary>
    private static object GetDefaultValue(string type)
    {
        if (string.IsNullOrWhiteSpace(type) || type.Trim().StartsWith("//") || type.Trim().StartsWith("#"))
            return null;
        type = type.Trim();

        // 1. 枚举类型
        Type enumType = AllEnumTypes.Find(t =>
            string.Equals(t.Name, type, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.FullName, type, StringComparison.OrdinalIgnoreCase));
        if (enumType != null && enumType.IsEnum)
        {
            Array values = Enum.GetValues(enumType);
            return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(enumType);
        }

        // 2. Unity类型
        switch (type.ToLower())
        {
            case "vector2": return Vector2.zero;
            case "vector2int": return Vector2Int.zero;
            case "vector3": return Vector3.zero;
            case "vector3int": return Vector3Int.zero;
            case "vector4": return Vector4.zero;
            case "color": return Color.clear;
            case "rect": return Rect.zero;
            case "quaternion": return Quaternion.identity;
        }

        // 3. 新增：集合类型（Dictionary/Array/List）返回空实例
        try
        {
            Type targetType = CSharpTypeToString.GetCSharpType(type);
            if (targetType != null)
            {
                // 数组：返回空数组（长度0）
                if (targetType.IsArray) return Array.CreateInstance(targetType.GetElementType(), 0);
                // 泛型集合（Dictionary/List）：返回空实例
                if (targetType.IsGenericType) return Activator.CreateInstance(targetType);
            }
        }
        catch
        {
            // 类型解析失败则跳过，走后续基础类型逻辑
        }

        // 4. 基础类型
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

    // 优化：注释处理仅针对整行/整列注释，不破坏集合内的字符
    private static List<string> SplitLineByTab(string line)
    {
        List<string> parts = new List<string>();
        string[] splitParts = line.Split(new[] { '\t' }, StringSplitOptions.None);
        parts.AddRange(splitParts);
        for (int i = 0; i < parts.Count; i++)
        {
            // 仅当单元格内容**整行**是注释时，才置空（避免集合内有//被误判）
            string val = parts[i].Trim();
            if (val.StartsWith("//") || val.StartsWith("#"))
                parts[i] = string.Empty;
        }
        return parts;
    }

    // 原有ParseTableHeader、ParseTableData方法**无需修改**
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
                string line = allLines[i];
                if (string.IsNullOrEmpty(line)) continue;
                // if(line.StartsWith("//")||line.StartsWith("#")) continue;
                List<string> cellValues = SplitLineByTab(line);
                Dictionary<string, object> dataDict = new Dictionary<string, object>();

                // 逐列转换数据类型
                for (int j = 1; j < fieldNames.Count; j++)
                {
                    string fieldName = fieldNames[j];
                    string fieldType = fieldTypes[j];
                    Debug.Log($"fieldName:{fieldName} fieldType:{fieldType} cellValue:{cellValues[j]} 行列：{i},{j}");

                    if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("//") || fieldName.StartsWith("#"))
                        continue;
                    if (string.IsNullOrEmpty(fieldType) || fieldType.StartsWith("//") || fieldType.StartsWith("#"))
                        continue;
                    if (cellValues[0].StartsWith("//") || cellValues[0].StartsWith("#"))
                        continue;
                    // string cellValue = j < cellValues.Count ? cellValues[j].Trim() : string.Empty;

                    object value = ConvertValueToType(cellValues[j], fieldType);
                    // Debug.Log($"字段名：{fieldName} 字段类型：{fieldType} 值：{value} 行：{i} 列：{j}");
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