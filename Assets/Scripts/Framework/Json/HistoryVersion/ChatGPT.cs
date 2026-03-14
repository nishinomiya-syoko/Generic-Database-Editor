// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;
// using System.IO;

// /// <summary>
// /// 通用数据编辑器窗口（修复版）
// /// 布局：
// /// - 上半区：左=数据类型按钮组 | 右=功能按钮（保存/读取/导出/导入）
// /// - 下半区：左=实例列表+新建按钮 | 右=字段编辑窗口
// /// </summary>
// public class GeneralEditorWindow : EditorWindow
// {
//     private static readonly string JSON_PATH = Constant.JSON_PATH;
//     private static readonly string JSON_EXTENSION = ".json";
//     private static readonly string EXCEL_PATH = Constant.EXCEL_PATH;

//     // UI常量
//     private const float TYPE_PANEL_WIDTH = 400f;
//     private const float INSTANCE_PANEL_WIDTH = 250f;
//     private const float BUTTON_WIDTH = 120f;
//     private const float SMALL_BUTTON_WIDTH = 60f;
//     private const float MID_BUTTON_WIDTH = 80f;
//     private const float LARGE_BUTTON_WIDTH = 100f;
//     private const float BUTTON_HEIGHT = 30f;
//     private const float TYPE_SCROLL_HEIGHT = 60f;
//     private const int TYPE_BUTTONS_PER_ROW = 3;

//     // 核心数据
//     private Type _selectedDataType;
//     private object _currentDataInstance;
//     private string _instanceName = "Default";
//     private List<Type> _editableDataTypes = new List<Type>();

//     // 布局相关
//     private Vector2 _typeButtonScrollPos;
//     private Vector2 _instanceListScrollPos;
//     private Vector2 _editAreaScrollPos;
//     private string _newInstanceName = "NewInstance";

//     // Foldout状态缓存
//     private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

//     [MenuItem("Tools/通用数据编辑器/General Editor Window")]
//     public static void OpenWindow()
//     {
//         GeneralEditorWindow window = GetWindow<GeneralEditorWindow>("通用数据编辑器");
//         window.minSize = new Vector2(800, 600);
//         window.Show();
//         window.Init();
//     }

//     private void OnEnable()
//     {
//         Init(false);
//     }

//     /// <summary>
//     /// 初始化：扫描所有标记[EditableData]的类
//     /// </summary>
//     private void Init(bool forceResetSelection = true)
//     {
//         _editableDataTypes = AppDomain.CurrentDomain.GetAssemblies()
//             .SelectMany(GetLoadableTypes)
//             .Where(t => t != null)
//             .Where(t => t.GetCustomAttribute<EditableDataAttribute>() != null && !t.IsAbstract && !t.IsInterface)
//             .OrderBy(t => t.Name)
//             .ToList();

//         if (_editableDataTypes.Count == 0)
//         {
//             _selectedDataType = null;
//             _currentDataInstance = null;
//             return;
//         }

//         bool needReset =
//             forceResetSelection ||
//             _selectedDataType == null ||
//             !_editableDataTypes.Contains(_selectedDataType);

//         if (needReset)
//         {
//             _selectedDataType = _editableDataTypes[0];
//             _instanceName = "Default";
//             LoadCurrentInstance();
//         }
//         else if (_selectedDataType != null && _currentDataInstance == null)
//         {
//             LoadCurrentInstance();
//         }
//     }

//     private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
//     {
//         try
//         {
//             return assembly.GetTypes();
//         }
//         catch (ReflectionTypeLoadException e)
//         {
//             return e.Types.Where(t => t != null);
//         }
//         catch
//         {
//             return Array.Empty<Type>();
//         }
//     }

//     private void OnGUI()
//     {
//         if (_editableDataTypes == null || _editableDataTypes.Count == 0)
//         {
//             Init(false);
//         }

//         EditorGUILayout.BeginVertical("Box");

//         EditorGUILayout.BeginHorizontal();

//         // 左：数据类型按钮
//         EditorGUILayout.BeginVertical(GUILayout.Width(TYPE_PANEL_WIDTH));
//         EditorGUILayout.LabelField("数据类型", EditorStyles.boldLabel);
//         _typeButtonScrollPos = EditorGUILayout.BeginScrollView(_typeButtonScrollPos, GUILayout.Height(TYPE_SCROLL_HEIGHT));
//         DrawDataTypeButtons();
//         EditorGUILayout.EndScrollView();
//         EditorGUILayout.EndVertical();

