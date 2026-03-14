// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;

// namespace SOEditor.Editor
// {
//     /// <summary>
//     /// ScriptableObject 编辑器窗口
//     /// </summary>
//     public class SOEditorWindow : EditorWindow
//     {
//         // 编辑器状态
//         private enum EditorState
//         {
//             List,   // 列表视图
//             Edit,   // 编辑视图
//             Create  // 创建视图
//         }
        
//         private EditorState _currentState = EditorState.List;
//         private string _selectedSOType = "Item";
//         private ScriptableObjectBase _selectedSO;
//         private Vector2 _listScrollPosition;
//         private Vector2 _editScrollPosition;
        
//         // 搜索和过滤
//         private string _searchTerm = "";
//         private bool _showFilters = false;
        
//         // 可用的 SO 类型
//         private readonly string[] _soTypes = new[] { "Item", "Character", "Skill", "Quest","BuildingData" };
//         private readonly Dictionary<string, Type> _soTypeMap = new Dictionary<string, Type>
//         {
//             { "Item", typeof(ItemSO) },
//             { "Character", typeof(CharacterSO) },
//             { "Skill", typeof(SkillSO) },
//             { "Quest", typeof(QuestSO) },
//             { "BuildingData", typeof(Top.BuildingDataSO) },
//         };
        
//         // 缓存的 SO 列表
//         private Dictionary<string, List<ScriptableObjectBase>> _soCache = new Dictionary<string, List<ScriptableObjectBase>>();
//         private bool _cacheDirty = true;
        
//         // 临时编辑数据
//         private SerializedObject _serializedObject;
//         private string _newSOName = "";
        
//         // GUI 样式
//         private GUIStyle _headerStyle;
//         private GUIStyle _subHeaderStyle;
//         private GUIStyle _boxStyle;
//         private GUIStyle _buttonStyle;
//         private GUIStyle _selectedButtonStyle;
        
//         [MenuItem("Tools/SO Tool/SO Editor")]
//         public static void ShowWindow()
//         {
//             SOEditorWindow window = GetWindow<SOEditorWindow>("SO Editor");
//             window.minSize = new Vector2(800, 600);
//             window.Show();
//         }
        
//         private void OnEnable()
//         {
//             RefreshCache();
//         }
        
//         private void OnFocus()
//         {
//             RefreshCache();
//         }
        
//         private void InitializeStyles()
//         {
//             _headerStyle = new GUIStyle()
//             {
//                 fontSize = 18,
//                 fontStyle = FontStyle.Bold,
//                 normal = { textColor = Color.white }
//             };
            
//             _subHeaderStyle = new GUIStyle()
//             {
//                 fontSize = 14,
//                 fontStyle = FontStyle.Bold,
//                 normal = { textColor = Color.white }
//             };
            
//             _boxStyle = new GUIStyle(GUI.skin.box)
//             {
//                 padding = new RectOffset(10, 10, 10, 10)
//             };
            
//             _buttonStyle = new GUIStyle(GUI.skin.button)
//             {
//                 fontSize = 12,
//                 padding = new RectOffset(10, 10, 5, 5)
//             };
            
//             _selectedButtonStyle = new GUIStyle(GUI.skin.button)
//             {
//                 fontSize = 12,
//                 padding = new RectOffset(10, 10, 5, 5),
//                 normal = { background = MakeTexture(2, 2, new Color(0.2f, 0.4f, 0.8f)) }
//             };
//         }
        
//         private Texture2D MakeTexture(int width, int height, Color color)
//         {
//             Color[] pixels = new Color[width * height];
//             for (int i = 0; i < pixels.Length; i++)
//             {
//                 pixels[i] = color;
//             }
//             Texture2D texture = new Texture2D(width, height);
//             texture.SetPixels(pixels);
//             texture.Apply();
//             return texture;
//         }
        
//         private void OnGUI()
//         {
//             if (_headerStyle == null)
//             {
//                 InitializeStyles();
//             }

//             DrawHeader();
            
//             switch (_currentState)
//             {
//                 case EditorState.List:
//                     DrawListView();
//                     break;
//                 case EditorState.Edit:
//                     DrawEditView();
//                     break;
//                 case EditorState.Create:
//                     DrawCreateView();
//                     break;
//             }
//         }
        
