// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;
// using System.IO;

// namespace GPT.New
// {
//     /// <summary>
//     /// 通用数据编辑器窗口（增强版）
//     /// 已修复 Dictionary 编辑逻辑
//     /// </summary>
//     public class GeneralEditorWindow : EditorWindow
//     {
//         // 新增：临时存储新增字典的键/值（解决立即模式GUI的帧同步问题）
//         private Dictionary<Type, object> _tempDictNewKey = new Dictionary<Type, object>();
//         private Dictionary<Type, object> _tempDictNewValue = new Dictionary<Type, object>();
        
//         // 原有常量/变量保持不变...
//         private static readonly string JSON_PATH = Constant.JSON_PATH;
//         private static readonly string JSON_EXTENSION = ".json";
//         private static readonly string EXCEL_PATH = Constant.EXCEL_PATH;

//         private const float TYPE_PANEL_WIDTH = 400f;
//         private const float INSTANCE_PANEL_WIDTH = 280f;
//         private const float BUTTON_WIDTH = 120f;
//         private const float SMALL_BUTTON_WIDTH = 60f;
//         private const float MID_BUTTON_WIDTH = 80f;
//         private const float LARGE_BUTTON_WIDTH = 100f;
//         private const float BUTTON_HEIGHT = 30f;
//         private const float TYPE_SCROLL_HEIGHT = 60f;
//         private const int TYPE_BUTTONS_PER_ROW = 3;
//         private const int MAX_RECURSION_DEPTH = 8;

//         private Type _selectedDataType;
//         private object _currentDataInstance;
//         private string _instanceName = "Default";
//         private List<Type> _editableDataTypes = new List<Type>();

//         private Vector2 _typeButtonScrollPos;
//         private Vector2 _instanceListScrollPos;
//         private Vector2 _editAreaScrollPos;
//         private string _newInstanceName = "NewInstance";
//         private string _instanceSearchText = string.Empty;
//         private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
//         private bool _isDirty = false;

//         // 原有方法（Init/OnGUI/DrawDataTypeButtons等）保持不变...

//         #region 集合绘制 - 修复后的 Dictionary 编辑逻辑
//         private object DrawDictionaryValue(Type dictType, object dictInstance, string path, int depth)
//         {
//             // 1. 基础校验与反射获取核心方法/属性
//             if (dictInstance == null)
//             {
//                 EditorGUILayout.HelpBox("字典未初始化", MessageType.Warning);
//                 if (GUILayout.Button("初始化空字典", GUILayout.Width(BUTTON_WIDTH)))
//                 {
//                     dictInstance = CreateEmptyCollection(dictType);
//                     MarkDirty();
//                 }
//                 return dictInstance;
//             }

//             Type[] dictArgs = dictType.GetGenericArguments();
//             Type keyType = dictArgs[0];
//             Type valueType = dictArgs[1];

//             // 获取 Dictionary 核心方法/属性（带空值检查）
//             PropertyInfo countProp = dictType.GetProperty("Count");
//             MethodInfo addMethod = dictType.GetMethod("Add", new[] { keyType, valueType });
//             MethodInfo removeMethod = dictType.GetMethod("Remove", new[] { keyType });
//             MethodInfo clearMethod = dictType.GetMethod("Clear");
//             PropertyInfo indexerProp = dictType.GetProperty("Item", new[] { keyType });
//             MethodInfo containsKeyMethod = dictType.GetMethod("ContainsKey", new[] { keyType });

//             if (countProp == null || addMethod == null || removeMethod == null || clearMethod == null || indexerProp == null || containsKeyMethod == null)
//             {
//                 EditorGUILayout.HelpBox("无法获取 Dictionary 反射信息", MessageType.Error);
//                 return dictInstance;
//             }

//             // 2. 顶部操作按钮（清空/总数）
//             int count = (int)countProp.GetValue(dictInstance);
//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//             {
//                 clearMethod.Invoke(dictInstance, null);
//                 MarkDirty();
//                 EditorGUILayout.EndHorizontal();
//                 return dictInstance;
//             }
//             EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
//             EditorGUILayout.EndHorizontal();

