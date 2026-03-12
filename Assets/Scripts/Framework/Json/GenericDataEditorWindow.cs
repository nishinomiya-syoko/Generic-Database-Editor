using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 通用数据编辑器窗口（支持任意标记[EditableData]的普通类）
/// </summary>
public class GenericDataEditorWindow : EditorWindow
{
    // 当前选中的可编辑类类型
    private Type _selectedDataType;
    // 当前编辑的数据实例
    private object _currentDataInstance;
    // 实例名（支持多实例）
    private string _instanceName = "Default";
    // 滚动位置
    private Vector2 _scrollPos;
    // 缓存所有标记[EditableData]的类
    private List<Type> _editableDataTypes;

    // 打开编辑器窗口
    [MenuItem("Tools/通用数据编辑器/打开编辑器")]
    public static void OpenWindow()
    {
        GenericDataEditorWindow window = GetWindow<GenericDataEditorWindow>("通用数据编辑器");
        window.minSize = new Vector2(500, 400);
        window.Show();
        window.Init();
    }

    // 初始化：扫描所有标记[EditableData]的类
    private void Init()
    {
        _editableDataTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(t => t.GetCustomAttribute<EditableDataAttribute>() != null && !t.IsAbstract && !t.IsInterface)
            .ToList();

        // 默认选中第一个可编辑类
        if (_editableDataTypes.Count > 0)
        {
            _selectedDataType = _editableDataTypes[0];
            LoadCurrentInstance();
        }
    }

    // 绘制编辑器UI
    private void OnGUI()
    {
        // 1. 顶部：选择可编辑类
        DrawTypeSelector();

        // 无可用类时提示
        if (_selectedDataType == null)
        {
            EditorGUILayout.HelpBox("未找到标记[EditableData]的类，请先给普通类添加该特性", MessageType.Info);
            return;
        }

        // 2. 实例名选择/输入
        DrawInstanceSelector();

        EditorGUILayout.Space(10);

        // 3. 按钮区域（保存/加载/重置）
        DrawActionButtons();

        EditorGUILayout.Space(10);

        // 4. 自动绘制数据编辑区域
        DrawDataFields();
    }

    #region UI绘制逻辑
    // 绘制类选择下拉框
    private void DrawTypeSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("选择数据类：", GUILayout.Width(100));

        // 构建类名列表（显示自定义名称或类名）
        var typeNames = _editableDataTypes.Select(t =>
        {
            var attr = t.GetCustomAttribute<EditableDataAttribute>();
            return string.IsNullOrEmpty(attr.DisplayName) ? t.Name : attr.DisplayName;
        }).ToArray();

        // 当前选中索引
        int currentIndex = _editableDataTypes.IndexOf(_selectedDataType);
        int newIndex = EditorGUILayout.Popup(currentIndex, typeNames);

        // 切换类时重置实例
        if (newIndex != currentIndex && newIndex >= 0)
        {
            _selectedDataType = _editableDataTypes[newIndex];
            _instanceName = "Default";
            LoadCurrentInstance();
        }

