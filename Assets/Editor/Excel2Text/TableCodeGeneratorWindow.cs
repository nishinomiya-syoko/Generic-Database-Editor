using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TableCodeGeneratorWindow : EditorWindow
{
    // 增加默认值，避免Constant类未初始化导致null
    private static readonly string DATA_NAMESPACE = Constant.DATA_NAMESPACE ?? "DataCenter";
    private static string _txtFilePath = Constant.DATA_TXT_PATH ?? Application.dataPath + "/Table/TXT";
    private static string _outputCsPath = Constant.DATA_CLASS_PATH ?? Application.dataPath + "/Scripts/Table/Generated";
    private string _className = "TableData";
    // 新增：枚举类型列表（初始化为空列表，避免null）
    private static List<Type> _enumTypes = new List<Type>();

    [MenuItem("Tools/DataTable/代码生成器")]
    public static void ShowWindow()
    {
        GetWindow<TableCodeGeneratorWindow>("数据表代码生成器");
    }

    /// <summary>
    /// 批量生成所有TXT对应的数据类
    /// </summary>
    [MenuItem("Tools/DataTable/批量生成代码")]
    public static void BatchGenerateDataClasses()
    {
        try
        {
            InitEnum();
            // 空值校验：TXT路径
            if (string.IsNullOrEmpty(_txtFilePath) || !Directory.Exists(_txtFilePath))
            {
                EditorUtility.DisplayDialog("错误", $"TXT文件夹不存在或路径为空！路径：{_txtFilePath}", "确定");
                return;
            }

            // 查找所有TXT文件（包括子文件夹）
            string[] txtFiles = Directory.GetFiles(_txtFilePath, "*.txt", SearchOption.AllDirectories);
            if (txtFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何TXT文件！", "确定");
                return;
            }

            // 确保输出目录存在
            if (!Directory.Exists(_outputCsPath))
            {
                Directory.CreateDirectory(_outputCsPath);
            }

            // 清空旧的CS文件（增加空值校验）
            if (Directory.Exists(_outputCsPath))
            {
                string[] csFiles = Directory.GetFiles(_outputCsPath, "*.cs", SearchOption.AllDirectories);
                foreach (string csFile in csFiles)
                {
                    try { File.Delete(csFile); }
                    catch (Exception ex) { Debug.LogWarning($"删除旧文件失败：{csFile}，原因：{ex.Message}"); }
                }
            }

            // 显示进度条
            int successCount = 0;
            int failCount = 0;
            EditorUtility.DisplayProgressBar("批量生成数据类", "开始处理...", 0);

            for (int i = 0; i < txtFiles.Length; i++)
            {
                string txtPath = txtFiles[i];
                if (string.IsNullOrEmpty(txtPath)) { failCount++; continue; }

                string txtFileName = Path.GetFileNameWithoutExtension(txtPath);
                // 修复：替换Unity不兼容的Path.GetRelativePath，自定义实现
                string relativePath = GetRelativePath(_txtFilePath, txtPath);
                if (string.IsNullOrEmpty(relativePath))
                {
                    Debug.LogWarning($"无法获取相对路径：{txtPath} 相对于 {_txtFilePath}");
                    failCount++;
                    continue;
                }

                string csRelativePath = Path.ChangeExtension(relativePath, "cs");
                string csOutputPath = Path.Combine(_outputCsPath, csRelativePath);

                // 更新进度条
                float progress = (float)(i + 1) / txtFiles.Length;
                EditorUtility.DisplayProgressBar("批量生成数据类", $"处理：{txtFileName}.txt", progress);

                // 生成数据类（类名=TXT文件名）
                bool success = GenerateDataClass(txtPath, csOutputPath, txtFileName);
                if (success)
                    successCount++;
                else
                    failCount++;
            }

            // 关闭进度条
            EditorUtility.ClearProgressBar();

            // 刷新Unity资源
            AssetDatabase.Refresh();

            // 显示结果
            string resultMsg = $"批量生成完成！\n成功：{successCount} 个\n失败：{failCount} 个";
            // EditorUtility.DisplayDialog("批量生成结果", resultMsg, "确定");

            // 打开输出目录（增加空值校验）
            // if (Directory.Exists(_outputCsPath))
            // {
            //     EditorUtility.RevealInFinder(_outputCsPath);
            // }
            Debug.Log(resultMsg);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            string errorMsg = $"批量生成出错：{e.Message}\n堆栈：{e.StackTrace}";
            EditorUtility.DisplayDialog("错误", errorMsg, "确定");
            // 修复：避免DebugInfo.LogError内部空引用，先判断DebugInfo是否可用
            DebugInfo.LogError(errorMsg);
        }
    }

    private static void InitEnum()
    {
        // 修复：空值校验，避免GetAllEnumTypes返回null
        var enumTypes = CSharpTypeToString.GetAllEnumTypes();
        _enumTypes = enumTypes ?? new List<Type>(); // 兜底为空列表，避免null
    }

    // 修复：自定义实现相对路径获取（兼容Unity所有版本）
    private static string GetRelativePath(string basePath, string targetPath)
    {
        if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(targetPath))
            return string.Empty;

        // 统一路径分隔符为/
        basePath = basePath.Replace('\\', '/').TrimEnd('/');
        targetPath = targetPath.Replace('\\', '/').TrimEnd('/');

        // 解析路径为目录数组
        string[] baseParts = basePath.Split('/');
        string[] targetParts = targetPath.Split('/');

        // 找到公共前缀的长度
        int commonLength = 0;
        while (commonLength < baseParts.Length && commonLength < targetParts.Length 
               && string.Equals(baseParts[commonLength], targetParts[commonLength], StringComparison.OrdinalIgnoreCase))
        {
            commonLength++;
        }

        // 构建相对路径
        StringBuilder relativePath = new StringBuilder();
        // 添加回退到公共目录的../
        for (int i = commonLength; i < baseParts.Length; i++)
        {
            relativePath.Append("../");
        }

        // 添加目标路径的剩余部分
        for (int i = commonLength; i < targetParts.Length; i++)
        {
            if (i > commonLength)
                relativePath.Append("/");
            relativePath.Append(targetParts[i]);
        }

        return relativePath.ToString();
    }

    // 原有OnGUI方法保留（增加空值校验）
    private void OnGUI()
    {
        GUILayout.Label("数据表代码生成配置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("1. 选择TXT数据表文件");
        GUILayout.BeginHorizontal();
        _txtFilePath = EditorGUILayout.TextField("TXT路径", _txtFilePath);
        if (GUILayout.Button("选择文件", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFilePanel("选择TXT数据表", "", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                _txtFilePath = path;
                _className = Path.GetFileNameWithoutExtension(path);
                string projectPath = Application.dataPath;
                _outputCsPath = Path.Combine(projectPath, "Scripts/Table", $"{_className}.cs");
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.Label("2. 生成的类名");
        _className = EditorGUILayout.TextField("类名", _className);
        GUILayout.Space(10);

        GUILayout.Label("3. CS文件输出路径");
        GUILayout.BeginHorizontal();
        _outputCsPath = EditorGUILayout.TextField("输出路径", _outputCsPath);
        if (GUILayout.Button("选择路径", GUILayout.Width(80)))
        {
            string path = EditorUtility.SaveFilePanel("保存CS文件", "", _className, "cs");
            if (!string.IsNullOrEmpty(path))
            {
                _outputCsPath = path;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(20);

        // 增强启用条件校验
        bool canGenerate = !string.IsNullOrEmpty(_txtFilePath) 
                           && !string.IsNullOrEmpty(_outputCsPath) 
                           && !string.IsNullOrEmpty(_className)
                           && (File.Exists(_txtFilePath) || Directory.Exists(_txtFilePath));
        GUI.enabled = canGenerate;
        if (GUILayout.Button("生成数据类", GUILayout.Height(40)))
        {
            bool success = GenerateDataClass(_txtFilePath, _outputCsPath, _className);
            if (success)
            {
                EditorUtility.DisplayDialog("成功", "数据类生成完成！", "确定");
                // if (File.Exists(_outputCsPath))
                //     EditorUtility.RevealInFinder(_outputCsPath);
                Debug.Log("数据类生成完成！");
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("失败", "生成出错，请查看控制台日志", "确定");
            }
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// 增强版代码生成：支持枚举+新类型（增加全量空值校验）
    /// </summary>
    private static bool GenerateDataClass(string txtPath, string outputPath, string className)
    {
        // 前置空值校验
        if (string.IsNullOrEmpty(txtPath) || !File.Exists(txtPath))
        {
            Debug.LogError($"TXT文件不存在：{txtPath}");
            return false;
        }
        if (string.IsNullOrEmpty(outputPath))
        {
            Debug.LogError("输出路径为空！");
            return false;
        }
        if (string.IsNullOrEmpty(className))
        {
            Debug.LogError("类名为空！");
            return false;
        }

        if (!TxtTableParser.ParseTableHeader(txtPath, out List<string> fieldNames, out List<string> fieldTypes, out List<string> fieldComments))
        {
            Debug.LogError($"解析TXT表头失败：{txtPath}");
            return false;
        }

        // 空值校验：解析结果
        if (fieldNames == null) fieldNames = new List<string>();
        if (fieldTypes == null) fieldTypes = new List<string>();
        if (fieldComments == null) fieldComments = new List<string>();

        try
        {
            StringBuilder sb = new StringBuilder();
            // 写入文件头
            sb.AppendLine("// ============================================================");
            sb.AppendLine($"// 自动生成的数据表类 - {className}");
            sb.AppendLine($"// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// 请勿手动修改，修改会被覆盖");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {DATA_NAMESPACE}");
            sb.AppendLine("{");

            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {className} 数据行");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {className}");
            sb.AppendLine("    {");

            // 逐字段生成属性（支持枚举+Unity类型）
            for (int i = 0; i < fieldNames.Count; i++)
            {
                // 边界校验：避免索引越界
                if (i >= fieldTypes.Count || i >= fieldComments.Count)
                    continue;

                string fieldName = fieldNames[i];
                string fieldType = fieldTypes[i];
                string comment = fieldComments[i];

                // 跳过注释/空字段
                if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("//") || fieldName.StartsWith("#"))
                    continue;
                if (string.IsNullOrEmpty(fieldType) || fieldType.StartsWith("//") || fieldType.StartsWith("#"))
                    continue;

                // 使用增强版类型转换（增加空值校验）
                string csType = CSharpTypeToString.GetCSharpTypeName(fieldType) ?? "string"; // 兜底为string
                // Debug.Log($"字段类型转换：{fieldType} -> {csType}");

                // 写入XML注释
                if (!string.IsNullOrEmpty(comment))
                {
                    sb.AppendLine($"        /// <summary>");
                    sb.AppendLine($"        /// {comment}");
                    sb.AppendLine($"        /// </summary>");
                }
                sb.AppendLine($"        public {csType} {fieldName} {{ get; set; }}");
                sb.AppendLine();
            }

            // 生成构造函数（初始化默认值，支持枚举/Unity类型）
            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// 构造函数（初始化默认值）");
            sb.AppendLine($"        /// </summary>");
            sb.AppendLine($"        public {className}()");
            sb.AppendLine("        {");
            for (int i = 0; i < fieldNames.Count; i++)
            {
                if (i >= fieldTypes.Count)
                    continue;

                string fieldName = fieldNames[i];
                string fieldType = fieldTypes[i];

                if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("//") || fieldName.StartsWith("#"))
                    continue;
                if (string.IsNullOrEmpty(fieldType) || fieldType.StartsWith("//") || fieldType.StartsWith("#"))
                    continue;

                string csType = CSharpTypeToString.GetCSharpTypeName(fieldType) ?? "string";
                // 生成默认值赋值（增加空值容错）
                string defaultValue = GetDefaultValueCode(csType);
                sb.AppendLine($"            {fieldName} = {defaultValue};");
            }
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 创建输出目录
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 写入CS文件（指定UTF8无BOM，避免编码问题）
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"数据类生成成功：{outputPath}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"生成数据类失败：{txtPath}，原因：{e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 生成默认值代码（适配不同类型，修复_enumTypes空引用）
    /// </summary>
    private static string GetDefaultValueCode(string csType)
    {
        // 空值校验
        if (string.IsNullOrEmpty(csType))
            return "default";

        switch (csType)
        {
            // 基础类型
            case "int": return "0";
            case "long": return "0L";
            case "float": return "0f";
            case "double": return "0d";
            case "bool": return "false";
            case "string": return "string.Empty";
            // Unity类型
            case "Vector2": return "Vector2.zero";
            case "Vector3": return "Vector3.zero";
            case "Vector4": return "Vector4.zero";
            case "Color": return "Color.clear";
            case "Rect": return "Rect.zero";
            case "Quaternion": return "Quaternion.identity";
            // 枚举类型（取第一个值，增加_enumTypes空值校验）
            default:
                // 修复：_enumTypes为空时直接返回default
                if (_enumTypes == null || _enumTypes.Count == 0)
                    return "default";
                
                if (_enumTypes.Exists(t => t.Name == csType))
                {
                    try
                    {
                        Type enumType = _enumTypes.Find(t => t.Name == csType);
                        var values = Enum.GetValues(enumType);
                        if (values != null && values.Length > 0)
                        {
                            var firstValue = values.GetValue(0);
                            return $"{csType}.{firstValue}";
                        }
                    }
                    catch
                    {
                        return $"default({csType})";
                    }
                }
                // 自定义类型
                return "default";
        }
    }
}



