// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using System.IO;
// using OfficeOpenXml; // EPPlus

// namespace Gemini
// {


//     public class DataEditorWindow : EditorWindow
//     {
//         private string targetClassName = "ItemData"; // 目标类名测试
//         private object dataListObj = null; // List<T>
//         private Type targetType = null;
//         private Vector2 scrollPos;

//         [MenuItem("Tools/Data Table Editor")]
//         public static void ShowWindow()
//         {
//             GetWindow<DataEditorWindow>("数据表编辑器");
//         }

//         private void OnGUI()
//         {
//             GUILayout.Label("数据表管理工具", EditorStyles.boldLabel);

//             targetClassName = EditorGUILayout.TextField("解析类名 (Sheet名):", targetClassName);

//             GUILayout.BeginHorizontal();
//             if (GUILayout.Button("1. 从 Txt/Bin 加载数据")) LoadData();
//             if (GUILayout.Button("2. 保存并同步到 Excel/Txt/Bin")) SaveData();
//             GUILayout.EndHorizontal();

//             DrawDataList();
//         }

//         private void LoadData()
//         {
//             // 动态获取类型
//             targetType = Type.GetType(targetClassName);
//             if (targetType == null)
//             {
//                 Debug.LogError($"找不到类: {targetClassName}，请确保已经生成该类！");
//                 return;
//             }

//             // 调用 DataManager.Instance.GetTable<T>()
//             MethodInfo getTableMethod = typeof(DataManager).GetMethod("GetTable").MakeGenericMethod(targetType);
//             dataListObj = getTableMethod.Invoke(DataManager.Instance, null);
//         }

//         private void DrawDataList()
//         {
//             if (dataListObj == null) return;

//             IList list = dataListObj as IList;
//             if (list == null) return;

//             scrollPos = GUILayout.BeginScrollView(scrollPos);

//             for (int i = 0; i < list.Count; i++)
//             {
//                 GUILayout.BeginVertical("box");
//                 object item = list[i];

//                 // 基于反射绘制字段
//                 FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
//                 foreach (var field in fields)
//                 {
//                     DrawField(item, field);
//                 }
//                 GUILayout.EndVertical();
//             }

//             GUILayout.EndScrollView();
//         }

//         private void DrawField(object item, FieldInfo field)
//         {
//             object value = field.GetValue(item);

//             // 简单类型可视化绘制 (支持 int, float, string)
//             if (field.FieldType == typeof(int))
//                 field.SetValue(item, EditorGUILayout.IntField(field.Name, (int)value));
//             else if (field.FieldType == typeof(float))
//                 field.SetValue(item, EditorGUILayout.FloatField(field.Name, (float)value));
//             else if (field.FieldType == typeof(string))
//                 field.SetValue(item, EditorGUILayout.TextField(field.Name, (string)value));
//             // 对于 Array/List/Dict 的可视化需要使用 EditorGUI.PropertyField 
//             // 这里的进阶实现比较长，生产环境中通常会写一个递归的反射 GUI 绘制函数
//             else
//             {
//                 GUILayout.Label($"{field.Name} (Complex Type: {field.FieldType.Name})");
//             }
//         }

//         private void SaveData()
//         {
//             if (dataListObj == null || targetType == null) return;

//             string excelPath = "Assets/Data/Excel/GameData.xlsx"; // 你的Excel路径
//             FileInfo fileInfo = new FileInfo(excelPath);

//             // 1. 同步回 Excel
//             using (ExcelPackage package = new ExcelPackage(fileInfo))
//             {
//                 ExcelWorksheet sheet = package.Workbook.Worksheets[targetType.Name];
//                 if (sheet != null)
//                 {
//                     IList list = dataListObj as IList;
//                     FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);

//                     // 从第四行开始覆盖数据
//                     for (int i = 0; i < list.Count; i++)
//                     {
//                         object item = list[i];
//                         for (int col = 0; col < fields.Length; col++)
//                         {
//                             object val = fields[col].GetValue(item);
//                             // 注意：这里需要将 List/Dict 反向序列化为字符串 (例如 1,2,3) 写入 Excel
//                             sheet.Cells[i + 4, col + 1].Value = ConvertValueToString(val, fields[col].FieldType);
//                         }
//                     }
//                     package.Save();
//                 }
//             }

//             // 2. 重新触发导出生成 txt 和 binary
//             ExcelImporter.ImportExcel(excelPath);
//             Debug.Log("保存并同步成功！");
//         }

//         // 辅助方法：将内存中的对象转回 Excel 支持的字符串格式
//         private string ConvertValueToString(object obj, Type type)
//         {
//             if (obj == null) return "";
//             if (type == typeof(int[]) && obj is int[] arr) return string.Join(",", arr);
//             // ... 添加更多 List, Dict 的序列化逻辑
//             return obj.ToString();
//         }
//     }
// }
// #endif