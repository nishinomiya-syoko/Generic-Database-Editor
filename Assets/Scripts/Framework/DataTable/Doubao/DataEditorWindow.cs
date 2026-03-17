#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using OfficeOpenXml; // EPPlus 4
using System.Text;
using System.Linq;

public class DataEditorWindow : EditorWindow
{
    private string targetClassName = "ItemData"; // 目标类名
    private object dataListObj = null; // List<T>
    private Type targetType = null;
    private Vector2 scrollPos;
    private bool useBinary = false; // 是否使用二进制加载
    private string excelRootPath = "Assets/Data/Excel/GameData.xlsx"; // Excel根路径

    [MenuItem("Tools/数据工具/Data Table Editor")]
    public static void ShowWindow()
    {
        GetWindow<DataEditorWindow>("数据表编辑器");
    }

    private void OnGUI()
    {
        GUILayout.Label("数据表管理工具", EditorStyles.boldLabel);
        
        // 基础配置区
        GUILayout.BeginVertical("box");
        targetClassName = EditorGUILayout.TextField("解析类名 (Sheet名):", targetClassName);
        useBinary = EditorGUILayout.Toggle("使用二进制加载", useBinary);
        excelRootPath = EditorGUILayout.TextField("Excel文件路径:", excelRootPath);
        GUILayout.EndVertical();

        // 操作按钮区
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1. 加载数据 (Txt/Bin)")) LoadData();
        if (GUILayout.Button("2. 保存到 Excel+Txt+Bin")) SaveData();
        if (GUILayout.Button("3. 重新导入Excel")) ReImportExcel();
        if (GUILayout.Button("清空缓存")) DataManager.Instance.ClearAllCache();
        GUILayout.EndHorizontal();

        // 数据展示区
        DrawDataList();
    }

    private void LoadData()
    {
        // 清空旧数据
        dataListObj = null;
        targetType = null;

        // 动态获取类型 (优先查找当前程序集)
        targetType = Type.GetType(targetClassName) ?? 
                     AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(a => a.GetTypes())
                             .FirstOrDefault(t => t.Name == targetClassName);

        if (targetType == null)
        {
            EditorUtility.DisplayDialog("错误", $"找不到类: {targetClassName}，请确保已经生成该类！", "确定");
            return;
        }

        try
        {
            // 调用 DataManager.Instance.GetTable<T>()
            MethodInfo getTableMethod = typeof(DataManager).GetMethod("GetTable")
                                                          .MakeGenericMethod(targetType);
            // 传递useBinary参数
            dataListObj = getTableMethod.Invoke(DataManager.Instance, new object[] { useBinary });

            if (dataListObj == null)
            {
                EditorUtility.DisplayDialog("警告", $"未加载到 {targetClassName} 的数据", "确定");
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("加载失败", ex.Message, "确定");
        }
    }

    private void DrawDataList()
    {
        if (dataListObj == null) return;

        IList list = dataListObj as IList;
        if (list == null)
        {
            GUILayout.Label("数据格式错误，非IList类型");
            return;
        }

        // 数据统计
        GUILayout.Label($"当前数据行数: {list.Count}", EditorStyles.miniLabel);

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        
        // 表头
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields)
        {
            GUILayout.Label(field.Name, EditorStyles.toolbarButton, GUILayout.Width(100));
        }
        GUILayout.EndHorizontal();

        // 数据行
        for (int i = 0; i < list.Count; i++)
        {
            GUILayout.BeginHorizontal("box");
            object item = list[i];
            
            // 基于反射绘制字段
            foreach (var field in fields)
            {
                DrawField(item, field, GUILayout.Width(100));
            }

            // 删除按钮
            if (GUILayout.Button("删除", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("确认", $"是否删除第{i+1}行数据？", "是", "否"))
                {
                    list.RemoveAt(i);
                    // 刷新列表
                    GUIUtility.ExitGUI();
                    return;
                }
            }
            GUILayout.EndHorizontal();
        }
        
        // 添加新行按钮
        if (GUILayout.Button("添加新行", EditorStyles.miniButton))
        {
            object newItem = Activator.CreateInstance(targetType);
            list.Add(newItem);
        }
        
        GUILayout.EndScrollView();
    }

