using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using DataCenter;

namespace Top
{
    // 【修复】添加 Unity 类型自定义转换器
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Vector2(dict["x"], dict["y"]);
        }
    }

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Vector3(dict["x"], dict["y"], dict["z"]);
        }
    }

    public class Vector4Converter : JsonConverter<Vector4>
    {
        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Vector4(dict["x"], dict["y"], dict["z"], dict["w"]);
        }
    }

    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("r");
            writer.WriteValue(value.r);
            writer.WritePropertyName("g");
            writer.WriteValue(value.g);
            writer.WritePropertyName("b");
            writer.WriteValue(value.b);
            writer.WritePropertyName("a");
            writer.WriteValue(value.a);
            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Color(dict["r"], dict["g"], dict["b"], dict["a"]);
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Quaternion(dict["x"], dict["y"], dict["z"], dict["w"]);
        }
    }

    public class RectConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("width");
            writer.WriteValue(value.width);
            writer.WritePropertyName("height");
            writer.WriteValue(value.height);
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, float>>(reader);
            return new Rect(dict["x"], dict["y"], dict["width"], dict["height"]);
        }
    }

    public class BoundsConverter : JsonConverter<Bounds>
    {
        public override void WriteJson(JsonWriter writer, Bounds value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("center");
            serializer.Serialize(writer, value.center);
            writer.WritePropertyName("size");
            serializer.Serialize(writer, value.size);
            writer.WriteEndObject();
        }

        public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dict = serializer.Deserialize<Dictionary<string, object>>(reader);
            Vector3 center = JsonConvert.DeserializeObject<Vector3>(dict["center"].ToString());
            Vector3 size = JsonConvert.DeserializeObject<Vector3>(dict["size"].ToString());
            return new Bounds(center, size);
        }
    }

    public class ExcelDataTableTool : EditorWindow
    {
        private string excelPath = Constant.EXCEL_PATH;
        private string classOutputFolder = Constant.DATA_CLASS_PATH;
        private static readonly string DATA_TXT_PATH = Constant.DATA_TXT_PATH;
        private static readonly string DATA_BINARY_PATH = Constant.DATA_BINARY_PATH;
        public static readonly string DATA_BINARY_NAMEEND = Constant.DATA_BINARY_NAMEEND;
        private static string classNamespace = Constant.DATA_NAMESPACE;
        private bool useBinary = true;
        private bool useTxt = true;
        private bool compressBinary = false;
        private bool showAdvancedSettings = false;

        // 批量处理统计
        private int totalFiles = 0;
        private int successFiles = 0;
        private int failedFiles = 0;
        private int totalSheets = 0;
        private int successSheets = 0;

        // 【修复】JSON 序列化设置
        private static JsonSerializerSettings _jsonSettings;

        private static JsonSerializerSettings GetJsonSettings()
        {
            if (_jsonSettings == null)
            {
                _jsonSettings = new JsonSerializerSettings
                {
                    // 【核心修复】忽略循环引用
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    // 【核心修复】忽略 null 值
                    NullValueHandling = NullValueHandling.Ignore,
                    // 【核心修复】使用自定义 Unity 类型转换器
                    Converters = new JsonConverter[]
                    {
                        new Vector2Converter(),
                        new Vector3Converter(),
                        new Vector4Converter(),
                        new ColorConverter(),
                        new QuaternionConverter(),
                        new RectConverter(),
                        new BoundsConverter()
                    },
                    // 格式化输出
                    Formatting = Formatting.Indented
                };
            }
            return _jsonSettings;
        }

        [MenuItem("Tools/Excel2DataTable/Choose Excel File Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExcelDataTableTool>("Excel 数据表工具");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // 【修复】设置 EPPlus License
            // ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // 初始化 JSON 设置
            GetJsonSettings();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("📊 Unity Excel 数据表生成工具", EditorStyles.boldLabel);
            GUILayout.Label("支持格式：.xlsx | 引擎版本：Unity 2022+", EditorStyles.miniLabel);
            
            GUILayout.Space(15);
            
            // Excel 文件选择
            GUILayout.Label("📁 Excel 配置", EditorStyles.boldLabel);
            excelPath = EditorGUILayout.TextField("Excel 文件路径", excelPath);
            if (GUILayout.Button("选择 Excel 文件", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFilePanel("选择 Excel 文件", "", "xlsx");
                if (!string.IsNullOrEmpty(path)) excelPath = path;
            }

            GUILayout.Space(15);
            
            // 输出路径
            GUILayout.Label("📂 输出配置", EditorStyles.boldLabel);
            classOutputFolder = EditorGUILayout.TextField("代码输出文件夹", classOutputFolder);
            classNamespace = EditorGUILayout.TextField("命名空间", classNamespace);
            GUILayout.Label($"文本输出文件夹:{DATA_TXT_PATH}", EditorStyles.boldLabel);
            GUILayout.Label($"二进制输出文件夹:{DATA_BINARY_PATH}", EditorStyles.boldLabel);

            GUILayout.Space(15);
            
            // 导出格式
            GUILayout.Label("🔧 导出格式", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            useTxt = EditorGUILayout.ToggleLeft("📄 TXT (JSON)", useTxt, GUILayout.Width(150));
            useBinary = EditorGUILayout.ToggleLeft("🔒 Binary (DAT)", useBinary, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            
            if (useBinary)
            {
                compressBinary = EditorGUILayout.Toggle("压缩二进制文件", compressBinary);
            }

            GUILayout.Space(15);
            
            // 高级设置
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "⚙️ 高级设置");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Excel 格式要求:", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox(
                    "第 1 行：字段名称\n" +
                    "第 2 行：数据类型 (int, string, List<float> 等)\n" +
                    "第 3 行：字段注释\n" +
                    "第 4 行起：数据内容", 
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);
            
            // 执行按钮
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("🚀 开始转换 & 生成代码", GUILayout.Height(40)))
            {
                ProcessExcel(excelPath);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);
            
            // 状态信息
            if (!string.IsNullOrEmpty(excelPath))
            {
                EditorGUILayout.HelpBox(
                    $"当前文件：{Path.GetFileName(excelPath)}\n" +
                    $"命名空间：{classNamespace}\n" +
                    $"代码输出：{classOutputFolder}", 
                    MessageType.None);
            }
        }

        [MenuItem("Tools/Excel2DataTable/Fast Generate Code (批量生成) _F5")]
        public static void FastGenerate()
        {
            string excelFolder = Constant.EXCEL_PATH;

            if (!Directory.Exists(excelFolder))
            {
                EditorUtility.DisplayDialog("错误",
                    $"Excel 目录不存在：{excelFolder}\n\n请创建该目录或修改 Constant.EXCEL_PATH 配置",
                    "确定");
                return;
            }

            // 【修复】正确获取所有 Excel 文件
            List<string> excelFiles = new List<string>();
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
                EditorUtility.DisplayDialog("提示",
                    $"在 {excelFolder} 目录下未找到 Excel 文件",
                    "确定");
                return;
            }

            // 创建输出目录
            if (!Directory.Exists(Constant.DATA_BINARY_PATH))
                Directory.CreateDirectory(Constant.DATA_BINARY_PATH);
            if (!Directory.Exists(Constant.DATA_TXT_PATH))
                Directory.CreateDirectory(Constant.DATA_TXT_PATH);
            if (!Directory.Exists(Constant.DATA_CLASS_PATH))
                Directory.CreateDirectory(Constant.DATA_CLASS_PATH);

            try
            {
                var tool = CreateInstance<ExcelDataTableTool>();
                tool.totalFiles = excelFiles.Count;
                tool.successFiles = 0;
                tool.failedFiles = 0;
                tool.totalSheets = 0;
                tool.successSheets = 0;

                Debug.Log($"[Fast Generate] 开始批量处理 {excelFiles.Count} 个 Excel 文件...");

                for (int i = 0; i < excelFiles.Count; i++)
                {
                    string file = excelFiles[i];
                    float progress = (float)(i + 1) / excelFiles.Count;
                    
                    EditorUtility.DisplayProgressBar("处理中",
                        $"[{i + 1}/{excelFiles.Count}] {Path.GetFileName(file)}",
                        progress);
                    
                    Debug.Log($"[Fast Generate] [{i + 1}/{excelFiles.Count}] {Path.GetFileName(file)}");

                    try
                    {
                        tool.ProcessExcel(file);
                        tool.successFiles++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Fast Generate] 文件处理失败：{file}\n{ex.Message}\n{ex.StackTrace}");
                        tool.failedFiles++;
                    }
                }

                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();

                string msg = $"✅ 批量处理完成!\n\n" +
                             $"📁 文件统计:\n" +
                             $"  总文件：{tool.totalFiles}\n" +
                             $"  成功：{tool.successFiles}\n" +
                             $"  失败：{tool.failedFiles}\n\n" +
                             $"📊 表格统计:\n" +
                             $"  总表格：{tool.totalSheets}\n" +
                             $"  成功：{tool.successSheets}";

                Debug.Log(msg);
                EditorUtility.DisplayDialog("Fast Generate 完成", msg, "确定");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[Fast Generate] 严重错误：{e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"批量处理失败:\n{e.Message}", "确定");
            }
        }
        
        private void ProcessExcel(string excelPath)
        {
            Debug.Log($"[ProcessExcel] 正在处理文件：{excelPath}");
            
            if (string.IsNullOrEmpty(excelPath) || !File.Exists(excelPath))
            {
                EditorUtility.DisplayDialog("错误", "请选择有效的 .xlsx 文件！", "确定");
                return;
            }

            if (!excelPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("警告", "EPPlus 仅支持 .xlsx 格式！", "确定");
                return;
            }

            // 创建输出目录
            if (!Directory.Exists(DATA_TXT_PATH))
                Directory.CreateDirectory(DATA_TXT_PATH);
            if (!Directory.Exists(DATA_BINARY_PATH))
                Directory.CreateDirectory(DATA_BINARY_PATH);
            if (!Directory.Exists(classOutputFolder))
                Directory.CreateDirectory(classOutputFolder);

            try
            {
                // 【修复】设置 EPPlus License
                // ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(excelPath)))
                {
                    int successCount = 0;
                    int skipCount = 0;

                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        totalSheets++; // 【修复】更新统计

                        if (string.IsNullOrWhiteSpace(worksheet.Name) ||
                            worksheet.Name.StartsWith("~") ||
                            worksheet.Dimension == null ||
                            worksheet.Dimension.Rows < 3)
                        {
                            skipCount++;
                            continue;
                        }

                        try
                        {
                            ProcessWorksheet(worksheet);
                            successCount++;
                            successSheets++; // 【修复】更新统计
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[错误] Sheet '{worksheet.Name}' 处理失败：{ex.Message}\n{ex.StackTrace}");
                        }
                    }

                    AssetDatabase.Refresh();

                    string msg = $"✅ 处理完成!\n" +
                                 $"成功：{successCount} 个表\n" +
                                 $"跳过：{skipCount} 个表\n\n" +
                                 $"⚠️ 如果是首次运行，请等待 Unity 编译完成后再次点击按钮导出数据。";

                    Debug.Log(msg);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[严重错误] {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("错误", $"处理失败:\n{e.Message}", "确定");
            }
        }

        private void ProcessWorksheet(ExcelWorksheet sheet)
        {
            string tableName = sheet.Name.Trim();
            Debug.Log($"[处理] 表：{tableName}");

            // 1. 解析表头
            List<FieldDefinition> fields = ParseSheetHeader(sheet);
            if (fields.Count == 0)
            {
                Debug.LogWarning($"[跳过] 表 '{tableName}' 没有有效字段");
                return;
            }

            // 2. 生成 C# 类
            GenerateCSharpClass(tableName, fields);

            // 3. 检查类是否已编译
            Type rowType = Type.GetType($"{classNamespace}.{tableName}Row");
            if (rowType == null)
            {
                Debug.LogWarning($"[等待] 类 '{tableName}Row' 尚未编译。请等待 Unity 编译完成后再次运行工具导出 TXT 与 BINARY 文件。");
                return;
            }

            // 4. 解析数据行
            List<object> dataList = ParseDataRows(sheet, fields, rowType);

            // 5. 导出文件
            string txtFullPath = Path.Combine(DATA_TXT_PATH, tableName);
            string binaryFullPath = Path.Combine(DATA_BINARY_PATH, tableName);

            if (useTxt)
            {
                try
                {
                    // 【核心修复】使用自定义 JSON 设置
                    string json = JsonConvert.SerializeObject(dataList, GetJsonSettings());
                    File.WriteAllText($"{txtFullPath}.txt", json, Encoding.UTF8);
                    Debug.Log($"[✓] 生成 TXT: {tableName}.txt ({dataList.Count} 行)");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[错误] 生成 TXT 失败：{tableName}.txt - {ex.Message}\n{ex.StackTrace}");
                }
            }

            if (useBinary)
            {
                try
                {
                    byte[] bytes = SerializeToBinary(dataList, fields);
                    
                    if (compressBinary)
                    {
                        bytes = Compress(bytes);
                        File.WriteAllBytes($"{binaryFullPath}{DATA_BINARY_NAMEEND}", bytes);
                        Debug.Log($"[✓] 生成 Binary(压缩): {tableName}{DATA_BINARY_NAMEEND} ({bytes.Length} bytes)");
                    }
                    else
                    {
                        File.WriteAllBytes($"{binaryFullPath}{DATA_BINARY_NAMEEND}", bytes);
                        Debug.Log($"[✓] 生成 Binary: {tableName}{DATA_BINARY_NAMEEND} ({bytes.Length} bytes)");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[错误] 生成 Binary 失败：{tableName}{DATA_BINARY_NAMEEND} - {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private List<FieldDefinition> ParseSheetHeader(ExcelWorksheet sheet)
        {
            List<FieldDefinition> fields = new List<FieldDefinition>();
            int colCount = sheet.Dimension.End.Column;

            for (int col = 1; col <= colCount; col++)
            {
                string name = sheet.Cells[1, col]?.Text?.Trim();
                string typeStr = sheet.Cells[2, col]?.Text?.Trim();
                string comment = sheet.Cells[3, col]?.Text?.Trim();

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(typeStr))
                    continue;
                if (typeStr.StartsWith("#"))
                        continue;

                try
                {
                    Type systemType = ParseTypeString(typeStr);
                    fields.Add(new FieldDefinition
                    {
                        Name = name,
                        RawType = typeStr,
                        SystemType = systemType,
                        Comment = comment
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[类型错误] 字段 '{name}' 类型 '{typeStr}': {ex.Message}");
                }
            }

            return fields;
        }

        private Type ParseTypeString(string typeStr)
        {
            typeStr = typeStr.Trim();

            if (typeStr.EndsWith("[]"))
            {
                string inner = typeStr.Substring(0, typeStr.Length - 2);
                Type innerType = GetSimpleType(inner);
                return innerType.MakeArrayType();
            }

            if (typeStr.StartsWith("List<") && typeStr.EndsWith(">"))
            {
                string inner = typeStr.Substring(5, typeStr.Length - 6);
                Type innerType = GetSimpleType(inner);
                return typeof(List<>).MakeGenericType(innerType);
            }

            if (typeStr.StartsWith("Dictionary<") && typeStr.EndsWith(">"))
            {
                string inner = typeStr.Substring(11, typeStr.Length - 12);
                string[] parts = inner.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new Exception($"Dictionary 格式错误：{typeStr}");
                
                Type keyType = GetSimpleType(parts[0].Trim());
                Type valType = GetSimpleType(parts[1].Trim());
                return typeof(Dictionary<,>).MakeGenericType(keyType, valType);
            }

            if (typeStr.StartsWith("Array<") && typeStr.EndsWith(">"))
            {
                string inner = typeStr.Substring(6, typeStr.Length - 7);
                Type innerType = GetSimpleType(inner);
                return typeof(List<>).MakeGenericType(innerType);
            }

            return GetSimpleType(typeStr);
        }

        private Type GetSimpleType(string name)
        {
            string lowerName = name.ToLower().Trim();
            
            switch (lowerName)
            {
                case "int":
                case "integer":
                    return typeof(int);
                case "long":
                case "int64":
                    return typeof(long);
                case "float":
                case "single":
                    return typeof(float);
                case "double":
                    return typeof(double);
                case "bool":
                case "boolean":
                    return typeof(bool);
                case "string":
                case "str":
                case "text":
                    return typeof(string);
                case "byte":
                    return typeof(byte);
                case "short":
                case "int16":
                    return typeof(short);
                case "uint":
                case "uint32":
                    return typeof(uint);
                case "ulong":
                case "uint64":
                    return typeof(ulong);
                case "vector2":
                case "vec2":
                    return typeof(Vector2);
                case "vector3":
                case "vec3":
                    return typeof(Vector3);
                case "vector4":
                case "vec4":
                    return typeof(Vector4);
                case "color":
                    return typeof(Color);
                case "color32":
                    return typeof(Color32);
                case "quaternion":
                case "quat":
                    return typeof(Quaternion);
                case "rect":
                    return typeof(Rect);
                case "bounds":
                    return typeof(Bounds);
                case "datetime":
                case "date":
                    return typeof(DateTime);
                case "enum":
                    return typeof(int);
                default:
                    var t = Type.GetType($"{classNamespace}.{name}");
                    if (t != null) return t;
                    
                    t = Type.GetType($"UnityEngine.{name}");
                    if (t != null) return t;
                    
                    t = Type.GetType($"System.{name}");
                    if (t != null) return t;
                    
                    throw new Exception($"未知类型：{name}");
            }
        }

        private void GenerateCSharpClass(string tableName, List<FieldDefinition> fields)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("// ============================================================");
            sb.AppendLine($"// 自动生成的数据表类 - {tableName}");
            sb.AppendLine($"// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// 请勿手动修改，修改会被覆盖");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine($"namespace {classNamespace}");
            sb.AppendLine("{");
            
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {tableName} 数据行");
            sb.AppendLine($"    /// </summary>");
            // sb.AppendLine($"    [EditableData]");
            sb.AppendLine($"    public class {tableName}Row");
            sb.AppendLine("    {");

            foreach (var field in fields)
            {
                string typeCode = GetCSharpTypeName(field.SystemType);
                
                if (!string.IsNullOrEmpty(field.Comment))
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {field.Comment}");
                    sb.AppendLine($"        /// </summary>");
                }
                sb.AppendLine($"        public {typeCode} {field.Name};");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
            
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {tableName} 数据表容器");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {tableName}Table : DataTableBase<{tableName}Row>");
            sb.AppendLine("    {");
            sb.AppendLine($"        private static {tableName}Table _instance;");
            sb.AppendLine($"        public static {tableName}Table Instance");
            sb.AppendLine("        {");
            sb.AppendLine("            get");
            sb.AppendLine("            {");
            sb.AppendLine($"                if (_instance == null) _instance = new {tableName}Table();");
            sb.AppendLine("                return _instance;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public void Load(bool useBinary = false) => LoadTable(\"{tableName}\", useBinary);");
            sb.AppendLine("    }");
            
            sb.AppendLine("}");

            string filePath = Path.Combine(classOutputFolder, $"{tableName}Row.cs");
            
            if (File.Exists(filePath))
            {
                string existing = File.ReadAllText(filePath);
                if (StripComments(existing) == StripComments(sb.ToString()))
                {
                    return;
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[代码] 生成：{tableName}Row.cs");
        }

        private string StripComments(string code)
        {
            return System.Text.RegularExpressions.Regex.Replace(code, @"//.*", "");
        }

        private string GetCSharpTypeName(Type t)
        {
            if (t == null) return "object";
            
            if (t.IsArray)
                return $"{GetCSharpTypeName(t.GetElementType())}[]";
            
            if (t.IsGenericType)
            {
                var genericDef = t.GetGenericTypeDefinition();
                
                if (genericDef == typeof(List<>))
                    return $"List<{GetCSharpTypeName(t.GetGenericArguments()[0])}>";
                
                if (genericDef == typeof(Dictionary<,>))
                {
                    var args = t.GetGenericArguments();
                    return $"Dictionary<{GetCSharpTypeName(args[0])}, {GetCSharpTypeName(args[1])}>";
                }
            }
            
            string name = t.Name;
            return name.Replace("Single", "float")
                      .Replace("Double", "double")
                      .Replace("Int32", "int")
                      .Replace("Int64", "long")
                      .Replace("Boolean", "bool")
                      .Replace("String", "string");
        }

        private List<object> ParseDataRows(ExcelWorksheet sheet, List<FieldDefinition> fields, Type rowType)
        {
            List<object> resultList = new List<object>();
            int rowCount = sheet.Dimension.End.Row;
            int emptyRowCount = 0;

            for (int row = 4; row <= rowCount; row++)
            {
                bool isEmpty = true;
                for (int col = 1; col <= fields.Count && col <= sheet.Dimension.End.Column; col++)
                {
                    var val = sheet.Cells[row, col]?.Value;
                    if (val != null && !string.IsNullOrEmpty(val.ToString().Trim()))
                    {
                        isEmpty = false;
                        break;
                    }
                }
                
                if (isEmpty)
                {
                    emptyRowCount++;
                    continue;
                }

                object rowObj = Activator.CreateInstance(rowType);

                for (int i = 0; i < fields.Count; i++)
                {
                    int colIndex = i + 1;
                    
                    if (colIndex > sheet.Dimension.End.Column)
                    {
                        var field = rowType.GetField(fields[i].Name);
                        field?.SetValue(rowObj, GetDefaultValue(fields[i].SystemType));
                        continue;
                    }
                    
                    var cellValue = sheet.Cells[row, colIndex]?.Value;
                    string strValue = cellValue?.ToString() ?? "";
                    
                    FieldDefinition fieldDef = fields[i];
                    
                    try
                    {
                        object parsedValue = ParseCellValue(strValue, fieldDef.SystemType);
                        var field = rowType.GetField(fieldDef.Name);
                        field?.SetValue(rowObj, parsedValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[数据错误] 表:{sheet.Name} 行:{row} 字段:{fields[i].Name} 值:{strValue} - {ex.Message}");
                        var field = rowType.GetField(fields[i].Name);
                        field?.SetValue(rowObj, GetDefaultValue(fields[i].SystemType));
                    }
                }
                
                resultList.Add(rowObj);
            }

            if (emptyRowCount > 0)
                Debug.Log($"[信息] 表 '{sheet.Name}' 跳过 {emptyRowCount} 个空行");

            return resultList;
        }

        private object ParseCellValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value) || value.Trim() == "")
                return GetDefaultValue(targetType);

            value = value.Trim();

            try
            {
                if (targetType == typeof(int)) return int.Parse(value);
                if (targetType == typeof(long)) return long.Parse(value);
                if (targetType == typeof(float)) return float.Parse(value);
                if (targetType == typeof(double)) return double.Parse(value);
                if (targetType == typeof(bool)) 
                    return value.ToLower() == "true" || value == "1" || value.ToLower() == "yes";
                if (targetType == typeof(string)) return value;
                if (targetType == typeof(byte)) return byte.Parse(value);
                if (targetType == typeof(short)) return short.Parse(value);
                if (targetType == typeof(uint)) return uint.Parse(value);
                if (targetType == typeof(ulong)) return ulong.Parse(value);

                if (targetType == typeof(Vector2)) return ParseVector2(value);
                if (targetType == typeof(Vector3)) return ParseVector3(value);
                if (targetType == typeof(Vector4)) return ParseVector4(value);
                if (targetType == typeof(Color)) return ParseColor(value);
                if (targetType == typeof(Color32)) return ParseColor32(value);
                if (targetType == typeof(Quaternion)) return ParseQuaternion(value);
                if (targetType == typeof(Rect)) return ParseRect(value);
                if (targetType == typeof(Bounds)) return ParseBounds(value);
                if (targetType == typeof(DateTime)) return ParseDateTime(value);

                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type innerType = targetType.GetGenericArguments()[0];
                    return ParseCollection(value, innerType, targetType, true);
                }

                if (targetType.IsArray)
                {
                    Type innerType = targetType.GetElementType();
                    return ParseCollection(value, innerType, targetType, false);
                }

                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return ParseDictionary(value, targetType);
                }

                if (!targetType.IsPrimitive && targetType != typeof(string) && !targetType.IsEnum)
                {
                    return JsonConvert.DeserializeObject(value, targetType);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"解析失败 [{targetType.Name}]: {ex.Message}");
            }

            return GetDefaultValue(targetType);
        }

        private Vector2 ParseVector2(string value) => (Vector2)ParseVector(value, 2);
        private Vector3 ParseVector3(string value) => (Vector3)ParseVector(value, 3);
        private Vector4 ParseVector4(string value) => (Vector4)ParseVector(value, 4);

        private object ParseVector(string value, int dimensions)
        {
            value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
            string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            float[] nums = new float[dimensions];
            for (int i = 0; i < dimensions && i < parts.Length; i++)
            {
                nums[i] = float.Parse(parts[i].Trim());
            }

            if (dimensions == 2) return new Vector2(nums[0], nums[1]);
            if (dimensions == 3) return new Vector3(nums[0], nums[1], nums[2]);
            if (dimensions == 4) return new Vector4(nums[0], nums[1], nums[2], nums[3]);
            
            return Vector3.zero;
        }

        private Color ParseColor(string value)
        {
            value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
            string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 3)
            {
                float r = float.Parse(parts[0].Trim());
                float g = float.Parse(parts[1].Trim());
                float b = float.Parse(parts[2].Trim());
                float a = parts.Length >= 4 ? float.Parse(parts[3].Trim()) : 1f;
                return new Color(r, g, b, a);
            }
            
            return Color.white;
        }

        private Color32 ParseColor32(string value)
        {
            value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
            string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 3)
            {
                byte r = byte.Parse(parts[0].Trim());
                byte g = byte.Parse(parts[1].Trim());
                byte b = byte.Parse(parts[2].Trim());
                byte a = parts.Length >= 4 ? byte.Parse(parts[3].Trim()) : (byte)255;
                return new Color32(r, g, b, a);
            }
            
            return Color.white;
        }

        private Quaternion ParseQuaternion(string value)
        {
            value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
            string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 4)
            {
                return new Quaternion(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim()),
                    float.Parse(parts[3].Trim())
                );
            }
            
            return Quaternion.identity;
        }

        private Rect ParseRect(string value)
        {
            value = value.Replace("(", "").Replace(")", "").Replace("{", "").Replace("}", "");
            string[] parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 4)
            {
                return new Rect(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim()),
                    float.Parse(parts[2].Trim()),
                    float.Parse(parts[3].Trim())
                );
            }
            
            return Rect.zero;
        }

        private Bounds ParseBounds(string value)
        {
            if (value.Trim().StartsWith("{"))
            {
                return JsonConvert.DeserializeObject<Bounds>(value, GetJsonSettings());
            }
            return new Bounds(Vector3.zero, Vector3.one);
        }

        private DateTime ParseDateTime(string value)
        {
            if (DateTime.TryParse(value, out DateTime result))
                return result;
            return DateTime.MinValue;
        }

        private object ParseCollection(string value, Type innerType, Type collectionType, bool isList)
        {
            if (value.Trim().StartsWith("["))
            {
                return JsonConvert.DeserializeObject(value, collectionType, GetJsonSettings());
            }

            char separator = value.Contains("|") ? '|' : ',';
            string[] parts = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            if (isList)
            {
                IList list = (IList)Activator.CreateInstance(collectionType);
                foreach (var part in parts)
                {
                    list.Add(ParseCellValue(part.Trim(), innerType));
                }
                return list;
            }
            else
            {
                Array arr = Array.CreateInstance(innerType, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    arr.SetValue(ParseCellValue(parts[i].Trim(), innerType), i);
                }
                return arr;
            }
        }

        private object ParseDictionary(string value, Type dictType)
        {
            if (value.Trim().StartsWith("{"))
            {
                return JsonConvert.DeserializeObject(value, dictType, GetJsonSettings());
            }

            throw new Exception("Dictionary 类型必须使用 JSON 格式，如：{\"key\": 1}");
        }

        private object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);
            return null;
        }

        private byte[] SerializeToBinary(List<object> dataList, List<FieldDefinition> fields)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)'D');
                bw.Write((byte)'T');
                bw.Write((byte)'B');
                bw.Write((byte)1);
                
                bw.Write(dataList.Count);

                foreach (var obj in dataList)
                {
                    foreach (var field in fields)
                    {
                        WriteFieldToBinary(bw, field.Name, field.SystemType, obj);
                    }
                }

                return ms.ToArray();
            }
        }

        private void WriteFieldToBinary(BinaryWriter bw, string fieldName, Type fieldType, object obj)
        {
            FieldInfo field = obj.GetType().GetField(fieldName);
            if (field == null)
            {
                WriteDefaultValue(bw, fieldType);
                return;
            }

            object value = field.GetValue(obj);

            if (value == null)
            {
                WriteDefaultValue(bw, fieldType);
                return;
            }

            if (fieldType == typeof(int)) bw.Write((int)value);
            else if (fieldType == typeof(long)) bw.Write((long)value);
            else if (fieldType == typeof(float)) bw.Write((float)value);
            else if (fieldType == typeof(double)) bw.Write((double)value);
            else if (fieldType == typeof(bool)) bw.Write((bool)value);
            else if (fieldType == typeof(string)) bw.Write((string)value ?? "");
            else if (fieldType == typeof(byte)) bw.Write((byte)value);
            else if (fieldType == typeof(short)) bw.Write((short)value);
            else if (fieldType == typeof(uint)) bw.Write((uint)value);
            else if (fieldType == typeof(ulong)) bw.Write((ulong)value);
            
            else if (fieldType == typeof(Vector2))
            {
                var v = (Vector2)value;
                bw.Write(v.x);
                bw.Write(v.y);
            }
            else if (fieldType == typeof(Vector3))
            {
                var v = (Vector3)value;
                bw.Write(v.x);
                bw.Write(v.y);
                bw.Write(v.z);
            }
            else if (fieldType == typeof(Vector4))
            {
                var v = (Vector4)value;
                bw.Write(v.x); bw.Write(v.y); bw.Write(v.z); bw.Write(v.w);
            }
            else if (fieldType == typeof(Color))
            {
                var c = (Color)value;
                bw.Write(c.r); bw.Write(c.g); bw.Write(c.b); bw.Write(c.a);
            }
            else if (fieldType == typeof(Color32))
            {
                var c = (Color32)value;
                bw.Write(c.r); bw.Write(c.g); bw.Write(c.b); bw.Write(c.a);
            }
            else if (fieldType == typeof(Quaternion))
            {
                var q = (Quaternion)value;
                bw.Write(q.x); bw.Write(q.y); bw.Write(q.z); bw.Write(q.w);
            }
            else if (fieldType == typeof(Rect))
            {
                var r = (Rect)value;
                bw.Write(r.x); bw.Write(r.y); bw.Write(r.width); bw.Write(r.height);
            }
            else if (fieldType == typeof(DateTime))
            {
                bw.Write(((DateTime)value).ToBinary());
            }
            
            else if (fieldType.IsArray)
            {
                Array arr = (Array)value;
                bw.Write(arr.Length);
                Type innerType = fieldType.GetElementType();
                foreach (var item in arr)
                {
                    WriteSimpleValue(bw, item, innerType);
                }
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                IList list = (IList)value;
                bw.Write(list.Count);
                Type innerType = fieldType.GetGenericArguments()[0];
                foreach (var item in list)
                {
                    WriteSimpleValue(bw, item, innerType);
                }
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                IDictionary dict = (IDictionary)value;
                bw.Write(dict.Count);
                Type keyType = fieldType.GetGenericArguments()[0];
                Type valType = fieldType.GetGenericArguments()[1];
                
                foreach (DictionaryEntry entry in dict)
                {
                    WriteSimpleValue(bw, entry.Key, keyType);
                    WriteSimpleValue(bw, entry.Value, valType);
                }
            }
            
            else
            {
                string json = JsonConvert.SerializeObject(value, GetJsonSettings());
                bw.Write(json ?? "");
            }
        }

        private void WriteSimpleValue(BinaryWriter bw, object value, Type type)
        {
            if (value == null)
            {
                WriteDefaultValue(bw, type);
                return;
            }

            if (type == typeof(int)) bw.Write((int)value);
            else if (type == typeof(long)) bw.Write((long)value);
            else if (type == typeof(float)) bw.Write((float)value);
            else if (type == typeof(double)) bw.Write((double)value);
            else if (type == typeof(bool)) bw.Write((bool)value);
            else if (type == typeof(string)) bw.Write((string)value ?? "");
            else if (type == typeof(byte)) bw.Write((byte)value);
            else if (type == typeof(short)) bw.Write((short)value);
            else bw.Write(value.ToString() ?? "");
        }

        private void WriteDefaultValue(BinaryWriter bw, Type t)
        {
            if (t == typeof(int)) bw.Write(0);
            else if (t == typeof(long)) bw.Write(0L);
            else if (t == typeof(float)) bw.Write(0f);
            else if (t == typeof(double)) bw.Write(0.0);
            else if (t == typeof(bool)) bw.Write(false);
            else if (t == typeof(string)) bw.Write("");
            else if (t == typeof(byte)) bw.Write((byte)0);
            else if (t == typeof(short)) bw.Write((short)0);
            else if (t == typeof(uint)) bw.Write(0u);
            else if (t == typeof(ulong)) bw.Write(0ul);
            else if (t == typeof(Vector2)) { bw.Write(0f); bw.Write(0f); }
            else if (t == typeof(Vector3)) { bw.Write(0f); bw.Write(0f); bw.Write(0f); }
            else if (t == typeof(Color)) { bw.Write(0f); bw.Write(0f); bw.Write(0f); bw.Write(1f); }
            else bw.Write(0);
        }

        private byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionMode.Compress))
                {
                    deflate.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        private byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    deflate.CopyTo(output);
                }
                return output.ToArray();
            }
        }
    }

    public class FieldDefinition
    {
        public string Name;
        public string RawType;
        public Type SystemType;
        public string Comment;
    }
}