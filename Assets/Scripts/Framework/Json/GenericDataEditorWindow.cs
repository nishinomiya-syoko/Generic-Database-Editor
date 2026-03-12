// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;

// public class GenericDataEditorWindow : EditorWindow
// {
//     // 核心数据
//     private Type _selectedDataType;
//     private object _currentDataInstance;
//     private string _instanceName = "Default";
//     private List<Type> _editableDataTypes;

//     // 布局相关
//     private Vector2 _typeButtonScrollPos; 
//     private Vector2 _instanceListScrollPos; 
//     private Vector2 _editAreaScrollPos; 
//     private string _newInstanceName = "NewInstance"; 

//     [MenuItem("Tools/通用数据编辑器/打开编辑器")]
//     public static void OpenWindow()
//     {
//         GenericDataEditorWindow window = GetWindow<GenericDataEditorWindow>("通用数据编辑器");
//         window.minSize = new Vector2(800, 600);
//         window.Show();
//         window.Init();
//     }

//     private void Init()
//     {
//         _editableDataTypes = AppDomain.CurrentDomain.GetAssemblies()
//             .SelectMany(asm => asm.GetTypes())
//             .Where(t => t.GetCustomAttribute<EditableDataAttribute>() != null && !t.IsAbstract && !t.IsInterface)
//             .ToList();

//         if (_editableDataTypes.Count > 0)
//         {
//             _selectedDataType = _editableDataTypes[0];
//             LoadCurrentInstance();
//         }
//     }

//     private void OnGUI()
//     {
//         // ========== 上半部分：操作按钮区 ==========
//         EditorGUILayout.BeginVertical("Box");
//         EditorGUILayout.BeginHorizontal();

//         // 左侧：数据类型按钮组
//         EditorGUILayout.BeginVertical(GUILayout.Width(400));
//         EditorGUILayout.LabelField("数据类型", EditorStyles.boldLabel);
//         _typeButtonScrollPos = EditorGUILayout.BeginScrollView(_typeButtonScrollPos, GUILayout.Height(60));
//         DrawDataTypeButtons();
//         EditorGUILayout.EndScrollView();
//         EditorGUILayout.EndVertical();

//         // 右侧：功能按钮组（新增删除按钮）
//         EditorGUILayout.BeginVertical();
//         EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
//         DrawFunctionButtons();
//         EditorGUILayout.EndVertical();

//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.EndVertical();

//         // ========== 下半部分：数据列表+编辑区 ==========
//         if (_selectedDataType == null)
//         {
//             EditorGUILayout.HelpBox("请先选择左侧的数据类型", MessageType.Info);
//             return;
//         }

//         EditorGUILayout.BeginHorizontal();

//         // 左侧：实例列表 + 新建按钮
//         EditorGUILayout.BeginVertical("Box", GUILayout.Width(250));
//         EditorGUILayout.LabelField($"{GetDataTypeDisplayName()} - 实例列表", EditorStyles.boldLabel);
//         DrawNewInstanceButton();
//         GUILayout.FlexibleSpace();
//         DrawInstanceList();
//         EditorGUILayout.EndVertical();

//         // 右侧：字段编辑窗口
//         EditorGUILayout.BeginVertical("Box");
//         EditorGUILayout.LabelField($"编辑：{_instanceName}", EditorStyles.boldLabel);
//         _editAreaScrollPos = EditorGUILayout.BeginScrollView(_editAreaScrollPos);
//         DrawDataFields();
//         EditorGUILayout.EndScrollView();
//         EditorGUILayout.EndVertical();

//         EditorGUILayout.EndHorizontal();
//     }

//     #region 布局组件绘制
//     private void DrawDataTypeButtons()
//     {
//         EditorGUILayout.BeginHorizontal();
//         int buttonIndex = 0;
//         foreach (var dataType in _editableDataTypes)
//         {
//             string displayName = GetDataTypeDisplayName(dataType);
//             bool isSelected = dataType == _selectedDataType;
//             Color originalColor = GUI.backgroundColor;
//             if (isSelected)
//                 GUI.backgroundColor = Color.cyan;

//             if (GUILayout.Button(displayName, GUILayout.Width(120), GUILayout.Height(40)))
//             {
//                 _selectedDataType = dataType;
//                 _instanceName = "Default";
//                 LoadCurrentInstance();
//             }

//             GUI.backgroundColor = originalColor;
//             buttonIndex++;

//             if (buttonIndex % 3 == 0)
//             {
//                 EditorGUILayout.EndHorizontal();
//                 EditorGUILayout.BeginHorizontal();
//             }
//         }
//         EditorGUILayout.EndHorizontal();
//     }

//     // 核心修改：新增删除按钮
//     private void DrawFunctionButtons()
//     {
//         EditorGUILayout.BeginHorizontal();

//         if (GUILayout.Button("读取数据", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             LoadCurrentInstance();
//             EditorUtility.DisplayDialog("提示", "当前实例数据读取完成", "确定");
//         }

//         if (GUILayout.Button("保存数据", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             bool success = SaveCurrentInstance();
//             EditorUtility.DisplayDialog(success ? "成功" : "失败", 
//                 success ? "当前实例数据保存完成" : "当前实例数据保存失败", "确定");
//         }

