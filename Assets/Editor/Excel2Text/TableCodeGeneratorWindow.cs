using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TableCodeGeneratorWindow : EditorWindow
{
    private static readonly string DATA_NAMESPACE = Constant.DATA_NAMESPACE;
    private static string _txtFilePath = Constant.DATA_TXT_PATH;
    private static string _outputCsPath = Constant.DATA_CLASS_PATH;
    private string _className = "TableData";
    // 新增：枚举类型列表
    private static List<Type> _enumTypes;

    [MenuItem("Tools/数据表工具/代码生成器")]
    public static void ShowWindow()
    {
        GetWindow<TableCodeGeneratorWindow>("数据表代码生成器");
    }
    /// <summary>
    /// 批量生成所有TXT对应的数据类
    /// </summary>
    /// <param name="_txtFilePath">TXT根文件夹</param>
    /// <param name="_outputCsPath">CS输出文件夹</param>
    [MenuItem("Tools/数据表工具/批量生成代码")]
    public static void BatchGenerateDataClasses()
    {
        try
        {
            InitEnum();
            if (!Directory.Exists(_txtFilePath))
            {
                EditorUtility.DisplayDialog("错误", "TXT文件夹不存在！", "确定");
                return;
            }

            // 查找所有TXT文件（包括子文件夹）
            string[] txtFiles = Directory.GetFiles(_txtFilePath, "*.txt", SearchOption.AllDirectories);
            if (txtFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何TXT文件！", "确定");
                return;
            }
            if (!Directory.Exists(_outputCsPath))
            {
                Directory.CreateDirectory(_outputCsPath);
            }
            string[] csFiles = Directory.GetFiles(_outputCsPath, "*.cs", SearchOption.AllDirectories);
            foreach (string csFile in csFiles)
            {
                File.Delete(csFile);
            }

            // 显示进度条
            int successCount = 0;
            int failCount = 0;
            EditorUtility.DisplayProgressBar("批量生成数据类", "开始处理...", 0);

            for (int i = 0; i < txtFiles.Length; i++)
            {
                string txtPath = txtFiles[i];
                string txtFileName = Path.GetFileNameWithoutExtension(txtPath);
                // 保持TXT的目录结构到CS输出目录
                string relativePath = Path.GetRelativePath(_txtFilePath, txtPath);
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
            EditorUtility.DisplayDialog("批量生成结果", resultMsg, "确定");

            // 打开输出目录
            EditorUtility.RevealInFinder(_outputCsPath);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"批量生成出错：{e.Message}", "确定");
        }
    }

    private static void InitEnum()
    {
        // 初始化枚举类型列表
        _enumTypes = CSharpTypeToString.GetAllEnumTypes();
    }

    // 原有OnGUI方法保留（无需修改）
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

        GUI.enabled = !string.IsNullOrEmpty(_txtFilePath) && !string.IsNullOrEmpty(_outputCsPath) && !string.IsNullOrEmpty(_className);
        if (GUILayout.Button("生成数据类", GUILayout.Height(40)))
        {
            bool success = GenerateDataClass(_txtFilePath, _outputCsPath, _className);
            if (success)
            {
                EditorUtility.DisplayDialog("成功", "数据类生成完成！", "确定");
                EditorUtility.RevealInFinder(_outputCsPath);
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
    /// 增强版代码生成：支持枚举+新类型
    /// </summary>
    private static bool GenerateDataClass(string txtPath, string outputPath, string className)
    {
        if (!TxtTableParser.ParseTableHeader(txtPath, out List<string> fieldNames, out List<string> fieldTypes, out List<string> fieldComments))
        {
            return false;
        }

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
                string fieldName = fieldNames[i];
                string fieldType = fieldTypes[i];
                string comment = fieldComments[i];

                if (fieldName.StartsWith("//") || fieldName.StartsWith("#") || fieldName == string.Empty)
                    continue;
                if (fieldType.StartsWith("//") || fieldType.StartsWith("#") || fieldType == string.Empty)
                    continue;

                // 使用增强版类型转换（传入枚举列表）
                string csType = CSharpTypeToString.GetCSharpTypeName(fieldType);
                Debug.Log($"字段类型：{fieldType} -> {csType}");

                // 写入XML注释
                if (!string.IsNullOrEmpty(comment))
                {
                    sb.AppendLine($"      /// <summary>");
                    sb.AppendLine($"      /// {comment}");
                    sb.AppendLine($"      /// </summary>");
                }
                sb.AppendLine($"      public {csType} {fieldName} {{ get; set; }}");
                sb.AppendLine();
            }

            // 生成构造函数（初始化默认值，支持枚举/Unity类型）
            sb.AppendLine($"      /// <summary>");
            sb.AppendLine($"      /// 构造函数（初始化默认值）");
            sb.AppendLine($"      /// </summary>");
            sb.AppendLine($"      public {className}()");
            sb.AppendLine("      {");
            for (int i = 0; i < fieldNames.Count; i++)
            {
                string fieldName = fieldNames[i];
                string fieldType = fieldTypes[i];

                if(fieldName.StartsWith("//") || fieldName.StartsWith("#") || fieldName == string.Empty)
                    continue;
                if (fieldType.StartsWith("//") || fieldType.StartsWith("#") || fieldType == string.Empty)
                    continue;
                string csType = CSharpTypeToString.GetCSharpTypeName(fieldType);
                    
                // 生成默认值赋值
                string defaultValue = GetDefaultValueCode(csType);
                sb.AppendLine($"        {fieldName} = {defaultValue};");
            }
            sb.AppendLine("      }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 创建输出目录
            string outputDir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 写入CS文件
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"数据类生成成功：{outputPath}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"生成数据类失败：{e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 生成默认值代码（适配不同类型）
    /// </summary>
    private static string GetDefaultValueCode(string csType)
    {
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
            // 枚举类型（取第一个值）
            default:
                if (_enumTypes.Exists(t => t.Name == csType))
                {
                    Type enumType = _enumTypes.Find(t => t.Name == csType);
                    var firstValue = Enum.GetValues(enumType).GetValue(0);
                    return $"{csType}.{firstValue}";
                }
                // 自定义类型
                return "default";
        }
    }
}