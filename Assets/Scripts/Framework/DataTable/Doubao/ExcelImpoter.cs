using System.IO;
using System.Text;
using OfficeOpenXml; // EPPlus 4 命名空间不变
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEditor;

public class ExcelImporter
{
    // 定义生成路径（统一使用Path.Combine保证跨平台）
    public static string ExcelPath = Constant.EXCEL_PATH;
    public static string ScriptOutputPath = Constant.DATA_CLASS_PATH;
    public static string TxtOutputPath = Constant.ASSET_TXT_PATH;
    public static string BinaryOutputPath = Constant.DATA_BINARY_PATH;



    // 确保目录存在
    static ExcelImporter()
    {
        Directory.CreateDirectory(ExcelPath);
        Directory.CreateDirectory(ScriptOutputPath);
        Directory.CreateDirectory(TxtOutputPath);
        Directory.CreateDirectory(BinaryOutputPath);
    }
    [UnityEditor.MenuItem("Tools/数据工具/导入Excel")]
    public static void ImportExcel()
    {
        string excelFolder = ExcelPath;
        if (!Directory.Exists(excelFolder))
        {
            Debug.LogError($"Excel文件夹不存在：{excelFolder}");
            return;
        }
        List<string> excelFiles = new List<string>();
        excelFiles.Clear();
        try
        {
            string[] files = Directory.GetFiles(excelFolder, "*.xlsx", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                if (fileName.StartsWith("~$"))
                    continue;
                excelFiles.Add(file);
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"[Fast Generate] 获取文件列表失败：{e.Message}");
            return;
        }
        if (excelFiles.Count == 0)
        {
            Debug.LogWarning($"在 {excelFolder} 目录下未找到 Excel 文件");
        }
        // if (!Directory.Exists(ScriptOutputPath))
        // {
        //     Directory.CreateDirectory(ScriptOutputPath);
        // }
        // if (!Directory.Exists(TxtOutputPath))
        // {
        //     Directory.CreateDirectory(TxtOutputPath);
        // }
        // if (!Directory.Exists(BinaryOutputPath))
        // {
        //     Directory.CreateDirectory(BinaryOutputPath);
        // }
        try
        {
            foreach (string file in excelFiles)
            {
                ImportExcel(file);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Fast Generate] 导入Excel文件失败：{e.Message}");
        }
    }

    public static void ImportExcel(string excelFilePath)
    {
        // EPPlus4 适配：检查文件是否存在
        if (!File.Exists(excelFilePath))
        {
            Debug.LogError($"Excel文件不存在：{excelFilePath}");
            return;
        }

        FileInfo fileInfo = new FileInfo(excelFilePath);
        // EPPlus4 适配：使用using正确释放资源
        using (ExcelPackage package = new ExcelPackage(fileInfo))
        {
            // 遍历所有工作表
            foreach (ExcelWorksheet sheet in package.Workbook.Worksheets)
            {
                string className = sheet.Name;
                // 空工作表跳过
                if (sheet.Dimension == null)
                {
                    Debug.LogWarning($"工作表 {className} 无数据，跳过处理");
                    continue;
                }

                int colCount = sheet.Dimension.End.Column;
                int rowCount = sheet.Dimension.End.Row;

                List<FieldInfoData> fields = new List<FieldInfoData>();

                // 解析前三行 (1: 字段名, 2: 类型, 3: 注释)
                for (int col = 1; col <= colCount; col++)
                {
                    // EPPlus4 适配：空单元格处理
                    string fieldName = sheet.Cells[1, col].Text?.Trim() ?? $"UnnamedCol{col}";
                    string fieldType = sheet.Cells[2, col].Text?.Trim() ?? "string";
                    string fieldComment = sheet.Cells[3, col].Text?.Trim() ?? "无注释";
                    if (
                        fieldName.StartsWith("#") || fieldName.StartsWith("//") || fieldName == "" ||
                        fieldType.StartsWith("#") || fieldType.StartsWith("//") || fieldType == "" ||
                        fieldComment.StartsWith("#") || fieldComment.StartsWith("//") || fieldComment == ""
                    )
                        continue;

                    fields.Add(new FieldInfoData
                    {
                        Name = fieldName,
                        Type = fieldType,
                        Comment = fieldComment
                    });
                }
                //删除所有生成的类
                FileUtil.DeleteFileOrDirectory(ScriptOutputPath);
                Directory.CreateDirectory(ScriptOutputPath);
                // 1. 生成 C# 解析类
                GenerateCSharpClass(className, fields);

                // 2. 提取数据并生成 TXT 和 Binary
                ExportData(sheet, className, fields, rowCount, colCount);
            }
        }
        Debug.Log($"Excel导入完成：{excelFilePath}");
    }