//             // 3. 遍历编辑现有键值对（解决遍历中修改的问题：先收集要删除的键）
//             List<object> keysToRemove = new List<object>();
//             List<object> keys = new List<object>();
//             foreach (object item in (IEnumerable)dictInstance)
//             {
//                 if (item == null) continue;
//                 Type kvpType = item.GetType();
//                 PropertyInfo keyProp = kvpType.GetProperty("Key");
//                 PropertyInfo valueProp = kvpType.GetProperty("Value");
//                 if (keyProp != null) keys.Add(keyProp.GetValue(item));
//             }

//             foreach (object key in keys)
//             {
//                 EditorGUILayout.BeginVertical("box");

//                 // 键编辑区域（只读/可编辑）
//                 EditorGUILayout.BeginHorizontal();
//                 EditorGUILayout.LabelField("键", GUILayout.Width(40));
//                 object editedKey = key;

//                 // 仅支持简单类型键的编辑
//                 if (CanEditDictionaryKeyType(keyType))
//                 {
//                     editedKey = DrawAnyField("", keyType, key, $"{path}.key[{key}]", depth, false);
//                 }
//                 else
//                 {
//                     EditorGUILayout.SelectableLabel(key?.ToString() ?? "null", GUILayout.Height(EditorGUIUtility.singleLineHeight));
//                 }

//                 // 删除按钮（标记待删除，避免遍历中修改）
//                 if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//                 {
//                     keysToRemove.Add(key);
//                 }
//                 EditorGUILayout.EndHorizontal();

//                 // 值编辑区域（支持复杂类型递归编辑）
//                 EditorGUILayout.BeginHorizontal();
//                 EditorGUILayout.LabelField("值", GUILayout.Width(40));
//                 object oldValue = indexerProp.GetValue(dictInstance, new object[] { key });
//                 object newValue = DrawAnyField("", valueType, oldValue, $"{path}.value[{key}]", depth, false);
//                 EditorGUILayout.EndHorizontal();

//                 // 应用值修改
//                 if (!AreValuesEqual(oldValue, newValue))
//                 {
//                     indexerProp.SetValue(dictInstance, newValue, new object[] { key });
//                     MarkDirty();
//                 }

//                 // 应用键修改（仅当键变化且不重复时）
//                 if (!AreValuesEqual(key, editedKey) && editedKey != null)
//                 {
//                     bool keyExists = (bool)containsKeyMethod.Invoke(dictInstance, new[] { editedKey });
//                     if (!keyExists)
//                     {
//                         // 先删旧键，再加新键
//                         removeMethod.Invoke(dictInstance, new[] { key });
//                         addMethod.Invoke(dictInstance, new[] { editedKey, oldValue });
//                         MarkDirty();
//                     }
//                     else
//                     {
//                         EditorGUILayout.HelpBox($"键 [{editedKey}] 已存在，无法修改", MessageType.Warning);
//                     }
//                 }

//                 EditorGUILayout.EndVertical();
//             }

//             // 批量删除标记的键
//             foreach (object key in keysToRemove)
//             {
//                 removeMethod.Invoke(dictInstance, new[] { key });
//                 MarkDirty();
//             }

//             // 4. 新增键值对区域
//             EditorGUILayout.Space();
//             EditorGUILayout.LabelField("新增键值对", EditorStyles.boldLabel);
//             EditorGUILayout.BeginHorizontal();

//             // 初始化临时键/值（按类型缓存，避免帧丢失）
//             if (!_tempDictNewKey.ContainsKey(keyType))
//                 _tempDictNewKey[keyType] = CreateDefaultValueForField(keyType);
//             if (!_tempDictNewValue.ContainsKey(valueType))
//                 _tempDictNewValue[valueType] = CreateDefaultValueForField(valueType);

//             // 绘制键输入框
//             EditorGUILayout.LabelField("键", GUILayout.Width(40));
//             object newKey = DrawAnyField("", keyType, _tempDictNewKey[keyType], $"{path}.newKey", depth, false);
//             _tempDictNewKey[keyType] = newKey;

//             // 绘制值输入框
//             EditorGUILayout.LabelField("值", GUILayout.Width(40));
//             object newValueTemp = DrawAnyField("", valueType, _tempDictNewValue[valueType], $"{path}.newValue", depth, false);
//             _tempDictNewValue[valueType] = newValueTemp;

