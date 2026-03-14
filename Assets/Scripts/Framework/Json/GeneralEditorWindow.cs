// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;
// using System.IO;

// /// <summary>
// /// 通用数据编辑器窗口（重构布局版）
// /// 布局：
// /// - 上半区：左=数据类型按钮组 | 右=功能按钮（保存/读取/导出/导入）
// /// - 下半区：左=实例列表+新建按钮 | 右=字段编辑窗口
// /// </summary>
// public class GeneralEditorWindow : EditorWindow
// {
//     private static readonly string JSON_PATH = Constant.JSON_PATH; // 数据保存基础路径
//     private static readonly string JSON_EXTENSION = ".json";
//     private static readonly string EXCEL_PATH = Constant.EXCEL_PATH; // Excel文件基础路径
//     // 核心数据
//     private Type _selectedDataType;
//     private object _currentDataInstance;
//     private string _instanceName = "Default";
//     private List<Type> _editableDataTypes;

//     // 布局相关
//     private Vector2 _typeButtonScrollPos; // 数据类型按钮滚动
//     private Vector2 _instanceListScrollPos; // 实例列表滚动
//     private Vector2 _editAreaScrollPos; // 编辑区滚动
//     private string _newInstanceName = "NewInstance"; // 新建实例的名称输入

//     // 打开编辑器窗口
//     [MenuItem("Tools/通用数据编辑器/General Editor Window")]
//     public static void OpenWindow()
//     {
//         GeneralEditorWindow window = GetWindow<GeneralEditorWindow>("通用数据编辑器");
//         window.minSize = new Vector2(800, 600); // 增大最小窗口尺寸适配新布局
//         window.Show();
//         window.Init();
//     }

//     // 初始化：扫描所有标记[EditableData]的类
//     private void Init()
//     {
//         _editableDataTypes = AppDomain.CurrentDomain.GetAssemblies()
//             .SelectMany(asm => asm.GetTypes())
//             .Where(t => t.GetCustomAttribute<EditableDataAttribute>() != null && !t.IsAbstract && !t.IsInterface)
//             .ToList();

//         // 默认选中第一个可编辑类
//         if (_editableDataTypes.Count > 0)
//         {
//             _selectedDataType = _editableDataTypes[0];
//             LoadCurrentInstance();
//         }
//     }

//     // 核心UI绘制（按新布局重构）
//     private void OnGUI()
//     {
//         // ========== 上半部分：操作按钮区 ==========
//         EditorGUILayout.BeginVertical("Box"); // 外框

//         // 上半区-行1：左=数据类型按钮 | 右=功能按钮
//         EditorGUILayout.BeginHorizontal();

//         // ---- 左侧：数据类型按钮组 ----
//         EditorGUILayout.BeginVertical(GUILayout.Width(400));
//         EditorGUILayout.LabelField("数据类型", EditorStyles.boldLabel);
//         _typeButtonScrollPos = EditorGUILayout.BeginScrollView(_typeButtonScrollPos, GUILayout.Height(60));
//         DrawDataTypeButtons(); // 绘制数据类型按钮
//         EditorGUILayout.EndScrollView();
//         EditorGUILayout.EndVertical();

//         // ---- 右侧：功能按钮组 ----
//         EditorGUILayout.BeginVertical();
//         EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
//         DrawFunctionButtons(); // 绘制保存/读取/导出/导入按钮
//         EditorGUILayout.EndVertical();

//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.EndVertical(); // 上半区外框结束

//         // ========== 下半部分：数据列表+编辑区 ==========
//         if (_selectedDataType == null)
//         {
//             EditorGUILayout.HelpBox("请先选择左侧的数据类型", MessageType.Info);
//             return;
//         }

//         EditorGUILayout.BeginHorizontal();

//         // ---- 左侧：实例列表 + 新建按钮 ----
//         EditorGUILayout.BeginVertical("Box", GUILayout.Width(250));
//         EditorGUILayout.LabelField($"{GetDataTypeDisplayName()} - 实例列表", EditorStyles.boldLabel);
//         DrawNewInstanceButton(); // 绘制新建实例按钮
//         DrawInstanceList(); // 绘制实例列表
//         EditorGUILayout.EndVertical();

//         // ---- 右侧：字段编辑窗口 ----
//         EditorGUILayout.BeginVertical("Box");
//         EditorGUILayout.LabelField($"编辑：{_instanceName}", EditorStyles.boldLabel);
//         _editAreaScrollPos = EditorGUILayout.BeginScrollView(_editAreaScrollPos);
//         DrawDataFields(); // 绘制字段编辑区
//         EditorGUILayout.EndScrollView();
//         EditorGUILayout.EndVertical();

