using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 通用数据编辑器窗口（重构布局版 - 支持集合类型）
/// 布局：
/// - 上半区：左=数据类型按钮组 | 右=功能按钮（保存/读取/导出/导入）
/// - 下半区：左=实例列表+新建按钮 | 右=字段编辑窗口
/// </summary>
public class GeneralEditorWindow : EditorWindow
{
    private static readonly string JSON_PATH = Constant.JSON_PATH; 
    private static readonly string JSON_EXTENSION = ".json";
    private static readonly string EXCEL_PATH = Constant.EXCEL_PATH; 
    
    // 核心数据
    private Type _selectedDataType;
    private object _currentDataInstance;
    private string _instanceName = "Default";
    private List<Type> _editableDataTypes;

    // 布局相关
    private Vector2 _typeButtonScrollPos;
    private Vector2 _instanceListScrollPos;
    private Vector2 _editAreaScrollPos;
    private string _newInstanceName = "NewInstance";

    // 集合编辑状态管理 (用于折叠/展开)
    private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/通用数据编辑器/General Editor Window")]
    public static void OpenWindow()
    {
        GeneralEditorWindow window = GetWindow<GeneralEditorWindow>("通用数据编辑器");
        window.minSize = new Vector2(800, 600);
        window.Show();
        window.Init();
    }

    private void Init()
    {
        _editableDataTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(t => t.GetCustomAttribute<EditableDataAttribute>() != null && !t.IsAbstract && !t.IsInterface)
            .ToList();

        if (_editableDataTypes.Count > 0)
        {
            _selectedDataType = _editableDataTypes[0];
            LoadCurrentInstance();
        }
    }

    private void OnGUI()
    {
        // ========== 上半部分：操作按钮区 ==========
        EditorGUILayout.BeginVertical("Box");

        EditorGUILayout.BeginHorizontal();

        // ---- 左侧：数据类型按钮组 ----
        EditorGUILayout.BeginVertical(GUILayout.Width(400));
        EditorGUILayout.LabelField("数据类型", EditorStyles.boldLabel);
        _typeButtonScrollPos = EditorGUILayout.BeginScrollView(_typeButtonScrollPos, GUILayout.Height(60));
        DrawDataTypeButtons();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // ---- 右侧：功能按钮组 ----
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        DrawFunctionButtons();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        if (_selectedDataType == null)
        {
            EditorGUILayout.HelpBox("请先选择左侧的数据类型", MessageType.Info);
            return;
        }

        // ========== 下半部分：数据列表+编辑区 ==========
        EditorGUILayout.BeginHorizontal();

        // ---- 左侧：实例列表 + 新建按钮 ----
        EditorGUILayout.BeginVertical("Box", GUILayout.Width(250));
        EditorGUILayout.LabelField($"{GetDataTypeDisplayName()} - 实例列表", EditorStyles.boldLabel);
        DrawNewInstanceButton();
        DrawInstanceList();
        EditorGUILayout.EndVertical();

        // ---- 右侧：字段编辑窗口 ----
        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField($"编辑：{_instanceName}", EditorStyles.boldLabel);
        _editAreaScrollPos = EditorGUILayout.BeginScrollView(_editAreaScrollPos);
        
        if (_currentDataInstance != null)
        {
            DrawDataFields();
        }
        else
        {
            EditorGUILayout.LabelField("当前实例数据为空");
        }
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    #region 布局组件绘制

    private void DrawDataTypeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        int buttonIndex = 0;
        foreach (var dataType in _editableDataTypes)
        {
            string displayName = GetDataTypeDisplayName(dataType);
            bool isSelected = dataType == _selectedDataType;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button(displayName, GUILayout.Width(120), GUILayout.Height(30)))
            {
                _selectedDataType = dataType;
                _instanceName = "Default";
                _foldoutStates.Clear(); // 切换类型时清空折叠状态
                LoadCurrentInstance();
            }

            GUI.backgroundColor = originalColor;
            buttonIndex++;

            if (buttonIndex % 3 == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFunctionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("读取数据", GUILayout.Width(120), GUILayout.Height(30)))
        {
            LoadCurrentInstance();
            EditorUtility.DisplayDialog("提示", "当前实例数据读取完成", "确定");
        }

        if (GUILayout.Button("保存数据", GUILayout.Width(120), GUILayout.Height(30)))
        {
            bool success = SaveCurrentInstance();
            EditorUtility.DisplayDialog(success ? "成功" : "失败",
                success ? "当前实例数据保存完成" : "当前实例数据保存失败", "确定");
        }

        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("删除当前实例", GUILayout.Width(120), GUILayout.Height(30)))
        {
            DeleteCurrentInstance();
        }
        GUI.backgroundColor = originalColor;