//         // 右：操作按钮
//         EditorGUILayout.BeginVertical();
//         EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
//         DrawFunctionButtons();
//         EditorGUILayout.EndVertical();

//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.EndVertical();

//         if (_selectedDataType == null)
//         {
//             EditorGUILayout.HelpBox("未找到可编辑的数据类型，请确认目标类是否添加了 [EditableData] 特性。", MessageType.Info);
//             return;
//         }

//         EditorGUILayout.BeginHorizontal();

//         // 左：实例列表
//         EditorGUILayout.BeginVertical("Box", GUILayout.Width(INSTANCE_PANEL_WIDTH));
//         EditorGUILayout.LabelField($"{GetDataTypeDisplayName()} - 实例列表", EditorStyles.boldLabel);
//         DrawNewInstanceButton();
//         DrawInstanceList();
//         EditorGUILayout.EndVertical();

//         // 右：字段编辑
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
//         if (_editableDataTypes == null || _editableDataTypes.Count == 0)
//         {
//             EditorGUILayout.LabelField("没有找到可编辑类型");
//             return;
//         }

//         int total = _editableDataTypes.Count;
//         int rowCount = Mathf.CeilToInt(total / (float)TYPE_BUTTONS_PER_ROW);

//         for (int row = 0; row < rowCount; row++)
//         {
//             EditorGUILayout.BeginHorizontal();

//             for (int col = 0; col < TYPE_BUTTONS_PER_ROW; col++)
//             {
//                 int index = row * TYPE_BUTTONS_PER_ROW + col;
//                 if (index >= total)
//                     break;

//                 Type dataType = _editableDataTypes[index];
//                 string displayName = GetDataTypeDisplayName(dataType);
//                 bool isSelected = dataType == _selectedDataType;

//                 Color originalColor = GUI.backgroundColor;
//                 if (isSelected)
//                     GUI.backgroundColor = Color.cyan;

//                 if (GUILayout.Button(displayName, GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
//                 {
//                     if (_selectedDataType != dataType)
//                     {
//                         _selectedDataType = dataType;
//                         _instanceName = "Default";
//                         _currentDataInstance = null;
//                         LoadCurrentInstance();
//                     }
//                 }

//                 GUI.backgroundColor = originalColor;
//             }

//             EditorGUILayout.EndHorizontal();
//         }
//     }

//     private void DrawFunctionButtons()
//     {
//         EditorGUILayout.BeginHorizontal();

//         if (GUILayout.Button("读取数据", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
//         {
//             LoadCurrentInstance();
//             EditorUtility.DisplayDialog("提示", "当前实例数据读取完成", "确定");
//         }

//         if (GUILayout.Button("保存数据", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
//         {
//             bool success = SaveCurrentInstance();
//             EditorUtility.DisplayDialog(success ? "成功" : "失败",
//                 success ? "当前实例数据保存完成" : "当前实例数据保存失败", "确定");
//         }

//         Color originalColor = GUI.backgroundColor;
//         GUI.backgroundColor = Color.red;
//         if (GUILayout.Button("删除当前实例", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
//         {
//             DeleteCurrentInstance();
//         }
//         GUI.backgroundColor = originalColor;

//         if (GUILayout.Button("导出所有实例到Excel", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
//         {
//             ExportAllInstancesToExcel();
//         }

//         if (GUILayout.Button("从Excel导入所有实例", GUILayout.Width(BUTTON_WIDTH), GUILayout.Height(BUTTON_HEIGHT)))
//         {
//             ImportAllInstancesFromExcel();
//         }

//         EditorGUILayout.EndHorizontal();
//     }

//     private void DrawNewInstanceButton()
//     {
//         EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

//         GUILayout.Label("新实例名", GUILayout.Width(60));
//         _newInstanceName = EditorGUILayout.TextField(_newInstanceName ?? string.Empty);

//         if (GUILayout.Button("新建", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//         {
//             string newName = (_newInstanceName ?? string.Empty).Trim();
//             if (string.IsNullOrEmpty(newName))
//             {
//                 Debug.LogWarning("实例名不能为空");
//                 return;
//             }

//             if (_selectedDataType == null)
//             {
//                 Debug.LogWarning("请先选择数据类型");
//                 return;
//             }

