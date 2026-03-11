using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using OfficeOpenXml;
using SOEditor;

public static class SOTExcelTool
{
    // Excel文件保存根路径（可自定义）
    private static readonly string ExcelRootPath = Constant.EXCEL_PATH;

    #region 1. 从SO子类生成Excel文件（含变量名/类型/注释表头）
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

        // 3. 构建Excel文件路径（修复：添加.xlsx后缀，处理类名格式）
        string className = typeof(T).Name;
        string excelFileName = $"{className}.xlsx"; // 文件名=类名+.xlsx
        string excelPath = Path.Combine(ExcelRootPath, excelFileName);
        
        FileInfo excelFile = new FileInfo(excelPath);
        if (excelFile.Exists) excelFile.Delete(); // 覆盖旧文件

        using (ExcelPackage package = new ExcelPackage(excelFile))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(className);

            // 4. 获取SO的可序列化字段（排除内置字段）
            List<FieldInfo> fields = GetSerializableFields<T>();

            // 5. 写入三行表头：第1行=字段名，第2行=字段类型，第3行=字段注释
            for (int i = 0; i < fields.Count; i++)
            {
                FieldInfo field = fields[i];
                int colIndex = i + 1;

                // 第1行：字段名（加粗）
                worksheet.Cells[1, colIndex].Value = field.Name;
                worksheet.Cells[1, colIndex].Style.Font.Bold = true;

                // 第2行：字段类型（如int、Vector2Int、BuildingType）
                string typeName = GetFriendlyTypeName(field.FieldType);
                worksheet.Cells[2, colIndex].Value = typeName;
                worksheet.Cells[2, colIndex].Style.Font.Italic = true; // 斜体区分

                // 第3行：字段注释（读取[Tooltip]特性，无则为空）
                string tooltip = GetFieldTooltip(field);
                worksheet.Cells[3, colIndex].Value = tooltip;
            }

            // 6. 写入每行数据（从第4行开始，前3行是表头）
            for (int row = 0; row < soList.Count; row++)
            {
                T so = soList[row];
                int dataRowIndex = row + 4; // 数据起始行=4
                for (int col = 0; col < fields.Count; col++)
                {
                    FieldInfo field = fields[col];
                    object value = field.GetValue(so);
                    worksheet.Cells[dataRowIndex, col + 1].Value = ConvertValueToExcelFormat(value);
                }
            }

            // 7. 保存Excel
            package.Save();
            AssetDatabase.Refresh(); // 刷新Unity资源
            Debug.Log($"Excel生成成功：{excelPath}");
        }
    }
    #endregion

    #region 2. 从Excel加载数据到SO（兼容三行表头）
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

        // 2. 构建Excel文件路径（匹配生成时的命名规则）
        string className = typeof(T).Name;
        string excelFileName = $"{className}.xlsx";
        string excelPath = Path.Combine(ExcelRootPath, excelFileName);

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
            if (worksheet.Dimension == null)
            {
                Debug.LogWarning("Excel工作表无数据！");
                return;
            }

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            if (rowCount < 4) // 至少需要3行表头+1行数据
            {
                Debug.LogWarning("Excel中无有效数据（需至少3行表头+1行数据）！");
                return;
            }

            // 5. 获取字段列表（仅读取第1行的字段名，跳过第2/3行的类型/注释）
            List<FieldInfo> fields = GetSerializableFields<T>();
            Dictionary<string, FieldInfo> fieldDict = new Dictionary<string, FieldInfo>();
            foreach (var field in fields) fieldDict[field.Name] = field;

            // 构建「字段名→列索引」映射（从第1行表头读取）
            Dictionary<string, int> fieldColMap = new Dictionary<string, int>();
            for (int col = 1; col <= colCount; col++)
            {
                string fieldName = worksheet.Cells[1, col].Value?.ToString();
                if (!string.IsNullOrEmpty(fieldName) && fieldDict.ContainsKey(fieldName))
                {
                    fieldColMap[fieldName] = col;
                }
            }

            // 检查Id字段是否存在
            if (!fieldColMap.ContainsKey("Id"))
            {
                Debug.LogError("Excel表头中未找到Id字段！");
                return;
            }

            // 6. 遍历每行数据（从第4行开始）
            for (int row = 4; row <= rowCount; row++)
            {
                // 获取SO的唯一标识（Id字段）
                int idCol = fieldColMap["Id"];
                string soId = worksheet.Cells[row, idCol].Value?.ToString();
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

                // 8. 赋值字段（按字段名匹配列）
                foreach (var kvp in fieldColMap)
                {
                    string fieldName = kvp.Key;
                    int col = kvp.Value;
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

    #region 辅助方法：反射/类型转换/注释读取
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
    /// 获取字段的[Tooltip]注释（无则返回空）
    /// </summary>
    private static string GetFieldTooltip(FieldInfo field)
    {
        // 读取Tooltip特性
        object[] attributes = field.GetCustomAttributes(typeof(TooltipAttribute), true);
        if (attributes != null && attributes.Length > 0)
        {
            return ((TooltipAttribute)attributes[0]).tooltip;
        }
        // 兼容Header特性（可选）
        attributes = field.GetCustomAttributes(typeof(HeaderAttribute), true);
        if (attributes != null && attributes.Length > 0)
        {
            return ((HeaderAttribute)attributes[0]).header;
        }
        return string.Empty;
    }

    /// <summary>
    /// 获取友好的类型名称（如Vector2Int而非System.Numerics.Vector2Int）
    /// </summary>
    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsEnum) return $"{type.Name}";
        if (type == typeof(Vector2Int)) return "Vector2Int";
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(GameObject)) return "GameObject";
        return type.Name;
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
        // 处理Unity引用类型（存路径/名称）
        else if (type == typeof(GameObject))
        {
            GameObject go = (GameObject)value;
            return go ? go.name : "空引用";
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