//         // 新增：删除当前选中实例按钮（红色背景警示）
//         Color originalColor = GUI.backgroundColor;
//         GUI.backgroundColor = Color.red;
//         if (GUILayout.Button("删除当前实例", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             DeleteCurrentInstance();
//         }
//         GUI.backgroundColor = originalColor;

//         EditorGUILayout.EndHorizontal();

//         EditorGUILayout.BeginHorizontal();

//         if (GUILayout.Button("导出所有实例到Excel", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             ExportAllInstancesToExcel();
//         }

//         if (GUILayout.Button("从Excel导入所有实例", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             ImportAllInstancesFromExcel();
//         }

//         EditorGUILayout.EndHorizontal();
//     }

//     private void DrawNewInstanceButton()
//     {
//         EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
//         _newInstanceName = EditorGUILayout.TextField("新实例名", _newInstanceName);

//         if (GUILayout.Button("新建", GUILayout.Width(60)))
//         {
//             if (string.IsNullOrEmpty(_newInstanceName.Trim()))
//             {
//                 EditorUtility.DisplayDialog("提示", "实例名不能为空", "确定");
//                 return;
//             }

//             string newName = _newInstanceName.Trim();
//             _instanceName = newName;
//             _currentDataInstance = Activator.CreateInstance(_selectedDataType);
            
//             bool saveSuccess = SaveCurrentInstance();
//             if (saveSuccess)
//             {
//                 EditorUtility.DisplayDialog("提示", $"已创建并保存新实例：{newName}", "确定");
//                 _newInstanceName = "NewInstance";
//             }
//             else
//             {
//                 EditorUtility.DisplayDialog("失败", $"创建新实例失败：保存失败", "确定");
//             }
//         }

//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.Space(5);
//     }

//     private void DrawInstanceList()
//     {
//         _instanceListScrollPos = EditorGUILayout.BeginScrollView(
//             _instanceListScrollPos, 
//             GUILayout.ExpandHeight(true),
//             GUILayout.MinHeight(100)
//         );

//         MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
//             .MakeGenericMethod(_selectedDataType);
//         string[] instanceNames = (string[])getInstanceNamesMethod.Invoke(null, null);

//         foreach (string name in instanceNames)
//         {
//             bool isSelected = name == _instanceName;
//             Color originalColor = GUI.backgroundColor;
//             if (isSelected)
//                 GUI.backgroundColor = Color.green;

//             if (GUILayout.Button(name, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
//             {
//                 _instanceName = name;
//                 LoadCurrentInstance();
//             }

//             GUI.backgroundColor = originalColor;
//         }

//         EditorGUILayout.EndScrollView();
//     }

//     private void DrawDataFields()
//     {
//         if (_currentDataInstance == null) return;

//         FieldInfo[] fields = _selectedDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//             .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
//             .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
//             .ToArray();

//         foreach (var field in fields)
//         {
//             object value = field.GetValue(_currentDataInstance);
//             object newValue = DrawFieldControl(field.Name, field.FieldType, value);

//             if (!Equals(newValue, value))
//             {
//                 field.SetValue(_currentDataInstance, newValue);
//             }
//         }
//     }

//     private object DrawFieldControl(string label, Type fieldType, object value)
//     {
//         EditorGUILayout.BeginHorizontal();
//         EditorGUILayout.LabelField(label, GUILayout.Width(150));

//         object newValue = value;

//         if (fieldType == typeof(string))
//             newValue = EditorGUILayout.TextField((string)value);
//         else if (fieldType == typeof(int))
//             newValue = EditorGUILayout.IntField((int)value);
//         else if (fieldType == typeof(float))
//             newValue = EditorGUILayout.FloatField((float)value);
//         else if (fieldType == typeof(bool))
//             newValue = EditorGUILayout.Toggle((bool)value);
//         else if (fieldType == typeof(double))
//             newValue = EditorGUILayout.DoubleField((double)value);
//         else if (fieldType == typeof(long))
//             newValue = EditorGUILayout.LongField((long)value);
//         else if (fieldType == typeof(Vector2))
//             newValue = EditorGUILayout.Vector2Field("", (Vector2)value);
//         else if (fieldType == typeof(Vector3))
//             newValue = EditorGUILayout.Vector3Field("", (Vector3)value);
//         else if (fieldType == typeof(Vector4))
//             newValue = EditorGUILayout.Vector4Field("", (Vector4)value);
//         else if (fieldType == typeof(Color))
//             newValue = EditorGUILayout.ColorField("", (Color)value);
//         else if (fieldType == typeof(Rect))
//             newValue = EditorGUILayout.RectField("", (Rect)value);
//         else if (fieldType == typeof(Bounds))
//             newValue = EditorGUILayout.BoundsField("", (Bounds)value);
//         else if (fieldType == typeof(Quaternion))
//             newValue = Quaternion.Euler(EditorGUILayout.Vector3Field("", ((Quaternion)value).eulerAngles));
//         else if (fieldType.IsEnum)
//             newValue = EditorGUILayout.EnumPopup((Enum)value);
//         else
//             EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}", GUILayout.Width(200));