//             try
//             {
//                 _instanceName = newName;
//                 _currentDataInstance = Activator.CreateInstance(_selectedDataType);

//                 bool saveSuccess = SaveCurrentInstance();
//                 if (saveSuccess)
//                 {
//                     Debug.Log($"已创建并保存新实例：{newName}");
//                     _newInstanceName = "NewInstance";
//                 }
//                 else
//                 {
//                     Debug.LogError("创建新实例失败：保存失败");
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"创建新实例失败：{e}");
//             }
//         }

//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.Space(5);
//     }

//     private void DrawInstanceList()
//     {
//         _instanceListScrollPos = EditorGUILayout.BeginScrollView(_instanceListScrollPos);

//         string[] instanceNames = GetAllInstanceNamesSafe(_selectedDataType);

//         if (instanceNames == null || instanceNames.Length == 0)
//         {
//             EditorGUILayout.HelpBox("当前类型还没有实例，可先新建一个。", MessageType.Info);
//         }
//         else
//         {
//             foreach (string name in instanceNames)
//             {
//                 bool isSelected = name == _instanceName;
//                 Color originalColor = GUI.backgroundColor;
//                 if (isSelected)
//                     GUI.backgroundColor = Color.green;

//                 if (GUILayout.Button(name, GUILayout.Height(BUTTON_HEIGHT)))
//                 {
//                     if (_instanceName != name)
//                     {
//                         _instanceName = name;
//                         LoadCurrentInstance();
//                     }
//                 }

//                 GUI.backgroundColor = originalColor;
//             }
//         }

//         EditorGUILayout.EndScrollView();
//     }

//     private void DrawDataFields()
//     {
//         if (_selectedDataType == null)
//         {
//             EditorGUILayout.HelpBox("未选择数据类型", MessageType.Warning);
//             return;
//         }

//         if (_currentDataInstance == null)
//         {
//             EditorGUILayout.HelpBox("当前实例为空，请尝试读取数据或新建实例。", MessageType.Warning);
//             return;
//         }

//         FieldInfo[] fields = _selectedDataType
//             .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//             .Where(f => !f.IsStatic && (f.IsPublic || f.GetCustomAttribute<SerializeField>() != null))
//             .Where(f => !f.Name.Contains("<") && !f.Name.Contains(">"))
//             .ToArray();

//         foreach (FieldInfo field in fields)
//         {
//             object value = field.GetValue(_currentDataInstance);

//             try
//             {
//                 if (IsCollectionType(field.FieldType))
//                 {
//                     DrawCollectionField(field, value);
//                 }
//                 else
//                 {
//                     object newValue = DrawFieldControl(field.Name, field.FieldType, value);
//                     if (!AreValuesEqual(newValue, value))
//                     {
//                         field.SetValue(_currentDataInstance, newValue);
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 EditorGUILayout.HelpBox($"字段 [{field.Name}] 绘制失败：{e.Message}", MessageType.Error);
//             }
//         }
//     }

//     #endregion

//     #region 实例管理

//     private void DeleteCurrentInstance()
//     {
//         if (_selectedDataType == null || string.IsNullOrEmpty(_instanceName))
//         {
//             Debug.LogWarning("请先选择要删除的实例");
//             return;
//         }

//         string deletedName = _instanceName;

//         bool confirm = EditorUtility.DisplayDialog(
//             "确认删除",
//             $"是否永久删除实例：{deletedName}？\n此操作不可恢复！",
//             "删除",
//             "取消"
//         );

//         if (!confirm) return;

//         try
//         {
//             MethodInfo getSavePathMethod = GetGenericStaticMethod(typeof(GenericDataPersistence), "GetSavePath", _selectedDataType);
//             if (getSavePathMethod == null)
//             {
//                 Debug.LogError("未找到 GenericDataPersistence.GetSavePath");
//                 return;
//             }

//             string savePath = (string)getSavePathMethod.Invoke(null, new object[] { deletedName });

//             if (File.Exists(savePath))
//             {
//                 File.Delete(savePath);
//                 AssetDatabase.Refresh();

//                 string[] remainingInstances = GetAllInstanceNamesSafe(_selectedDataType);

//                 if (remainingInstances.Length > 0)
//                 {
//                     _instanceName = remainingInstances.Contains("Default") ? "Default" : remainingInstances[0];
//                     LoadCurrentInstance();
//                 }
//                 else
//                 {
//                     _instanceName = "Default";
//                     _currentDataInstance = Activator.CreateInstance(_selectedDataType);
//                 }