//         #region Header
        
//         private void DrawHeader()
//         {
//             EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
//             GUILayout.Label("SO Editor", _headerStyle, GUILayout.Width(100));
//             GUILayout.FlexibleSpace();
            
//             // 数据库操作按钮
//             if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
//             {
//                 RefreshCache();
//             }
            
//             if (GUILayout.Button("导出", EditorStyles.toolbarButton, GUILayout.Width(60)))
//             {
//                 ExportDatabase();
//             }
            
//             if (GUILayout.Button("导入", EditorStyles.toolbarButton, GUILayout.Width(60)))
//             {
//                 ImportDatabase();
//             }
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.Space(5);
//         }
        
//         #endregion
        
//         #region List View
        
//         private void DrawListView()
//         {
//             EditorGUILayout.BeginHorizontal();
            
//             // 左侧类型选择
//             DrawTypeSidebar();
            
//             // 右侧列表
//             DrawSOList();
            
//             EditorGUILayout.EndHorizontal();
//         }
        
//         private void DrawTypeSidebar()
//         {
//             EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(120), GUILayout.ExpandHeight(true));
            
//             GUILayout.Label("类型", _subHeaderStyle);
//             EditorGUILayout.Space(5);
            
//             foreach (string soType in _soTypes)
//             {
//                 bool isSelected = _selectedSOType == soType;
//                 GUIStyle style = isSelected ? _selectedButtonStyle : _buttonStyle;
                
//                 if (GUILayout.Button(soType, style, GUILayout.Height(30)))
//                 {
//                     _selectedSOType = soType;
//                     _cacheDirty = true;
//                 }
//             }
            
//             EditorGUILayout.Space(10);
            
//             if (GUILayout.Button("+ 新建", _buttonStyle, GUILayout.Height(35)))
//             {
//                 _currentState = EditorState.Create;
//                 _newSOName = "";
//             }
            
//             EditorGUILayout.EndVertical();
//         }
        
//         private void DrawSOList()
//         {
//             EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            
//             // 搜索栏
//             EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
//             GUILayout.Label("搜索:", GUILayout.Width(40));
//             _searchTerm = EditorGUILayout.TextField(_searchTerm, GUILayout.Width(200));
            
//             if (GUILayout.Button("清除", EditorStyles.toolbarButton, GUILayout.Width(50)))
//             {
//                 _searchTerm = "";
//             }
            
//             GUILayout.FlexibleSpace();
            
//             // 显示数量
//             var list = GetFilteredList();
//             GUILayout.Label($"共 {list.Count} 个", EditorStyles.label);
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.Space(5);
            
//             // 列表内容
//             _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);
            
//             if (_cacheDirty)
//             {
//                 RefreshCache();
//             }
            
//             foreach (var so in list)
//             {
//                 DrawSOListItem(so);
//             }
            
//             EditorGUILayout.EndScrollView();
            
//             EditorGUILayout.EndVertical();
//         }
        
//         private void DrawSOListItem(ScriptableObjectBase so)
//         {
//             EditorGUILayout.BeginHorizontal(GUI.skin.box);
            
//             // 图标
//             if (so.Icon != null)
//             {
//                 GUILayout.Label(so.Icon.texture, GUILayout.Width(40), GUILayout.Height(40));
//             }
//             else
//             {
//                 GUILayout.Box("", GUILayout.Width(40), GUILayout.Height(40));
//             }
            
//             EditorGUILayout.BeginVertical();
            
//             // 名称和 ID
//             GUILayout.Label($"<b>{so.DisplayName}</b>", new GUIStyle { richText = true, fontSize = 14 });
//             GUILayout.Label($"ID: {so.Id}", EditorStyles.miniLabel);
            
//             // 描述（截断）
//             string desc = so.Description;
//             if (desc.Length > 50)
//             {
//                 desc = desc.Substring(0, 50) + "...";
//             }
//             GUILayout.Label(desc, EditorStyles.miniLabel);
            
//             EditorGUILayout.EndVertical();
            
//             GUILayout.FlexibleSpace();
            