        EditorGUILayout.EndHorizontal();
    }

    // 绘制实例名选择/输入框
    private void DrawInstanceSelector()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("实例名：", GUILayout.Width(100));

        // 获取该类的所有实例名
        MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
            .MakeGenericMethod(_selectedDataType);
        string[] instanceNames = (string[])getInstanceNamesMethod.Invoke(null, null);

        // 实例名下拉框
        int instanceIndex = Array.IndexOf(instanceNames, _instanceName);
        instanceIndex = instanceIndex < 0 ? 0 : instanceIndex;
        instanceIndex = EditorGUILayout.Popup(instanceIndex, instanceNames, GUILayout.Width(150));

        // 自定义实例名输入
        string newInstanceName = EditorGUILayout.TextField(_instanceName);
        if (newInstanceName != _instanceName)
        {
            _instanceName = newInstanceName.Trim();
            if (string.IsNullOrEmpty(_instanceName))
                _instanceName = "Default";
            LoadCurrentInstance();
        }

        EditorGUILayout.EndHorizontal();
    }

    // 绘制操作按钮
    // private void DrawActionButtons()
    // {
    //     EditorGUILayout.BeginHorizontal();

    //     if (GUILayout.Button("加载数据", GUILayout.Width(100)))
    //     {
    //         LoadCurrentInstance();
    //         EditorUtility.DisplayDialog("提示", "数据加载完成", "确定");
    //     }

    //     if (GUILayout.Button("保存数据", GUILayout.Width(100)))
    //     {
    //         bool success = SaveCurrentInstance();
    //         if (success)
    //             EditorUtility.DisplayDialog("成功", "数据保存完成", "确定");
    //         else
    //             EditorUtility.DisplayDialog("失败", "数据保存失败", "确定");
    //     }

    //     if (GUILayout.Button("重置数据", GUILayout.Width(100)))
    //     {
    //         if (EditorUtility.DisplayDialog("确认", "是否重置为默认值？", "是", "否"))
    //         {
    //             _currentDataInstance = Activator.CreateInstance(_selectedDataType);
    //         }
    //     }

    //     EditorGUILayout.EndHorizontal();
    // }
    // 绘制操作按钮
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("加载数据", GUILayout.Width(100)))
        {
            LoadCurrentInstance();
            // EditorUtility.DisplayDialog("提示", "数据加载完成", "确定");
            Debug.Log("数据加载完成");
        }

        if (GUILayout.Button("保存数据", GUILayout.Width(100)))
        {
            bool success = SaveCurrentInstance();
            if (success)
                // EditorUtility.DisplayDialog("成功", "数据保存完成", "确定");
                Debug.Log("数据保存完成");
            else
                // EditorUtility.DisplayDialog("失败", "数据保存失败", "确定");
                Debug.Log("数据保存失败");
        }

        if (GUILayout.Button("重置数据", GUILayout.Width(100)))
        {
            if (EditorUtility.DisplayDialog("确认", "是否重置为默认值？", "是", "否"))
            {
                _currentDataInstance = Activator.CreateInstance(_selectedDataType);
            }
        }

        // ====== 新增Excel导出按钮 ======
        if (GUILayout.Button("导出Excel", GUILayout.Width(100)))
        {
            ExportCurrentDataToExcel();
        }

        // ====== 新增Excel导入按钮 ======
        if (GUILayout.Button("导入Excel", GUILayout.Width(100)))
        {
            ImportDataFromExcel();
        }

        // EditorUtility.DisplayDialog("成功", "数据保存完成", "确定");
        // Debug.Log("数据保存完成");
        EditorGUILayout.EndHorizontal();
    }

    // 新增：导出当前数据到Excel
    private void ExportCurrentDataToExcel()
    {
        if (_currentDataInstance == null || _selectedDataType == null)
        {
            // EditorUtility.DisplayDialog("提示", "无数据可导出", "确定");
            Debug.Log("无数据可导出");
            return;
        }

        // 选择保存路径
        string defaultFileName = $"{_selectedDataType.Name}_{_instanceName}";
        string excelPath = ExcelDataUtility.SelectExcelSavePath(defaultFileName);
        if (string.IsNullOrEmpty(excelPath))
            return;

        // 调用导出方法（反射适配泛型）
        MethodInfo exportMethod = typeof(ExcelDataUtility).GetMethod("ExportToExcel")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)exportMethod.Invoke(null, new[] { _currentDataInstance, excelPath, _instanceName });

        if (success)
            // EditorUtility.DisplayDialog("成功", $"Excel导出成功：\n{excelPath}", "确定");
            Debug.Log($"Excel导出成功：\n{excelPath}");
        else
            // EditorUtility.DisplayDialog("失败", "Excel导出失败", "确定");
            Debug.Log("Excel导出失败");
    }

    // 新增：从Excel导入数据
    private void ImportDataFromExcel()
    {
        if (_selectedDataType == null)
        {
            // EditorUtility.DisplayDialog("提示", "请先选择数据类", "确定");
            Debug.Log("请先选择数据类");

            return;
        }

        // 选择Excel文件
        string excelPath = ExcelDataUtility.SelectExcelLoadPath();
        if (string.IsNullOrEmpty(excelPath))
            return;

        // 调用导入方法（反射适配泛型）
        MethodInfo importMethod = typeof(ExcelDataUtility).GetMethod("ImportFromExcel")
            .MakeGenericMethod(_selectedDataType);
        _currentDataInstance = importMethod.Invoke(null, new[] { excelPath, _instanceName });

        // EditorUtility.DisplayDialog("成功", "Excel数据导入完成", "确定");
        Debug.Log("Excel数据导入完成");
    }

    // 自动绘制数据字段（核心：反射+编辑器控件）
    private void DrawDataFields()
    {
        if (_currentDataInstance == null) return;

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // 获取所有可序列化的字段（非静态、公共字段，或标记[SerializeField]的私有字段）
        FieldInfo[] fields = _selectedDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f =>
            {
                // 排除静态字段
                if (f.IsStatic) return false;
                // 公共字段 或 标记[SerializeField]的私有字段
                return f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
            }).ToArray();

        // 遍历字段并绘制编辑器控件
        foreach (var field in fields)
        {
            // 跳过编译器自动生成的字段
            if (field.Name.Contains("<") || field.Name.Contains(">"))
                continue;

            // 获取字段当前值
            object value = field.GetValue(_currentDataInstance);

            // 根据字段类型绘制对应的编辑器控件
            object newValue = DrawFieldControl(field.Name, field.FieldType, value);

            // 值变化时更新
            if (!Equals(newValue, value))
            {
                field.SetValue(_currentDataInstance, newValue);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    // 根据类型绘制对应的编辑器控件（支持常见Unity类型）
    private object DrawFieldControl(string label, Type fieldType, object value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(150));

        object newValue = value;

        // 基础类型
        if (fieldType == typeof(string))
            newValue = EditorGUILayout.TextField((string)value);
        else if (fieldType == typeof(int))
            newValue = EditorGUILayout.IntField((int)value);
        else if (fieldType == typeof(float))
            newValue = EditorGUILayout.FloatField((float)value);
        else if (fieldType == typeof(bool))
            newValue = EditorGUILayout.Toggle((bool)value);
        else if (fieldType == typeof(double))
            newValue = EditorGUILayout.DoubleField((double)value);
        else if (fieldType == typeof(long))
            newValue = EditorGUILayout.LongField((long)value);

        // Unity常用类型
        else if (fieldType == typeof(Vector2))
            newValue = EditorGUILayout.Vector2Field("", (Vector2)value);
        else if (fieldType == typeof(Vector3))
            newValue = EditorGUILayout.Vector3Field("", (Vector3)value);
        else if (fieldType == typeof(Vector4))
            newValue = EditorGUILayout.Vector4Field("", (Vector4)value);
        else if (fieldType == typeof(Color))
            newValue = EditorGUILayout.ColorField("", (Color)value);
        else if (fieldType == typeof(Rect))
            newValue = EditorGUILayout.RectField("", (Rect)value);
        else if (fieldType == typeof(Bounds))
            newValue = EditorGUILayout.BoundsField("", (Bounds)value);
        else if (fieldType == typeof(Quaternion))
            newValue = EditorGUILayout.Vector4Field("", ((Quaternion)value).eulerAngles);

        // 暂不支持的类型（显示文本）
        else
        {
            EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}", GUILayout.Width(200));
        }

        EditorGUILayout.EndHorizontal();
        return newValue;
    }
    #endregion

    #region 数据加载/保存
    // 加载当前选中类+实例名的数据
    private void LoadCurrentInstance()
    {
        MethodInfo loadMethod = typeof(GenericDataPersistence).GetMethod("LoadData")
            .MakeGenericMethod(_selectedDataType);
        _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });
    }

    // 保存当前编辑的数据
    private bool SaveCurrentInstance()
    {
        MethodInfo saveMethod = typeof(GenericDataPersistence).GetMethod("SaveData")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
        AssetDatabase.Refresh(); // 刷新资源库
        return success;
    }
    #endregion
}