//                 Debug.Log($"实例 {deletedName} 已删除");
//             }
//             else
//             {
//                 Debug.LogWarning($"实例文件不存在，无需删除：{savePath}");
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"删除实例 {deletedName} 失败：{e}");
//         }
//     }

//     private void ExportAllInstancesToExcel()
//     {
//         if (_selectedDataType == null)
//         {
//             Debug.LogWarning("请先选择数据类型");
//             return;
//         }

//         try
//         {
//             string fileName = $"{_selectedDataType.Name}.xlsx";
//             string excelPath = Path.Combine(EXCEL_PATH, fileName);

//             MethodInfo exportMethod = GetGenericStaticMethod(typeof(ExcelDataUtility), "ExportAllInstancesToExcel", _selectedDataType);
//             if (exportMethod == null)
//             {
//                 Debug.LogError("未找到 ExcelDataUtility.ExportAllInstancesToExcel");
//                 return;
//             }

//             bool success = (bool)exportMethod.Invoke(null, new object[] { excelPath });
//             Debug.Log(success
//                 ? $"[{GetDataTypeDisplayName()}] 所有实例导出到Excel成功\n路径：{excelPath}"
//                 : "导出失败");
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"导出Excel失败：{e}");
//         }
//     }

//     private void ImportAllInstancesFromExcel()
//     {
//         if (_selectedDataType == null)
//         {
//             EditorUtility.DisplayDialog("提示", "请先选择数据类型", "确定");
//             return;
//         }

//         try
//         {
//             MethodInfo selectMethod = typeof(ExcelDataUtility).GetMethod("SelectExcelLoadPath", BindingFlags.Public | BindingFlags.Static);
//             if (selectMethod == null)
//             {
//                 EditorUtility.DisplayDialog("失败", "未找到 ExcelDataUtility.SelectExcelLoadPath", "确定");
//                 return;
//             }

//             string excelPath = (string)selectMethod.Invoke(null, null);
//             if (string.IsNullOrEmpty(excelPath))
//                 return;

//             MethodInfo importMethod = GetGenericStaticMethod(typeof(ExcelDataUtility), "ImportAllInstancesFromExcel", _selectedDataType);
//             if (importMethod == null)
//             {
//                 EditorUtility.DisplayDialog("失败", "未找到 ExcelDataUtility.ImportAllInstancesFromExcel", "确定");
//                 return;
//             }

//             bool success = (bool)importMethod.Invoke(null, new object[] { excelPath });

//             LoadCurrentInstance();
//             EditorUtility.DisplayDialog(success ? "成功" : "失败",
//                 success
//                     ? $"[{GetDataTypeDisplayName()}] 从Excel导入所有实例成功"
//                     : "导入失败（无有效实例或文件错误）",
//                 "确定");
//         }
//         catch (Exception e)
//         {
//             EditorUtility.DisplayDialog("失败", $"导入Excel失败：{e.Message}", "确定");
//             Debug.LogError($"导入Excel失败：{e}");
//         }
//     }

//     #endregion

//     #region 集合类型处理

//     private bool IsCollectionType(Type type)
//     {
//         if (type == null) return false;

//         if (type.IsArray) return true;
//         if (type.IsGenericType)
//         {
//             Type genericDef = type.GetGenericTypeDefinition();
//             return genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>);
//         }

//         return false;
//     }

//     private void DrawCollectionField(FieldInfo field, object currentValue)
//     {
//         string foldoutKey = $"{_selectedDataType.FullName}.{field.Name}";
//         if (!_foldoutStates.ContainsKey(foldoutKey))
//             _foldoutStates[foldoutKey] = true;

//         string elementTypeName = currentValue == null
//             ? "未初始化"
//             : (GetCollectionElementType(field.FieldType)?.Name ?? "未知");

//         string foldoutLabel = $"{field.Name} ({elementTypeName} 集合)";
//         _foldoutStates[foldoutKey] = EditorGUILayout.Foldout(_foldoutStates[foldoutKey], foldoutLabel, true);

//         if (!_foldoutStates[foldoutKey])
//             return;

//         EditorGUI.indentLevel++;

