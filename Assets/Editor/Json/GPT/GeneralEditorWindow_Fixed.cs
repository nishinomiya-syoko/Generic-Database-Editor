using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace GPT
{
    /// <summary>
    /// 通用数据编辑器窗口（增强版）
    /// 功能：
    /// - 数据类型切换
    /// - 实例列表 / 新建 / 删除 / 搜索
    /// - 读取 / 保存 / Excel导入导出
    /// - 基础类型编辑
    /// - Unity常见结构编辑
    /// - 枚举编辑
    /// - 数组 / List / Dictionary 编辑
    /// - [Serializable] 自定义类/结构体递归编辑
    /// - List/Array 中复杂对象递归编辑
    /// - Dictionary value 复杂对象递归编辑
    /// - 脏标记提示
    /// </summary>
    public class GeneralEditorWindow : EditorWindow
    {
        private static readonly string JSON_PATH = Constant.JSON_PATH;
        private static readonly string JSON_EXTENSION = ".json";
        private static readonly string EXCEL_PATH = Constant.EXCEL_PATH;

        // UI常量
        private const float TYPE_PANEL_WIDTH = 400f;
        private const float INSTANCE_PANEL_WIDTH = 280f;
        private const float BUTTON_WIDTH = 120f;
        private const float SMALL_BUTTON_WIDTH = 60f;
        private const float MID_BUTTON_WIDTH = 80f;
        private const float LARGE_BUTTON_WIDTH = 100f;
        private const float BUTTON_HEIGHT = 30f;
        private const float TYPE_SCROLL_HEIGHT = 60f;
        private const int TYPE_BUTTONS_PER_ROW = 3;
        private const int MAX_RECURSION_DEPTH = 8;

        // 核心数据
        private Type _selectedDataType;
        private object _currentDataInstance;
        private string _instanceName = "Default";
        private List<Type> _editableDataTypes = new List<Type>();

        // UI状态
        private Vector2 _typeButtonScrollPos;
        private Vector2 _instanceListScrollPos;
        private Vector2 _editAreaScrollPos;
        private string _newInstanceName = "NewInstance";
        private string _instanceSearchText = string.Empty;

        // Foldout状态
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        // 新增：临时存储新增字典的键/值（解决立即模式GUI的帧同步问题）
        private Dictionary<Type, object> _tempDictNewKey = new Dictionary<Type, object>();
        private Dictionary<Type, object> _tempDictNewValue = new Dictionary<Type, object>();
        
        // 脏标记
        private bool _isDirty = false;

        [MenuItem("Tools/Generic Database/General Editor Window Gpt")]
        public static void OpenWindow()
        {
            GeneralEditorWindow window = GetWindow<GeneralEditorWindow>("通用数据编辑器");
            window.minSize = new Vector2(950, 650);
            window.Show();
            window.Init();
        }

        private void OnEnable()
        {
            Init(false);
        }

        private void OnDisable()
        {
            // 不强制弹窗，避免Unity关闭窗口/重编译时打断流程
        }

        private void Init(bool forceResetSelection = true)
        {
            _editableDataTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(t => t != null)
                .Where(t => t.GetCustomAttribute<EditableDataAttribute>() != null && !t.IsAbstract && !t.IsInterface)
                .OrderBy(t => t.Name)
                .ToList();

            if (_editableDataTypes.Count == 0)
            {
                _selectedDataType = null;
                _currentDataInstance = null;
                return;
            }

            bool needReset =
                forceResetSelection ||
                _selectedDataType == null ||
                !_editableDataTypes.Contains(_selectedDataType);

            if (needReset)
            {
                _selectedDataType = _editableDataTypes[0];
                _instanceName = "Default";
                LoadCurrentInstance();
            }
            else if (_selectedDataType != null && _currentDataInstance == null)
            {
                LoadCurrentInstance();
            }
        }

        private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private void OnGUI()
        {
            if (_editableDataTypes == null || _editableDataTypes.Count == 0)
            {
                Init(false);
            }

            DrawDirtyBanner();

            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(TYPE_PANEL_WIDTH));
            EditorGUILayout.LabelField("数据类型", EditorStyles.boldLabel);
            _typeButtonScrollPos = EditorGUILayout.BeginScrollView(_typeButtonScrollPos, GUILayout.Height(TYPE_SCROLL_HEIGHT));
            DrawDataTypeButtons();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            DrawFunctionButtons();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if (_selectedDataType == null)
            {
                EditorGUILayout.HelpBox("未找到可编辑的数据类型，请确认目标类是否添加了 [EditableData] 特性。", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical("Box", GUILayout.Width(INSTANCE_PANEL_WIDTH));
            EditorGUILayout.LabelField($"{GetDataTypeDisplayName()} - 实例列表", EditorStyles.boldLabel);
            DrawNewInstanceButton();
            DrawInstanceSearchBar();
            DrawInstanceList();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            string dirtyMark = _isDirty ? " *未保存" : string.Empty;
            EditorGUILayout.LabelField($"编辑：{_instanceName}{dirtyMark}", EditorStyles.boldLabel);
            _editAreaScrollPos = EditorGUILayout.BeginScrollView(_editAreaScrollPos);
            DrawDataFields();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        #region 顶部提示

        private void DrawDirtyBanner()
        {
            if (!_isDirty) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("当前实例有未保存修改", EditorStyles.boldLabel);

            if (GUILayout.Button("立即保存", GUILayout.Width(100)))
            {
                bool success = SaveCurrentInstance();
                if (success)
                {
                    _isDirty = false;
                    Repaint();
                }
            }

            if (GUILayout.Button("放弃修改并重载", GUILayout.Width(120)))
            {
                bool confirm = EditorUtility.DisplayDialog("确认", "是否放弃当前未保存修改并重新读取磁盘数据？", "放弃修改", "取消");
                if (confirm)
                {
                    LoadCurrentInstance();
                    _isDirty = false;
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 主布局绘制

        private void DrawDataTypeButtons()
        {
            if (_editableDataTypes == null || _editableDataTypes.Count == 0)
            {
                EditorGUILayout.LabelField("没有找到可编辑类型");
                return;
            }

            int total = _editableDataTypes.Count;
            int rowCount = Mathf.CeilToInt(total / (float)TYPE_BUTTONS_PER_ROW);

            for (int row = 0; row < rowCount; row++)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < TYPE_BUTTONS_PER_ROW; col++)
                {
                    int index = row * TYPE_BUTTONS_PER_ROW + col;
                    if (index >= total)
                        break;

                    Type dataType = _editableDataTypes[index];
                    string displayName = GetDataTypeDisplayName(dataType);
                    bool isSelected = dataType == _selectedDataType;

                    Color originalColor = GUI.backgroundColor;
                    if (isSelected)
                        GUI.backgroundColor = Color.cyan;

                    if (GUILayout.Button(displayName, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        if (_selectedDataType != dataType)
                        {
                            if (!TryHandleUnsavedChangesBeforeSwitch())
                            {
                                GUI.backgroundColor = originalColor;
                                EditorGUILayout.EndHorizontal();
                                return;
                            }

                            _selectedDataType = dataType;
                            _instanceName = "Default";
                            _currentDataInstance = null;
                            _isDirty = false;
                            LoadCurrentInstance();
                        }
                    }

                    GUI.backgroundColor = originalColor;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFunctionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("读取数据", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                if (_isDirty)
                {
                    bool confirm = EditorUtility.DisplayDialog("提示", "当前有未保存修改，是否放弃修改并读取磁盘数据？", "读取", "取消");
                    if (!confirm)
                    {
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }

                LoadCurrentInstance();
                _isDirty = false;
                // EditorUtility.DisplayDialog("提示", "当前实例数据读取完成", "确定");
                Debug.Log("当前实例数据读取完成");
            }

            if (GUILayout.Button("保存数据", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                bool success = SaveCurrentInstance();
                if (success)
                    _isDirty = false;

                // EditorUtility.DisplayDialog(success ? "成功" : "失败",
                //     success ? "当前实例数据保存完成" : "当前实例数据保存失败", "确定");
                Debug.Log(success ? "当前实例数据保存完成" : "当前实例数据保存失败");
            }

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("删除当前实例", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                DeleteCurrentInstance();
            }
            GUI.backgroundColor = originalColor;

            if (GUILayout.Button("导出所有实例到Excel", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                ExportAllInstancesToExcel();
            }

            if (GUILayout.Button("从Excel导入所有实例", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
            {
                ImportAllInstancesFromExcel();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNewInstanceButton()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

            GUILayout.Label("新实例名", GUILayout.Width(60));
            _newInstanceName = EditorGUILayout.TextField(_newInstanceName ?? string.Empty);

            if (GUILayout.Button("新建", GUILayout.Width(SMALL_BUTTON_WIDTH)))
            {
                string newName = (_newInstanceName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(newName))
                {
                    Debug.LogWarning("实例名不能为空");
                    return;
                }

                if (_selectedDataType == null)
                {
                    Debug.LogWarning("请先选择数据类型");
                    return;
                }

                if (!TryHandleUnsavedChangesBeforeSwitch())
                    return;

                try
                {
                    _instanceName = newName;
                    _currentDataInstance = Activator.CreateInstance(_selectedDataType);
                    _isDirty = true;

                    bool saveSuccess = SaveCurrentInstance();
                    if (saveSuccess)
                    {
                        _isDirty = false;
                        Debug.Log($"已创建并保存新实例：{newName}");
                        _newInstanceName = "NewInstance";
                    }
                    else
                    {
                        Debug.LogError("创建新实例失败：保存失败");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"创建新实例失败：{e}");
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawInstanceSearchBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("搜索", GUILayout.Width(35));
            _instanceSearchText = EditorGUILayout.TextField(_instanceSearchText ?? string.Empty);

            if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
            {
                _instanceSearchText = string.Empty;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        private void DrawInstanceList()
        {
            _instanceListScrollPos = EditorGUILayout.BeginScrollView(_instanceListScrollPos);

            string[] instanceNames = GetAllInstanceNamesSafe(_selectedDataType);

            if (!string.IsNullOrEmpty(_instanceSearchText))
            {
                instanceNames = instanceNames
                    .Where(n => !string.IsNullOrEmpty(n) &&
                                n.IndexOf(_instanceSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            }

            if (instanceNames == null || instanceNames.Length == 0)
            {
                EditorGUILayout.HelpBox("没有匹配实例。", MessageType.Info);
            }
            else
            {
                foreach (string name in instanceNames)
                {
                    bool isSelected = name == _instanceName;
                    Color originalColor = GUI.backgroundColor;
                    if (isSelected)
                        GUI.backgroundColor = Color.green;

                    if (GUILayout.Button(name, GUILayout.Height(BUTTON_HEIGHT)))
                    {
                        if (_instanceName != name)
                        {
                            if (!TryHandleUnsavedChangesBeforeSwitch())
                            {
                                GUI.backgroundColor = originalColor;
                                EditorGUILayout.EndScrollView();
                                return;
                            }

                            _instanceName = name;
                            _isDirty = false;
                            LoadCurrentInstance();
                        }
                    }

                    GUI.backgroundColor = originalColor;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDataFields()
        {
            if (_selectedDataType == null)
            {
                EditorGUILayout.HelpBox("未选择数据类型", MessageType.Warning);
                return;
            }

            if (_currentDataInstance == null)
            {
                EditorGUILayout.HelpBox("当前实例为空，请尝试读取数据或新建实例。", MessageType.Warning);
                return;
            }

            FieldInfo[] fields = GetSerializableFields(_selectedDataType);

            foreach (FieldInfo field in fields)
            {
                try
                {
                    object oldValue = field.GetValue(_currentDataInstance);
                    object newValue = DrawAnyField(field.Name, field.FieldType, oldValue, BuildFieldPath(field.Name), 0, true);

                    if (!AreValuesEqual(oldValue, newValue))
                    {
                        field.SetValue(_currentDataInstance, newValue);
                        MarkDirty();
                    }
                }
                catch (Exception e)
                {
                    EditorGUILayout.HelpBox($"字段 [{field.Name}] 绘制失败：{e.Message}", MessageType.Error);
                }
            }
        }

        #endregion

        #region 实例管理

        private bool TryHandleUnsavedChangesBeforeSwitch()
        {
            if (!_isDirty) return true;

            int result = EditorUtility.DisplayDialogComplex(
                "未保存修改",
                $"当前实例 [{_instanceName}] 有未保存修改，是否先保存？",
                "保存并继续",
                "取消",
                "不保存继续"
            );

            if (result == 1) // 取消
                return false;

            if (result == 0) // 保存并继续
            {
                bool success = SaveCurrentInstance();
                if (!success)
                {
                    // EditorUtility.DisplayDialog("失败", "保存失败，已取消切换", "确定");
                    Debug.LogWarning("保存失败，已取消切换");
                    return false;
                }

                _isDirty = false;
                return true;
            }

            if (result == 2) // 不保存继续
            {
                _isDirty = false;
                return true;
            }

            return true;
        }

        private void DeleteCurrentInstance()
        {
            if (_selectedDataType == null || string.IsNullOrEmpty(_instanceName))
            {
                Debug.LogWarning("请先选择要删除的实例");
                return;
            }

            string deletedName = _instanceName;

            bool confirm = EditorUtility.DisplayDialog(
                "确认删除",
                $"是否永久删除实例：{deletedName}？\n此操作不可恢复！",
                "删除",
                "取消"
            );

            if (!confirm) return;

            try
            {
                MethodInfo getSavePathMethod = GetGenericStaticMethod(typeof(GenericDataPersistence), "GetSavePath", _selectedDataType);
                if (getSavePathMethod == null)
                {
                    Debug.LogError("未找到 GenericDataPersistence.GetSavePath");
                    return;
                }

                string savePath = (string)getSavePathMethod.Invoke(null, new object[] { deletedName });

                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    AssetDatabase.Refresh();

                    string[] remainingInstances = GetAllInstanceNamesSafe(_selectedDataType);

                    if (remainingInstances.Length > 0)
                    {
                        _instanceName = remainingInstances.Contains("Default") ? "Default" : remainingInstances[0];
                        LoadCurrentInstance();
                    }
                    else
                    {
                        _instanceName = "Default";
                        _currentDataInstance = Activator.CreateInstance(_selectedDataType);
                    }

                    _isDirty = false;
                    Debug.Log($"实例 {deletedName} 已删除");
                }
                else
                {
                    Debug.LogWarning($"实例文件不存在，无需删除：{savePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"删除实例 {deletedName} 失败：{e}");
            }
        }

        private void ExportAllInstancesToExcel()
        {
            if (_selectedDataType == null)
            {
                Debug.LogWarning("请先选择数据类型");
                return;
            }

            try
            {
                string fileName = $"{_selectedDataType.Name}.xlsx";
                string excelPath = Path.Combine(EXCEL_PATH, fileName);

                MethodInfo exportMethod = GetGenericStaticMethod(typeof(ExcelDataUtility), "ExportAllInstancesToExcel", _selectedDataType);
                if (exportMethod == null)
                {
                    Debug.LogError("未找到 ExcelDataUtility.ExportAllInstancesToExcel");
                    return;
                }

                bool success = (bool)exportMethod.Invoke(null, new object[] { excelPath });
                Debug.Log(success
                    ? $"[{GetDataTypeDisplayName()}] 所有实例导出到Excel成功\n路径：{excelPath}"
                    : "导出失败");
            }
            catch (Exception e)
            {
                Debug.LogError($"导出Excel失败：{e}");
            }
        }

        private void ImportAllInstancesFromExcel()
        {
            if (_selectedDataType == null)
            {
                EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
                return;
            }

            try
            {
                MethodInfo selectMethod = typeof(ExcelDataUtility).GetMethod("SelectExcelLoadPath", BindingFlags.Public | BindingFlags.Static);
                if (selectMethod == null)
                {
                    EditorUtility.DisplayDialog("失败", "未找到 ExcelDataUtility.SelectExcelLoadPath", "确定");
                    return;
                }

                string excelPath = (string)selectMethod.Invoke(null, null);
                if (string.IsNullOrEmpty(excelPath))
                    return;

                MethodInfo importMethod = GetGenericStaticMethod(typeof(ExcelDataUtility), "ImportAllInstancesFromExcel", _selectedDataType);
                if (importMethod == null)
                {
                    EditorUtility.DisplayDialog("失败", "未找到 ExcelDataUtility.ImportAllInstancesFromExcel", "确定");
                    return;
                }

                bool success = (bool)importMethod.Invoke(null, new object[] { excelPath });

                LoadCurrentInstance();
                _isDirty = false;

                EditorUtility.DisplayDialog(success ? "成功" : "失败",
                    success
                        ? $"[{GetDataTypeDisplayName()}] 从Excel导入所有实例成功"
                        : "导入失败（无有效实例或文件错误）",
                    "确定");
            }
            catch (Exception e)
            {
                // EditorUtility.DisplayDialog("失败", $"导入Excel失败：{e.Message}", "确定");
                Debug.LogError($"导入Excel失败：{e}");
            }
        }

        #endregion

        #region 通用字段绘制核心

        private object DrawAnyField(string label, Type fieldType, object value, string path, int depth, bool showLabel)
        {
            if (depth > MAX_RECURSION_DEPTH)
            {
                EditorGUILayout.HelpBox($"嵌套层级过深：{label}", MessageType.Warning);
                return value;
            }

            if (IsSimpleType(fieldType))
            {
                return DrawSimpleField(label, fieldType, value, showLabel);
            }

            if (IsUnityObjectReference(fieldType))
            {
                return DrawUnityObjectField(label, fieldType, value, showLabel);
            }

            if (IsCollectionType(fieldType))
            {
                return DrawCollectionValue(label, fieldType, value, path, depth, showLabel);
            }

            if (IsSerializableComplexType(fieldType))
            {
                return DrawComplexObjectField(label, fieldType, value, path, depth, showLabel);
            }

            EditorGUILayout.BeginHorizontal();
            if (showLabel)
                EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}");
            EditorGUILayout.EndHorizontal();

            return value;
        }

        private object DrawSimpleField(string label, Type fieldType, object value, bool showLabel)
        {
            EditorGUILayout.BeginHorizontal();

            if (showLabel)
                EditorGUILayout.LabelField(label, GUILayout.Width(150));

            object newValue = value;

            if (fieldType == typeof(string))
                newValue = EditorGUILayout.TextField((string)(value ?? string.Empty));
            else if (fieldType == typeof(int))
                newValue = EditorGUILayout.IntField(value != null ? (int)value : 0);
            else if (fieldType == typeof(float))
                newValue = EditorGUILayout.FloatField(value != null ? (float)value : 0f);
            else if (fieldType == typeof(bool))
                newValue = EditorGUILayout.Toggle(value != null && (bool)value);
            else if (fieldType == typeof(double))
                newValue = EditorGUILayout.DoubleField(value != null ? (double)value : 0d);
            else if (fieldType == typeof(long))
                newValue = EditorGUILayout.LongField(value != null ? (long)value : 0L);
            else if (fieldType == typeof(Vector2))
                newValue = EditorGUILayout.Vector2Field("", value != null ? (Vector2)value : default);
            else if (fieldType == typeof(Vector2Int))
                newValue = EditorGUILayout.Vector2IntField("", value != null ? (Vector2Int)value : default);
            else if (fieldType == typeof(Vector3))
                newValue = EditorGUILayout.Vector3Field("", value != null ? (Vector3)value : default);
            else if (fieldType == typeof(Vector3Int))
                newValue = EditorGUILayout.Vector3IntField("", value != null ? (Vector3Int)value : default);
            else if (fieldType == typeof(Vector4))
                newValue = EditorGUILayout.Vector4Field("", value != null ? (Vector4)value : default);
            else if (fieldType == typeof(Color))
                newValue = EditorGUILayout.ColorField("", value != null ? (Color)value : Color.white);
            else if (fieldType == typeof(Rect))
                newValue = EditorGUILayout.RectField("", value != null ? (Rect)value : default);
            else if (fieldType == typeof(Bounds))
                newValue = EditorGUILayout.BoundsField("", value != null ? (Bounds)value : default);
            else if (fieldType == typeof(Quaternion))
            {
                Quaternion q = value != null ? (Quaternion)value : Quaternion.identity;
                newValue = Quaternion.Euler(EditorGUILayout.Vector3Field("", q.eulerAngles));
            }
            else if (fieldType.IsEnum)
            {
                Array enumValues = Enum.GetValues(fieldType);
                Enum enumValue = value as Enum;
                if (enumValue == null && enumValues.Length > 0)
                    enumValue = (Enum)enumValues.GetValue(0);

                newValue = EditorGUILayout.EnumPopup(enumValue);
            }

            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        private object DrawUnityObjectField(string label, Type fieldType, object value, bool showLabel)
        {
            EditorGUILayout.BeginHorizontal();
            if (showLabel)
                EditorGUILayout.LabelField(label, GUILayout.Width(150));

            UnityEngine.Object obj = value as UnityEngine.Object;
            UnityEngine.Object newObj = EditorGUILayout.ObjectField(obj, fieldType, true);
            EditorGUILayout.EndHorizontal();
            return newObj;
        }

        private object DrawComplexObjectField(string label, Type fieldType, object value, string path, int depth, bool showLabel)
        {
            bool isNull = value == null;
            string foldKey = $"complex:{path}";
            bool expanded = GetFoldoutState(foldKey, true);

            string displayLabel = showLabel ? $"{label} ({fieldType.Name})" : fieldType.Name;
            if (isNull)
                displayLabel += " [null]";

            expanded = EditorGUILayout.Foldout(expanded, displayLabel, true);
            SetFoldoutState(foldKey, expanded);

            if (!expanded)
                return value;

            EditorGUI.indentLevel++;

            object workingObject = value;

            if (workingObject == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("对象为空", GUILayout.Width(120));
                if (GUILayout.Button("创建实例", GUILayout.Width(100)))
                {
                    workingObject = CreateDefaultComplexObject(fieldType);
                    MarkDirty();
                }
                EditorGUILayout.EndHorizontal();

                if (workingObject == null)
                {
                    EditorGUI.indentLevel--;
                    return value;
                }
            }

            FieldInfo[] subFields = GetSerializableFields(fieldType);
            if (subFields.Length == 0)
            {
                EditorGUILayout.LabelField("无可编辑字段");
                EditorGUI.indentLevel--;
                return workingObject;
            }

            foreach (FieldInfo subField in subFields)
            {
                object oldSubValue = subField.GetValue(workingObject);
                object newSubValue = DrawAnyField(
                    subField.Name,
                    subField.FieldType,
                    oldSubValue,
                    $"{path}.{subField.Name}",
                    depth + 1,
                    true);

                if (!AreValuesEqual(oldSubValue, newSubValue))
                {
                    subField.SetValue(workingObject, newSubValue);
                    MarkDirty();
                }
            }

            EditorGUI.indentLevel--;
            return workingObject;
        }

        private object DrawCollectionValue(string label, Type collectionType, object value, string path, int depth, bool showLabel)
        {
            string foldKey = $"collection:{path}";
            bool expanded = GetFoldoutState(foldKey, true);
            // string elementTypeName = GetCollectionElementType(collectionType)?.Name ?? "Unknown";
            // string elementTypeName = GetCollectionTypeName(collectionType)? ?? "Unknown";
            string displayLabel = showLabel ? $"{label} ({GetCollectionTypeName(collectionType)} 集合)" : $"{GetCollectionTypeName(collectionType)} 集合";

            expanded = EditorGUILayout.Foldout(expanded, displayLabel, true);
            SetFoldoutState(foldKey, expanded);

            if (!expanded)
                return value;

            EditorGUI.indentLevel++;

            object workingValue = value;
            if (workingValue == null)
            {
                EditorGUILayout.HelpBox("集合未初始化", MessageType.Warning);
                if (GUILayout.Button("初始化空集合", GUILayout.Width(BUTTON_WIDTH)))
                {
                    workingValue = CreateEmptyCollection(collectionType);
                    MarkDirty();
                }

                EditorGUI.indentLevel--;
                return workingValue;
            }

            if (collectionType.IsArray)
            {
                workingValue = DrawArrayValue(collectionType, workingValue, path, depth + 1);
            }
            else if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
            {
                workingValue = DrawListValue(collectionType, workingValue, path, depth + 1);
            }
            else if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                workingValue = DrawDictionaryValue(collectionType, workingValue, path, depth + 1);
            }

            EditorGUI.indentLevel--;
            return workingValue;
        }

        #endregion

        #region 集合绘制

        private object DrawArrayValue(Type arrayType, object arrayInstance, string path, int depth)
        {
            Type elementType = arrayType.GetElementType();
            Array array = (Array)arrayInstance;
            int count = array.Length;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
            {
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                return Array.CreateInstance(elementType, 0);
            }

            if (GUILayout.Button("添加元素", GUILayout.Width(MID_BUTTON_WIDTH)))
            {
                Array newArray = Array.CreateInstance(elementType, count + 1);
                Array.Copy(array, newArray, count);
                newArray.SetValue(CreateDefaultValueForField(elementType), count);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                return newArray;
            }

            EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"索引 [{i}]", EditorStyles.boldLabel);

                if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
                {
                    Array newArray = Array.CreateInstance(elementType, count - 1);
                    for (int j = 0, k = 0; j < count; j++)
                    {
                        if (j == i) continue;
                        newArray.SetValue(array.GetValue(j), k++);
                    }

                    MarkDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return newArray;
                }
                EditorGUILayout.EndHorizontal();

                object oldElement = array.GetValue(i);
                object newElement = DrawAnyField("", elementType, oldElement, $"{path}[{i}]", depth, false);

                if (!AreValuesEqual(oldElement, newElement))
                {
                    array.SetValue(newElement, i);
                    MarkDirty();
                }

                EditorGUILayout.EndVertical();
            }

            return array;
        }

        private object DrawListValue(Type listType, object listInstance, string path, int depth)
        {
            Type elementType = listType.GetGenericArguments()[0];

            PropertyInfo countProp = listType.GetProperty("Count");
            MethodInfo addMethod = listType.GetMethod("Add");
            MethodInfo removeAtMethod = listType.GetMethod("RemoveAt");
            MethodInfo clearMethod = listType.GetMethod("Clear");
            PropertyInfo indexerProp = listType.GetProperty("Item");

            if (countProp == null || addMethod == null || removeAtMethod == null || clearMethod == null || indexerProp == null)
            {
                EditorGUILayout.HelpBox("List 反射信息获取失败", MessageType.Error);
                return listInstance;
            }

            int count = (int)countProp.GetValue(listInstance);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
            {
                clearMethod.Invoke(listInstance, null);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                return listInstance;
            }

            if (GUILayout.Button("添加元素", GUILayout.Width(MID_BUTTON_WIDTH)))
            {
                addMethod.Invoke(listInstance, new[] { CreateDefaultValueForField(elementType) });
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                return listInstance;
            }

            EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"索引 [{i}]", EditorStyles.boldLabel);

                if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
                {
                    removeAtMethod.Invoke(listInstance, new object[] { i });
                    MarkDirty();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return listInstance;
                }
                EditorGUILayout.EndHorizontal();

                object oldElement = indexerProp.GetValue(listInstance, new object[] { i });
                object newElement = DrawAnyField("", elementType, oldElement, $"{path}[{i}]", depth, false);

                if (!AreValuesEqual(oldElement, newElement))
                {
                    indexerProp.SetValue(listInstance, newElement, new object[] { i });
                    MarkDirty();
                }

                EditorGUILayout.EndVertical();
            }

            return listInstance;
        }

        // private object DrawDictionaryValue(Type dictType, object dictInstance, string path, int depth)
        // {
        //     Type[] dictArgs = dictType.GetGenericArguments();
        //     Type keyType = dictArgs[0];
        //     Type valueType = dictArgs[1];

        //     PropertyInfo countProp = dictType.GetProperty("Count");
        //     MethodInfo addMethod = dictType.GetMethod("Add");
        //     MethodInfo removeMethod = dictType.GetMethod("Remove");
        //     MethodInfo clearMethod = dictType.GetMethod("Clear");
        //     PropertyInfo indexerProp = dictType.GetProperty("Item");
        //     MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey");

        //     if (countProp == null || addMethod == null || removeMethod == null || clearMethod == null || indexerProp == null || containsKeyMethod == null)
        //     {
        //         EditorGUILayout.HelpBox("Dictionary 反射信息获取失败", MessageType.Error);
        //         return dictInstance;
        //     }

        //     int count = (int)countProp.GetValue(dictInstance);

        //     EditorGUILayout.BeginHorizontal();

        //     if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
        //     {
        //         clearMethod.Invoke(dictInstance, null);
        //         MarkDirty();
        //         EditorGUILayout.EndHorizontal();
        //         return dictInstance;
        //     }

        //     if (GUILayout.Button("添加键值对", GUILayout.Width(LARGE_BUTTON_WIDTH)))
        //     {
        //         if (TryCreateNewDictionaryKey(dictInstance, keyType, containsKeyMethod, count, out object newKey))
        //         {
        //             object defaultValue = CreateDefaultValueForField(valueType);
        //             addMethod.Invoke(dictInstance, new object[] { newKey, defaultValue });
        //             MarkDirty();
        //         }
        //         else
        //         {
        //             EditorUtility.DisplayDialog("提示", $"当前不支持为键类型 {keyType.Name} 自动生成新键", "确定");
        //         }

        //         EditorGUILayout.EndHorizontal();
        //         return dictInstance;
        //     }

        //     EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
        //     EditorGUILayout.EndHorizontal();

        //     List<object> keys = new List<object>();
        //     foreach (object item in (IEnumerable)dictInstance)
        //     {
        //         if (item == null) continue;
        //         Type kvpType = item.GetType();
        //         PropertyInfo keyProp = kvpType.GetProperty("Key");
        //         if (keyProp != null)
        //             keys.Add(keyProp.GetValue(item));
        //     }

        //     for (int i = 0; i < keys.Count; i++)
        //     {
        //         object key = keys[i];
        //         EditorGUILayout.BeginVertical("box");

        //         object currentValue = indexerProp.GetValue(dictInstance, new object[] { key });

        //         EditorGUILayout.BeginHorizontal();
        //         object editedKey = key;

        //         if (CanEditDictionaryKeyType(keyType))
        //         {
        //             editedKey = DrawAnyField("键", keyType, key, $"{path}.key[{i}]", depth, true);
        //         }
        //         else
        //         {
        //             EditorGUILayout.LabelField("键", GUILayout.Width(150));
        //             EditorGUILayout.SelectableLabel(key?.ToString() ?? "null", GUILayout.Height(EditorGUIUtility.singleLineHeight));
        //         }

        //         if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
        //         {
        //             removeMethod.Invoke(dictInstance, new object[] { key });
        //             MarkDirty();
        //             EditorGUILayout.EndHorizontal();
        //             EditorGUILayout.EndVertical();
        //             return dictInstance;
        //         }
        //         EditorGUILayout.EndHorizontal();

        //         if (!AreValuesEqual(key, editedKey))
        //         {
        //             if (editedKey == null)
        //             {
        //                 EditorGUILayout.HelpBox("字典键不能为 null。", MessageType.Warning);
        //             }
        //             else if ((bool)containsKeyMethod.Invoke(dictInstance, new object[] { editedKey }))
        //             {
        //                 EditorGUILayout.HelpBox($"键 [{editedKey}] 已存在，不能重复。", MessageType.Warning);
        //             }
        //             else
        //             {
        //                 removeMethod.Invoke(dictInstance, new object[] { key });
        //                 addMethod.Invoke(dictInstance, new object[] { editedKey, currentValue });
        //                 MarkDirty();
        //                 EditorGUILayout.EndVertical();
        //                 return dictInstance;
        //             }
        //         }

        //         object oldValue = indexerProp.GetValue(dictInstance, new object[] { key });
        //         object newValue = DrawAnyField("值", valueType, oldValue, $"{path}[{key}]", depth, true);

        //         if (!AreValuesEqual(oldValue, newValue))
        //         {
        //             indexerProp.SetValue(dictInstance, newValue, new object[] { key });
        //             MarkDirty();
        //         }

        //         EditorGUILayout.EndVertical();
        //     }

        //     return dictInstance;
        // }

    //      private void DrawDictionaryValue(string label, Type fieldType, object value, string path, int depth)
    // {
    //     Type keyType = fieldType.GetGenericArguments()[0];
    //     Type valueType = fieldType.GetGenericArguments()[1];

    //     // 注意：Dictionary 在遍历时不能直接修改结构（增删），所以我们需要暂存操作
    //     object keyToRemove = null;
        
    //     // 临时存储新键值
    //     // object newKey = null;
    //     // object newValue = null;
    //     var dict = (IDictionary)value;
    //     // 绘制现有项
    //     // 将 Keys 复制到数组以避免枚举期间修改异常
    //     var keys = dict.Keys.Cast<object>().ToArray();
        
    //     foreach (var key in keys)
    //     {
    //         EditorGUILayout.BeginHorizontal();
            
    //         // 绘制 Key (通常只读，或者允许编辑但需要重建键值对)
    //         // 为简化，这里 Key 设为只读显示，若需编辑 Key，通常做法是删除旧项加新项
    //         EditorGUILayout.LabelField(key.ToString(), GUILayout.Width(100), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            
    //         object val = dict[key];
    //         object newVal = DrawAnyField("", valueType, val,$"{path}[{key});

    //         if (!Equals(newVal, val))
    //         {
    //             dict[key] = newVal;
    //         }

    //         if (GUILayout.Button("X", GUILayout.Width(20)))
    //         {
    //             keyToRemove = key;
    //         }
    //         EditorGUILayout.EndHorizontal();
    //     }

    //     if (keyToRemove != null)
    //     {
    //         dict.Remove(keyToRemove);
    //         return;
    //     }

    //     // 添加新项区域
    //     EditorGUILayout.Space();
    //     EditorGUILayout.LabelField("添加新项", EditorStyles.miniLabel);
    //     EditorGUILayout.BeginHorizontal();

    //     // 简单的 Key 输入器 (仅支持基础类型作为 Key 的输入演示)
    //     var p = DrawSimpleKeyInput(keyType);
    //     if (p != null) m_key = p;
        
    //     if (m_key != null)
    //     {
    //          // 临时 Value
    //          object defaultVal = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
    //          if (valueType == typeof(string)) defaultVal = "";
             
    //          // 这里为了能在同一行绘制并获取值，我们用一个临时的匿名对象或者再次调用 DrawFieldControl
    //          // 但由于 GUILayout 是立即模式，我们需要在一个单独的 pass 或者用临时变量
    //          // 简化处理：分两行，或者假设用户先输入 Key 点击添加后，下一帧再编辑 Value
    //          // 更好的方式：使用一个临时的 Dictionary entry 编辑器
             
    //          // 这里采用：如果 Key 有效且不为空，显示 Value 输入框和确认按钮
    //          EditorGUILayout.LabelField("Value:", GUILayout.Width(40));
    //         var q = DrawFieldControl("", valueType, defaultVal);
    //          if (q != null) m_value = q;
             
    //          if (GUILayout.Button("Add", GUILayout.Width(40)))
    //          {
    //              if (!dict.Contains(m_key))
    //              {
    //                  dict.Add(m_key, m_value);
    //                  // 强制刷新 UI 状态可能需要标记 Dirty，但在 EditorWindow 中通常下一帧自动重绘
    //              }
    //              else
    //              {
    //                  EditorUtility.DisplayDialog("错误", "Key 已存在", "OK");
    //              }
    //          }
    //     }
    //     else
    //     {
    //          EditorGUILayout.LabelField("不支持该类型的 Key 快速输入", EditorStyles.miniLabel);
    //     }

    //     EditorGUILayout.EndHorizontal();
    // }

        private object DrawDictionaryValue(Type dictType, object dictInstance, string path, int depth)
        {
            // 1. 基础校验与反射获取核心方法/属性
            if (dictInstance == null)
            {
                EditorGUILayout.HelpBox("字典未初始化", MessageType.Warning);
                if (GUILayout.Button("初始化空字典", GUILayout.Width(BUTTON_WIDTH)))
                {
                    dictInstance = CreateEmptyCollection(dictType);
                    MarkDirty();
                }
                return dictInstance;
            }

            Type[] dictArgs = dictType.GetGenericArguments();
            Type keyType = dictArgs[0];
            Type valueType = dictArgs[1];

            // 获取 Dictionary 核心方法/属性（带空值检查）
            PropertyInfo countProp = dictType.GetProperty("Count");
            MethodInfo addMethod = dictType.GetMethod("Add", new[] { keyType, valueType });
            MethodInfo removeMethod = dictType.GetMethod("Remove", new[] { keyType });
            MethodInfo clearMethod = dictType.GetMethod("Clear");
            PropertyInfo indexerProp = dictType.GetProperty("Item", new[] { keyType });
            MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey", new[] { keyType });

            if (countProp == null || addMethod == null || removeMethod == null || clearMethod == null || indexerProp == null || containsKeyMethod == null)
            {
                EditorGUILayout.HelpBox("无法获取 Dictionary 反射信息", MessageType.Error);
                return dictInstance;
            }

            // 2. 顶部操作按钮（清空/总数）
            int count = (int)countProp.GetValue(dictInstance);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
            {
                clearMethod.Invoke(dictInstance, null);
                MarkDirty();
                EditorGUILayout.EndHorizontal();
                return dictInstance;
            }
            EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // 3. 遍历编辑现有键值对（解决遍历中修改的问题：先收集要删除的键）
            List<object> keysToRemove = new List<object>();
            List<object> keys = new List<object>();
            foreach (object item in (IEnumerable)dictInstance)
            {
                if (item == null) continue;
                Type kvpType = item.GetType();
                PropertyInfo keyProp = kvpType.GetProperty("Key");
                PropertyInfo valueProp = kvpType.GetProperty("Value");
                if (keyProp != null) keys.Add(keyProp.GetValue(item));
            }

            foreach (object key in keys)
            {
                EditorGUILayout.BeginVertical("box");

                // 键编辑区域（只读/可编辑）
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("键", GUILayout.Width(40));
                object editedKey = key;

                // 仅支持简单类型键的编辑
                if (CanEditDictionaryKeyType(keyType))
                {
                    editedKey = DrawAnyField("", keyType, key, $"{path}.key[{key}]", depth, false);
                }
                else
                {
                    EditorGUILayout.SelectableLabel(key?.ToString() ?? "null", GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }

                // // 删除按钮（标记待删除，避免遍历中修改）
                // if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
                // {
                //     keysToRemove.Add(key);
                // }
                // EditorGUILayout.EndHorizontal();

                // // 值编辑区域（支持复杂类型递归编辑）
                // EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("值", GUILayout.Width(40));
                object oldValue = indexerProp.GetValue(dictInstance, new object[] { key });
                object newValue = DrawAnyField("", valueType, oldValue, $"{path}.value[{key}]", depth, false);
                
                 // 删除按钮（标记待删除，避免遍历中修改）
                if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
                {
                    keysToRemove.Add(key);
                }
                EditorGUILayout.EndHorizontal();

                // 应用值修改
                if (!AreValuesEqual(oldValue, newValue))
                {
                    indexerProp.SetValue(dictInstance, newValue, new object[] { key });
                    MarkDirty();
                }

                // 应用键修改（仅当键变化且不重复时）
                if (!AreValuesEqual(key, editedKey) && editedKey != null)
                {
                    bool keyExists = (bool)containsKeyMethod.Invoke(dictInstance, new[] { editedKey });
                    if (!keyExists)
                    {
                        // 先删旧键，再加新键
                        removeMethod.Invoke(dictInstance, new[] { key });
                        addMethod.Invoke(dictInstance, new[] { editedKey, oldValue });
                        MarkDirty();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"键 [{editedKey}] 已存在，无法修改", MessageType.Warning);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            // 批量删除标记的键
            foreach (object key in keysToRemove)
            {
                removeMethod.Invoke(dictInstance, new[] { key });
                MarkDirty();
            }

            // 4. 新增键值对区域
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("新增键值对", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            // 初始化临时键/值（按类型缓存，避免帧丢失）
            if (!_tempDictNewKey.ContainsKey(keyType))
                _tempDictNewKey[keyType] = CreateDefaultValueForField(keyType);
            if (!_tempDictNewValue.ContainsKey(valueType))
                _tempDictNewValue[valueType] = CreateDefaultValueForField(valueType);

            // 绘制键输入框
            EditorGUILayout.LabelField("键", GUILayout.Width(40));
            object newKey = DrawAnyField("", keyType, _tempDictNewKey[keyType], $"{path}.newKey", depth, false);
            _tempDictNewKey[keyType] = newKey;

            // 绘制值输入框
            EditorGUILayout.LabelField("值", GUILayout.Width(40));
            object newValueTemp = DrawAnyField("", valueType, _tempDictNewValue[valueType], $"{path}.newValue", depth, false);
            _tempDictNewValue[valueType] = newValueTemp;

            // 新增按钮
            if (GUILayout.Button("添加", GUILayout.Width(MID_BUTTON_WIDTH)))
            {
                if (newKey == null)
                {
                    EditorUtility.DisplayDialog("错误", "键不能为空", "确定");
                }
                else if ((bool)containsKeyMethod.Invoke(dictInstance, new[] { newKey }))
                {
                    EditorUtility.DisplayDialog("错误", $"键 [{newKey}] 已存在", "确定");
                }
                else
                {
                    addMethod.Invoke(dictInstance, new[] { newKey, newValueTemp });
                    MarkDirty();
                    // 重置临时键值
                    _tempDictNewKey[keyType] = CreateDefaultValueForField(keyType);
                    _tempDictNewValue[valueType] = CreateDefaultValueForField(valueType);
                }
            }
            EditorGUILayout.EndHorizontal();

            return dictInstance;
        }
    
        #endregion

        #region 类型判断与辅助


        private bool CanEditDictionaryKeyType(Type keyType)
        {
            if (keyType == null) return false;
            if (IsSimpleType(keyType)) return true;
            return false;
        }

        private bool IsSimpleType(Type type)
        {
            if (type == null) return false;

            return type == typeof(string) ||
                   type == typeof(int) ||
                   type == typeof(float) ||
                   type == typeof(bool) ||
                   type == typeof(double) ||
                   type == typeof(long) ||
                   type == typeof(Vector2) ||
                   type == typeof(Vector2Int) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector3Int) ||
                   type == typeof(Vector4) ||
                   type == typeof(Color) ||
                   type == typeof(Rect) ||
                   type == typeof(Bounds) ||
                   type == typeof(Quaternion) ||
                   type.IsEnum;
        }

        private bool IsUnityObjectReference(Type type)
        {
            return type != null && typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        private bool IsCollectionType(Type type)
        {
            if (type == null) return false;

            if (type.IsArray) return true;

            if (type.IsGenericType)
            {
                Type genericDef = type.GetGenericTypeDefinition();
                return genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>);
            }

            return false;
        }

        private bool IsSerializableComplexType(Type type)
        {
            if (type == null) return false;
            if (IsSimpleType(type)) return false;
            if (IsUnityObjectReference(type)) return false;
            if (IsCollectionType(type)) return false;
            if (type == typeof(decimal)) return false;
            if (type.IsAbstract || type.IsInterface) return false;

            return type.IsSerializable || type.GetCustomAttribute<SerializableAttribute>() != null;
        }

        private FieldInfo[] GetSerializableFields(Type type)
        {
            if (type == null) return Array.Empty<FieldInfo>();

            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsStatic)
                .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
                .ToArray();
        }

        private Type GetCollectionElementType(Type collectionType)
        {
            if (collectionType == null) return null;

            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
            {
                Type genericDef = collectionType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>))
                    return collectionType.GetGenericArguments()[0];

                if (genericDef == typeof(Dictionary<,>))
                {
                    Type[] args = collectionType.GetGenericArguments();
                    return typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]);
                }
            }

            return null;
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

        private object CreateEmptyCollection(Type collectionType)
        {
            if (collectionType == null) return null;

            if (collectionType.IsArray)
            {
                Type elementType = collectionType.GetElementType();
                return Array.CreateInstance(elementType, 0);
            }

            if (collectionType.IsGenericType)
            {
                Type genericDef = collectionType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>))
                    return Activator.CreateInstance(collectionType);
            }

            return null;
        }

        private object CreateDefaultValueForField(Type type)
        {
            if (type == null) return null;

            if (type == typeof(string))
                return string.Empty;

            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
            }

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            if (IsSerializableComplexType(type))
                return CreateDefaultComplexObject(type);

            return null;
        }

        private object CreateDefaultComplexObject(Type type)
        {
            if (type == null) return null;

            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private bool TryCreateNewDictionaryKey(object dictInstance, Type keyType, MethodInfo containsKeyMethod, int count, out object newKey)
        {
            newKey = null;

            if (keyType == typeof(string))
            {
                string baseKey = "NewKey";
                string candidate = baseKey;
                int suffix = 1;

                while ((bool)containsKeyMethod.Invoke(dictInstance, new object[] { candidate }))
                {
                    candidate = $"{baseKey}{suffix++}";
                }

                newKey = candidate;
                return true;
            }

            if (keyType == typeof(int))
            {
                int candidate = 0;
                while ((bool)containsKeyMethod.Invoke(dictInstance, new object[] { candidate }))
                {
                    candidate++;
                }

                newKey = candidate;
                return true;
            }

            if (keyType.IsEnum)
            {
                Array values = Enum.GetValues(keyType);
                foreach (object value in values)
                {
                    if (!(bool)containsKeyMethod.Invoke(dictInstance, new object[] { value }))
                    {
                        newKey = value;
                        return true;
                    }
                }
                return false;
            }

            return false;
        }

        private string BuildFieldPath(string fieldName)
        {
            return $"{_selectedDataType?.FullName ?? "UnknownType"}.{_instanceName}.{fieldName}";
        }

        private bool GetFoldoutState(string key, bool defaultValue)
        {
            if (!_foldoutStates.TryGetValue(key, out bool value))
            {
                _foldoutStates[key] = defaultValue;
                return defaultValue;
            }
            return value;
        }

        private void SetFoldoutState(string key, bool value)
        {
            _foldoutStates[key] = value;
        }

        private void MarkDirty()
        {
            _isDirty = true;
            Repaint();
        }

        private bool AreValuesEqual(object a, object b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            return Equals(a, b);
        }

        #endregion

        #region 数据读写与反射

        private string GetDataTypeDisplayName(Type type = null)
        {
            type ??= _selectedDataType;
            if (type == null) return string.Empty;

            EditableDataAttribute attr = type.GetCustomAttribute<EditableDataAttribute>();
            return attr == null || string.IsNullOrEmpty(attr.DisplayName) ? type.Name : attr.DisplayName;
        }

        private void LoadCurrentInstance()
        {
            if (_selectedDataType == null)
                return;

            try
            {
                MethodInfo loadMethod = GetGenericStaticMethod(typeof(GenericDataPersistence), "LoadData", _selectedDataType);
                if (loadMethod == null)
                {
                    Debug.LogError("未找到 GenericDataPersistence.LoadData");
                    _currentDataInstance = Activator.CreateInstance(_selectedDataType);
                    return;
                }

                _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });

                if (_currentDataInstance == null)
                {
                    Debug.LogWarning($"实例 {_instanceName} 加载为空，自动创建空实例");
                    _currentDataInstance = Activator.CreateInstance(_selectedDataType);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载实例失败：{_instanceName}\n{e}");
                _currentDataInstance = Activator.CreateInstance(_selectedDataType);
            }
        }

        private bool SaveCurrentInstance()
        {
            if (_selectedDataType == null || _currentDataInstance == null)
                return false;

            try
            {
                MethodInfo saveMethod = GetGenericStaticMethod(typeof(GenericDataPersistence), "SaveData", _selectedDataType);
                if (saveMethod == null)
                {
                    Debug.LogError("未找到 GenericDataPersistence.SaveData");
                    return false;
                }

                bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
                AssetDatabase.Refresh();
                return success;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存实例失败：{_instanceName}\n{e}");
                return false;
            }
        }

        private string[] GetAllInstanceNamesSafe(Type dataType)
        {
            if (dataType == null)
                return Array.Empty<string>();

            try
            {
                MethodInfo method = GetGenericStaticMethod(typeof(GenericDataPersistence), "GetAllInstanceNames", dataType);
                if (method == null)
                {
                    Debug.LogError("未找到 GenericDataPersistence.GetAllInstanceNames");
                    return Array.Empty<string>();
                }

                return (string[])method.Invoke(null, null) ?? Array.Empty<string>();
            }
            catch (Exception e)
            {
                Debug.LogError($"获取实例列表失败：{e}");
                return Array.Empty<string>();
            }
        }

        private MethodInfo GetGenericStaticMethod(Type ownerType, string methodName, Type genericArg)
        {
            if (ownerType == null || string.IsNullOrEmpty(methodName) || genericArg == null)
                return null;

            MethodInfo method = ownerType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == methodName && m.IsGenericMethodDefinition);

            if (method == null)
                return null;

            try
            {
                return method.MakeGenericMethod(genericArg);
            }
            catch (Exception e)
            {
                Debug.LogError($"泛型方法构造失败：{ownerType.Name}.{methodName}<{genericArg.Name}> \n{e}");
                return null;
            }
        }

        #endregion
    }
}