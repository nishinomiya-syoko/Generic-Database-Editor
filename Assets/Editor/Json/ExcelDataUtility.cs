using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Excel 工具：同一类所有数据 → 同一个 Sheet，一行 = 一条实例
/// 格式：
/// 行1：实例名 | 字段1名 | 字段2名 | 字段3名 ...
/// 行2：string  | 字段1类型| 字段2类型| ...
/// 行3：注释    | 字段1注释| 字段2注释| ...
/// 行4~：实例1  | 值1 | 值2 | ...
/// </summary>
public static class ExcelDataUtility
{
    // static ExcelDataUtility()
    // {
    //     ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    // }

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
                ws.Cells[1, 1].Value = "实例名";
                for (int c = 0; c < fields.Count; c++)
                {
                    ws.Cells[1, 2 + c].Value = fields[c].Name;
                }

                // ========== 2. 类型行 ==========
                ws.Cells[2, 1].Value = "string";
                for (int c = 0; c < fields.Count; c++)
                {
                    ws.Cells[2, 2 + c].Value = GetFriendlyTypeName(fields[c].FieldType);
                }

                // ========== 3. 注释行 ==========
                ws.Cells[3, 1].Value = "实例名称";
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
                if (ws == null || ws.Dimension == null) return false;

                // 读取表头：第1行是字段名
                Dictionary<string, int> fieldNameToCol = new Dictionary<string, int>();
                for (int c = 1; c <= ws.Dimension.End.Column; c++)
                {
                    string name = ws.Cells[1, c].Text.Trim();
                    if (!string.IsNullOrEmpty(name))
                        fieldNameToCol[name] = c;
                }

                if (!fieldNameToCol.ContainsKey("实例名"))
                {
                    Debug.LogError("Excel 第一行必须有「实例名」列");
                    return false;
                }

                // 从第4行开始读数据
                int dataStartRow = 4;
                int rowCount = ws.Dimension.End.Row;

                for (int r = dataStartRow; r <= rowCount; r++)
                {
                    string instName = ws.Cells[r, fieldNameToCol["实例名"]].Text.Trim();
                    if (string.IsNullOrEmpty(instName)) continue;

                    // 新建实例
                    var data = new T();

                    // 赋值所有字段
                    foreach (var field in GetSerializableFields(dataType))
                    {
                        if (!fieldNameToCol.TryGetValue(field.Name, out int col)) continue;

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
    private static List<FieldInfo> GetSerializableFields(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
            .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
            .ToList();
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null)
            return GetFriendlyTypeName(Nullable.GetUnderlyingType(type)) + "?";

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

    private static object ConvertValueToExcelCompatible(object value)
    {
        if (value == null) return "";
        if (value is Vector2 v2) return $"{v2.x},{v2.y}";
        if (value is Vector3 v3) return $"{v3.x},{v3.y},{v3.z}";
        if (value is Vector4 v4) return $"{v4.x},{v4.y},{v4.z},{v4.w}";
        if (value is Color c) return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
        if (value is Enum e) return e.ToString();
        return value;
    }

    private static object ConvertExcelValueToType(string text, Type targetType)
    {
        Type ut = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try
        {
            if (ut == typeof(Vector2)) { var p = text.Split(','); return new Vector2(float.Parse(p[0]), float.Parse(p[1])); }
            if (ut == typeof(Vector3)) { var p = text.Split(','); return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2])); }
            if (ut == typeof(Color)) { ColorUtility.TryParseHtmlString(text.StartsWith("#") ? text : $"#{text}", out var c); return c; }
            if (ut.IsEnum) return Enum.Parse(ut, text);
            return Convert.ChangeType(text, ut);
        }
        catch { return Activator.CreateInstance(targetType); }
    }

    public static string SelectExcelSavePathForClass(Type dataType)
    {
        return EditorUtility.SaveFilePanel("导出全部到Excel", Application.dataPath, $"{dataType.Name}_AllData.xlsx", "xlsx");
    }

    public static string SelectExcelLoadPath()
    {
        return EditorUtility.OpenFilePanel("导入全部Excel", Application.dataPath, "xlsx");
    }
    #endregion
}