//         if (currentValue == null)
//         {
//             EditorGUILayout.HelpBox("集合未初始化，点击下方按钮创建空集合", MessageType.Warning);
//             if (GUILayout.Button("初始化空集合", GUILayout.Width(BUTTON_WIDTH)))
//             {
//                 currentValue = CreateEmptyCollection(field.FieldType);
//                 field.SetValue(_currentDataInstance, currentValue);
//             }

//             EditorGUI.indentLevel--;
//             return;
//         }

//         Type collectionType = field.FieldType;

//         if (collectionType.IsArray)
//         {
//             DrawArrayField(field, currentValue);
//         }
//         else if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
//         {
//             DrawListField(field, currentValue);
//         }
//         else if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
//         {
//             DrawDictionaryField(field, currentValue);
//         }
//         else
//         {
//             EditorGUILayout.LabelField($"暂不支持的集合类型：{collectionType.Name}");
//         }

//         EditorGUI.indentLevel--;
//     }

//     private Type GetCollectionElementType(Type collectionType)
//     {
//         if (collectionType == null) return null;

//         if (collectionType.IsArray)
//         {
//             return collectionType.GetElementType();
//         }

//         if (collectionType.IsGenericType)
//         {
//             Type genericDef = collectionType.GetGenericTypeDefinition();
//             if (genericDef == typeof(List<>))
//             {
//                 return collectionType.GetGenericArguments()[0];
//             }

//             if (genericDef == typeof(Dictionary<,>))
//             {
//                 Type[] args = collectionType.GetGenericArguments();
//                 return typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]);
//             }
//         }

//         return null;
//     }

//     private object CreateEmptyCollection(Type collectionType)
//     {
//         if (collectionType == null) return null;

//         if (collectionType.IsArray)
//         {
//             Type elementType = collectionType.GetElementType();
//             return Array.CreateInstance(elementType, 0);
//         }

//         if (collectionType.IsGenericType)
//         {
//             Type genericDef = collectionType.GetGenericTypeDefinition();
//             if (genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>))
//             {
//                 return Activator.CreateInstance(collectionType);
//             }
//         }

//         return null;
//     }

//     private void DrawArrayField(FieldInfo field, object arrayInstance)
//     {
//         Type elementType = field.FieldType.GetElementType();
//         Array array = (Array)arrayInstance;
//         int count = array.Length;

//         EditorGUILayout.BeginHorizontal();
//         if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//         {
//             Array emptyArray = Array.CreateInstance(elementType, 0);
//             field.SetValue(_currentDataInstance, emptyArray);
//             EditorGUILayout.EndHorizontal();
//             return;
//         }

//         if (GUILayout.Button("添加元素", GUILayout.Width(MID_BUTTON_WIDTH)))
//         {
//             Array newArray = Array.CreateInstance(elementType, count + 1);
//             Array.Copy(array, newArray, count);
//             newArray.SetValue(CreateDefaultValue(elementType), count);
//             field.SetValue(_currentDataInstance, newArray);
//             EditorGUILayout.EndHorizontal();
//             return;
//         }

//         EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
//         EditorGUILayout.EndHorizontal();

//         for (int i = 0; i < count; i++)
//         {
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.LabelField($"索引 [{i}]", GUILayout.Width(60));

//             object elementValue = array.GetValue(i);
//             object newValue = DrawFieldControl("", elementType, elementValue);

//             if (!AreValuesEqual(newValue, elementValue))
//             {
//                 array.SetValue(newValue, i);
//                 field.SetValue(_currentDataInstance, array);
//             }

//             if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//             {
//                 Array newArray = Array.CreateInstance(elementType, count - 1);
//                 for (int j = 0, k = 0; j < count; j++)
//                 {
//                     if (j == i) continue;
//                     newArray.SetValue(array.GetValue(j), k++);
//                 }
//                 field.SetValue(_currentDataInstance, newArray);
//                 EditorGUILayout.EndHorizontal();
//                 return;
//             }

//             EditorGUILayout.EndHorizontal();
//         }
//     }

//     private void DrawListField(FieldInfo field, object listInstance)
//     {
//         Type listType = listInstance.GetType();
//         Type elementType = listType.GetGenericArguments()[0];

//         PropertyInfo countProp = listType.GetProperty("Count");
//         MethodInfo addMethod = listType.GetMethod("Add");
//         MethodInfo removeAtMethod = listType.GetMethod("RemoveAt");
//         MethodInfo clearMethod = listType.GetMethod("Clear");
//         PropertyInfo indexerProp = listType.GetProperty("Item");