//         EditorGUILayout.EndHorizontal();
//     }

//     #region 布局组件绘制
//     // 1. 绘制数据类型按钮组（横向排列，选中高亮）
//     private void DrawDataTypeButtons()
//     {
//         EditorGUILayout.BeginHorizontal();
//         int buttonIndex = 0;
//         foreach (var dataType in _editableDataTypes)
//         {
//             string displayName = GetDataTypeDisplayName(dataType);
//             // 选中状态高亮
//             bool isSelected = dataType == _selectedDataType;
//             Color originalColor = GUI.backgroundColor;
//             if (isSelected)
//                 GUI.backgroundColor = Color.cyan;

//             // 绘制按钮
//             if (GUILayout.Button(displayName, GUILayout.Width(120), GUILayout.Height(30)))
//             {
//                 _selectedDataType = dataType;
//                 _instanceName = "Default"; // 切换类型重置实例
//                 LoadCurrentInstance();
//             }

//             GUI.backgroundColor = originalColor;
//             buttonIndex++;

//             // 每3个按钮换行（优化布局）
//             if (buttonIndex % 3 == 0)
//             {
//                 EditorGUILayout.EndHorizontal();
//                 EditorGUILayout.BeginHorizontal();
//             }
//         }
//         EditorGUILayout.EndHorizontal();
//     }

// private void DrawFunctionButtons()
// {
//     EditorGUILayout.BeginHorizontal();

//     // 读取按钮
//     if (GUILayout.Button("读取数据", GUILayout.Width(120), GUILayout.Height(30)))
//     {
//         LoadCurrentInstance();
//         EditorUtility.DisplayDialog("提示", "当前实例数据读取完成", "确定");
//     }

//     // 保存按钮
//     if (GUILayout.Button("保存数据", GUILayout.Width(120), GUILayout.Height(30)))
//     {
//         bool success = SaveCurrentInstance();
//         EditorUtility.DisplayDialog(success ? "成功" : "失败", 
//             success ? "当前实例数据保存完成" : "当前实例数据保存失败", "确定");
//     }

// // 新增：删除当前选中实例按钮（红色背景警示）
//         Color originalColor = GUI.backgroundColor;
//         GUI.backgroundColor = Color.red;
//         if (GUILayout.Button("删除当前实例", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             DeleteCurrentInstance();
//         }
//         GUI.backgroundColor = originalColor;

//         // 导出所有实例到Excel（核心修改）
//         if (GUILayout.Button("导出所有实例到Excel", GUILayout.Width(120), GUILayout.Height(30)))
//         {
//             ExportAllInstancesToExcel();
//         }

//     // 从Excel导入所有实例（核心修改）
//     if (GUILayout.Button("从Excel导入所有实例", GUILayout.Width(120), GUILayout.Height(30)))
//     {
//         ImportAllInstancesFromExcel();
//     }

//     EditorGUILayout.EndHorizontal();
// }
// #region 新增：删除当前选中实例的核心逻辑
//     private void DeleteCurrentInstance()
//     {
//         if (_selectedDataType == null || string.IsNullOrEmpty(_instanceName))
//         {
//             // EditorUtility.DisplayDialog("提示", "请先选择要删除的实例", "确定");
//             Debug.LogWarning("请先选择要删除的实例");
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
//                 // EditorUtility.DisplayDialog("成功", $"实例 {_instanceName} 已删除", "确定");
//                 Debug.Log($"实例 {_instanceName} 已删除");
//             }
//             else
//             {
//                 // EditorUtility.DisplayDialog("提示", "实例文件不存在，无需删除", "确定");
//                 Debug.LogWarning($"实例文件不存在，无需删除");
//             }
//         }
//         catch (Exception e)
//         {
//             // EditorUtility.DisplayDialog("失败", $"删除实例失败：{e.Message}", "确定");
//             Debug.LogError($"删除实例 {_instanceName} 失败：{e}");
//         }
//     }
//     #endregion
// // 新增：导出该类所有实例到单个Excel
// private void ExportAllInstancesToExcel()
// {
//         if (_selectedDataType == null)
//         {
//             // EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
//             Debug.LogWarning("请先选择数据类型");
//             return;
//         }

//     // 选择保存路径（默认文件名：类名.xlsx）
//     // string excelPath = ExcelDataUtility.SelectExcelSavePathForClass(_selectedDataType);
//     string excelPath = EXCEL_PATH + $"{GetDataTypeDisplayName()}.xlsx";
//     if (string.IsNullOrEmpty(excelPath))
//         return;