//             // 新增按钮
//             if (GUILayout.Button("添加", GUILayout.Width(MID_BUTTON_WIDTH)))
//             {
//                 if (newKey == null)
//                 {
//                     EditorUtility.DisplayDialog("错误", "键不能为空", "确定");
//                 }
//                 else if ((bool)containsKeyMethod.Invoke(dictInstance, new[] { newKey }))
//                 {
//                     EditorUtility.DisplayDialog("错误", $"键 [{newKey}] 已存在", "确定");
//                 }
//                 else
//                 {
//                     addMethod.Invoke(dictInstance, new[] { newKey, newValueTemp });
//                     MarkDirty();
//                     // 重置临时键值
//                     _tempDictNewKey[keyType] = CreateDefaultValueForField(keyType);
//                     _tempDictNewValue[valueType] = CreateDefaultValueForField(valueType);
//                 }
//             }
//             EditorGUILayout.EndHorizontal();

//             return dictInstance;
//         }
//         #endregion

//         #region 原有辅助方法（补充/修正）
//         private object DrawAnyField(string label, Type fieldType, object value, string path, int depth, bool showLabel)
//         {
//             if (depth > MAX_RECURSION_DEPTH)
//             {
//                 EditorGUILayout.HelpBox($"嵌套层级过深：{label}", MessageType.Warning);
//                 return value;
//             }

//             if (IsSimpleType(fieldType))
//             {
//                 return DrawSimpleField(label, fieldType, value, showLabel);
//             }

//             if (IsUnityObjectReference(fieldType))
//             {
//                 return DrawUnityObjectField(label, fieldType, value, showLabel);
//             }

//             if (IsCollectionType(fieldType))
//             {
//                 // 修复：调用正确的 Dictionary 绘制方法
//                 if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
//                 {
//                     return DrawDictionaryValue(fieldType, value, path, depth);
//                 }
//                 else if (fieldType.IsArray)
//                 {
//                     return DrawArrayValue(fieldType, value, path, depth);
//                 }
//                 else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
//                 {
//                     return DrawListValue(fieldType, value, path, depth);
//                 }
//                 return value;
//             }

//             if (IsSerializableComplexType(fieldType))
//             {
//                 return DrawComplexObjectField(label, fieldType, value, path, depth, showLabel);
//             }

//             EditorGUILayout.BeginHorizontal();
//             if (showLabel)
//                 EditorGUILayout.LabelField(label, GUILayout.Width(150));
//             EditorGUILayout.LabelField($"不支持的类型：{fieldType.Name}");
//             EditorGUILayout.EndHorizontal();

//             return value;
//         }

//         // 原有 DrawArrayValue/DrawListValue 保持不变...
//         private object DrawArrayValue(Type arrayType, object arrayInstance, string path, int depth)
//         {
//             // 原有逻辑不变...
//             Type elementType = arrayType.GetElementType();
//             Array array = (Array)arrayInstance;
//             int count = array.Length;

//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//             {
//                 MarkDirty();
//                 EditorGUILayout.EndHorizontal();
//                 return Array.CreateInstance(elementType, 0);
//             }

//             if (GUILayout.Button("添加元素", GUILayout.Width(MID_BUTTON_WIDTH)))
//             {
//                 Array newArray = Array.CreateInstance(elementType, count + 1);
//                 Array.Copy(array, newArray, count);
//                 newArray.SetValue(CreateDefaultValueForField(elementType), count);
//                 MarkDirty();
//                 EditorGUILayout.EndHorizontal();
//                 return newArray;
//             }

//             EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
//             EditorGUILayout.EndHorizontal();

//             for (int i = 0; i < count; i++)
//             {
//                 EditorGUILayout.BeginVertical("box");

//                 EditorGUILayout.BeginHorizontal();
//                 EditorGUILayout.LabelField($"索引 [{i}]", EditorStyles.boldLabel);

//                 if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//                 {
//                     Array newArray = Array.CreateInstance(elementType, count - 1);
//                     for (int j = 0, k = 0; j < count; j++)
//                     {
//                         if (j == i) continue;
//                         newArray.SetValue(array.GetValue(j), k++);
//                     }

//                     MarkDirty();
//                     EditorGUILayout.EndHorizontal();
//                     EditorGUILayout.EndVertical();
//                     return newArray;
//                 }
//                 EditorGUILayout.EndHorizontal();