//         EditorGUILayout.EndHorizontal();
//         return newValue;
//     }
//     #endregion

//     #region 新增：删除当前选中实例的核心逻辑
//     private void DeleteCurrentInstance()
//     {
//         if (_selectedDataType == null || string.IsNullOrEmpty(_instanceName))
//         {
//             EditorUtility.DisplayDialog("提示", "请先选择要删除的实例", "确定");
//             return;
//         }

//         // 防误删确认
//         bool confirm = EditorUtility.DisplayDialog(
//             "确认删除", 
//             $"是否永久删除实例：{_instanceName}？\n此操作不可恢复！", 
//             "删除", 
//             "取消"
//         );

//         if (!confirm) return;

//         try
//         {
//             // 获取实例的保存路径
//             MethodInfo getSavePathMethod = typeof(GenericDataPersistence).GetMethod("GetSavePath")
//                 .MakeGenericMethod(_selectedDataType);
//             string savePath = (string)getSavePathMethod.Invoke(null, new object[] { _instanceName });

//             // 删除文件
//             if (File.Exists(savePath))
//             {
//                 File.Delete(savePath);
//                 AssetDatabase.Refresh();

//                 // 重置实例：优先选Default，无则选第一个实例，无则新建空实例
//                 MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
//                     .MakeGenericMethod(_selectedDataType);
//                 string[] remainingInstances = (string[])getInstanceNamesMethod.Invoke(null, null);

//                 if (remainingInstances.Length > 0)
//                 {
//                     _instanceName = remainingInstances.Contains("Default") ? "Default" : remainingInstances[0];
//                 }
//                 else
//                 {
//                     _instanceName = "Default";
//                     _currentDataInstance = Activator.CreateInstance(_selectedDataType);
//                 }

//                 // 重新加载实例
//                 LoadCurrentInstance();
//                 EditorUtility.DisplayDialog("成功", $"实例 {_instanceName} 已删除", "确定");
//             }
//             else
//             {
//                 EditorUtility.DisplayDialog("提示", "实例文件不存在，无需删除", "确定");
//             }
//         }
//         catch (Exception e)
//         {
//             EditorUtility.DisplayDialog("失败", $"删除实例失败：{e.Message}", "确定");
//             Debug.LogError($"删除实例 {_instanceName} 失败：{e}");
//         }
//     }
//     #endregion

//     #region 其他核心方法（无修改）
//     private string GetDataTypeDisplayName(Type type = null)
//     {
//         type ??= _selectedDataType;
//         if (type == null) return "";

//         var attr = type.GetCustomAttribute<EditableDataAttribute>();
//         return string.IsNullOrEmpty(attr.DisplayName) ? type.Name : attr.DisplayName;
//     }

//     private void LoadCurrentInstance()
//     {
//         if (_selectedDataType == null) return;

//         MethodInfo loadMethod = typeof(GenericDataPersistence).GetMethod("LoadData")
//             .MakeGenericMethod(_selectedDataType);
//         _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });
//     }

//     private bool SaveCurrentInstance()
//     {
//         if (_selectedDataType == null || _currentDataInstance == null) return false;

//         MethodInfo saveMethod = typeof(GenericDataPersistence).GetMethod("SaveData")
//             .MakeGenericMethod(_selectedDataType);
//         bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
//         AssetDatabase.Refresh();
//         return success;
//     }

//     private void ExportAllInstancesToExcel()
//     {
//         if (_selectedDataType == null)
//         {
//             EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
//             return;
//         }

//         string excelPath = ExcelDataUtility.SelectExcelSavePathForClass(_selectedDataType);
//         if (string.IsNullOrEmpty(excelPath))
//             return;

//         MethodInfo exportMethod = typeof(ExcelDataUtility).GetMethod("ExportAllInstancesToExcel")
//             .MakeGenericMethod(_selectedDataType);
//         bool success = (bool)exportMethod.Invoke(null, new[] { excelPath });

//         EditorUtility.DisplayDialog(success ? "成功" : "失败",
//             success ? $"[{GetDataTypeDisplayName()}] 所有实例导出到Excel成功\n路径：{excelPath}" : "导出失败", "确定");
//     }

//     private void ImportAllInstancesFromExcel()
//     {
//         if (_selectedDataType == null)
//         {
//             EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
//             return;
//         }

//         string excelPath = ExcelDataUtility.SelectExcelLoadPath();
//         if (string.IsNullOrEmpty(excelPath))
//             return;

//         MethodInfo importMethod = typeof(ExcelDataUtility).GetMethod("ImportAllInstancesFromExcel")
//             .MakeGenericMethod(_selectedDataType);
//         bool success = (bool)importMethod.Invoke(null, new[] { excelPath });

//         LoadCurrentInstance();
//         EditorUtility.DisplayDialog(success ? "成功" : "失败",
//             success ? $"[{GetDataTypeDisplayName()}] 从Excel导入所有实例成功" : "导入失败（无有效实例或文件错误）", "确定");
//     }
//     #endregion
// }