using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using OfficeOpenXml;

public static class SOTExcelTool
{
    // Excel文件保存根路径（可自定义）
    private static readonly string ExcelRootPath = Path.Combine(Application.dataPath, "ExcelData");

    #region 1. 从SO子类生成Excel文件
    /// <summary>
    /// 生成指定类型SO的Excel文件（包含所有该类型SO实例的数据）
    /// </summary>
    /// <typeparam name="T">SO子类（继承ExcelableSO）</typeparam>
    public static void GenerateExcelFromSO<T>() where T : ExcelableSO
    {
        // 1. 检查路径
        if (!Directory.Exists(ExcelRootPath))
        {
            Directory.CreateDirectory(ExcelRootPath);
        }

        // 2. 获取所有该类型的SO实例
        List<T> soList = LoadAllSOOfType<T>();
        if (soList.Count == 0)
        {
            Debug.LogWarning($"未找到任何 {typeof(T).Name} 类型的SO实例！");
            return;
        }

        // 3. 创建Excel包
        string excelPath = Path.Combine(ExcelRootPath, soList[0].GetExcelFileName());
        FileInfo excelFile = new FileInfo(excelPath);
        if (excelFile.Exists) excelFile.Delete(); // 覆盖旧文件
        using (ExcelPackage package = new ExcelPackage(excelFile))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(typeof(T).Name);

            // 4. 获取SO的可序列化字段（排除内置字段）
            List<FieldInfo> fields = GetSerializableFields<T>();

            // 5. 写入表头（字段名）
            for (int i = 0; i < fields.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = fields[i].Name;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true; // 表头加粗
            }

            // 6. 写入每行数据（每个SO实例）
            for (int row = 0; row < soList.Count; row++)
            {
                T so = soList[row];
                for (int col = 0; col < fields.Count; col++)
                {
                    FieldInfo field = fields[col];
                    object value = field.GetValue(so);
                    worksheet.Cells[row + 2, col + 1].Value = ConvertValueToExcelFormat(value);
                }
            }

            // 7. 保存Excel
            package.Save();
            AssetDatabase.Refresh(); // 刷新Unity资源
            Debug.Log($"Excel生成成功：{excelPath}");
        }
    }

    #endregion

    #region 2. 从Excel加载数据到SO
    /// <summary>
    /// 从Excel加载数据，更新/创建SO实例
    /// </summary>
    /// <typeparam name="T">SO子类（继承ExcelableSO）</typeparam>
    /// <param name="soSavePath">SO保存的路径（如 "Assets/Resources/Buildings/"）</param>
    public static void LoadSOFromExcel<T>(string soSavePath) where T : ExcelableSO
    {
        // 1. 检查路径
        if (!Directory.Exists(ExcelRootPath))
        {
            Debug.LogError($"Excel目录不存在：{ExcelRootPath}");
            return;
        }

        // 2. 获取Excel文件
        T tempSO = ScriptableObject.CreateInstance<T>();
        string excelPath = Path.Combine(ExcelRootPath, tempSO.GetExcelFileName());
        ScriptableObject.DestroyImmediate(tempSO); // 临时SO销毁

        if (!File.Exists(excelPath))
        {
            Debug.LogError($"Excel文件不存在：{excelPath}");
            return;
        }

        // 3. 确保SO保存路径存在
        if (!Directory.Exists(soSavePath))
        {
            Directory.CreateDirectory(soSavePath);
        }

        // 4. 读取Excel
        FileInfo excelFile = new FileInfo(excelPath);
        using (ExcelPackage package = new ExcelPackage(excelFile))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            if (rowCount < 2) // 至少需要表头+1行数据
            {
                Debug.LogWarning("Excel中无有效数据！");
                return;
            }

            // 5. 获取字段列表（与表头对应）
            List<FieldInfo> fields = GetSerializableFields<T>();
            Dictionary<string, FieldInfo> fieldDict = new Dictionary<string, FieldInfo>();
            foreach (var field in fields) fieldDict[field.Name] = field;

            // 6. 遍历每行数据（从第2行开始，第1行是表头）
            for (int row = 2; row <= rowCount; row++)
            {
                // 获取SO的唯一标识（这里用Id字段，可自定义）
                string soId = worksheet.Cells[row, fieldDict["Id"].MetadataToken].Value?.ToString();
                if (string.IsNullOrEmpty(soId))
                {
                    Debug.LogWarning($"第{row}行数据缺少Id，跳过！");
                    continue;
                }

                // 7. 查找/创建SO实例
                T so = FindSOById<T>(soSavePath, soId);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance<T>();
                    so.name = soId; // 用Id命名SO
                    string soPath = Path.Combine(soSavePath, soId + ".asset");
                    AssetDatabase.CreateAsset(so, soPath);
                }

                // 8. 赋值字段
                for (int col = 1; col <= colCount; col++)
                {
                    string fieldName = worksheet.Cells[1, col].Value?.ToString();
                    if (!fieldDict.ContainsKey(fieldName)) continue;

                    FieldInfo field = fieldDict[fieldName];
                    string cellValue = worksheet.Cells[row, col].Value?.ToString();
                    object value = ConvertValueFromExcelFormat(cellValue, field.FieldType);
                    field.SetValue(so, value);
                }

                // 标记SO为脏（需要保存）
                EditorUtility.SetDirty(so);
            }
        }

        // 9. 保存所有修改
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"从Excel加载SO成功！路径：{soSavePath}");
    }

    #endregion

    #region 辅助方法：反射/类型转换/资源加载
    /// <summary>
    /// 获取SO的可序列化字段（排除m_Script等内置字段）
    /// </summary>
    private static List<FieldInfo> GetSerializableFields<T>() where T : ExcelableSO
    {
        List<FieldInfo> fields = new List<FieldInfo>();
        // 获取所有公共字段 + 带[SerializeField]的私有/保护字段
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        foreach (FieldInfo field in typeof(T).GetFields(flags))
        {
            // 排除内置字段
            if (field.Name == "m_Script" || field.Name.StartsWith("m_")) continue;
            // 只保留可序列化字段（public 或 带[SerializeField]）
            if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
            {
                fields.Add(field);
            }
        }
        return fields;
    }

    /// <summary>
    /// 将值转换为Excel可存储的格式（如Vector2Int转"x,y"）
    /// </summary>
    private static object ConvertValueToExcelFormat(object value)
    {
        if (value == null) return string.Empty;

        Type type = value.GetType();
        // 处理Vector2Int
        if (type == typeof(Vector2Int))
        {
            Vector2Int v = (Vector2Int)value;
            return $"{v.x},{v.y}";
        }
        // 处理枚举（存字符串）
        else if (type.IsEnum)
        {
            return Enum.GetName(type, value);
        }
        // 处理嵌套类（示例：ResourceCost，可扩展）
        else if (type == typeof(ResourceCost))
        {
            ResourceCost cost = (ResourceCost)value;
            return $"{cost.resourceType},{cost.amount}";
        }
        // 基础类型直接返回
        else
        {
            return value;
        }
    }

    /// <summary>
    /// 从Excel字符串转换为目标类型（如"2,2"转Vector2Int）
    /// </summary>
    private static object ConvertValueFromExcelFormat(string cellValue, Type targetType)
    {
        if (string.IsNullOrEmpty(cellValue)) return GetDefaultValue(targetType);

        // 处理Vector2Int
        if (targetType == typeof(Vector2Int))
        {
            string[] parts = cellValue.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
            {
                return new Vector2Int(x, y);
            }
            return Vector2Int.zero;
        }
        // 处理枚举
        else if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, cellValue, true, out object enumValue))
            {
                return enumValue;
            }
            return Enum.GetValues(targetType).GetValue(0); // 默认第一个枚举值
        }
        // 处理嵌套类（示例：ResourceCost）
        else if (targetType == typeof(ResourceCost))
        {
            string[] parts = cellValue.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[1], out int amount))
            {
                return new ResourceCost { resourceType = parts[0], amount = amount };
            }
            return new ResourceCost();
        }
        // 基础类型转换
        else
        {
            try
            {
                return Convert.ChangeType(cellValue, targetType);
            }
            catch
            {
                return GetDefaultValue(targetType);
            }
        }
    }

    /// <summary>
    /// 获取类型的默认值
    /// </summary>
    private static object GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    /// <summary>
    /// 加载指定类型的所有SO实例
    /// </summary>
    private static List<T> LoadAllSOOfType<T>() where T : ExcelableSO
    {
        List<T> soList = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T so = AssetDatabase.LoadAssetAtPath<T>(path);
            if (so != null) soList.Add(so);
        }
        return soList;
    }

    /// <summary>
    /// 根据Id查找SO实例
    /// </summary>
    private static T FindSOById<T>(string path, string id) where T : ExcelableSO
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
        foreach (string guid in guids)
        {
            string soPath = AssetDatabase.GUIDToAssetPath(guid);
            T so = AssetDatabase.LoadAssetAtPath<T>(soPath);
            if (so != null && so.GetType().GetField("Id")?.GetValue(so)?.ToString() == id)
            {
                return so;
            }
        }
        return null;
    }
    #endregion
}