//                 object oldElement = array.GetValue(i);
//                 object newElement = DrawAnyField("", elementType, oldElement, $"{path}[{i}]", depth, false);

//                 if (!AreValuesEqual(oldElement, newElement))
//                 {
//                     array.SetValue(newElement, i);
//                     MarkDirty();
//                 }

//                 EditorGUILayout.EndVertical();
//             }

//             return array;
//         }

//         private object DrawListValue(Type listType, object listInstance, string path, int depth)
//         {
//             // 原有逻辑不变...
//             Type elementType = listType.GetGenericArguments()[0];

//             PropertyInfo countProp = listType.GetProperty("Count");
//             MethodInfo addMethod = listType.GetMethod("Add");
//             MethodInfo removeAtMethod = listType.GetMethod("RemoveAt");
//             MethodInfo clearMethod = listType.GetMethod("Clear");
//             PropertyInfo indexerProp = listType.GetProperty("Item");

//             if (countProp == null || addMethod == null || removeAtMethod == null || clearMethod == null || indexerProp == null)
//             {
//                 EditorGUILayout.HelpBox("List 反射信息获取失败", MessageType.Error);
//                 return listInstance;
//             }

//             int count = (int)countProp.GetValue(listInstance);

//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("清空", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//             {
//                 clearMethod.Invoke(listInstance, null);
//                 MarkDirty();
//                 EditorGUILayout.EndHorizontal();
//                 return listInstance;
//             }

//             if (GUILayout.Button("添加元素", GUILayout.Width(MID_BUTTON_WIDTH)))
//             {
//                 addMethod.Invoke(listInstance, new[] { CreateDefaultValueForField(elementType) });
//                 MarkDirty();
//                 EditorGUILayout.EndHorizontal();
//                 return listInstance;
//             }

//             EditorGUILayout.LabelField($"总数：{count}", GUILayout.Width(60));
//             EditorGUILayout.EndHorizontal();

//             for (int i = 0; i < count; i++)
//             {
//                 EditorGUILayout.BeginVertical("box");

//                 EditorGUILayout.BeginHorizontal();
//                 EditorGUILayout.LabelField($"索引 [{i}]", EditorStyles.boldLabel);

//                 if (GUILayout.Button("删除", GUILayout.Width(SMALL_BUTTON_WIDTH)))
//                 {
//                     removeAtMethod.Invoke(listInstance, new object[] { i });
//                     MarkDirty();
//                     EditorGUILayout.EndHorizontal();
//                     EditorGUILayout.EndVertical();
//                     return listInstance;
//                 }
//                 EditorGUILayout.EndHorizontal();

//                 object oldElement = indexerProp.GetValue(listInstance, new object[] { i });
//                 object newElement = DrawAnyField("", elementType, oldElement, $"{path}[{i}]", depth, false);

//                 if (!AreValuesEqual(oldElement, newElement))
//                 {
//                     indexerProp.SetValue(listInstance, newElement, new object[] { i });
//                     MarkDirty();
//                 }

//                 EditorGUILayout.EndVertical();
//             }

//             return listInstance;
//         }

//         // 其他原有辅助方法（IsSimpleType/CreateDefaultValueForField等）保持不变...
//         private bool CanEditDictionaryKeyType(Type keyType)
//         {
//             if (keyType == null) return false;
//             if (IsSimpleType(keyType)) return true;
//             return false;
//         }

//         private bool IsSimpleType(Type type)
//         {
//             if (type == null) return false;

//             return type == typeof(string) ||
//                    type == typeof(int) ||
//                    type == typeof(float) ||
//                    type == typeof(bool) ||
//                    type == typeof(double) ||
//                    type == typeof(long) ||
//                    type == typeof(Vector2) ||
//                    type == typeof(Vector2Int) ||
//                    type == typeof(Vector3) ||
//                    type == typeof(Vector3Int) ||
//                    type == typeof(Vector4) ||
//                    type == typeof(Color) ||
//                    type == typeof(Rect) ||
//                    type == typeof(Bounds) ||
//                    type == typeof(Quaternion) ||
//                    type.IsEnum;
//         }

