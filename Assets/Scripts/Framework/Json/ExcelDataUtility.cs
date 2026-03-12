using System;
using System.IO;
using System.Reflection;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// Excel数据导入导出工具（第一行：变量名 | 第二行：变量类型 | 第三行：注释）
/// </summary>
public static class ExcelDataUtility
{
    // // 设置EPPlus许可证（必须）
    // static ExcelDataUtility()
    // {
    //     ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // 非商用授权
    // }

    /// <summary>
    /// 将数据类实例导出为Excel
    /// </summary>
    public static bool ExportToExcel<T>(T dataInstance, string excelPath, string instanceName = "Default") where T : class, new()
    {
        try
        {
            if (dataInstance == null)
            {
                Debug.LogError("导出失败：数据实例为空");
                return false;
            }

            // 创建目录
            string directory = Path.GetDirectoryName(excelPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // 创建Excel包
            using (var package = new ExcelPackage())
            {
                // 创建工作表（以实例名命名）
                var worksheet = package.Workbook.Worksheets.Add(instanceName);

                // 获取数据类的所有可序列化字段
                FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
                    .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
                    .ToArray();

                // 填充Excel内容
                for (int col = 0; col < fields.Length; col++)
                {
                    FieldInfo field = fields[col];
                    int columnIndex = col + 1; // EPPlus列从1开始

                    // 第一行：变量名
                    worksheet.Cells[1, columnIndex].Value = field.Name;
                    worksheet.Cells[1, columnIndex].Style.Font.Bold = true; // 加粗

                    // 第二行：变量类型（简化显示）
                    string typeName = GetFriendlyTypeName(field.FieldType);
                    worksheet.Cells[2, columnIndex].Value = typeName;

                    // 第三行：注释（优先取Header特性，无则空）
                    string comment = field.GetCustomAttribute<HeaderAttribute>()?.header ?? "";
                    worksheet.Cells[3, columnIndex].Value = comment;

                    // 第四行：变量值
                    object value = field.GetValue(dataInstance);
                    worksheet.Cells[4, columnIndex].Value = ConvertValueToExcelCompatible(value);
                }

                // 自动调整列宽
                // worksheet.Cells.AutoFitColumns();

                // 保存Excel文件
                package.SaveAs(new FileInfo(excelPath));
            }

            AssetDatabase.Refresh();
            Debug.Log($"Excel导出成功：{excelPath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Excel导出失败：{e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 从Excel导入数据到类实例
    /// </summary>
    public static T ImportFromExcel<T>(string excelPath, string instanceName = "Default") where T : class, new()
    {
        try
        {
            if (!File.Exists(excelPath))
            {
                Debug.LogError($"导入失败：Excel文件不存在 {excelPath}");
                return new T();
            }

            T dataInstance = new T();
            Type dataType = typeof(T);

            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                // 获取指定名称的工作表（无则取第一个）
                var worksheet = package.Workbook.Worksheets[instanceName] ?? package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    Debug.LogError($"导入失败：未找到工作表 {instanceName}");
                    return dataInstance;
                }

                // 遍历Excel列（第一行是变量名）
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    // 第一行：变量名
                    string fieldName = worksheet.Cells[1, col].Text?.Trim();
                    if (string.IsNullOrEmpty(fieldName))
                        continue;

                    // 查找对应字段
                    FieldInfo field = dataType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null)
                    {
                        Debug.LogWarning($"未找到字段：{fieldName}，跳过");
                        continue;
                    }

                    // 第四行：变量值
                    string cellValue = worksheet.Cells[4, col].Text?.Trim();
                    if (string.IsNullOrEmpty(cellValue))
                        continue;

                    // 转换值并赋值
                    object value = ConvertExcelValueToType(cellValue, field.FieldType);
                    field.SetValue(dataInstance, value);
                }
            }

            Debug.Log($"Excel导入成功：{excelPath}");
            return dataInstance;
        }
        catch (Exception e)
        {
            Debug.LogError($"Excel导入失败：{e.Message}\n{e.StackTrace}");
            return new T();
        }
    }

    #region 辅助方法
    // 获取友好的类型名称（如Vector3而非System.Numerics.Vector3）
    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return GetFriendlyTypeName(type.GetGenericArguments()[0]) + "?";
        }

        return type.Name switch
        {
            "String" => "string",
            "Int32" => "int",
            "Single" => "float",
            "Boolean" => "bool",
            "Double" => "double",
            "Int64" => "long",
            _ => type.Name
        };
    }

    // 将值转换为Excel兼容格式
    private static object ConvertValueToExcelCompatible(object value)
    {
        if (value == null)
            return "";

        Type type = value.GetType();

        // Unity基础类型转换
        if (value is Vector2 v2)
            return $"{v2.x},{v2.y}";
        if (value is Vector3 v3)
            return $"{v3.x},{v3.y},{v3.z}";
        if (value is Vector4 v4)
            return $"{v4.x},{v4.y},{v4.z},{v4.w}";
        if (value is Color color)
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        if (value is Enum enumValue)
            return enumValue.ToString();

        // 基础类型直接返回
        return value;
    }

    // 将Excel字符串值转换为指定类型
    private static object ConvertExcelValueToType(string value, Type targetType)
    {
        // 处理可空类型
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            // Unity基础类型解析
            if (underlyingType == typeof(Vector2))
            {
                string[] parts = value.Split(',');
                return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }
            if (underlyingType == typeof(Vector3))
            {
                string[] parts = value.Split(',');
                return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
            }
            if (underlyingType == typeof(Vector4))
            {
                string[] parts = value.Split(',');
                return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
            }
            if (underlyingType == typeof(Color))
            {
                if (value.StartsWith("#"))
                    value = value.Substring(1);
                Color color;
                ColorUtility.TryParseHtmlString($"#{value}", out color);
                return color;
            }
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, value);
            }

            // 基础类型转换
            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            Debug.LogWarning($"值转换失败：{value} → {targetType.Name}，使用默认值");
            return Activator.CreateInstance(targetType);
        }
    }
    #endregion

    // 选择Excel保存路径
    public static string SelectExcelSavePath(string defaultFileName)
    {
        return EditorUtility.SaveFilePanel("导出为Excel", Application.dataPath, defaultFileName, "xlsx");
    }

    // 选择Excel读取路径
    public static string SelectExcelLoadPath()
    {
        return EditorUtility.OpenFilePanel("从Excel导入", Application.dataPath, "xlsx");
    }
}