        if (GUILayout.Button("导出所有实例到Excel", GUILayout.Width(120), GUILayout.Height(30)))
        {
            ExportAllInstancesToExcel();
        }

        if (GUILayout.Button("从Excel导入所有实例", GUILayout.Width(120), GUILayout.Height(30)))
        {
            ImportAllInstancesFromExcel();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DeleteCurrentInstance()
    {
        if (_selectedDataType == null || string.IsNullOrEmpty(_instanceName))
        {
            Debug.LogWarning("请先选择要删除的实例");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "确认删除",
            $"是否永久删除实例：{_instanceName}？\n此操作不可恢复！",
            "删除",
            "取消"
        );

        if (!confirm) return;

        try
        {
            MethodInfo getSavePathMethod = typeof(GenericDataPersistence).GetMethod("GetSavePath")
                .MakeGenericMethod(_selectedDataType);
            string savePath = (string)getSavePathMethod.Invoke(null, new object[] { _instanceName });

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                AssetDatabase.Refresh();

                MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
                    .MakeGenericMethod(_selectedDataType);
                string[] remainingInstances = (string[])getInstanceNamesMethod.Invoke(null, null);

                if (remainingInstances.Length > 0)
                {
                    _instanceName = remainingInstances.Contains("Default") ? "Default" : remainingInstances[0];
                }
                else
                {
                    _instanceName = "Default";
                    _currentDataInstance = Activator.CreateInstance(_selectedDataType);
                }

                LoadCurrentInstance();
                Debug.Log($"实例 {_instanceName} 已删除");
            }
            else
            {
                Debug.LogWarning($"实例文件不存在，无需删除");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"删除实例 {_instanceName} 失败：{e}");
        }
    }

    private void ExportAllInstancesToExcel()
    {
        if (_selectedDataType == null)
        {
            Debug.LogWarning("请先选择数据类型");
            return;
        }

        string excelPath = EXCEL_PATH + $"{GetDataTypeDisplayName()}.xlsx";
        // 确保目录存在
        string dir = Path.GetDirectoryName(excelPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        MethodInfo exportMethod = typeof(ExcelDataUtility).GetMethod("ExportAllInstancesToExcel")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)exportMethod.Invoke(null, new[] { excelPath });

        Debug.Log(success ? $"[{GetDataTypeDisplayName()}] 所有实例导出到Excel成功\n路径：{excelPath}" : "导出失败");
    }

    private void ImportAllInstancesFromExcel()
    {
        if (_selectedDataType == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
            return;
        }

        string excelPath = ExcelDataUtility.SelectExcelLoadPath();
        if (string.IsNullOrEmpty(excelPath))
            return;

        MethodInfo importMethod = typeof(ExcelDataUtility).GetMethod("ImportAllInstancesFromExcel")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)importMethod.Invoke(null, new[] { excelPath });