//         if (countProp == null || addMethod == null || removeAtMethod == null || clearMethod == null || indexerProp == null)
//         {
//             EditorGUILayout.HelpBox("List 反射信息获取失败", MessageType.Error);
//             return;
//         }

//         int count = (int)countProp.GetValue(listInstance);

//         EditorGUILayout.BeginHorizontal();
//         if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//         {
//             clearMethod.Invoke(listInstance, null);
//             EditorGUILayout.EndHorizontal();
//             return;
//         }

//         if (GUILayout.Button("添加元素", GUILayout.Width(MID_BUTTON_WIDTH)))
//         {
//             addMethod.Invoke(listInstance, new[] { CreateDefaultValue(elementType) });
//             EditorGUILayout.EndHorizontal();
//             return;
//         }

//         EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
//         EditorGUILayout.EndHorizontal();

//         for (int i = 0; i < count; i++)
//         {
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.LabelField($"索引 [{i}]", GUILayout.Width(60));

//             object elementValue = indexerProp.GetValue(listInstance, new object[] { i });
//             object newValue = DrawFieldControl("", elementType, elementValue);

//             if (!AreValuesEqual(newValue, elementValue))
//             {
//                 indexerProp.SetValue(listInstance, newValue, new object[] { i });
//             }

//             if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//             {
//                 removeAtMethod.Invoke(listInstance, new object[] { i });
//                 EditorGUILayout.EndHorizontal();
//                 return;
//             }

//             EditorGUILayout.EndHorizontal();
//         }
//     }

//     private void DrawDictionaryField(FieldInfo field, object dictInstance)
//     {
//         Type dictType = dictInstance.GetType();
//         Type[] dictArgs = dictType.GetGenericArguments();
//         Type keyType = dictArgs[0];
//         Type valueType = dictArgs[1];

//         PropertyInfo countProp = dictType.GetProperty("Count");
//         MethodInfo addMethod = dictType.GetMethod("Add");
//         MethodInfo removeMethod = dictType.GetMethod("Remove");
//         MethodInfo clearMethod = dictType.GetMethod("Clear");
//         PropertyInfo indexerProp = dictType.GetProperty("Item");
//         MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey");

//         if (countProp == null || addMethod == null || removeMethod == null || clearMethod == null || indexerProp == null || containsKeyMethod == null)
//         {
//             EditorGUILayout.HelpBox("Dictionary 反射信息获取失败", MessageType.Error);
//             return;
//         }

//         int count = (int)countProp.GetValue(dictInstance);

//         EditorGUILayout.BeginHorizontal();

//         if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//         {
//             clearMethod.Invoke(dictInstance, null);
//             EditorGUILayout.EndHorizontal();
//             return;
//         }

//         if (GUILayout.Button("添加键值对", GUILayout.Width(LARGE_BUTTON_WIDTH)))
//         {
//             if (TryCreateNewDictionaryKey(dictInstance, keyType, containsKeyMethod, count, out object defaultKey))
//             {
//                 object defaultValue = CreateDefaultValue(valueType);
//                 addMethod.Invoke(dictInstance, new object[] { defaultKey, defaultValue });
//             }
//             else
//             {
//                 EditorUtility.DisplayDialog("提示", $"当前不支持为键类型 {keyType.Name} 自动生成新键", "确定");
//             }

//             EditorGUILayout.EndHorizontal();
//             return;
//         }

//         EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
//         EditorGUILayout.EndHorizontal();

//         List<object> keys = new List<object>();
//         foreach (object item in (IEnumerable)dictInstance)
//         {
//             if (item == null) continue;
//             Type kvpType = item.GetType();
//             PropertyInfo keyProp = kvpType.GetProperty("Key");
//             if (keyProp != null)
//             {
//                 keys.Add(keyProp.GetValue(item));
//             }
//         }

//         foreach (object keyValue in keys)
//         {
//             EditorGUILayout.BeginHorizontal();

//             EditorGUILayout.LabelField("键：", GUILayout.Width(40));
//             EditorGUILayout.LabelField(keyValue?.ToString() ?? "null", GUILayout.Width(120));

//             EditorGUILayout.LabelField("值：", GUILayout.Width(40));
//             object valueValue = indexerProp.GetValue(dictInstance, new object[] { keyValue });
//             object newValue = DrawFieldControl("", valueType, valueValue);

