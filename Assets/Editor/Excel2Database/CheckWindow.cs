using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using DataCenter;

/// <summary>
/// 数据表检查工具（Editor专用）
/// </summary>
public class DataTableCheckerWindow : EditorWindow
{
    // 核心配置
    private string _selectedTableName = "";
    private bool _useBinary = false;
    private string _loadStatus = "未加载";
    private string _loadError = "";
    private int _dataRowCount = 0;
    private string _loadPath = "";
    private object _loadedData = null;
    private Vector2 _scrollPos;
    private bool _showDataPreview = true;
    private bool _showAdvancedInfo = false;
    
    // 缓存信息
    private List<string> _cachedTableNames = new List<string>();
    private Vector2 _cacheScrollPos;

    // 样式定义
    private readonly GUIStyle _errorStyle = new GUIStyle
    {
        normal = { textColor = Color.red },
        wordWrap = true
    };
    private readonly GUIStyle _successStyle = new GUIStyle
    {
        normal = { textColor = Color.green },
        wordWrap = true
    };
    private readonly GUIStyle _titleStyle = new GUIStyle
    {
        fontSize = 14,
        fontStyle = FontStyle.Bold,
        normal = { textColor = Color.white }
    };

    [MenuItem("Tools/数据工具/数据表检查器")]
    public static void OpenWindow()
    {
        DataTableCheckerWindow window = GetWindow<DataTableCheckerWindow>("数据表检查器");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }

    private void OnEnable()
    {
        // 初始化时获取缓存信息
        RefreshCacheInfo();
    }

    private void OnGUI()
    {
        DrawHeader();
        GUILayout.Space(10);
        
        DrawLoadControls();
        GUILayout.Space(10);
        
        DrawLoadStatus();
        GUILayout.Space(10);
        
        DrawDataPreview();
        GUILayout.Space(10);
        
        DrawCacheManager();
    }

