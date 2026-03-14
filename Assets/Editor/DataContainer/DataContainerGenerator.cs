using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;
using UnityEngine;
using System;

public class DataContainerGenerator : EditorWindow
{
    private static readonly string TEMPLATE_PATH = Constant.DATA_CONTAINER_PATH; // 生成的数据容器存放路径
    private MonoScript _targetSOScript; // 选中的 SO 脚本

    [MenuItem("Tools/SO Tool/Generate SO to DataClass")]
    public static void ShowWindow()
    {
        GetWindow<DataContainerGenerator>("生成数据容器");
    }

    private void OnGUI()
    {
        GUILayout.Label("选择 ScriptableObject 脚本生成数据容器", EditorStyles.boldLabel);
        _targetSOScript = (MonoScript)EditorGUILayout.ObjectField("SO 脚本", _targetSOScript, typeof(MonoScript), false);

        if (GUILayout.Button("生成代码") && _targetSOScript != null)
        {
            GenerateDataContainer(_targetSOScript);
        }
    }

    private void GenerateDataContainer(MonoScript soScript)
    {
        // 获取 SO 类的类型
        Type soType = soScript.GetClass();
        if (soType == null || !typeof(ScriptableObject).IsAssignableFrom(soType))
        {
            EditorUtility.DisplayDialog("错误", "请选择继承自 ScriptableObject 的脚本", "确定");
            return;
        }

        // 生成数据容器类名（如 ItemSO → ItemData）
        string dataClassName = soType.Name.Replace("SO", "Data");
        // string outputPath = Path.Combine(Path.GetDirectoryName(soScript.path), $"{dataClassName}.cs");
        string outputPath = Path.Combine(TEMPLATE_PATH, $"{dataClassName}.cs");
        if (!Directory.Exists(TEMPLATE_PATH))
        {
            Directory.CreateDirectory(TEMPLATE_PATH);
        }

        // 生成代码内容
        StringBuilder code = new StringBuilder();
        code.AppendLine("namespace Top\n{\n");
        code.AppendLine(" using UnityEngine;");
        code.AppendLine(" using System;");
        code.AppendLine();
        code.AppendLine( "[Serializable]");
        code.AppendLine($" public class {dataClassName}");
        code.AppendLine(" {");

        // 遍历 SO 的字段，生成数据容器字段
        foreach (FieldInfo field in soType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            // 跳过非序列化字段、静态字段、Unity 内部字段
            if (field.IsStatic || Attribute.IsDefined(field, typeof(NonSerializedAttribute))) continue;
            if (field.Name.StartsWith("m_") || field.Name == "m_Script") continue;

            // 获取字段的序列化特性
            var serializeAttr = Attribute.IsDefined(field, typeof(SerializeField)) ? "[SerializeField] " : "";
            // var serializeAttr = Attribute.IsDefined(field, typeof(SerializeFieldAttribute)) ? "[SerializeField] " : "";
            // 字段修饰符（公开/私有，数据容器建议公开或加 [SerializeField]）
            string modifier = field.IsPublic ? "public " : "";
            // 字段类型和名称
            string fieldType = field.FieldType.FullName?.Replace("System.", "") ?? field.FieldType.Name;
            string fieldName = field.Name;

            // 生成字段行
            code.AppendLine($"    {serializeAttr}{modifier}{fieldType} {fieldName};");
        }

        // 生成构造函数（从 SO 初始化）
        code.AppendLine();
        code.AppendLine($"    public {dataClassName}() {{ }}");
        code.AppendLine();
        code.AppendLine($"    public {dataClassName}({soType.Name} so)");
        code.AppendLine("    {");
        foreach (FieldInfo field in soType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.IsStatic || Attribute.IsDefined(field, typeof(NonSerializedAttribute)) || field.Name.StartsWith("m_") || field.Name == "m_Script") continue;
            code.AppendLine($"        {field.Name} = so.{field.Name};");
        }
        code.AppendLine("    }");

        code.AppendLine("}\n}");

        // 写入文件
        File.WriteAllText(outputPath, code.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("成功", $"数据容器已生成：\n{outputPath}", "确定");
    }
}