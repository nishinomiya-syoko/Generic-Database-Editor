using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using System.Text;

namespace Top
{
    /// <summary>
    /// Excel 工具：同一类所有数据 → 同一个 Sheet，一行 = 一条实例
    /// 格式：
    /// 行1：实例名 | 字段1名 | 字段2名 | 字段3名 ...
    /// 行2：string  | 字段1类型| 字段2类型| ...
    /// 行3：注释    | 字段1注释| 字段2注释| ...
    /// 行4~：实例1  | 值1 | 值2 | ...
    /// 新增支持：Array、List<T>、Dictionary<K,V> 类型的导出/导入
    /// </summary>
    public static class ExcelDataUtility
    {
        public static readonly string EXCEL_SEPARATOR = Constant.EXCEL_SEPARATOR;

        // 初始化EPPlus授权上下文（必须配置，否则报错）
        static ExcelDataUtility()
        {
            // ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // 非商用授权，商用需替换为Commercial
        }

        #region 核心：导出一个类的所有实例 → 同一个Sheet，一行一条数据
        public static bool ExportAllInstancesToExcel<T>(string excelPath) where T : class, new()
        {
            try
            {
                Type dataType = typeof(T);
                string[] instanceNames = GenericDataPersistence.GetAllInstanceNames<T>();

                if (instanceNames == null || instanceNames.Length == 0)
                {
                    Debug.LogWarning("没有可导出的实例");
                    return false;
                }

                // 拿到所有可序列化字段
                var fields = GetSerializableFields(dataType);

                using (var package = new ExcelPackage())
                {
                    // 只建一个 Sheet，用类名命名
                    var ws = package.Workbook.Worksheets.Add(dataType.Name);

                    // ========== 1. 表头行：实例名 | 字段名1 | 字段名2 ... ==========
                    ws.Cells[1, 1].Value = "#实例名";
                    for (int c = 0; c < fields.Count; c++)
                    {
                        ws.Cells[1, 2 + c].Value = fields[c].Name;
                    }

                    // ========== 2. 类型行 ==========
                    ws.Cells[2, 1].Value = "#类型";
                    for (int c = 0; c < fields.Count; c++)
                    {
                        ws.Cells[2, 2 + c].Value = GetFriendlyTypeName(fields[c].FieldType);
                    }

                    // ========== 3. 注释行 ==========
                    ws.Cells[3, 1].Value = "#注释";
                    for (int c = 0; c < fields.Count; c++)
                    {
                        ws.Cells[3, 2 + c].Value = fields[c].GetCustomAttribute<HeaderAttribute>()?.header ?? "";
                    }

                    // ========== 4. 数据行：一行 = 一个实例 ==========
                    for (int rowIdx = 0; rowIdx < instanceNames.Length; rowIdx++)
                    {
                        string instName = instanceNames[rowIdx];
                        var data = GenericDataPersistence.LoadData<T>(instName);

                        // 第1列：实例名
                        ws.Cells[4 + rowIdx, 1].Value = instName;

                        // 第2列开始：字段值
                        for (int colIdx = 0; colIdx < fields.Count; colIdx++)
                        {
                            object value = fields[colIdx].GetValue(data);
                            ws.Cells[4 + rowIdx, 2 + colIdx].Value = ConvertValueToExcelCompatible(value);
                        }
                    }

                    // 自动适配列宽（可选，提升可读性）
                    // ws.Cells.AutoFitColumns();

                    // 保存
                    var dir = Path.GetDirectoryName(excelPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    package.SaveAs(new FileInfo(excelPath));
                }

                AssetDatabase.Refresh();
                Debug.Log($"导出成功：{excelPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"导出失败：{e}");
                return false;
            }
        }
        #endregion

        #region 核心：从一个Sheet导入所有行 → 所有实例
        public static bool ImportAllInstancesFromExcel<T>(string excelPath) where T : class, new()
        {
            try
            {
                if (!File.Exists(excelPath))
                {
                    Debug.LogError("文件不存在");
                    return false;
                }

                Type dataType = typeof(T);

                using (var package = new ExcelPackage(new FileInfo(excelPath)))
                {
                    var ws = package.Workbook.Worksheets.First(); // 只认第一个Sheet
                    if (ws == null || ws.Dimension == null)
                    {
                        Debug.LogError("Excel工作表为空或无数据");
                        return false;
                    }

                    // 读取表头：第1行是字段名
                    Dictionary<string, int> fieldNameToCol = new Dictionary<string, int>();
                    for (int c = 1; c <= ws.Dimension.End.Column; c++)
                    {
                        string name = ws.Cells[1, c].Text.Trim();
                        if (!string.IsNullOrEmpty(name))
                            fieldNameToCol[name] = c;
                    }

                    // 检查Sheet名称与类型匹配
                    if (dataType.Name != ws.Name)
                    {
                        Debug.LogError($"Excel表头名称与数据类型不一致\n Type: {dataType.Name} Excel: {ws.Name}");
                        return false;
                    }

                    if (!fieldNameToCol.ContainsKey("#实例名"))
                    {
                        Debug.LogError("Excel 第一行必须有「#实例名」列");
                        return false;
                    }

                    // 从第4行开始读数据
                    int dataStartRow = 4;
                    int rowCount = ws.Dimension.End.Row;

                    for (int r = dataStartRow; r <= rowCount; r++)
                    {
                        string instName = ws.Cells[r, fieldNameToCol["#实例名"]].Text.Trim();
                        if (string.IsNullOrEmpty(instName)) continue;

                        // 新建实例
                        var data = new T();

                        // 赋值所有字段
                        foreach (var field in GetSerializableFields(dataType))
                        {
                            if (!fieldNameToCol.TryGetValue(field.Name, out int col))
                            {
                                Debug.LogWarning($"字段 {field.Name} 在Excel中未找到，跳过赋值");
                                continue;
                            }

                            string cellText = ws.Cells[r, col].Text.Trim();
                            object value = ConvertExcelValueToType(cellText, field.FieldType);
                            field.SetValue(data, value);
                        }

                        // 保存
                        GenericDataPersistence.SaveData(data, instName);
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log("导入完成");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"导入失败：{e}");
                return false;
            }
        }
        #endregion

        #region 通用工具
        /// <summary>
        /// 获取可序列化的字段（公开字段 或 带SerializeField的私有字段）
        /// </summary>
        private static List<FieldInfo> GetSerializableFields(Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
                .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">")) // 排除自动生成的属性字段
                .ToList();
        }

        /// <summary>
        /// 获取友好的类型名称（支持泛型、数组、List、Dictionary）
        /// </summary>
        // private static string GetFriendlyTypeName(Type type)
        // {
        //     // 处理可空类型
        //     if (Nullable.GetUnderlyingType(type) != null)
        //         return $"{GetFriendlyTypeName(Nullable.GetUnderlyingType(type))}?";

        //     // 处理数组
        //     if (type.IsArray)
        //     {
        //         Type elemType = type.GetElementType();
        //         return $"{GetFriendlyTypeName(elemType)}[]";
        //     }

        //     // 处理泛型类型（List、Dictionary）
        //     if (type.IsGenericType)
        //     {
        //         string genericName = type.GetGenericTypeDefinition().Name;
        //         // 去掉泛型后缀（如 List`1 → List）
        //         genericName = genericName.Substring(0, genericName.IndexOf('`'));
        //         // 获取泛型参数
        //         Type[] genericArgs = type.GetGenericArguments();
        //         string argsStr = string.Join(",", genericArgs.Select(GetFriendlyTypeName));
        //         return $"{genericName}<{argsStr}>";
        //     }

        //     // 基础类型映射
        //     return type.Name switch
        //     {
        //         "String" => "string",
        //         "Int32" => "int",
        //         "Single" => "float",
        //         "Boolean" => "bool",
        //         "Double" => "double",
        //         "Int64" => "long",
        //         "Vector2" => "Vector2",
        //         "Vector3" => "Vector3",
        //         "Vector4" => "Vector4",
        //         "Color" => "Color",
        //         _ => type.Name
        //     };
        // }


        /// <summary>
        /// 获取友好的类型名称（支持泛型、数组、List、Dictionary，用户自定义类型自动拼接namespace）
        /// </summary>
        private static string GetFriendlyTypeName(Type type)
        {
            // 处理可空类型
            if (Nullable.GetUnderlyingType(type) != null)
                return $"{GetFriendlyTypeName(Nullable.GetUnderlyingType(type))}?";

            // 处理数组
            if (type.IsArray)
            {
                Type elemType = type.GetElementType();
                return $"{GetFriendlyTypeName(elemType)}[]";
            }

            // 处理泛型类型（List、Dictionary）
            if (type.IsGenericType)
            {
                string genericName = type.GetGenericTypeDefinition().Name;
                // 去掉泛型后缀（如 List`1 → List）
                genericName = genericName.Substring(0, genericName.IndexOf('`'));
                // 获取泛型参数（递归处理泛型参数中的自定义类型）
                Type[] genericArgs = type.GetGenericArguments();
                string argsStr = string.Join(",", genericArgs.Select(GetFriendlyTypeName));
                return $"{genericName}<{argsStr}>";
            }

            // 1. 基础类型映射（保持原有逻辑）
            var basicTypeName = type.Name switch
            {
                "String" => "string",
                "Int32" => "int",
                "Single" => "float",
                "Boolean" => "bool",
                "Double" => "double",
                "Int64" => "long",
                "Vector2" => "Vector2",
                "Vector3" => "Vector3",
                "Vector4" => "Vector4",
                "Color" => "Color",
                _ => null
            };

            // 匹配到基础类型直接返回
            if (basicTypeName != null)
                return basicTypeName;

            // 2. 判断是否为系统/框架内置类型（非用户自定义）
            if (IsSystemBuiltInType(type))
                return type.Name;

            // 3. 用户自定义类型：拼接命名空间 + 类型名
            return $"{type.Namespace}.{type.Name}";
        }

        /// <summary>
        /// 辅助方法：判断是否为系统/框架内置类型（非用户自定义）
        /// </summary>
        private static bool IsSystemBuiltInType(Type type)
        {
            if (string.IsNullOrEmpty(type.Namespace))
                return true; // 极少数无命名空间的基础类型

            // 可根据项目扩展（如Unity、ASP.NET等框架类型）
            return type.Namespace.StartsWith("System")       // .NET系统类型
                || type.Namespace.StartsWith("UnityEngine")  // Unity引擎类型
                || type.Namespace.StartsWith("UnityEditor")  // Unity编辑器类型
                || type.Namespace.StartsWith("Microsoft");   // Microsoft框架类型
        }

        /// <summary>
        /// 将值转换为Excel兼容的字符串（支持Array、List、Dictionary）
        /// 格式约定：
        /// - 数组/List：元素1|元素2|元素3（用|分隔）
        /// - Dictionary：键1=值1|键2=值2（用|分隔键值对，=分隔键值）
        /// </summary>
        private static object ConvertValueToExcelCompatible(object value)
        {
            if (value == null) return "";

            // 处理Vector/Color/Enum（原有逻辑保留）
            if (value is Vector2 v2) return $"{v2.x},{v2.y}";
            if (value is Vector3 v3) return $"{v3.x},{v3.y},{v3.z}";
            if (value is Vector4 v4) return $"{v4.x},{v4.y},{v4.z},{v4.w}";
            if (value is Color c) return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
            if (value is Enum e) return e.ToString();

            // 处理数组
            if (value is Array array)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < array.Length; i++)
                {
                    if (i > 0) sb.Append(EXCEL_SEPARATOR);
                    sb.Append(ConvertValueToExcelCompatible(array.GetValue(i)));
                }
                return sb.ToString();
            }