    // 补全复杂类型的GUI绘制
    private void DrawField(object item, FieldInfo field, params GUILayoutOption[] options)
    {
        object value = field.GetValue(item);
        
        // 基础类型
        if (field.FieldType == typeof(int))
            field.SetValue(item, EditorGUILayout.IntField((int)value, options));
        else if (field.FieldType == typeof(float))
            field.SetValue(item, EditorGUILayout.FloatField((float)value, options));
        else if (field.FieldType == typeof(string))
            field.SetValue(item, EditorGUILayout.TextField((string)value, options));
        // 数组类型 (int[])
        else if (field.FieldType == typeof(int[]))
        {
            int[] arr = value as int[] ?? new int[0];
            string arrStr = string.Join(",", arr);
            string newArrStr = EditorGUILayout.TextField(arrStr, options);
            // 解析新值
            try
            {
                int[] newArr = Array.ConvertAll(newArrStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), int.Parse);
                field.SetValue(item, newArr);
            }
            catch
            {
                // 解析失败保留原值
            }
        }
        // List<string>
        else if (field.FieldType == typeof(List<string>))
        {
            List<string> list = value as List<string> ?? new List<string>();
            string listStr = string.Join(",", list);
            string newListStr = EditorGUILayout.TextField(listStr, options);
            field.SetValue(item, new List<string>(newListStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)));
        }
        // Dictionary<int,string>
        else if (field.FieldType == typeof(Dictionary<int, string>))
        {
            Dictionary<int, string> dict = value as Dictionary<int, string> ?? new Dictionary<int, string>();
            List<string> dictStrList = new List<string>();
            foreach (var kv in dict) dictStrList.Add($"{kv.Key}:{kv.Value}");
            string dictStr = string.Join(",", dictStrList);
            
            string newDictStr = EditorGUILayout.TextField(dictStr, options);
            Dictionary<int, string> newDict = new Dictionary<int, string>();
            try
            {
                foreach (var pair in newDictStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length == 2)
                    {
                        newDict.Add(int.Parse(kv[0]), kv[1]);
                    }
                }
                field.SetValue(item, newDict);
            }
            catch
            {
                // 解析失败保留原值
            }
        }
        // 其他复杂类型
        else
        {
            GUILayout.Label($"{field.FieldType.Name}", options);
        }
    }

    private void SaveData()
    {
        if (dataListObj == null || targetType == null)
        {
            EditorUtility.DisplayDialog("警告", "无数据可保存", "确定");
            return;
        }

        if (!File.Exists(excelRootPath))
        {
            EditorUtility.DisplayDialog("错误", $"Excel文件不存在：{excelRootPath}", "确定");
            return;
        }

        try
        {
            // 1. 同步回 Excel (EPPlus4 适配)
            FileInfo fileInfo = new FileInfo(excelRootPath);
            // EPPlus4 适配：复制文件避免占用
            string tempPath = Path.Combine(Path.GetDirectoryName(excelRootPath), $"~temp_{Path.GetFileName(excelRootPath)}");
            File.Copy(excelRootPath, tempPath, true);

            using (ExcelPackage package = new ExcelPackage(new FileInfo(tempPath)))
            {
                ExcelWorksheet sheet = package.Workbook.Worksheets[targetType.Name];
                if (sheet == null)
                {
                    EditorUtility.DisplayDialog("错误", $"Excel中不存在工作表：{targetType.Name}", "确定");
                    return;
                }

                IList list = dataListObj as IList;
                FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                
                // 清空原有数据行 (保留前三行表头)
                if (sheet.Dimension != null && sheet.Dimension.End.Row > 3)
                {
                    sheet.DeleteRow(4, sheet.Dimension.End.Row - 3);
                }

                // 写入新数据 (从第四行开始)
                for (int i = 0; i < list.Count; i++)
                {
                    object item = list[i];
                    for (int col = 0; col < fields.Length; col++)
                    {
                        object val = fields[col].GetValue(item);
                        string cellValue = ConvertValueToString(val, fields[col].FieldType);
                        sheet.Cells[i + 4, col + 1].Value = cellValue;
                    }
                }

                // 保存Excel
                package.Save();
                // 替换原文件
                File.Copy(tempPath, excelRootPath, true);
                File.Delete(tempPath);
            }

            // 2. 重新导入Excel生成Txt和Binary
            ExcelImporter.ImportExcel(excelRootPath);
            // 清空缓存
            // DataManager.Instance.ClearCache(targetType);
            

            EditorUtility.DisplayDialog("成功", "数据保存并同步完成！", "确定");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("保存失败", ex.Message, "确定");
        }
    }

    // 重新导入Excel
    private void ReImportExcel()
    {
        if (!File.Exists(excelRootPath))
        {
            EditorUtility.DisplayDialog("错误", $"Excel文件不存在：{excelRootPath}", "确定");
            return;
        }

        try
        {
            ExcelImporter.ImportExcel(excelRootPath);
            EditorUtility.DisplayDialog("成功", "Excel重新导入完成！", "确定");
            // 重新加载数据
            LoadData();
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("导入失败", ex.Message, "确定");
        }
    }

    // 辅助方法：将内存中的对象转回 Excel 支持的字符串格式
    private string ConvertValueToString(object obj, Type type)
    {
        if (obj == null) return "";

        if (type == typeof(int[]))
            return string.Join(",", (int[])obj);
        else if (type == typeof(List<string>))
            return string.Join(",", (List<string>)obj);
        else if (type == typeof(Dictionary<int, string>))
        {
            Dictionary<int, string> dict = (Dictionary<int, string>)obj;
            List<string> parts = new List<string>();
            foreach (var kv in dict) parts.Add($"{kv.Key}:{kv.Value}");
            return string.Join(",", parts);
        }
        else if (type == typeof(float))
            return ((float)obj).ToString("0.00"); // 格式化浮点数
        else
            return obj.ToString();
    }
}
#endif