//             // 操作按钮
//             EditorGUILayout.BeginVertical(GUILayout.Width(80));
            
//             if (GUILayout.Button("编辑", GUILayout.Height(25)))
//             {
//                 SelectSO(so);
//             }
            
//             GUI.backgroundColor = Color.red;
//             if (GUILayout.Button("删除", GUILayout.Height(20)))
//             {
//                 if (EditorUtility.DisplayDialog("确认删除", $"确定要删除 '{so.DisplayName}' 吗？", "删除", "取消"))
//                 {
//                     DeleteSO(so);
//                 }
//             }
//             GUI.backgroundColor = Color.white;
            
//             EditorGUILayout.EndVertical();
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.Space(2);
//         }
        
//         private List<ScriptableObjectBase> GetFilteredList()
//         {
//             var list = GetSOList(_selectedSOType);
            
//             if (!string.IsNullOrEmpty(_searchTerm))
//             {
//                 string term = _searchTerm.ToLower();
//                 list = list.Where(so => 
//                     so.DisplayName.ToLower().Contains(term) ||
//                     so.Description.ToLower().Contains(term) ||
//                     so.Id.ToLower().Contains(term)
//                 ).ToList();
//             }
            
//             return list;
//         }
        
//         #endregion
        
//         #region Edit View
        
//         private void DrawEditView()
//         {
//             if (_selectedSO == null)
//             {
//                 _currentState = EditorState.List;
//                 return;
//             }
            
//             EditorGUILayout.BeginHorizontal();
            
//             // 返回按钮
//             if (GUILayout.Button("← 返回列表", GUILayout.Width(100)))
//             {
//                 EditorGUILayout.EndHorizontal();// 结束当前水平布局,防止报错
//                 _currentState = EditorState.List;
//                 _selectedSO = null;
//                 return;
//             }
            
//             GUILayout.FlexibleSpace();
            
//             // 保存按钮
//             GUI.backgroundColor = Color.green;
//             if (GUILayout.Button("保存", GUILayout.Width(80), GUILayout.Height(30)))
//             {
//                 SaveSO();
//             }
//             GUI.backgroundColor = Color.white;
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.Space(10);
            
//             // 编辑区域
//             _editScrollPosition = EditorGUILayout.BeginScrollView(_editScrollPosition);
            
//             EditorGUILayout.LabelField("编辑 ScriptableObject", _headerStyle);
//             EditorGUILayout.Space(10);
            
//             // 基础信息
//             EditorGUILayout.BeginVertical(_boxStyle);
//             EditorGUILayout.LabelField("基础信息", _subHeaderStyle);
//             EditorGUILayout.Space(5);
            
//             EditorGUILayout.LabelField($"ID: {_selectedSO.Id}");
//             EditorGUILayout.LabelField($"类型: {_selectedSO.GetSOType()}");
            
//             EditorGUILayout.Space(5);
            
//             // 使用 SerializedObject 进行编辑
//             if (_serializedObject == null || _serializedObject.targetObject != _selectedSO)
//             {
//                 _serializedObject = new SerializedObject(_selectedSO);
//             }
            
//             _serializedObject.Update();
            
//             // 绘制所有属性
//             SerializedProperty property = _serializedObject.GetIterator();
//             property.NextVisible(true); // 跳过脚本引用
            
//             while (property.NextVisible(false))
//             {
//                 EditorGUILayout.PropertyField(property, true);
//             }
            
//             _serializedObject.ApplyModifiedProperties();
            
//             EditorGUILayout.EndVertical();
            
//             EditorGUILayout.Space(20);
            
//             // 操作按钮区域
//             EditorGUILayout.BeginHorizontal();
            
//             if (GUILayout.Button("复制", GUILayout.Height(30)))
//             {
//                 DuplicateSO();
//             }
            
//             GUI.backgroundColor = Color.red;
//             if (GUILayout.Button("删除", GUILayout.Height(30)))
//             {
//                 if (EditorUtility.DisplayDialog("确认删除", $"确定要删除 '{_selectedSO.DisplayName}' 吗？", "删除", "取消"))
//                 {
//                     DeleteSO(_selectedSO);
//                     _currentState = EditorState.List;
//                 }
//             }
//             GUI.backgroundColor = Color.white;
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.EndScrollView();
//         }
        