    private static void GenerateCSharpClass(string className, List<FieldInfoData> fields)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("");
        sb.AppendLine("[Serializable]");
        sb.AppendLine($"public class {className} ");
        sb.AppendLine("{");

        foreach (var field in fields)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {field.Comment}");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public {field.Type} {field.Name};");
        }
        sb.AppendLine("}");

        string classPath = Path.Combine(ScriptOutputPath, $"{className}.cs");
        File.WriteAllText(classPath, sb.ToString(), Encoding.UTF8); // 显式指定UTF8避免乱码
    }

    // private static void ExportData(ExcelWorksheet sheet, string className, List<FieldInfoData> fields, int rowCount, int colCount)
    // {
    //     List<object> allData = new List<object>();
    //     Type dataType = Type.GetType(className);
    //     if (dataType == null)
    //     {
    //         Debug.LogError($"找不到生成的类 {className}，请检查类名是否正确");
    //         return;
    //     }

    //     // 读取数据 (从第4行开始，前三行是表头)
    //     for (int row = 4; row <= rowCount; row++)
    //     {
    //         object dataItem = Activator.CreateInstance(dataType);
    //         bool isEmptyRow = true;

    //         for (int col = 1; col <= colCount; col++)
    //         {
    //             string cellValue = sheet.Cells[row, col].Text?.Trim() ?? "";
    //             if (!string.IsNullOrEmpty(cellValue)&&!cellValue.StartsWith("#")) isEmptyRow = false;

    //             FieldInfoData field = fields[col - 1];
    //             object parsedValue = ParseType(cellValue, field.Type);

    //             // 反射设置字段值
    //             var fieldInfo = dataType.GetField(field.Name);
    //             if (fieldInfo != null && parsedValue != null)
    //             {
    //                 fieldInfo.SetValue(dataItem, parsedValue);
    //             }
    //         }

    //         // 跳过空行
    //         if (!isEmptyRow)
    //         {
    //             allData.Add(dataItem);
    //         }
    //     }

    //     // 导出 TXT (JSON 格式)
    //     string json = JsonConvert.SerializeObject(allData, Formatting.Indented);
    //     string txtPath = Path.Combine(TxtOutputPath, $"{className}.txt");
    //     File.WriteAllText(txtPath, json, Encoding.UTF8);

    //     // 导出 Binary (补全二进制写入逻辑)
    //     string binaryPath = Path.Combine(BinaryOutputPath, $"{className}.bytes");
    //     using (FileStream fs = new FileStream(binaryPath, FileMode.Create, FileAccess.Write))
    //     using (BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8))
    //     {
    //         // 写入数据总数
    //         bw.Write(allData.Count);

    //         // 遍历每一行数据
    //         foreach (var dataItem in allData)
    //         {
    //             // 遍历每个字段
    //             foreach (var field in fields)
    //             {
    //                 var fieldInfo = dataType.GetField(field.Name);
    //                 if (fieldInfo == null) continue;

    //                 object fieldValue = fieldInfo.GetValue(dataItem);
    //                 WriteBinaryData(bw, fieldValue, field.Type);
    //             }
    //         }
    //     }
    // }

    // 基础类型解析工具 (支持 array, list, dictionary)
    private static void ExportData(ExcelWorksheet sheet, string className, List<FieldInfoData> fields, int rowCount, int colCount)
    {
        List<object> allData = new List<object>();
        Type dataType = Type.GetType(className);
        if (dataType == null)
        {
            Debug.LogError($"找不到生成的类 {className}，请检查类名是否正确");
            return;
        }

        // 读取数据 (从第 4 行开始，前三行是表头)
        for (int row = 4; row <= rowCount; row++)
        {
            object dataItem = Activator.CreateInstance(dataType);
            bool isEmptyRow = true;

            // ✅ 修改：遍历 fields 而不是 colCount，避免索引越界
            for (int colIndex = 0; colIndex < fields.Count; colIndex++)
            {
                int excelCol = colIndex + 1; // Excel 列从 1 开始
                string cellValue = sheet.Cells[row, excelCol].Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(cellValue) && !cellValue.StartsWith("#"))
                    isEmptyRow = false;

                FieldInfoData field = fields[colIndex]; // ✅ 安全访问
                object parsedValue = ParseType(cellValue, field.Type);

                // 反射设置字段值
                var fieldInfo = dataType.GetField(field.Name);
                if (fieldInfo != null && parsedValue != null)
                {
                    fieldInfo.SetValue(dataItem, parsedValue);
                }
            }

            // 跳过空行
            if (!isEmptyRow)
            {
                allData.Add(dataItem);
            }
        }

        // 导出 TXT (JSON 格式)
        string json = JsonConvert.SerializeObject(allData, Formatting.Indented);
        string txtPath = Path.Combine(TxtOutputPath, $"{className}.txt");
        File.WriteAllText(txtPath, json, Encoding.UTF8);

        // 导出 Binary
        string binaryPath = Path.Combine(BinaryOutputPath, $"{className}.bytes");
        using (FileStream fs = new FileStream(binaryPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8))
        {
            bw.Write(allData.Count);

            foreach (var dataItem in allData)
            {
                foreach (var field in fields)
                {
                    var fieldInfo = dataType.GetField(field.Name);
                    if (fieldInfo == null) continue;

                    object fieldValue = fieldInfo.GetValue(dataItem);
                    WriteBinaryData(bw, fieldValue, field.Type);
                }
            }
        }
    }
    private static object ParseType(string value, string type)
    {
        if (string.IsNullOrEmpty(value))
        {
            // 根据类型返回默认值
            switch (type)
            {
                case "int": return 0;
                case "float": return 0f;
                case "int[]": return new int[0];
                case "List<string>": return new List<string>();
                case "Dictionary<int,string>": return new Dictionary<int, string>();
                default: return null;
            }
        }

        try
        {
            switch (type)
            {
                case "int": return int.Parse(value);
                case "float": return float.Parse(value);
                case "string": return value;
                case "int[]": // 逗号分隔: 1,2,3
                    return Array.ConvertAll(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), int.Parse);
                case "List<string>": // 逗号分隔: a,b,c
                    return new List<string>(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                case "Dictionary<int,string>": // 格式: 1:apple,2:banana
                    var dict = new Dictionary<int, string>();
                    foreach (var pair in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = pair.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (kv.Length == 2)
                        {
                            dict.Add(int.Parse(kv[0]), kv[1]);
                        }
                    }
                    return dict;
                default: return value; // 未知类型返回字符串
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析类型失败: value={value}, type={type}, error={ex.Message}");
            return null;
        }
    }

    // 补全二进制写入逻辑 (EPPlus4 适配)
    private static void WriteBinaryData(BinaryWriter bw, object data, string type)
    {
        if (data == null)
        {
            // 写入空标记
            bw.Write(false);
            return;
        }

        // 标记为非空
        bw.Write(true);

        switch (type)
        {
            case "int":
                bw.Write((int)data);
                break;
            case "float":
                bw.Write((float)data);
                break;
            case "string":
                bw.Write((string)data);
                break;
            case "int[]":
                int[] intArr = (int[])data;
                bw.Write(intArr.Length);
                foreach (int num in intArr) bw.Write(num);
                break;
            case "List<string>":
                List<string> strList = (List<string>)data;
                bw.Write(strList.Count);
                foreach (string str in strList) bw.Write(str);
                break;
            case "Dictionary<int,string>":
                Dictionary<int, string> dict = (Dictionary<int, string>)data;
                bw.Write(dict.Count);
                foreach (var kv in dict)
                {
                    bw.Write(kv.Key);
                    bw.Write(kv.Value);
                }
                break;
            default:
                // 未知类型写入字符串形式
                bw.Write(data.ToString());
                break;
        }
    }

    // 字段信息模型
    public class FieldInfoData
    {
        public string Name;
        public string Type;
        public string Comment;
    }
}