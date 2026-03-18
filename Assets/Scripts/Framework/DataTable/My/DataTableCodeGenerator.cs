using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using DataCenter;

namespace DataCenter
{
    /// <summary>
    /// 数据表代码生成编辑器
    /// </summary>
    public class DataTableCodeGenerator : EditorWindow
    {
        private static readonly string TXT_PATH = Constant.DATA_TXT_PATH;
        private static readonly string BINARY_PATH = Constant.DATA_BINARY_PATH;
        private static readonly string CLASS_PATH = Constant.DATA_CLASS_PATH;
        private static readonly string DATA_NAMESPACE = Constant.DATA_NAMESPACE;


        private string _txtFilePath; // 选择的TXT文件路径
        private string _outputCsPath; // 生成的CS文件路径
        private string _className = "TableData"; // 生成的类名

        // 编辑器菜单入口
        [MenuItem("Tools/DataTable/Generate Code")]
        public static void ShowWindow()
        {
            GetWindow<DataTableCodeGenerator>("数据表代码生成器");
        }

        private void OnGUI()
        {
            GUILayout.Label("数据表代码生成配置", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 1. 选择TXT文件
            GUILayout.Label("1. 选择TXT数据表文件");
            GUILayout.BeginHorizontal();
            _txtFilePath = EditorGUILayout.TextField("TXT路径", _txtFilePath);
            if (GUILayout.Button("选择文件", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFilePanel("选择TXT数据表", "", "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    _txtFilePath = path;
                    // 自动填充类名（取TXT文件名）
                    _className = Path.GetFileNameWithoutExtension(path);
                    // 自动填充输出路径（默认到Scripts/Table目录）
                    string projectPath = Application.dataPath;
                    _outputCsPath = Path.Combine(projectPath, "Scripts/Table", $"{_className}.cs");
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // 2. 设置类名
            GUILayout.Label("2. 生成的类名");
            _className = EditorGUILayout.TextField("类名", _className);
            GUILayout.Space(10);

            // 3. 设置输出路径
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

            // 4. 生成按钮
            GUI.enabled = !string.IsNullOrEmpty(_txtFilePath) && !string.IsNullOrEmpty(_outputCsPath) && !string.IsNullOrEmpty(_className);
            if (GUILayout.Button("生成数据类", GUILayout.Height(40)))
            {
                bool success = GenerateDataClass(_txtFilePath, _outputCsPath, _className);
                if (success)
                {
                    EditorUtility.DisplayDialog("成功", "数据类生成完成！", "确定");
                    EditorUtility.RevealInFinder(_outputCsPath);
                    // 刷新Unity资源
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
        /// 生成数据实体类
        /// </summary>
        private bool GenerateDataClass(string txtPath, string outputPath, string className)
        {
            //    解析表头
            if (!DataTableParser.ParseTableHeader(txtPath, out List<string> fieldNames, out List<string> fieldTypes, out List<string> fieldComments))
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

                // 逐字段生成属性
                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    string fieldType = fieldTypes[i];
                    string comment = fieldComments[i];

                    // 转换为C#类型（小写转首字母大写）
                    string csType = GetCSharpType(fieldType);

                    // 写入XML注释
                    sb.AppendLine($"      /// <summary>");
                    sb.AppendLine($"      /// {comment}");
                    sb.AppendLine($"      /// </summary>");
                    // 生成自动属性 
                    sb.AppendLine($"     public {csType} {fieldName} {{ get; set; }}");
                    sb.AppendLine();
                }

                // 生成构造函数（初始化默认值）
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// 构造函数（初始化默认值）");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public {className}()");
                sb.AppendLine("    {");
                foreach (var fieldName in fieldNames)
                {
                    sb.AppendLine($"        {fieldName} = default;");
                }
                sb.AppendLine("    }");

                sb.AppendLine(" }");
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
        /// 将数据表类型转换为C#类型
        /// </summary>
        private string GetCSharpType(string tableType)
        {
            switch (tableType.ToLower())
            {
                case "int": return "int";
                case "float": return "float";
                case "bool": return "bool";
                case "string": return "string";
                default:
                    Debug.LogWarning($"未识别的类型{tableType}，默认使用string");
                    return "string";
            }
        }
    }
}