//     // 调用批量导出方法
//     MethodInfo exportMethod = typeof(ExcelDataUtility).GetMethod("ExportAllInstancesToExcel")
//         .MakeGenericMethod(_selectedDataType);
//         bool success = (bool)exportMethod.Invoke(null, new[] { excelPath });

//     // EditorUtility.DisplayDialog(success ? "成功" : "失败",
//     //     success ? $"[{GetDataTypeDisplayName()}] 所有实例导出到Excel成功\n路径：{excelPath}" : "导出失败", "确定");
//     Debug.Log(success ? $"[{GetDataTypeDisplayName()}] 所有实例导出到Excel成功\n路径：{excelPath}" : "导出失败");
// }

// // 新增：从Excel导入该类所有实例
// private void ImportAllInstancesFromExcel()
// {
//     if (_selectedDataType == null)
//     {
//         EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
//         return;
//     }

//     // 选择Excel文件
//     string excelPath = ExcelDataUtility.SelectExcelLoadPath();
//     if (string.IsNullOrEmpty(excelPath))
//         return;

//     // 调用批量导入方法
//     MethodInfo importMethod = typeof(ExcelDataUtility).GetMethod("ImportAllInstancesFromExcel")
//         .MakeGenericMethod(_selectedDataType);
//     bool success = (bool)importMethod.Invoke(null, new[] { excelPath });

//     // 导入后刷新实例列表
//     LoadCurrentInstance(); // 重置当前实例加载，刷新列表
//     EditorUtility.DisplayDialog(success ? "成功" : "失败",
//         success ? $"[{GetDataTypeDisplayName()}] 从Excel导入所有实例成功" : "导入失败（无有效实例或文件错误）", "确定");
// }



//     // 修复：新建按钮逻辑 + 移到列表上方
//     private void DrawNewInstanceButton()
//     {
//         EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
//         // _newInstanceName = EditorGUILayout.TextField("新实例名", _newInstanceName);
//         GUILayout.Label("新实例名");
//         _newInstanceName = EditorGUILayout.TextField(_newInstanceName);

//         if (GUILayout.Button("新建", GUILayout.Width(60)))
//         {
//             if (string.IsNullOrEmpty(_newInstanceName.Trim()))
//             {
//                 // EditorUtility.DisplayDialog("提示", "实例名不能为空", "确定");
//                 Debug.LogWarning("实例名不能为空");
//                 return;
//             }

//             // 核心修复1：新建后立即保存到本地文件（持久化）
//             string newName = _newInstanceName.Trim();
//             _instanceName = newName;
//             _currentDataInstance = Activator.CreateInstance(_selectedDataType);

//             // 保存新实例到本地，确保列表能读取到
//             bool saveSuccess = SaveCurrentInstance();
//             if (saveSuccess)
//             {
//                 // EditorUtility.DisplayDialog("提示", $"已创建并保存新实例：{newName}", "确定");
//                 Debug.Log($"已创建并保存新实例：{newName}");
//                 // 清空输入框，提升体验
//                 _newInstanceName = "NewInstance";
//             }
//             else
//             {
//                 // EditorUtility.DisplayDialog("失败", $"创建新实例失败：保存失败", "确定");
//                 Debug.LogError($"创建新实例失败：保存失败");
//             }
//         }

//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.Space(5); // 按钮和列表间留空
//     }
//      // 3. 绘制当前类型的实例列表
//     private void DrawInstanceList()
//     {
//         _instanceListScrollPos = EditorGUILayout.BeginScrollView(_instanceListScrollPos);

//         // 获取该类型的所有实例名
//         MethodInfo getInstanceNamesMethod = typeof(GenericDataPersistence).GetMethod("GetAllInstanceNames")
//             .MakeGenericMethod(_selectedDataType);
//         string[] instanceNames = (string[])getInstanceNamesMethod.Invoke(null, null);

//         // 绘制每个实例项（选中高亮）
//         foreach (string name in instanceNames)
//         {
//             bool isSelected = name == _instanceName;
//             Color originalColor = GUI.backgroundColor;
//             if (isSelected)
//                 GUI.backgroundColor = Color.green;

//             // 点击切换实例
//             if (GUILayout.Button(name, GUILayout.Height(30)))
//             {
//                 _instanceName = name;
//                 LoadCurrentInstance();
//             }

//             GUI.backgroundColor = originalColor;
//         }

//         EditorGUILayout.EndScrollView();
//     }

//     // 5. 绘制字段编辑区（原有逻辑，调整滚动容器）
//     private void DrawDataFields()
//     {
//         if (_currentDataInstance == null) return;