//             if (!AreValuesEqual(newValue, valueValue))
//             {
//                 indexerProp.SetValue(dictInstance, newValue, new object[] { keyValue });
//             }

//             if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//             {
//                 removeMethod.Invoke(dictInstance, new object[] { keyValue });
//                 EditorGUILayout.EndHorizontal();
//                 return;
//             }

//             EditorGUILayout.EndHorizontal();
//         }
//     }

//     private bool TryCreateNewDictionaryKey(object dictInstance, Type keyType, MethodInfo containsKeyMethod, int count, out object newKey)
//     {
//         newKey = null;

//         if (keyType == typeof(string))
//         {
//             string baseKey = "NewKey";
//             string candidate = baseKey;
//             int suffix = 1;

//             while ((bool)containsKeyMethod.Invoke(dictInstance, new object[] { candidate }))
//             {
//                 candidate = $"{baseKey}{suffix++}";
//             }

//             newKey = candidate;
//             return true;
//         }

//         if (keyType == typeof(int))
//         {
//             int candidate = 0;
//             while ((bool)containsKeyMethod.Invoke(dictInstance, new object[] { candidate }))
//             {
//                 candidate++;
//             }

//             newKey = candidate;
//             return true;
//         }

//         if (keyType.IsEnum)
//         {
//             Array values = Enum.GetValues(keyType);
//             foreach (object value in values)
//             {
//                 if (!(bool)containsKeyMethod.Invoke(dictInstance, new object[] { value }))
//                 {
//                     newKey = value;
//                     return true;
//                 }
//             }
//             return false;
//         }

//         return false;
//     }

//     #endregion

//     #region 字段绘制

//     private object DrawFieldControl(string label, Type fieldType, object value)
//     {
//         EditorGUILayout.BeginHorizontal();

//         if (!string.IsNullOrEmpty(label))
//         {
//             EditorGUILayout.LabelField(label, GUILayout.Width(150));
//         }

//         object newValue = value;

//         if (fieldType == typeof(string))
//         {
//             newValue = EditorGUILayout.TextField((string)(value ?? string.Empty));
//         }
//         else if (fieldType == typeof(int))
//         {
//             newValue = EditorGUILayout.IntField(value != null ? (int)value : 0);
//         }
//         else if (fieldType == typeof(float))
//         {
//             newValue = EditorGUILayout.FloatField(value != null ? (float)value : 0f);
//         }
//         else if (fieldType == typeof(bool))
//         {
//             newValue = EditorGUILayout.Toggle(value != null && (bool)value);
//         }
//         else if (fieldType == typeof(double))
//         {
//             newValue = EditorGUILayout.DoubleField(value != null ? (double)value : 0d);
//         }
//         else if (fieldType == typeof(long))
//         {
//             newValue = EditorGUILayout.LongField(value != null ? (long)value : 0L);
//         }
//         else if (fieldType == typeof(Vector2))
//         {
//             newValue = EditorGUILayout.Vector2Field("", value != null ? (Vector2)value : default);
//         }
//         else if (fieldType == typeof(Vector2Int))
//         {
//             newValue = EditorGUILayout.Vector2IntField("", value != null ? (Vector2Int)value : default);
//         }
//         else if (fieldType == typeof(Vector3))
//         {
//             newValue = EditorGUILayout.Vector3Field("", value != null ? (Vector3)value : default);
//         }
//         else if (fieldType == typeof(Vector3Int))
//         {
//             newValue = EditorGUILayout.Vector3IntField("", value != null ? (Vector3Int)value : default);
//         }
//         else if (fieldType == typeof(Vector4))
//         {
//             newValue = EditorGUILayout.Vector4Field("", value != null ? (Vector4)value : default);
//         }
//         else if (fieldType == typeof(Color))
//         {
//             newValue = EditorGUILayout.ColorField("", value != null ? (Color)value : Color.white);
//         }
//         else if (fieldType == typeof(Rect))
//         {
//             newValue = EditorGUILayout.RectField("", value != null ? (Rect)value : default);
//         }
//         else if (fieldType == typeof(Bounds))
//         {
//             newValue = EditorGUILayout.BoundsField("", value != null ? (Bounds)value : default);
//         }
//         else if (fieldType == typeof(Quaternion))
//         {
//             Quaternion q = value != null ? (Quaternion)value : Quaternion.identity;
//             newValue = Quaternion.Euler(EditorGUILayout.Vector3Field("", q.eulerAngles));
//         }
//         else if (fieldType.IsEnum)
//         {
//             Array enumValues = Enum.GetValues(fieldType);
//             Enum enumValue = value as Enum;
//             if (enumValue == null && enumValues.Length > 0)
//             {
//                 enumValue = (Enum)enumValues.GetValue(0);
//             }

