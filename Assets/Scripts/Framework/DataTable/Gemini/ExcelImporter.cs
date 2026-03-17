// using System.IO;
// using System.Text;
// using OfficeOpenXml; // EPPlus
// using System.Collections.Generic;
// using Newtonsoft.Json; // 用于生成TXT(Json格式)
// using System;

// namespace Gemini
// {
//     public class ExcelImporter
//     {
//         // 定义生成路径
//         public static string ScriptOutputPath = "Assets/Scripts/Data/";
//         public static string TxtOutputPath = "Assets/Data/Txt/";
//         public static string BinaryOutputPath = "Assets/Data/Binary/";

//         public static void ImportExcel(string excelFilePath)
//         {
//             FileInfo fileInfo = new FileInfo(excelFilePath);
//             using (ExcelPackage package = new ExcelPackage(fileInfo))
//             {
//                 foreach (ExcelWorksheet sheet in package.Workbook.Worksheets)
//                 {
//                     string className = sheet.Name;
//                     int colCount = sheet.Dimension.End.Column;
//                     int rowCount = sheet.Dimension.End.Row;

//                     List<FieldInfoData> fields = new List<FieldInfoData>();

//                     // 解析前三行 (1: Name, 2: Type, 3: Comment)
//                     for (int col = 1; col <= colCount; col++)
//                     {
//                         fields.Add(new FieldInfoData
//                         {
//                             Name = sheet.Cells[1, col].Text,
//                             Type = sheet.Cells[2, col].Text,
//                             Comment = sheet.Cells[3, col].Text
//                         });
//                     }

//                     // 1. 生成 C# 解析类
//                     GenerateCSharpClass(className, fields);

//                     // 2. 提取数据并生成 TXT 和 Binary
//                     ExportData(sheet, className, fields, rowCount, colCount);
//                 }
//             }
//         }

//         private static void GenerateCSharpClass(string className, List<FieldInfoData> fields)
//         {
//             StringBuilder sb = new StringBuilder();
//             sb.AppendLine("using System;");
//             sb.AppendLine("using System.Collections.Generic;\n");
//             sb.AppendLine("[Serializable]");
//             sb.AppendLine($"public class {className} {{");

//             foreach (var field in fields)
//             {
//                 sb.AppendLine($"    /// <summary>");
//                 sb.AppendLine($"    /// {field.Comment}");
//                 sb.AppendLine($"    /// </summary>");
//                 sb.AppendLine($"    public {field.Type} {field.Name};");
//             }
//             sb.AppendLine("}");

//             File.WriteAllText(Path.Combine(ScriptOutputPath, $"{className}.cs"), sb.ToString());
//         }

//         private static void ExportData(ExcelWorksheet sheet, string className, List<FieldInfoData> fields, int rowCount, int colCount)
//         {
//             List<Dictionary<string, object>> allData = new List<Dictionary<string, object>>();

//             // 读取数据 (从第4行开始)
//             for (int row = 4; row <= rowCount; row++)
//             {
//                 Dictionary<string, object> rowData = new Dictionary<string, object>();
//                 for (int col = 1; col <= colCount; col++)
//                 {
//                     string cellValue = sheet.Cells[row, col].Text;
//                     object parsedValue = ParseType(cellValue, fields[col - 1].Type);
//                     rowData[fields[col - 1].Name] = parsedValue;
//                 }
//                 allData.Add(rowData);
//             }

//             // 导出 TXT (使用 JSON，因为 JSON 原生支持 List, Array, Dictionary 结构)
//             string json = JsonConvert.SerializeObject(allData, Formatting.Indented);
//             File.WriteAllText(Path.Combine(TxtOutputPath, $"{className}.txt"), json);

//             // 导出 Binary
//             using (FileStream fs = new FileStream(Path.Combine(BinaryOutputPath, $"{className}.bytes"), FileMode.Create))
//             using (BinaryWriter bw = new BinaryWriter(fs))
//             {
//                 bw.Write(allData.Count); // 写入总行数
//                 foreach (var rowData in allData)
//                 {
//                     foreach (var field in fields)
//                     {
//                         WriteBinaryData(bw, rowData[field.Name], field.Type);
//                     }
//                 }
//             }
//         }

//         // 基础类型解析工具 (支持 array, list, dictionary)
//         private static object ParseType(string value, string type)
//         {
//             if (string.IsNullOrEmpty(value)) return null;

//             if (type == "int") return int.Parse(value);
//             if (type == "float") return float.Parse(value);
//             if (type == "string") return value;
//             if (type == "int[]") // 假设逗号分隔: 1,2,3
//                 return Array.ConvertAll(value.Split(','), int.Parse);
//             if (type == "List<string>")
//                 return new List<string>(value.Split(','));
//             if (type == "Dictionary<int,string>") // 假设格式: 1:apple,2:banana
//             {
//                 var dict = new Dictionary<int, string>();
//                 foreach (var pair in value.Split(','))
//                 {
//                     var kv = pair.Split(':');
//                     dict.Add(int.Parse(kv[0]), kv[1]);
//                 }
//                 return dict;
//             }
//             return value;
//         }

//         private static void WriteBinaryData(BinaryWriter bw, object data, string type)
//         {
//             // 这里需要实现具体的二进制写入逻辑，例如 bw.Write((int)data);
//             // 为了保持代码简洁，通常在生产中会结合 Protobuf 或 MemoryPack
//         }

//         public class FieldInfoData { public string Name; public string Type; public string Comment; }
//     }
// }