//         #endregion
        
//         #region Create View
        
//         private void DrawCreateView()
//         {
//             EditorGUILayout.BeginHorizontal();
            
//             if (GUILayout.Button("← 返回", GUILayout.Width(80)))
//             {
//                 _currentState = EditorState.List;
//                 return;
//             }
            
//             EditorGUILayout.EndHorizontal();
            
//             EditorGUILayout.Space(20);
            
//             EditorGUILayout.LabelField("创建新的 ScriptableObject", _headerStyle);
//             EditorGUILayout.Space(20);
            
//             EditorGUILayout.BeginVertical(_boxStyle);
            
//             // 选择类型
//             EditorGUILayout.LabelField("选择类型:", _subHeaderStyle);
//             _selectedSOType = _soTypes[EditorGUILayout.Popup(
//                 Array.IndexOf(_soTypes, _selectedSOType),
//                 _soTypes
//             )];
            
//             EditorGUILayout.Space(10);
            
//             // 输入名称
//             EditorGUILayout.LabelField("名称:", _subHeaderStyle);
//             _newSOName = EditorGUILayout.TextField(_newSOName);
            
//             EditorGUILayout.Space(20);
            
//             // 创建按钮
//             GUI.enabled = !string.IsNullOrEmpty(_newSOName);
            
//             GUI.backgroundColor = Color.green;
//             if (GUILayout.Button("创建", GUILayout.Height(40)))
//             {
//                 CreateNewSO();
//             }
//             GUI.backgroundColor = Color.white;
            
//             GUI.enabled = true;
            
//             EditorGUILayout.EndVertical();
//         }
        
//         #endregion
        
//         #region 操作方法
        
//         private void SelectSO(ScriptableObjectBase so)
//         {
//             _selectedSO = so;
//             _serializedObject = new SerializedObject(so);
//             _currentState = EditorState.Edit;
//         }
        
//         private void CreateNewSO()
//         {
//             Type soType = _soTypeMap[_selectedSOType];
//             ScriptableObjectBase newSO = ScriptableObject.CreateInstance(soType) as ScriptableObjectBase;
            
//             if (newSO != null)
//             {
//                 newSO.GenerateId();
//                 newSO.SetDisplayName(_newSOName);
//                 newSO.SetDescription("");
                
//                 // 保存到数据库
//                 if (SODatabase.SaveScriptableObject(newSO))
//                 {
//                     EditorUtility.DisplayDialog("成功", $"'{_newSOName}' 创建成功！", "确定");
//                     _cacheDirty = true;
//                     _currentState = EditorState.List;
//                 }
//                 else
//                 {
//                     EditorUtility.DisplayDialog("错误", "创建失败，请查看控制台日志。", "确定");
//                 }
//             }
//         }
        
//         private void SaveSO()
//         {
//             if (_selectedSO != null)
//             {
//                 if (SODatabase.SaveScriptableObject(_selectedSO))
//                 {
//                     EditorUtility.DisplayDialog("成功", "保存成功！", "确定");
//                     _cacheDirty = true;
//                 }
//                 else
//                 {
//                     EditorUtility.DisplayDialog("错误", "保存失败，请查看控制台日志。", "确定");
//                 }
//             }
//         }
        
//         private void DeleteSO(ScriptableObjectBase so)
//         {
//             if (SODatabase.DeleteScriptableObject(so.GetSOType(), so.Id))
//             {
//                 _cacheDirty = true;
//                 RefreshCache();
//             }
//         }
        
//         private void DuplicateSO()
//         {
//             if (_selectedSO == null) return;
            
//             Type soType = _soTypeMap[_selectedSO.GetSOType()];
//             ScriptableObjectBase newSO = ScriptableObject.CreateInstance(soType) as ScriptableObjectBase;
            
//             if (newSO != null)
//             {
//                 // 复制数据
//                 object data = _selectedSO.GetSerializableData();
//                 newSO.LoadFromSerializableData(data);
//                 newSO.GenerateId();
//                 newSO.SetDisplayName($"{_selectedSO.DisplayName} (复制)");
                