//         private bool IsUnityObjectReference(Type type)
//         {
//             return type != null && typeof(UnityEngine.Object).IsAssignableFrom(type);
//         }

//         private bool IsCollectionType(Type type)
//         {
//             if (type == null) return false;

//             if (type.IsArray) return true;

//             if (type.IsGenericType)
//             {
//                 Type genericDef = type.GetGenericTypeDefinition();
//                 return genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>);
//             }

//             return false;
//         }

//         private bool IsSerializableComplexType(Type type)
//         {
//             if (type == null) return false;
//             if (IsSimpleType(type)) return false;
//             if (IsUnityObjectReference(type)) return false;
//             if (IsCollectionType(type)) return false;
//             if (type == typeof(decimal)) return false;
//             if (type.IsAbstract || type.IsInterface) return false;

//             return type.IsSerializable || type.GetCustomAttribute<SerializableAttribute>() != null;
//         }

//         private object CreateDefaultValueForField(Type type)
//         {
//             if (type == null) return null;

//             if (type == typeof(string))
//                 return string.Empty;

//             if (type.IsEnum)
//             {
//                 Array values = Enum.GetValues(type);
//                 return values.Length > 0 ? values.GetValue(0) : Activator.CreateInstance(type);
//             }

//             if (type.IsValueType)
//                 return Activator.CreateInstance(type);

//             if (IsSerializableComplexType(type))
//                 return CreateDefaultComplexObject(type);

//             return null;
//         }

//         private object CreateDefaultComplexObject(Type type)
//         {
//             if (type == null) return null;

//             try
//             {
//                 return Activator.CreateInstance(type);
//             }
//             catch
//             {
//                 return null;
//             }
//         }

//         private void MarkDirty()
//         {
//             _isDirty = true;
//             Repaint();
//         }

//         private bool AreValuesEqual(object a, object b)
//         {
//             if (ReferenceEquals(a, b)) return true;
//             if (a == null || b == null) return false;
//             return Equals(a, b);
//         }
//         // 其他原有方法（LoadCurrentInstance/SaveCurrentInstance等）保持不变...
//     }

//     // 补充缺失的常量类和特性（确保代码可运行）
//     public static class Constant
//     {
//         public static string JSON_PATH = Application.dataPath + "/Resources/JsonData";
//         public static string EXCEL_PATH = Application.dataPath + "/Resources/ExcelData";
//     }

//     [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
//     public class EditableDataAttribute : Attribute
//     {
//         public string DisplayName { get; set; }
//         public EditableDataAttribute(string displayName = null)
//         {
//             DisplayName = displayName;
//         }
//     }

//     // 占位：实际项目中需替换为真实的 GenericDataPersistence 实现
//     public static class GenericDataPersistence
//     {
//         public static bool SaveData<T>(T data, string instanceName)
//         {
//             try
//             {
//                 string path = GetSavePath<T>(instanceName);
//                 Directory.CreateDirectory(Path.GetDirectoryName(path));
//                 string json = JsonUtility.ToJson(data, true);
//                 File.WriteAllText(path, json);
//                 return true;
//             }
//             catch
//             {
//                 return false;
//             }
//         }

//         public static T LoadData<T>(string instanceName)
//         {
//             try
//             {
//                 string path = GetSavePath<T>(instanceName);
//                 if (!File.Exists(path)) return default;
//                 string json = File.ReadAllText(path);
//                 return JsonUtility.FromJson<T>(json);
//             }
//             catch
//             {
//                 return default;
//             }
//         }

//         public static string GetSavePath<T>(string instanceName)
//         {
//             return Path.Combine(Constant.JSON_PATH, $"{typeof(T).Name}_{instanceName}{Constant.JSON_EXTENSION}");
//         }

//         public static string[] GetAllInstanceNames<T>()
//         {
//             try
//             {
//                 Directory.CreateDirectory(Constant.JSON_PATH);
//                 return Directory.GetFiles(Constant.JSON_PATH, $"{typeof(T).Name}_*{Constant.JSON_EXTENSION}")
//                     .Select(f => Path.GetFileNameWithoutExtension(f).Replace($"{typeof(T).Name}_", ""))
//                     .ToArray();
//             }
//             catch
//             {
//                 return new string[0];
//             }
//         }
//     }

  
// }