//         // 获取所有可序列化字段
//         FieldInfo[] fields = _selectedDataType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//             .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
//             .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
//             .ToArray();

//         // 遍历绘制字段
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

//     // 辅助：绘制单个字段控件（原有逻辑）
//     private object DrawFieldControl(string label, Type fieldType, object value)
//     {
//         EditorGUILayout.BeginHorizontal();
//         EditorGUILayout.LabelField(label, GUILayout.Width(150));

//         object newValue = value;

//         // 基础类型
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

//         // Unity常用类型
//         else if (fieldType == typeof(Vector2))
//             newValue = EditorGUILayout.Vector2Field("", (Vector2)value);
//         else if (fieldType == typeof(Vector2Int))
//             newValue = EditorGUILayout.Vector2IntField("", (Vector2Int)value);
//         else if (fieldType == typeof(Vector3))
//             newValue = EditorGUILayout.Vector3Field("", (Vector3)value);
//         else if (fieldType == typeof(Vector3Int))
//             newValue = EditorGUILayout.Vector3IntField("", (Vector3Int)value);
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

//         // 枚举类型
//         else if (fieldType.IsEnum)
//             newValue = EditorGUILayout.EnumPopup((Enum)value);

//         // 暂不支持的类型
//         else
//             EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}", GUILayout.Width(200));

//         EditorGUILayout.EndHorizontal();
//         return newValue;
//     }
//     #endregion

//     #region 辅助方法
//     // 获取数据类型的显示名称（优先自定义，无则用类名）
//     private string GetDataTypeDisplayName(Type type = null)
//     {
//         type ??= _selectedDataType;
//         if (type == null) return "";

//         var attr = type.GetCustomAttribute<EditableDataAttribute>();
//         return string.IsNullOrEmpty(attr.DisplayName) ? type.Name : attr.DisplayName;
//     }

//     // 加载当前实例数据
//     private void LoadCurrentInstance()
//     {
//         if (_selectedDataType == null) return;

//         MethodInfo loadMethod = typeof(GenericDataPersistence).GetMethod("LoadData")
//             .MakeGenericMethod(_selectedDataType);
//         _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });
//     }

//     // 保存当前实例数据
//     private bool SaveCurrentInstance()
//     {
//         if (_selectedDataType == null || _currentDataInstance == null) return false;

//         MethodInfo saveMethod = typeof(GenericDataPersistence).GetMethod("SaveData")
//             .MakeGenericMethod(_selectedDataType);
//         bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
//         AssetDatabase.Refresh();
//         return success;
//     }

//     // // 导出Excel
//     // private void ExportCurrentDataToExcel()
//     // {
//     //     if (_currentDataInstance == null || _selectedDataType == null)
//     //     {
//     //         // EditorUtility.DisplayDialog("提示", "无数据可导出", "确定");
//     //         Debug.Log("无数据可导出");
//     //         return;
//     //     }

//     //     string defaultFileName = $"{_selectedDataType.Name}_{_instanceName}";
//     //     string excelPath = ExcelDataUtility.SelectExcelSavePath(defaultFileName);
//     //     if (string.IsNullOrEmpty(excelPath))
//     //         return;

//     //     MethodInfo exportMethod = typeof(ExcelDataUtility).GetMethod("ExportToExcel")
//     //         .MakeGenericMethod(_selectedDataType);
//     //     bool success = (bool)exportMethod.Invoke(null, new[] { _currentDataInstance, excelPath, _instanceName });

//     //     // EditorUtility.DisplayDialog(success ? "成功" : "失败",
//     //     //     success ? $"Excel导出成功：\n{excelPath}" : "Excel导出失败", "确定");
//     //     Debug.Log(success ? $"Excel导出成功：\n{excelPath}" : "Excel导出失败");
//     // }

//     // // 导入Excel
//     // private void ImportDataFromExcel()
//     // {
//     //     if (_selectedDataType == null)
//     //     {
//     //         // EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
//     //         Debug.Log("请先选择数据类型");
//     //         return;
//     //     }

//     //     string excelPath = ExcelDataUtility.SelectExcelLoadPath();
//     //     if (string.IsNullOrEmpty(excelPath))
//     //         return;

//     //     MethodInfo importMethod = typeof(ExcelDataUtility).GetMethod("ImportFromExcel")
//     //         .MakeGenericMethod(_selectedDataType);
//     //     _currentDataInstance = importMethod.Invoke(null, new[] { excelPath, _instanceName });

//     //     // EditorUtility.DisplayDialog("成功", "Excel数据导入完成", "确定");
//     //     Debug.Log("Excel数据导入完成");
//     // }
//     #endregion
// }