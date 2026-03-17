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
/// 用于验证所有[EditableData]属性的数据表类是否正常读取数据
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

    // 【新增】数据表扫描结果
    private List<TableScanResult> _scannedTables = new List<TableScanResult>();
    private Vector2 _scanScrollPos;
    private bool _autoScanOnEnable = true;
    private bool _showOnlyFailed = false;
    private bool _isScanning = false;
    private string _scanFilter = "";

    // 样式定义
    private GUIStyle _errorStyle;
    private GUIStyle _successStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _centerStyle;
    

    [MenuItem("Tools/数据工具/数据表检查器")]
    public static void OpenWindow()
    {
    
        DataTableCheckerWindow window = GetWindow<DataTableCheckerWindow>("数据表检查器");
        window.minSize = new Vector2(900, 650);
        window.Show();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        // Initialize GUI Styles here
        _errorStyle = new GUIStyle
        {
            normal = { textColor = Color.red },
            wordWrap = true
        };
        _successStyle = new GUIStyle
        {
            normal = { textColor = Color.green },
            wordWrap = true
        };
        _warningStyle = new GUIStyle
        {
            normal = { textColor = Color.yellow },
            wordWrap = true
        };
        _titleStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        _centerStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        // 初始化时获取缓存信息
        RefreshCacheInfo();

        // 自动扫描所有数据表
        if (_autoScanOnEnable)
        {
            ScanAllDataTables();
        }
    }

    private void OnGUI()
    {
        //检查是否运行
        if( !Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先运行游戏", MessageType.Warning);
            return;
        }
        DrawHeader();
        GUILayout.Space(10);
        
        DrawScanControls();
        GUILayout.Space(10);
        
        DrawScanResults();
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
        GUILayout.Label("用于验证所有[EditableData]属性的数据表类是否正常读取数据", EditorStyles.miniLabel);
        GUILayout.EndVertical();
    }
    #endregion

    #region 界面绘制 - 扫描控制区【新增】
    private void DrawScanControls()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("批量扫描配置", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();
        
        // 扫描按钮
        GUI.enabled = !_isScanning;
        if (GUILayout.Button("🔍 扫描所有数据表", GUILayout.Width(150), GUILayout.Height(25)))
        {
            ScanAllDataTables();
        }
        GUI.enabled = true;
        
        // 重新扫描
        if (GUILayout.Button("🔄 重新扫描", GUILayout.Width(100), GUILayout.Height(25)))
        {
            _scannedTables.Clear();
            ScanAllDataTables();
        }
        
        // 导出报告
        if (GUILayout.Button("📄 导出报告", GUILayout.Width(100), GUILayout.Height(25)))
        {
            ExportScanReport();
        }
        
        GUILayout.FlexibleSpace();
        
        // 过滤选项
        GUILayout.Label("过滤:", GUILayout.Width(40));
        _showOnlyFailed = EditorGUILayout.ToggleLeft("仅显示失败", _showOnlyFailed, GUILayout.Width(90));
        
        GUILayout.Label("搜索:", GUILayout.Width(40));
        _scanFilter = EditorGUILayout.TextField(_scanFilter, GUILayout.Width(150));
        
        GUILayout.EndHorizontal();
        
        // 扫描进度
        if (_isScanning)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("扫描中...", _warningStyle);
            GUILayout.EndHorizontal();
        }
        
        GUILayout.EndVertical();
    }
    #endregion

    #region 界面绘制 - 扫描结果区【新增】
    private void DrawScanResults()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.BeginHorizontal();
        GUILayout.Label($"数据表扫描结果 (共 {_scannedTables.Count} 个)", EditorStyles.boldLabel);
        
        // 统计信息
        int successCount = _scannedTables.Count(t => t.LoadSuccess);
        int failCount = _scannedTables.Count(t => !t.LoadSuccess);
        GUILayout.Label($"✓ 成功: {successCount}", _successStyle, GUILayout.Width(80));
        GUILayout.Label($"✗ 失败: {failCount}", _errorStyle, GUILayout.Width(80));
        
        GUILayout.FlexibleSpace();
        
        // 批量操作
        GUI.enabled = failCount > 0;
        if (GUILayout.Button("重试失败项", GUILayout.Width(100)))
        {
            RetryFailedTables();
        }
        GUI.enabled = true;
        
        GUILayout.EndHorizontal();
        
        _scanScrollPos = EditorGUILayout.BeginScrollView(_scanScrollPos, GUILayout.Height(200));
        
        var displayTables = _scannedTables;
        if (_showOnlyFailed)
        {
            displayTables = _scannedTables.Where(t => !t.LoadSuccess).ToList();
        }
        if (!string.IsNullOrEmpty(_scanFilter))
        {
            displayTables = displayTables.Where(t => 
                t.TableName.IndexOf(_scanFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }
        
        if (displayTables.Count == 0)
        {
            GUILayout.Label(_scannedTables.Count == 0 ? "未扫描到数据表，请点击扫描按钮" : "没有符合条件的数据表", EditorStyles.miniLabel);
        }
        else
        {
            // 表头
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label("状态", GUILayout.Width(50));
            GUILayout.Label("数据表名称", GUILayout.Width(200));
            GUILayout.Label("类型", GUILayout.Width(150));
            GUILayout.Label("行数", GUILayout.Width(60));
            GUILayout.Label("格式", GUILayout.Width(50));
            GUILayout.Label("耗时", GUILayout.Width(60));
            GUILayout.Label("错误信息");
            GUILayout.EndHorizontal();
            
            foreach (var table in displayTables)
            {
                GUILayout.BeginHorizontal("Box");
                
                // 状态
                if (table.LoadSuccess)
                {
                    GUILayout.Label("✓", _successStyle, GUILayout.Width(50));
                }
                else
                {
                    GUILayout.Label("✗", _errorStyle, GUILayout.Width(50));
                }
                
                // 表名
                if (GUILayout.Button(table.TableName, EditorStyles.linkLabel, GUILayout.Width(200)))
                {
                    _selectedTableName = table.TableName;
                    _useBinary = table.UseBinary;
                    LoadDataTable();
                }
                
                // 类型
                GUILayout.Label(table.DataTypeShortName, GUILayout.Width(150));
                
                // 行数
                GUILayout.Label(table.RowCount.ToString(), GUILayout.Width(60));
                
                // 格式
                GUILayout.Label(table.UseBinary ? "Bin" : "Txt", GUILayout.Width(50));
                
                // 耗时
                GUILayout.Label($"{table.LoadTimeMs}ms", GUILayout.Width(60));
                
                // 错误信息
                if (!string.IsNullOrEmpty(table.ErrorMessage))
                {
                    GUILayout.Label(table.ErrorMessage, _errorStyle);
                }
                else
                {
                    GUILayout.Label("-");
                }
                
                GUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        GUILayout.EndVertical();
    }
    #endregion

    #region 界面绘制 - 加载控制区
    private void DrawLoadControls()
    {
        GUILayout.BeginVertical("Box");
        GUILayout.Label("单表加载配置", EditorStyles.boldLabel);
        
        // 数据表名称输入
        GUILayout.BeginHorizontal();
        GUILayout.Label("数据表名称：", GUILayout.Width(80));
        _selectedTableName = EditorGUILayout.TextField(_selectedTableName);
        
        // 快速选择
        if (_scannedTables.Count > 0 && GUILayout.Button("▼", GUILayout.Width(25)))
        {
            // 可以添加下拉菜单逻辑
        }
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

    #region 核心功能 - 扫描所有数据表【新增】
    /// <summary>
    /// 扫描结果数据类
    /// </summary>
    private class TableScanResult
    {
        public string TableName { get; set; }
        public string DataTypeName { get; set; }
        public string DataTypeShortName { get; set; }
        public bool UseBinary { get; set; }
        public bool LoadSuccess { get; set; }
        public int RowCount { get; set; }
        public string ErrorMessage { get; set; }
        public long LoadTimeMs { get; set; }
        public Type DataType { get; set; }
    }

    /// <summary>
    /// 扫描所有带有[EditableData]属性的数据表类
    /// </summary>
    private void ScanAllDataTables()
    {
        _isScanning = true;
        _scannedTables.Clear();
        Repaint();

        try
        {
            // 获取EditableData属性类型
            Type editableDataType = Type.GetType("DataCenter.EditableDataAttribute, Assembly-CSharp");
            
            if (editableDataType == null)
            {
                // 尝试在其他程序集中查找
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    editableDataType = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "EditableDataAttribute");
                    if (editableDataType != null) break;
                }
            }

            if (editableDataType == null)
            {
                Debug.LogError("[DataTableChecker] 未找到 EditableDataAttribute 类型");
                _isScanning = false;
                Repaint();
                return;
            }

            // 扫描所有带有该属性的类
            var dataTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.GetCustomAttribute(editableDataType) != null)
                        .Where(t => !t.IsAbstract && !t.IsInterface);
                    dataTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 跳过无法加载的程序集
                    Debug.LogWarning($"[DataTableChecker] 程序集 {assembly.GetName().Name} 加载部分类型失败");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DataTableChecker] 扫描程序集 {assembly.GetName().Name} 时出错: {ex.Message}");
                }
            }

            Debug.Log($"[DataTableChecker] 找到 {dataTypes.Count} 个数据表类型");

            // 遍历每个数据表类型并尝试加载
            int currentIndex = 0;
            foreach (var dataType in dataTypes)
            {
                currentIndex++;
                EditorUtility.DisplayProgressBar("扫描数据表", 
                    $"正在检查: {dataType.Name} ({currentIndex}/{dataTypes.Count})", 
                    (float)currentIndex / dataTypes.Count);

                var result = ScanSingleTable(dataType);
                _scannedTables.Add(result);
            }

            // 按名称排序
            _scannedTables.Sort((a, b) => string.Compare(a.TableName, b.TableName, StringComparison.Ordinal));

            Debug.Log($"[DataTableChecker] 扫描完成，成功: {_scannedTables.Count(t => t.LoadSuccess)}, 失败: {_scannedTables.Count(t => !t.LoadSuccess)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataTableChecker] 扫描过程出错: {ex.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            _isScanning = false;
            Repaint();
        }
    }

    /// <summary>
    /// 扫描单个数据表
    /// </summary>
    private TableScanResult ScanSingleTable(Type dataType)
    {
        var result = new TableScanResult
        {
            DataType = dataType,
            DataTypeName = dataType.FullName,
            DataTypeShortName = dataType.Name
        };

        try
        {
            // 从属性获取表名
            Type editableDataType = Type.GetType("DataCenter.EditableDataAttribute, Assembly-CSharp");
            if (editableDataType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    editableDataType = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "EditableDataAttribute");
                    if (editableDataType != null) break;
                }
            }

            var attr = dataType.GetCustomAttribute(editableDataType);
            if (attr != null)
            {
                // 尝试获取TableName属性
                var tableNameProp = editableDataType.GetProperty("TableName");
                if (tableNameProp != null)
                {
                    result.TableName = tableNameProp.GetValue(attr)?.ToString() ?? dataType.Name;
                }
                else
                {
                    result.TableName = dataType.Name;
                }

                // 尝试获取UseBinary属性
                var useBinaryProp = editableDataType.GetProperty("UseBinary");
                if (useBinaryProp != null)
                {
                    result.UseBinary = (bool)(useBinaryProp.GetValue(attr) ?? false);
                }
            }
            else
            {
                result.TableName = dataType.Name;
            }

            // 调用DataTableManager加载数据
            var startTime = EditorApplication.timeSinceStartup;
            
            MethodInfo getTableMethod = typeof(DataTableManager)
                .GetMethod("GetTable", new[] { typeof(string), typeof(bool) });
            
            if (getTableMethod != null)
            {
                var genericMethod = getTableMethod.MakeGenericMethod(dataType);
                object loadedData = genericMethod.Invoke(null, new object[] { result.TableName, result.UseBinary });
                
                var endTime = EditorApplication.timeSinceStartup;
                result.LoadTimeMs = (long)((endTime - startTime) * 1000);
                
                if (loadedData is IList list)
                {
                    result.RowCount = list.Count;
                    result.LoadSuccess = true;
                }
                else
                {
                    result.RowCount = 0;
                    result.LoadSuccess = loadedData != null;
                }
            }
            else
            {
                result.LoadSuccess = false;
                result.ErrorMessage = "未找到 GetTable 方法";
            }
        }
        catch (TargetInvocationException ex)
        {
            result.LoadSuccess = false;
            result.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            result.LoadTimeMs = (long)((EditorApplication.timeSinceStartup) * 1000);
        }
        catch (Exception ex)
        {
            result.LoadSuccess = false;
            result.ErrorMessage = ex.Message;
            result.LoadTimeMs = (long)((EditorApplication.timeSinceStartup) * 1000);
        }

        return result;
    }

    /// <summary>
    /// 重试失败的数据表
    /// </summary>
    private void RetryFailedTables()
    {
        var failedTables = _scannedTables.Where(t => !t.LoadSuccess).ToList();
        if (failedTables.Count == 0) return;

        int successCount = 0;
        foreach (var table in failedTables)
        {
            var result = ScanSingleTable(table.DataType);
            var index = _scannedTables.IndexOf(table);
            if (index >= 0)
            {
                _scannedTables[index] = result;
            }
            if (result.LoadSuccess) successCount++;
        }

        EditorUtility.DisplayDialog("重试完成", 
            $"重试 {failedTables.Count} 个失败的数据表\n成功: {successCount}\n仍失败: {failedTables.Count - successCount}", 
            "确定");
    }

    /// <summary>
    /// 导出扫描报告
    /// </summary>
    private void ExportScanReport()
    {
        string path = EditorUtility.SaveFilePanel("导出扫描报告", "", "DataTableScanReport", "txt");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== 数据表扫描报告 =====");
            sb.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"总计: {_scannedTables.Count} 个数据表");
            sb.AppendLine($"成功: {_scannedTables.Count(t => t.LoadSuccess)} 个");
            sb.AppendLine($"失败: {_scannedTables.Count(t => !t.LoadSuccess)} 个");
            sb.AppendLine();
            sb.AppendLine("===== 详细列表 =====");
            
            foreach (var table in _scannedTables)
            {
                sb.AppendLine();
                sb.AppendLine($"表名: {table.TableName}");
                sb.AppendLine($"类型: {table.DataTypeName}");
                sb.AppendLine($"状态: {(table.LoadSuccess ? "✓ 成功" : "✗ 失败")}");
                sb.AppendLine($"行数: {table.RowCount}");
                sb.AppendLine($"格式: {(table.UseBinary ? "二进制" : "文本")}");
                sb.AppendLine($"耗时: {table.LoadTimeMs}ms");
                if (!string.IsNullOrEmpty(table.ErrorMessage))
                {
                    sb.AppendLine($"错误: {table.ErrorMessage}");
                }
            }

            File.WriteAllText(path, sb.ToString());
            EditorUtility.DisplayDialog("导出完成", $"报告已保存到:\n{path}", "确定");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("导出失败", $"导出报告时出错:\n{ex.Message}", "确定");
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

            // 尝试从扫描结果中获取数据类型
            Type dataType = null;
            var scannedTable = _scannedTables.FirstOrDefault(t => t.TableName == _selectedTableName);
            if (scannedTable != null && scannedTable.DataType != null)
            {
                dataType = scannedTable.DataType;
            }

            // 调用DataTableManager加载数据
            MethodInfo getTableMethod = typeof(DataTableManager)
                .GetMethod("GetTable", new[] { typeof(string), typeof(bool) });
            
            if (getTableMethod != null && dataType != null)
            {
                var genericMethod = getTableMethod.MakeGenericMethod(dataType);
                object result = genericMethod.Invoke(null, new object[] { _selectedTableName, _useBinary });
                _loadedData = result;
            }
            else
            {
                // 兜底：使用object类型
                var genericMethod = getTableMethod.MakeGenericMethod(typeof(object));
                object result = genericMethod.Invoke(null, new object[] { _selectedTableName, _useBinary });
                _loadedData = result;
            }
            
            // 处理结果
            if (_loadedData is IList list)
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