//                 if (SODatabase.SaveScriptableObject(newSO))
//                 {
//                     EditorUtility.DisplayDialog("成功", "复制成功！", "确定");
//                     _cacheDirty = true;
//                     _currentState = EditorState.List;
//                 }
//             }
//         }
        
//         private void ExportDatabase()
//         {
//             string path = EditorUtility.SaveFolderPanel("导出数据库", "", "SO_Export");
//             if (!string.IsNullOrEmpty(path))
//             {
//                 if (SODatabase.ExportDatabase(path))
//                 {
//                     EditorUtility.DisplayDialog("成功", $"数据库已导出到:\n{path}", "确定");
//                 }
//                 else
//                 {
//                     EditorUtility.DisplayDialog("错误", "导出失败", "确定");
//                 }
//             }
//         }
        
//         private void ImportDatabase()
//         {
//             if (EditorUtility.DisplayDialog("确认", "导入将覆盖现有数据库，是否继续？", "继续", "取消"))
//             {
//                 string path = EditorUtility.OpenFolderPanel("导入数据库", "", "");
//                 if (!string.IsNullOrEmpty(path))
//                 {
//                     if (SODatabase.ImportDatabase(path))
//                     {
//                         EditorUtility.DisplayDialog("成功", "数据库导入成功！", "确定");
//                         _cacheDirty = true;
//                         RefreshCache();
//                     }
//                     else
//                     {
//                         EditorUtility.DisplayDialog("错误", "导入失败", "确定");
//                     }
//                 }
//             }
//         }
        
//         #endregion
        
//         #region 缓存管理
        
//         private void RefreshCache()
//         {
//             _soCache.Clear();
            
//             foreach (string soType in _soTypes)
//             {
//                 _soCache[soType] = LoadSOListFromDatabase(soType);
//             }
            
//             _cacheDirty = false;
//         }
        
//         private List<ScriptableObjectBase> GetSOList(string soType)
//         {
//             if (!_soCache.ContainsKey(soType))
//             {
//                 _soCache[soType] = LoadSOListFromDatabase(soType);
//             }
//             return _soCache[soType];
//         }
        
//         private List<ScriptableObjectBase> LoadSOListFromDatabase(string soType)
//         {
//             List<ScriptableObjectBase> list = new List<ScriptableObjectBase>();
            
//             switch (soType)
//             {
//                 case "Item":
//                     var itemDataList = SODatabase.LoadAllData<ItemData>("Item");
//                     foreach (var data in itemDataList)
//                     {
//                         ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
//                         item.LoadFromSerializableData(data);
//                         list.Add(item);
//                     }
//                     break;
                    
//                 case "Character":
//                     var charDataList = SODatabase.LoadAllData<CharacterData>("Character");
//                     foreach (var data in charDataList)
//                     {
//                         CharacterSO character = ScriptableObject.CreateInstance<CharacterSO>();
//                         character.LoadFromSerializableData(data);
//                         list.Add(character);
//                     }
//                     break;
                    
//                 case "Skill":
//                     var skillDataList = SODatabase.LoadAllData<SkillData>("Skill");
//                     foreach (var data in skillDataList)
//                     {
//                         SkillSO skill = ScriptableObject.CreateInstance<SkillSO>();
//                         skill.LoadFromSerializableData(data);
//                         list.Add(skill);
//                     }
//                     break;
                    
//                 case "Quest":
//                     var questDataList = SODatabase.LoadAllData<QuestData>("Quest");
//                     foreach (var data in questDataList)
//                     {
//                         QuestSO quest = ScriptableObject.CreateInstance<QuestSO>();
//                         quest.LoadFromSerializableData(data);
//                         list.Add(quest);
//                     }
//                     break;
//                 case "BuildingData":
//                     var buildingDataList = SODatabase.LoadAllData<Top.BuildingDataSO>(soType);
//                     foreach (var data in buildingDataList)
//                     {
//                         Top.BuildingDataSO buildingData = ScriptableObject.CreateInstance<Top.BuildingDataSO>();
//                         buildingData.LoadFromSerializableData(data);
//                         list.Add(buildingData);
//                     }
//                     break;
//             }
            
//             return list;
//         }
        
//         #endregion
//     }
// }