//             newValue = EditorGUILayout.EnumPopup(enumValue);
//         }
//         else
//         {
//             EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}", GUILayout.Width(220));
//         }

//         EditorGUILayout.EndHorizontal();
//         return newValue;
//     }

//     #endregion

//     #region 辅助方法

//     private string GetDataTypeDisplayName(Type type = null)
//     {
//         type ??= _selectedDataType;
//         if (type == null) return string.Empty;

//         EditableDataAttribute attr = type.GetCustomAttribute<EditableDataAttribute>();
//         return attr == null || string.IsNullOrEmpty(attr.DisplayName) ? type.Name : attr.DisplayName;
//     }

//     private void LoadCurrentInstance()
//     {
//         if (_selectedDataType == null)
//             return;

//         try
//         {
//             MethodInfo loadMethod = GetGenericStaticMethod(typeof(GenericDataPersistence), "LoadData", _selectedDataType);
//             if (loadMethod == null)
//             {
//                 Debug.LogError("未找到 GenericDataPersistence.LoadData");
//                 _currentDataInstance = Activator.CreateInstance(_selectedDataType);
//                 return;
//             }

//             _currentDataInstance = loadMethod.Invoke(null, new object[] { _instanceName });

//             if (_currentDataInstance == null)
//             {
//                 Debug.LogWarning($"实例 {_instanceName} 加载为空，自动创建空实例");
//                 _currentDataInstance = Activator.CreateInstance(_selectedDataType);
//             }
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"加载实例失败：{_instanceName}\n{e}");
//             _currentDataInstance = Activator.CreateInstance(_selectedDataType);
//         }
//     }

//     private bool SaveCurrentInstance()
//     {
//         if (_selectedDataType == null || _currentDataInstance == null)
//             return false;

//         try
//         {
//             MethodInfo saveMethod = GetGenericStaticMethod(typeof(GenericDataPersistence), "SaveData", _selectedDataType);
//             if (saveMethod == null)
//             {
//                 Debug.LogError("未找到 GenericDataPersistence.SaveData");
//                 return false;
//             }

//             bool success = (bool)saveMethod.Invoke(null, new[] { _currentDataInstance, _instanceName });
//             AssetDatabase.Refresh();
//             return success;
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"保存实例失败：{_instanceName}\n{e}");
//             return false;
//         }
//     }

//     private string[] GetAllInstanceNamesSafe(Type dataType)
//     {
//         if (dataType == null)
//             return Array.Empty<string>();

//         try
//         {
//             MethodInfo method = GetGenericStaticMethod(typeof(GenericDataPersistence), "GetAllInstanceNames", dataType);
//             if (method == null)
//             {
//                 Debug.LogError("未找到 GenericDataPersistence.GetAllInstanceNames");
//                 return Array.Empty<string>();
//             }

//             return (string[])method.Invoke(null, null) ?? Array.Empty<string>();
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"获取实例列表失败：{e}");
//             return Array.Empty<string>();
//         }
//     }

//     private MethodInfo GetGenericStaticMethod(Type ownerType, string methodName, Type genericArg)
//     {
//         if (ownerType == null || string.IsNullOrEmpty(methodName) || genericArg == null)
//             return null;

//         MethodInfo method = ownerType
//             .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
//             .FirstOrDefault(m => m.Name == methodName && m.IsGenericMethodDefinition);

//         if (method == null)
//             return null;

//         try
//         {
//             return method.MakeGenericMethod(genericArg);
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"泛型方法构造失败：{ownerType.Name}.{methodName}<{genericArg.Name}> \n{e}");
//             return null;
//         }
//     }

//     private object CreateDefaultValue(Type type)
//     {
//         if (type == null) return null;

//         if (type == typeof(string))
//             return string.Empty;

//         if (type.IsValueType)
//             return Activator.CreateInstance(type);

//         return null;
//     }

//     private bool AreValuesEqual(object a, object b)
//     {
//         if (ReferenceEquals(a, b)) return true;
//         if (a == null || b == null) return false;
//         return Equals(a, b);
//     }

//     #endregion
// }