            // 处理List<T>
            if (value is System.Collections.IList list)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(EXCEL_SEPARATOR);
                    sb.Append(ConvertValueToExcelCompatible(list[i]));
                }
                return sb.ToString();
            }

            // 处理Dictionary<K,V>
            if (value is System.Collections.IDictionary dict)
            {
                StringBuilder sb = new StringBuilder();
                int idx = 0;
                foreach (var key in dict.Keys)
                {
                    if (idx > 0) sb.Append(EXCEL_SEPARATOR);
                    object val = dict[key];
                    sb.Append($"{ConvertValueToExcelCompatible(key)}={ConvertValueToExcelCompatible(val)}");
                    idx++;
                }
                return sb.ToString();
            }

            // 基础类型直接返回
            return value;
        }

        /// <summary>
        /// 将Excel字符串转换为目标类型（支持Array、List、Dictionary）
        /// </summary>
        private static object ConvertExcelValueToType(string text, Type targetType)
        {
            // 空字符串处理（返回类型默认值）
            if (string.IsNullOrEmpty(text))
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                // 处理基础类型（原有逻辑保留）
                if (underlyingType == typeof(Vector2))
                {
                    var p = text.Split(',');
                    return new Vector2(float.Parse(p[0]), float.Parse(p[1]));
                }
                if (underlyingType == typeof(Vector3))
                {
                    var p = text.Split(',');
                    return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                }
                if (underlyingType == typeof(Vector4))
                {
                    var p = text.Split(',');
                    return new Vector4(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
                }
                if (underlyingType == typeof(Color))
                {
                    ColorUtility.TryParseHtmlString(text.StartsWith("#") ? text : $"#{text}", out var c);
                    return c;
                }
                if (underlyingType.IsEnum)
                {
                    return Enum.Parse(underlyingType, text);
                }

                // 处理数组
                if (underlyingType.IsArray)
                {
                    Type elemType = underlyingType.GetElementType();
                    string[] elemTexts = text.Split(EXCEL_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                    Array array = Array.CreateInstance(elemType, elemTexts.Length);
                    for (int i = 0; i < elemTexts.Length; i++)
                    {
                        array.SetValue(ConvertExcelValueToType(elemTexts[i], elemType), i);
                    }
                    return array;
                }

                // 处理List<T>
                if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elemType = underlyingType.GetGenericArguments()[0];
                    string[] elemTexts = text.Split(EXCEL_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                    var list = (System.Collections.IList)Activator.CreateInstance(underlyingType);
                    foreach (var elemText in elemTexts)
                    {
                        list.Add(ConvertExcelValueToType(elemText, elemType));
                    }
                    return list;
                }

                // 处理Dictionary<K,V>
                if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] dictArgs = underlyingType.GetGenericArguments();
                    Type keyType = dictArgs[0];
                    Type valType = dictArgs[1];

                    var dict = (System.Collections.IDictionary)Activator.CreateInstance(underlyingType);
                    string[] kvPairs = text.Split(EXCEL_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var kvText in kvPairs)
                    {
                        string[] kv = kvText.Split('=', 2); // 只分割第一个=，避免值包含=
                        if (kv.Length != 2) continue;

                        object key = ConvertExcelValueToType(kv[0].Trim(), keyType);
                        object val = ConvertExcelValueToType(kv[1].Trim(), valType);
                        dict[key] = val;
                    }
                    return dict;
                }

                // 其他基础类型（int/float/string等）
                return Convert.ChangeType(text, underlyingType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"转换类型失败：文本={text}，目标类型={targetType.Name}，错误={ex.Message}");
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        /// <summary>
        /// 选择Excel保存路径（按类名命名）
        /// </summary>
        public static string SelectExcelSavePathForClass(Type dataType)
        {
            if (dataType == null)
            {
                Debug.LogError("数据类型不能为空");
                return "";
            }
            return EditorUtility.SaveFilePanel("导出全部到Excel", Application.dataPath, $"{dataType.Name}_AllData.xlsx", "xlsx");
        }

        /// <summary>
        /// 选择Excel加载路径
        /// </summary>
        public static string SelectExcelLoadPath()
        {
            return EditorUtility.OpenFilePanel("导入全部Excel", Application.dataPath, "xlsx");
        }
        #endregion
    }
}