        LoadCurrentInstance();
        EditorUtility.DisplayDialog(success ? "成功" : "失败",
            success ? $"[{GetDataTypeDisplayName()}] 从Excel导入所有实例成功" : "导入失败（无有效实例或文件错误）", "确定");
    }

    private void DrawNewInstanceButton()
    {
        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        GUILayout.Label("新实例名", GUILayout.Width(60));
        _newInstanceName = EditorGUILayout.TextField(_newInstanceName);

        if (GUILayout.Button("新建", GUILayout.Width(60)))
        {
            if (string.IsNullOrEmpty(_newInstanceName.Trim()))
            {
                Debug.LogWarning("实例名不能为空");
                return;
            }

            string newName = _newInstanceName.Trim();
            _instanceName = newName;
            _currentDataInstance = Activator.CreateInstance(_selectedDataType);

            bool saveSuccess = SaveCurrentInstance();
            if (saveSuccess)
            {
                Debug.Log($"已创建并保存新实例：{newName}");
                _newInstanceName = "NewInstance";
                _foldoutStates.Clear();
            }
            else
            {
                Debug.LogError($"创建新实例失败：保存失败");
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    private void DrawInstanceList()
    {
        _instanceListScrollPos = EditorGUILayout.BeginScrollView(_instanceListScrollPos);

        MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
            .MakeGenericMethod(_selectedDataType);
        string[] instanceNames = (string[])getInstanceNamesMethod.Invoke(null, null);

        foreach (string name in instanceNames)
        {
            bool isSelected = name == _instanceName;
            Color originalColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = Color.green;

            if (GUILayout.Button(name, GUILayout.Height(30)))
            {
                _instanceName = name;
                _foldoutStates.Clear();
                LoadCurrentInstance();
            }

            GUI.backgroundColor = originalColor;
        }

        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region 核心编辑逻辑 (支持 Array, List, Dictionary)

    // 5. 绘制字段编辑区 (重构以支持集合)
    private void DrawDataFields()
    {
        if (_currentDataInstance == null) return;

        FieldInfo[] fields = _selectedDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
            .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
            .ToArray();

        foreach (var field in fields)
        {
            object value = field.GetValue(_currentDataInstance);
            
            // 检查是否是集合类型
            if (IsCollectionType(field.FieldType))
            {
                DrawCollectionField(field, value);
            }
            else
            {
                // 普通字段
                object newValue = DrawFieldControl(field.Name, field.FieldType, value);
                if (!Equals(newValue, value))
                {
                    field.SetValue(_currentDataInstance, newValue);
                }
            }
        }
    }

    // 判断是否为支持的集合类型
    private bool IsCollectionType(Type type)
    {
        if (type.IsArray) return true;
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>))
                return true;
        }
        return false;
    }

    // 绘制集合字段 (List, Array, Dictionary)
    private void DrawCollectionField(FieldInfo field, object currentValue)
    {
        Type fieldType = field.FieldType;
        string fieldKey = field.Name; // 用于 foldout 状态唯一标识
        
        // 获取或初始化 Foldout 状态
        if (!_foldoutStates.ContainsKey(fieldKey))
            _foldoutStates[fieldKey] = true;

        // 绘制折叠头
        string label = $"{field.Name} ({GetCollectionTypeName(fieldType)})";
        _foldoutStates[fieldKey] = EditorGUILayout.Foldout(_foldoutStates[fieldKey], label, true, EditorStyles.foldoutHeader);

        if (!_foldoutStates[fieldKey])
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("Box");

        if (currentValue == null)
        {
            EditorGUILayout.LabelField("null (点击初始化)", EditorStyles.miniLabel);
            if (GUILayout.Button("初始化集合", GUILayout.Width(100)))
            {
                object newInstance = Activator.CreateInstance(fieldType);
                field.SetValue(_currentDataInstance, newInstance);
                currentValue = newInstance;
            }
        }
        else
        {
            if (fieldType.IsArray)
            {
                DrawArrayField(field, (Array)currentValue);
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                DrawListField(field, (IList)currentValue);
            }
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                DrawDictionaryField(field, (IDictionary)currentValue);
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private string GetCollectionTypeName(Type type)
    {
        if (type.IsArray) return $"Array[{type.GetElementType().Name}]";
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(List<>)) return $"List<{type.GetGenericArguments()[0].Name}>";
            if (def == typeof(Dictionary<,>)) return $"Dict<{type.GetGenericArguments()[0].Name}, {type.GetGenericArguments()[1].Name}>";
        }
        return type.Name;
    }

    // 绘制 Array
    private void DrawArrayField(FieldInfo field, Array array)
    {
        int length = array.Length;
        Type elementType = field.FieldType.GetElementType();

        EditorGUILayout.LabelField($"长度: {length}", EditorStyles.miniLabel);

        for (int i = 0; i < length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
            
            object itemValue = array.GetValue(i);
            object newItemValue = DrawFieldControl("", elementType, itemValue);

            if (!Equals(newItemValue, itemValue))
            {
                array.SetValue(newItemValue, i);
            }

            // 数组不支持动态删除单个元素（需重建），这里仅提供提示或整体重置
            // 若要支持删除，通常建议转为 List 编辑后再转回，或者提供"移除该项"按钮并重建数组
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                // 简单实现：创建一个新数组，少一个元素
                var newList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                for (int j = 0; j < length; j++)
                {
                    if (j != i) newList.Add(array.GetValue(j));
                }
                var newArray = Array.CreateInstance(elementType, newList.Count);
                newList.CopyTo(newArray, 0);
                field.SetValue(_currentDataInstance, newArray);
                return; // 重建后立即返回，避免索引错误
            }
            EditorGUILayout.EndHorizontal();
        }

        // 添加元素按钮 (数组需要扩容)
        if (GUILayout.Button("+ 添加元素"))
        {
            var newList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
            foreach (var item in array) newList.Add(item);
            
            // 添加默认值
            object defaultVal = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
            if (elementType == typeof(string)) defaultVal = "";
            newList.Add(defaultVal);

            var newArray = Array.CreateInstance(elementType, newList.Count);
            newList.CopyTo(newArray, 0);
            field.SetValue(_currentDataInstance, newArray);
        }
    }

    // 绘制 List
    private void DrawListField(FieldInfo field, IList list)
    {
        Type elementType = field.FieldType.GetGenericArguments()[0];
        
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));

            object itemValue = list[i];
            object newItemValue = DrawFieldControl("", elementType, itemValue);

            if (!Equals(newItemValue, itemValue))
            {
                list[i] = newItemValue;
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                list.RemoveAt(i);
                return; // 修改集合后立即返回
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ 添加元素"))
        {
            elementType = field.FieldType.GetGenericArguments()[0];
            object defaultVal = elementType.IsValueType ? Activator.CreateInstance(elementType) : null;
            if (elementType == typeof(string)) defaultVal = "";
            else if (elementType.IsClass && elementType != typeof(string)) defaultVal = Activator.CreateInstance(elementType);
            
            list.Add(defaultVal);
        }
    }

    // 绘制 Dictionary
    private void DrawDictionaryField(FieldInfo field, IDictionary dict)
    {
        Type keyType = field.FieldType.GetGenericArguments()[0];
        Type valueType = field.FieldType.GetGenericArguments()[1];

        // 注意：Dictionary 在遍历时不能直接修改结构（增删），所以我们需要暂存操作
        object keyToRemove = null;
        bool addNewRequested = false;
        
        // 临时存储新键值
        object newKey = null;
        object newValue = null;

        // 绘制现有项
        // 将 Keys 复制到数组以避免枚举期间修改异常
        var keys = dict.Keys.Cast<object>().ToArray();
        
        foreach (var key in keys)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 绘制 Key (通常只读，或者允许编辑但需要重建键值对)
            // 为简化，这里 Key 设为只读显示，若需编辑 Key，通常做法是删除旧项加新项
            EditorGUILayout.LabelField(key.ToString(), GUILayout.Width(100), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            
            object val = dict[key];
            object newVal = DrawFieldControl("", valueType, val);

            if (!Equals(newVal, val))
            {
                dict[key] = newVal;
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                keyToRemove = key;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (keyToRemove != null)
        {
            dict.Remove(keyToRemove);
            return;
        }

        // 添加新项区域
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("添加新项", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        
        // 简单的 Key 输入器 (仅支持基础类型作为 Key 的输入演示)
        newKey = DrawSimpleKeyInput(keyType);
        
        if (newKey != null)
        {
             // 临时 Value
             object defaultVal = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
             if (valueType == typeof(string)) defaultVal = "";
             
             // 这里为了能在同一行绘制并获取值，我们用一个临时的匿名对象或者再次调用 DrawFieldControl
             // 但由于 GUILayout 是立即模式，我们需要在一个单独的 pass 或者用临时变量
             // 简化处理：分两行，或者假设用户先输入 Key 点击添加后，下一帧再编辑 Value
             // 更好的方式：使用一个临时的 Dictionary entry 编辑器
             
             // 这里采用：如果 Key 有效且不为空，显示 Value 输入框和确认按钮
             EditorGUILayout.LabelField("Value:", GUILayout.Width(40));
             newValue = DrawFieldControl("", valueType, defaultVal);
             
             if (GUILayout.Button("Add", GUILayout.Width(40)))
             {
                 if (!dict.Contains(newKey))
                 {
                     dict.Add(newKey, newValue);
                     // 强制刷新 UI 状态可能需要标记 Dirty，但在 EditorWindow 中通常下一帧自动重绘
                 }
                 else
                 {
                     EditorUtility.DisplayDialog("错误", "Key 已存在", "OK");
                 }
             }
        }
        else
        {
             EditorGUILayout.LabelField("不支持该类型的 Key 快速输入", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();
    }

    // 辅助：绘制简单的 Key 输入 (仅支持 string, int, enum 等简单类型作为 Key)
    private object DrawSimpleKeyInput(Type keyType)
    {
        if (keyType == typeof(string))
        {
            return EditorGUILayout.TextField("", "");
        }
        else if (keyType == typeof(int))
        {
            return EditorGUILayout.IntField(0);
        }
        else if (keyType == typeof(long))
        {
            return EditorGUILayout.LongField(0);
        }
        else if (keyType.IsEnum)
        {
            return EditorGUILayout.EnumPopup((Enum)Activator.CreateInstance(keyType));
        }
        // 其他复杂类型作为 Key 在此简化处理，暂不支持直接输入
        return null;
    }

    // 辅助：绘制单个字段控件 (增强版)
    private object DrawFieldControl(string label, Type fieldType, object value)
    {
        // 如果是嵌套的可编辑数据类，也可以在这里递归处理，但目前先处理基础类型和集合
        // 如果 value 是一个类且不是 Unity/Object 也不是基础类型，可以选择展开或显示提示
        
        EditorGUILayout.BeginHorizontal();
        if (!string.IsNullOrEmpty(label))
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
        else if (fieldType == typeof(char))
        {
            string s = EditorGUILayout.TextField(value?.ToString() ?? "");
            if (!string.IsNullOrEmpty(s)) newValue = s[0];
        }

        // Unity 常用类型
        else if (fieldType == typeof(Vector2))
            newValue = EditorGUILayout.Vector2Field("", (Vector2)value);
        else if (fieldType == typeof(Vector2Int))
            newValue = EditorGUILayout.Vector2IntField("", (Vector2Int)value);
        else if (fieldType == typeof(Vector3))
            newValue = EditorGUILayout.Vector3Field("", (Vector3)value);
        else if (fieldType == typeof(Vector3Int))
            newValue = EditorGUILayout.Vector3IntField("", (Vector3Int)value);
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
        else if (fieldType == typeof(UnityEngine.Object) || fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
            newValue = EditorGUILayout.ObjectField("", (UnityEngine.Object)value, fieldType, true);

        // 枚举类型
        else if (fieldType.IsEnum)
            newValue = EditorGUILayout.EnumPopup((Enum)value);
            
        // 嵌套类 (非集合，非基础) - 简单提示或尝试展开 (此处简化为只显示类型名，避免无限递归复杂化)
        else if (!fieldType.IsPrimitive && !fieldType.IsArray && !fieldType.Namespace.StartsWith("System.Collections"))
        {
             EditorGUILayout.LabelField($"[Object] {fieldType.Name}", GUILayout.Width(200));
             // 如果需要编辑嵌套类，可以在此处递归调用 DrawDataFields 的逻辑，但这需要更复杂的布局管理
        }

        // 暂不支持的类型
        else
            EditorGUILayout.LabelField($"Unsupported: {fieldType.Name}", GUILayout.Width(200));

        EditorGUILayout.EndHorizontal();
        return newValue;
    }

    #endregion

    #region 辅助方法

    private string GetDataTypeDisplayName(Type type = null)
    {
        type ??= _selectedDataType;
        if (type == null) return "";

        var attr = type.GetCustomAttribute<EditableDataAttribute>();
        return string.IsNullOrEmpty(attr.DisplayName) ? type.Name : attr.DisplayName;
    }

    private void LoadCurrentInstance()
    {
        if (_selectedDataType == null) return;

        MethodInfo loadMethod = typeof(GenericDataPersistence).GetMethod("LoadData")
            .MakeGenericMethod(_selectedDataType);
        _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });
        _foldoutStates.Clear(); // 加载新实例时重置折叠状态
    }

    private bool SaveCurrentInstance()
    {
        if (_selectedDataType == null || _currentDataInstance == null) return false;

        MethodInfo saveMethod = typeof(GenericDataPersistence).GetMethod("SaveData")
            .MakeGenericMethod(_selectedDataType);
        bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
        AssetDatabase.Refresh();
        return success;
    }

    #endregion
}