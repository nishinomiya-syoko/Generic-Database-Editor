using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 通用数据编辑器窗口（重构布局版）
/// 布局：
/// - 上半区：左=数据类型按钮组 | 右=功能按钮（保存/读取/导出/导入）
/// - 下半区：左=实例列表+新建按钮 | 右=字段编辑窗口
/// </summary>
public class GeneralEditorWindow : EditorWindow
{
    // 核心数据
    private Type _selectedDataType;
    private object _currentDataInstance;
    private string _instanceName = "Default";
    private List<Type> _editableDataTypes;

    // 布局相关
    private Vector2 _typeButtonScrollPos; // 数据类型按钮滚动
    private Vector2 _instanceListScrollPos; // 实例列表滚动
    private Vector2 _editAreaScrollPos; // 编辑区滚动
    private string _newInstanceName = "NewInstance"; // 新建实例的名称输入

    // 打开编辑器窗口
    [MenuItem("Tools/通用数据编辑器/General Editor Window")]
    public static void OpenWindow()
    {
        GeneralEditorWindow window = GetWindow<GeneralEditorWindow>("通用数据编辑器");
        window.minSize = new Vector2(800, 600); // 增大最小窗口尺寸适配新布局
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

    // 核心UI绘制（按新布局重构）
    private void OnGUI()
    {
        // ========== 上半部分：操作按钮区 ==========
        EditorGUILayout.BeginVertical("Box"); // 外框

        // 上半区-行1：左=数据类型按钮 | 右=功能按钮
        EditorGUILayout.BeginHorizontal();

        // ---- 左侧：数据类型按钮组 ----
        EditorGUILayout.BeginVertical(GUILayout.Width(400));
        EditorGUILayout.LabelField("数据类型", EditorStyles.boldLabel);
        _typeButtonScrollPos = EditorGUILayout.BeginScrollView(_typeButtonScrollPos, GUILayout.Height(60));
        DrawDataTypeButtons(); // 绘制数据类型按钮
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ---- 右侧：功能按钮组 ----
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        DrawFunctionButtons(); // 绘制保存/读取/导出/导入按钮
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical(); // 上半区外框结束

        // ========== 下半部分：数据列表+编辑区 ==========
        if (_selectedDataType == null)
        {
            EditorGUILayout.HelpBox("请先选择左侧的数据类型", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // ---- 左侧：实例列表 + 新建按钮 ----
        EditorGUILayout.BeginVertical("Box", GUILayout.Width(250));
        EditorGUILayout.LabelField($"{GetDataTypeDisplayName()} - 实例列表", EditorStyles.boldLabel);
        DrawInstanceList(); // 绘制实例列表
        DrawNewInstanceButton(); // 绘制新建实例按钮
        EditorGUILayout.EndVertical();

        // ---- 右侧：字段编辑窗口 ----
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField($"编辑：{_instanceName}", EditorStyles.boldLabel);
        _editAreaScrollPos = EditorGUILayout.BeginScrollView(_editAreaScrollPos);
        DrawDataFields(); // 绘制字段编辑区
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    #region 布局组件绘制
    // 1. 绘制数据类型按钮组（横向排列，选中高亮）
    private void DrawDataTypeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        int buttonIndex = 0;
        foreach (var dataType in _editableDataTypes)
        {
            string displayName = GetDataTypeDisplayName(dataType);
            // 选中状态高亮
            bool isSelected = dataType == _selectedDataType;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = Color.cyan;

            // 绘制按钮
            if (GUILayout.Button(displayName, GUILayout.Width(120), GUILayout.Height(40)))
            {
                _selectedDataType = dataType;
                _instanceName = "Default"; // 切换类型重置实例
                LoadCurrentInstance();
            }

            GUI.backgroundColor = originalColor;
            buttonIndex++;

            // 每3个按钮换行（优化布局）
            if (buttonIndex % 3 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // 2. 绘制功能按钮（保存/读取/导出/导入）
    private void DrawFunctionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        // 读取按钮
        if (GUILayout.Button("读取数据", GUILayout.Width(100), GUILayout.Height(30)))
        {
            LoadCurrentInstance();
            // EditorUtility.DisplayDialog("提示", "数据读取完成", "确定");
            Debug.Log("数据读取完成");
        }

        // 保存按钮
        if (GUILayout.Button("保存数据", GUILayout.Width(100), GUILayout.Height(30)))
        {
            bool success = SaveCurrentInstance();
            // EditorUtility.DisplayDialog(success ? "成功" : "失败", 
            //     success ? "数据保存完成" : "数据保存失败", "确定");
            Debug.Log(success ? "数据保存完成" : "数据保存失败");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        // 导出Excel按钮
        if (GUILayout.Button("导出Excel", GUILayout.Width(100), GUILayout.Height(30)))
        {
            ExportCurrentDataToExcel();
        }

        // 导入Excel按钮
        if (GUILayout.Button("导入Excel", GUILayout.Width(100), GUILayout.Height(30)))
        {
            ImportDataFromExcel();
        }

        EditorGUILayout.EndHorizontal();
    }

    // 3. 绘制当前类型的实例列表
    private void DrawInstanceList()
    {
        _instanceListScrollPos = EditorGUILayout.BeginScrollView(_instanceListScrollPos, GUILayout.Height(200));

        // 获取该类型的所有实例名
        MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
            .MakeGenericMethod(_selectedDataType);
        string[] instanceNames = (string[])getInstanceNamesMethod.Invoke(null, null);

        // 绘制每个实例项（选中高亮）
        foreach (string name in instanceNames)
        {
            bool isSelected = name == _instanceName;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = Color.green;

            // 点击切换实例
            if (GUILayout.Button(name, GUILayout.Height(30)))
            {
                _instanceName = name;
                LoadCurrentInstance();
            }

            GUI.backgroundColor = originalColor;
        }

        EditorGUILayout.EndScrollView();
    }

    // 4. 绘制新建实例按钮
    private void DrawNewInstanceButton()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        // 新建实例名称输入框
        _newInstanceName = EditorGUILayout.TextField("新实例名", _newInstanceName);

        // 新建按钮
        if (GUILayout.Button("新建", GUILayout.Width(60)))
        {
            if (string.IsNullOrEmpty(_newInstanceName.Trim()))
            {
                // EditorUtility.DisplayDialog("提示", "实例名不能为空", "确定");
                Debug.Log("实例名不能为空");
                return;
            }

            // 创建新实例并切换
            _instanceName = _newInstanceName.Trim();
            _currentDataInstance = Activator.CreateInstance(_selectedDataType);
            // EditorUtility.DisplayDialog("提示", $"已创建新实例：{_instanceName}", "确定");
            Debug.Log($"已创建新实例：{_instanceName}");
        }

        EditorGUILayout.EndHorizontal();
    }

    // 5. 绘制字段编辑区（原有逻辑，调整滚动容器）
    private void DrawDataFields()
    {
        if (_currentDataInstance == null) return;

        // 获取所有可序列化字段
        FieldInfo[] fields = _selectedDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
            .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
            .ToArray();

        // 遍历绘制字段
        foreach (var field in fields)
        {
            object value = field.GetValue(_currentDataInstance);
            object newValue = DrawFieldControl(field.Name, field.FieldType, value);

            if (!Equals(newValue, value))
            {
                field.SetValue(_currentDataInstance, newValue);
            }
        }
    }

    // 辅助：绘制单个字段控件（原有逻辑）
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
            newValue = Quaternion.Euler(EditorGUILayout.Vector3Field("", ((Quaternion)value).eulerAngles));

        // 枚举类型
        else if (fieldType.IsEnum)
            newValue = EditorGUILayout.EnumPopup((Enum)value);

        // 暂不支持的类型
        else
            EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}", GUILayout.Width(200));

        EditorGUILayout.EndHorizontal();
        return newValue;
    }
    #endregion

    #region 辅助方法
    // 获取数据类型的显示名称（优先自定义，无则用类名）
    private string GetDataTypeDisplayName(Type type = null)
    {
        type ??= _selectedDataType;
        if (type == null) return "";

        var attr = type.GetCustomAttribute<EditableDataAttribute>();
        return string.IsNullOrEmpty(attr.DisplayName) ? type.Name : attr.DisplayName;
    }

    // 加载当前实例数据
    private void LoadCurrentInstance()
    {
        if (_selectedDataType == null) return;

        MethodInfo loadMethod = typeof(GenericDataPersistence).GetMethod("LoadData")
            .MakeGenericMethod(_selectedDataType);
        _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });
    }

    // 保存当前实例数据
    private bool SaveCurrentInstance()
    {
        if (_selectedDataType == null || _currentDataInstance == null) return false;

        MethodInfo saveMethod = typeof(GenericDataPersistence).GetMethod("SaveData")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
        AssetDatabase.Refresh();
        return success;
    }

    // 导出Excel
    private void ExportCurrentDataToExcel()
    {
        if (_currentDataInstance == null || _selectedDataType == null)
        {
            // EditorUtility.DisplayDialog("提示", "无数据可导出", "确定");
            Debug.Log("无数据可导出");
            return;
        }

        string defaultFileName = $"{_selectedDataType.Name}_{_instanceName}";
        string excelPath = ExcelDataUtility.SelectExcelSavePath(defaultFileName);
        if (string.IsNullOrEmpty(excelPath))
            return;

        MethodInfo exportMethod = typeof(ExcelDataUtility).GetMethod("ExportToExcel")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)exportMethod.Invoke(null, new[] { _currentDataInstance, excelPath, _instanceName });

        // EditorUtility.DisplayDialog(success ? "成功" : "失败",
        //     success ? $"Excel导出成功：\n{excelPath}" : "Excel导出失败", "确定");
        Debug.Log(success ? $"Excel导出成功：\n{excelPath}" : "Excel导出失败");
    }

    // 导入Excel
    private void ImportDataFromExcel()
    {
        if (_selectedDataType == null)
        {
            // EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
            Debug.Log("请先选择数据类型");
            return;
        }

        string excelPath = ExcelDataUtility.SelectExcelLoadPath();
        if (string.IsNullOrEmpty(excelPath))
            return;

        MethodInfo importMethod = typeof(ExcelDataUtility).GetMethod("ImportFromExcel")
            .MakeGenericMethod(_selectedDataType);
        _currentDataInstance = importMethod.Invoke(null, new[] { excelPath, _instanceName });

        // EditorUtility.DisplayDialog("成功", "Excel数据导入完成", "确定");
        Debug.Log("Excel数据导入完成");
    }
    #endregion
}