    #region 界面绘制 - 头部
    private void DrawHeader()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("数据表读取检查工具", _titleStyle);
        GUILayout.Label("用于验证DataTableManager的数据加载情况", EditorStyles.miniLabel);
        GUILayout.EndVertical();
    }
    #endregion

    #region 界面绘制 - 加载控制区
    private void DrawLoadControls()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("加载配置", EditorStyles.boldLabel);
        
        // 数据表名称输入
        GUILayout.BeginHorizontal();
        GUILayout.Label("数据表名称：", GUILayout.Width(80));
        _selectedTableName = EditorGUILayout.TextField(_selectedTableName);
        GUILayout.EndHorizontal();
        
        // 读取格式选择
        GUILayout.BeginHorizontal();
        GUILayout.Label("读取格式：", GUILayout.Width(80));
        _useBinary = EditorGUILayout.ToggleLeft("二进制", _useBinary, GUILayout.Width(80));
        EditorGUILayout.ToggleLeft("文本(JSON)", !_useBinary, GUILayout.Width(80));
        GUILayout.EndHorizontal();
        
        // 操作按钮
        GUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrEmpty(_selectedTableName);
        if (GUILayout.Button("加载数据表", GUILayout.Width(120)))
        {
            LoadDataTable();
        }
        GUI.enabled = true;
        
        if (GUILayout.Button("清空全部缓存", GUILayout.Width(120)))
        {
            DataTableManager.ClearCache();
            RefreshCacheInfo();
            EditorUtility.DisplayDialog("提示", "已清空所有数据表缓存", "确定");
        }
        
        if (GUILayout.Button("清空当前表缓存", GUILayout.Width(120)))
        {
            if (!string.IsNullOrEmpty(_selectedTableName))
            {
                DataTableManager.ClearCache(_selectedTableName);
                RefreshCacheInfo();
                EditorUtility.DisplayDialog("提示", $"已清空 {_selectedTableName} 的缓存", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请先输入数据表名称", "确定");
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
    #endregion

    #region 界面绘制 - 加载状态区
    private void DrawLoadStatus()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("加载状态", EditorStyles.boldLabel);
        
        // 基础状态
        GUILayout.BeginHorizontal();
        GUILayout.Label("状态：", GUILayout.Width(80));
        if (_loadStatus.Contains("成功"))
        {
            GUILayout.Label(_loadStatus, _successStyle);
        }
        else if (_loadStatus.Contains("失败"))
        {
            GUILayout.Label(_loadStatus, _errorStyle);
        }
        else
        {
            GUILayout.Label(_loadStatus);
        }
        GUILayout.EndHorizontal();
        
        // 数据行数
        GUILayout.BeginHorizontal();
        GUILayout.Label("数据行数：", GUILayout.Width(80));
        GUILayout.Label(_dataRowCount.ToString());
        GUILayout.EndHorizontal();
        
        // 加载路径
        GUILayout.BeginHorizontal();
        GUILayout.Label("加载路径：", GUILayout.Width(80));
        GUILayout.Label(_loadPath);
        GUILayout.EndHorizontal();
        
        // 错误信息
        if (!string.IsNullOrEmpty(_loadError))
        {
            GUILayout.Space(5);
            GUILayout.Label("错误详情：", EditorStyles.boldLabel);
            GUILayout.Label(_loadError, _errorStyle);
        }
        
        // 高级信息折叠
        _showAdvancedInfo = EditorGUILayout.Foldout(_showAdvancedInfo, "高级信息");
        if (_showAdvancedInfo)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("缓存Key规则：", GUILayout.Width(80));
            GUILayout.Label($"类型全名称_是否二进制");
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("默认读取格式：", GUILayout.Width(80));

            // bool currentDefault = !DataTableManager
            //     .GetType()
            //     .GetField("_defaultUseBinary", BindingFlags.NonPublic | BindingFlags.Static)
            //     .GetValue(null)
            //     .Equals(false);
            bool currentDefault = DataTableManager.useBinary;
            GUILayout.Label(currentDefault ? "二进制" : "文本(JSON)");
            GUILayout.EndHorizontal();
        }
        
        GUILayout.EndVertical();
    }
    #endregion

    #region 界面绘制 - 数据预览区
    private void DrawDataPreview()
    {
        GUILayout.BeginVertical("Box");
        _showDataPreview = EditorGUILayout.Foldout(_showDataPreview, "数据预览");
        
        if (_showDataPreview && _loadedData != null)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            try
            {
                // 处理List类型（DataTableManager返回的都是List<T>）
                if (_loadedData is IList listData)
                {
                    DrawListData(listData);
                }
                else
                {
                    // 非List类型的兜底显示
                    DrawObjectData("数据", _loadedData, 0);
                }
            }
            catch (Exception ex)
            {
                GUILayout.Label($"数据预览失败：{ex.Message}", _errorStyle);
            }
            
            EditorGUILayout.EndScrollView();
        }
        else if (_showDataPreview && _loadedData == null && _loadStatus.Contains("成功"))
        {
            GUILayout.Label("数据为空（行数为0）", EditorStyles.miniLabel);
        }
        
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制List数据
    /// </summary>
    private void DrawListData(IList list)
    {
        if (list.Count == 0)
        {
            GUILayout.Label("当前数据表无数据", EditorStyles.miniLabel);
            return;
        }

        // 只显示前50条避免卡顿，可根据需要调整
        int displayCount = Mathf.Min(list.Count, 50);
        GUILayout.Label($"显示前 {displayCount} 条（共 {list.Count} 条）", EditorStyles.miniBoldLabel);
        
        for (int i = 0; i < displayCount; i++)
        {
            GUILayout.BeginVertical("Box");
            GUILayout.Label($"行 [{i}]", EditorStyles.boldLabel);
            DrawObjectData($"行{i}", list[i], 1);
            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        if (list.Count > 50)
        {
            GUILayout.Label($"已省略 {list.Count - 50} 条数据", EditorStyles.miniLabel);
        }
    }

    /// <summary>
    /// 递归绘制对象数据
    /// </summary>
    private void DrawObjectData(string label, object obj, int indentLevel)
    {
        if (obj == null)
        {
            EditorGUI.indentLevel = indentLevel;
            GUILayout.Label($"{label}：null");
            return;
        }

        Type objType = obj.GetType();
        EditorGUI.indentLevel = indentLevel;

        // 基础类型直接显示
        if (IsBasicType(objType))
        {
            GUILayout.Label($"{label} ({objType.Name})：{obj}");
            return;
        }

        // 数组类型
        if (objType.IsArray)
        {
            Array arr = (Array)obj;
            GUILayout.Label($"{label} ({objType.Name}) 长度：{arr.Length}");
            EditorGUI.indentLevel++;
            for (int i = 0; i < arr.Length; i++)
            {
                DrawObjectData($"[{i}]", arr.GetValue(i), indentLevel + 1);
            }
            EditorGUI.indentLevel--;
            return;
        }

        // List类型
        if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(List<>))
        {
            IList list = (IList)obj;
            GUILayout.Label($"{label} ({objType.Name}) 长度：{list.Count}");
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                DrawObjectData($"[{i}]", list[i], indentLevel + 1);
            }
            EditorGUI.indentLevel--;
            return;
        }

        // Dictionary类型
        if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            IDictionary dict = (IDictionary)obj;
            GUILayout.Label($"{label} ({objType.Name}) 数量：{dict.Count}");
            EditorGUI.indentLevel++;
            foreach (DictionaryEntry entry in dict)
            {
                DrawObjectData($"Key: {entry.Key}", entry.Value, indentLevel + 1);
            }
            EditorGUI.indentLevel--;
            return;
        }

        // 自定义类/结构体（反射显示字段）
        FieldInfo[] fields = objType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        if (fields.Length == 0)
        {
            GUILayout.Label($"{label} ({objType.Name})：{obj}");
            return;
        }

        GUILayout.Label($"{label} ({objType.Name})");
        EditorGUI.indentLevel++;
        foreach (FieldInfo field in fields)
        {
            object fieldValue = field.GetValue(obj);
            DrawObjectData(field.Name, fieldValue, indentLevel + 1);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 判断是否为基础类型
    /// </summary>
    private bool IsBasicType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(float) ||
               type == typeof(double) || type == typeof(bool) || type == typeof(string) ||
               type == typeof(byte) || type == typeof(short) || type == typeof(uint) ||
               type == typeof(ulong) || type == typeof(Vector2) || type == typeof(Vector3) ||
               type == typeof(Color) || type == typeof(DateTime);
    }
    #endregion

    #region 界面绘制 - 缓存管理区
    private void DrawCacheManager()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("缓存管理", EditorStyles.boldLabel);
        
        _cacheScrollPos = EditorGUILayout.BeginScrollView(_cacheScrollPos, GUILayout.Height(100));
        
        if (_cachedTableNames.Count == 0)
        {
            GUILayout.Label("当前无缓存数据", EditorStyles.miniLabel);
        }
        else
        {
            foreach (string cacheKey in _cachedTableNames)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(cacheKey);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    // 解析缓存Key并删除
                    string tableName = cacheKey.Split('_')[0];
                    DataTableManager.ClearCache(tableName);
                    RefreshCacheInfo();
                    break;
                }
                GUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("刷新缓存列表", GUILayout.Width(120)))
        {
            RefreshCacheInfo();
        }
        
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 刷新缓存信息（通过反射获取私有缓存字典）
    /// </summary>
    private void RefreshCacheInfo()
    {
        try
        {
            FieldInfo cacheField = typeof(DataTableManager)
                .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Static);
            if (cacheField != null)
            {
                Dictionary<string, object> cacheDict = (Dictionary<string, object>)cacheField.GetValue(null);
                _cachedTableNames = cacheDict.Keys.ToList();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"刷新缓存列表失败：{ex.Message}");
            _cachedTableNames.Clear();
        }
    }
    #endregion

    #region 核心功能 - 加载数据表
    private void LoadDataTable()
    {
        // 重置状态
        _loadStatus = "加载中...";
        _loadError = "";
        _dataRowCount = 0;
        _loadPath = "";
        _loadedData = null;
        Repaint();

        try
        {
            // 构建加载路径（模拟DataTableManager的路径逻辑）
            string extension = _useBinary ? DataTableManager.DATA_BINARY_NAMEEND : ".txt";
            string basePath = _useBinary ? DataTableManager.DATA_BINARY_PATH : DataTableManager.DATA_TXT_PATH;
            _loadPath = $"{basePath}/{_selectedTableName}{extension}";

            // 调用DataTableManager加载数据（使用泛型方法）
            // 注意：这里需要用户根据实际数据类型调整，或使用动态类型
            // 简化处理：使用object接收，实际项目中可扩展类型选择
            MethodInfo getTableMethod = typeof(DataTableManager)
                .GetMethod("GetTable")
                .MakeGenericMethod(typeof(object)); // 替换为实际数据类型，如typeof(RoleData)
            
            object result = getTableMethod.Invoke(null, new object[] { _selectedTableName, _useBinary });
            
            // 处理结果
            _loadedData = result;
            if (result is IList list)
            {
                _dataRowCount = list.Count;
            }
            
            _loadStatus = "加载成功";
            Debug.Log($"[DataTableChecker] 成功加载 {_selectedTableName}，行数：{_dataRowCount}");
        }
        catch (TargetInvocationException ex)
        {
            // 捕获GetTable内部的异常
            _loadStatus = "加载失败";
            _loadError = ex.InnerException?.Message ?? ex.Message;
            Debug.LogError($"[DataTableChecker] 加载失败：{_loadError}");
        }
        catch (Exception ex)
        {
            _loadStatus = "加载失败";
            _loadError = ex.Message;
            Debug.LogError($"[DataTableChecker] 加载失败：{_loadError}");
        }

        Repaint();
    }
    #endregion
}