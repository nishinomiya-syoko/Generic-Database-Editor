// using System.Collections.Generic;
// using UnityEngine;
// using UnityEditor;
// using UnityEditor.IMGUI.Controls;
// using System.Linq;
// using System.IO;


// public class EntityEditorWindow : EditorWindow
// {
//     public class SimpleTreeView : TreeView
//     {
//         public SimpleTreeView(TreeViewState treeViewState) : base(treeViewState)
//         {
//             if (EntityEditorWindow.itemsCollection != null && EntityEditorWindow.itemsCollection.list.Count > 0)
//                 Reload();
//         }


//         protected override TreeViewItem BuildRoot()
//         {
//             var mainRoot = new TreeViewItem { id = 0, depth = -1, displayName = "MainRoot" };
//             // 确保 children 不为 null（即使为空列表）
//             mainRoot.children = new List<TreeViewItem>();

//             if (EntityEditorWindow.itemsCollection != null && EntityEditorWindow.itemsCollection.list.Count > 0)
//             {
//                 var list = EntityEditorWindow.itemsCollection.list;
//                 for (int i = 0; i < list.Count; i++)
//                 {
//                     ItemData item = list[i];

//                     // 筛选逻辑：End 表示不过滤
//                     if (EntityEditorWindow.instance.filterType != EnumEntityType.End &&
//                         item.entityType != EntityEditorWindow.instance.filterType)
//                         continue;

//                     // 搜索逻辑
//                     if (!string.IsNullOrEmpty(EntityEditorWindow.instance.searchText) &&
//                         !item.name.ToLower().Contains(EntityEditorWindow.instance.searchText.ToLower()))
//                         continue;

//                     // 使用原始列表索引作为 id（+1 避开 root 的 0）
//                     var treeItem = new TreeViewItem { id = i + 1, displayName = item.name };
//                     mainRoot.AddChild(treeItem);
//                 }
//             }

//             SetupDepthsFromParentsAndChildren(mainRoot);
//             return mainRoot;
//         }


//         protected override void SelectionChanged(IList<int> selectedIds)
//         {
//             base.SelectionChanged(selectedIds);

//             if (selectedIds.Count > 0)
//             {
//                 int selectedId = selectedIds[0];
//                 EntityEditorWindow.instance.SelectItem(selectedId - 1);
//             }
//         }

//         protected override void RowGUI(RowGUIArgs args)
//         {
//             Rect position = args.rowRect;
//             position.x += GetContentIndent(args.item);
//             GUI.Label(position, args.item.displayName, EditorStyles.boldLabel);
//             GUI.color = Color.white;
//         }
//     }

//     public static ItemsCollectionSO itemsCollection;

//     const float WidthOfLetPanel = 200;
//     const float WidthOfRightPanel = 260;

//     Vector2 leftPanelScrollPos;
//     Vector2 rightPanelScrollPos;

//     [SerializeField] TreeViewState _treeViewState;
//     SimpleTreeView _simpleTreeView;

//     [MenuItem("Window/Entity Editor Window")]
//     static void Init()
//     {
//         EnsureItemsCollection();
//         EntityEditorWindow window = (EntityEditorWindow)GetWindow(typeof(EntityEditorWindow));
//         window.minSize = new Vector2(800, 500);
//         window.Show();
//     }

//     void Update() { }

//     public static EntityEditorWindow instance => GetWindow<EntityEditorWindow>();

//     static void EnsureItemsCollection()
//     {
//         if (itemsCollection != null)
//             return;
//         if (!Directory.Exists(Constant.ENTITY_PATH))
//             Directory.CreateDirectory(Constant.ENTITY_PATH);

//         var p = AssetLoader.LoadAllAssetsInFolder(Constant.ENTITY_PATH);
//         itemsCollection = p.FirstOrDefault(x => x.GetType() == typeof(ItemsCollectionSO)) as ItemsCollectionSO;
//         if (itemsCollection == null)
//         {
//             ItemsCollectionSO asset = ScriptableObject.CreateInstance<ItemsCollectionSO>();
//             asset.name = "Items Collection";
//             string path = $"{Constant.ENTITY_PATH}{asset.name}.asset";
//             AssetDatabase.CreateAsset(asset, path);
//             AssetDatabase.SaveAssets();
//             AssetDatabase.Refresh();
//             EditorUtility.FocusProjectWindow();
//             Selection.activeObject = asset;
//         }
//     }

//     void OnEnable()
//     {
//         EnsureItemsCollection();
//         if (_treeViewState == null)
//             _treeViewState = new TreeViewState();
//         _simpleTreeView = new SimpleTreeView(_treeViewState);
//     }

//     void OnFocus() => OnEnable();

//     void OnGUI()
//     {
//         // DrawMatchPanel();
//         // GUILayout.Space(5);

//         GUILayout.BeginHorizontal();
//         RenderLeftPanel();
//         RenderRightPanel();
//         RenderPreviewModel();
//         GUILayout.EndHorizontal();
//     }

//     // 搜索文字
//     public string searchText;
//     // ✅ 新增枚举筛选字段
//     public EnumEntityType filterType = EnumEntityType.End;
//     void DrawMatchPanel()
//     {
//         GUILayout.BeginHorizontal();
//         GUILayout.Label("筛选", GUILayout.Width(100));
//         filterType = (EnumEntityType)GUILayout.Toolbar((int)filterType, System.Enum.GetNames(typeof(EnumEntityType)),GUILayout.Width(500));
//         GUILayout.EndHorizontal();
//     }

//     void RenderLeftPanel()
//     {
//         GUI.Box(new Rect(0, 0, WidthOfLetPanel, position.height), "", EditorStyles.helpBox);
//         leftPanelScrollPos = EditorGUILayout.BeginScrollView(leftPanelScrollPos, GUILayout.Width(WidthOfLetPanel), GUILayout.Height(position.height));

//         // ✅ 增加筛选下拉框
//         GUILayout.BeginHorizontal();
//         GUILayout.Label("Filter:", GUILayout.Width(45));
//         EnumEntityType oldFilter = filterType;
//         filterType = (EnumEntityType)EditorGUILayout.EnumPopup(filterType, GUILayout.Width(130));
//         GUILayout.EndHorizontal();
//         if (oldFilter != filterType)
//         {
//             _simpleTreeView.Reload();
//         }

//         GUILayout.Space(5);

//         GUILayout.BeginHorizontal();        
//         // 工具栏
//         // GUI.Box(new Rect(0, 35, WidthOfLetPanel - 1, 17), "", "toolbar");
//         // if (GUI.Button(new Rect(4, 35, 45, 14), "Create", "toolbarbutton"))
//         if (GUILayout.Button("Create", EditorStyles.toolbarButton))
//             TreeViewMenu(new Rect(4, 35, 45, 14), -1);

//         // GUI.BeginGroup(new Rect(76, 37, WidthOfLetPanel - 2, 14));
//         // searchText = GUI.TextField(new Rect(-16, 0, WidthOfLetPanel - 78, 14), searchText, "toolbarsearchTextField");
//         searchText = GUILayout.TextField(searchText, "toolbarsearchTextField");
//         // GUI.EndGroup();

//         if (GUILayout.Button( "", "ToolbarsearchCancelButton"))
//         {
//             searchText = "";
//             _simpleTreeView.Reload();
//             Repaint();
//         }
//         GUILayout.EndHorizontal();

//         if (itemsCollection != null && itemsCollection.list.Count > 0)
//             TreeView(new Rect(0, 55, WidthOfLetPanel - 2, position.height - 55));

//         EditorGUILayout.EndScrollView();
//     }

//     void TreeView(Rect position)
//     {
//         if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
//         {
//             _simpleTreeView.SetSelection(new List<int>());
//             selectedItemIndex = -1;
//         }

//         _simpleTreeView.OnGUI(position);

//         if (Event.current.button == 1 && Event.current.type == EventType.MouseUp && position.Contains(Event.current.mousePosition))
//         {
//             IList<int> selections = _simpleTreeView.GetSelection();
//             int selectedId = selections.Count > 0 ? selections[0] : -1;
//             TreeViewMenu(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - 10, 0, 0), selectedId);
//         }
//     }

//     private int selectedItemIndex = -1;
//     public void SelectItem(int index)
//     {
//         selectedItemIndex = index;
//         if (itemsCollection != null && itemsCollection.list.Count > 0 && index <= itemsCollection.list.Count - 1)
//             LoadDataValues();
//         DestroyImmediate(modelview);
//     }

//     private void LoadDataValues()
//     {
//         if (selectedItemIndex == -1)
//             return;

//         ItemData itemData = itemsCollection.list[selectedItemIndex];

//         id = itemData.id.ToString();
//         _name = itemData.name;
//         thumb = itemData.thumb;
//         modelPath = itemData.modelPath;
//         model = itemData.model;

//         gridSize = itemData.gridSize;
//         entityType = itemData.entityType;
//         buildingFunction = itemData.buildingFunction;
//     }

//     void TreeViewMenu(Rect position, int selectedId)
//     {
//         GenericMenu toolsMenu = new GenericMenu();

//         if (selectedId == -1)
//         {
//             toolsMenu.AddItem(new GUIContent("Create Item"), false, data =>
//             {
//                 TriggerMenuEvent((string)data, selectedId);
//             }, "Create Item");
//         }

//         if (selectedId > 0)
//         {
//             toolsMenu.AddItem(new GUIContent("Delete Item"), false, data =>
//             {
//                 TriggerMenuEvent((string)data, selectedId);
//             }, "Delete Item");
//         }

//         toolsMenu.DropDown(position);
//         EditorGUIUtility.ExitGUI();
//     }

//     void TriggerMenuEvent(string evt, int selectedIndex)
//     {
//         EnsureItemsCollection();

//         switch (evt)
//         {
//             case "Create Item":
//                 if (selectedIndex == 1)
//                     selectedIndex = -1;
//                 itemsCollection.AddNewItem();
//                 _simpleTreeView.Reload();
//                 _simpleTreeView.SetSelection(new List<int>() { itemsCollection.list.Count });
//                 SelectItem(itemsCollection.list.Count - 1);
//                 break;

//             case "Delete Item":
//                 itemsCollection.RemoveItem(selectedIndex);
//                 if (itemsCollection != null && itemsCollection.list.Count > 0)
//                     _simpleTreeView.Reload();
//                 break;
//         }
//     }

//     public string id;
//     public string _oldId = "";
//     public string _name;
//     private string _oldName = "";

//     private bool spritesFoldout;
//     private bool configFoldout;
//     private bool statusFoldout;

//     private string modelPath = string.Empty;
//     private string _oldModelPath = string.Empty;
//     private GameObject model = null;
//     private GameObject _oldModel = null;

//     private string[] gridSizeOptions = new string[] {
//         "None", "1x1", "2x2", "3x3", "4x4", "5x5", "6x6"
//     };
//     private int gridSize = 4;
//     private int _oldGridSize = 4;
//     private Texture2D thumb;

//     private EnumEntityType entityType;
//     private EnumBuildingFunction buildingFunction;

//     void RenderRightPanel()
//     {
//         GUILayout.BeginArea(new Rect(position.width - WidthOfRightPanel, 0, WidthOfRightPanel, position.height));
//         GUI.Box(new Rect(0, 0, WidthOfRightPanel, position.height), "", EditorStyles.helpBox);

//         if (selectedItemIndex != -1)
//         {
//             rightPanelScrollPos = EditorGUILayout.BeginScrollView(rightPanelScrollPos, GUILayout.Width(WidthOfRightPanel), GUILayout.Height(position.height));

//             GUILayout.BeginHorizontal();
//             GUILayout.Label("ID");
//             EditorGUILayout.SelectableLabel(id, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
//             GUILayout.EndHorizontal();

//             GUILayout.BeginHorizontal();
//             GUILayout.Label("Name");
//             _name = GUILayout.TextField(_name);
//             if (_name != _oldName)
//             {
//                 _oldName = _name;
//                 _simpleTreeView.Reload();
//             }
//             GUILayout.EndHorizontal();

//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

//             GUILayout.Space(5);
//             spritesFoldout = EditorGUILayout.Foldout(spritesFoldout, "Model", true);

//             if (spritesFoldout)
//             {
//                 GUILayout.Label("ModelPath");
//                 GUILayout.Space(5);
//                 modelPath = EditorGUILayout.TextField(modelPath);
//                 if (modelPath != _oldModelPath && modelPath != string.Empty)
//                 {
//                     _oldModelPath = modelPath;
//                     model = AssetDatabase.LoadAssetAtPath(modelPath, typeof(GameObject)) as GameObject;
//                 }
//                 GUILayout.Space(5);
//                 model = EditorGUILayout.ObjectField("Model", model, typeof(GameObject), false) as GameObject;
//                 if (model != null && model != _oldModel)
//                 {
//                     _oldModel = model;
//                     modelPath = AssetDatabase.GetAssetPath(model);
//                 }
//                 GUILayout.Space(5);
//                 thumb = EditorGUILayout.ObjectField("Thumb", thumb, typeof(Texture2D), false) as Texture2D;
//             }

//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

//             GUILayout.Space(5);
//             configFoldout = EditorGUILayout.Foldout(configFoldout, "CONFIG", true);
//             if (configFoldout)
//             {
//                 GUILayout.BeginHorizontal();
//                 GUILayout.Space(20);
//                 gridSize = EditorGUILayout.Popup("Grid Size", gridSize / 2, gridSizeOptions) * 2;
//                 if (gridSize != _oldGridSize)
//                 {
//                     _oldGridSize = gridSize;
//                     UpdateDataValues();
//                     LoadDataValues();
//                 }
//                 GUILayout.EndHorizontal();

//                 GUILayout.BeginHorizontal();
//                 GUILayout.Space(20);
//                 System.Enum oldType = (System.Enum)entityType;
//                 System.Enum newType = EditorGUILayout.EnumPopup("Entity Type", oldType);
//                 if (!oldType.Equals(newType))
//                 {
//                     entityType = (EnumEntityType)newType;
//                 }
//                 GUILayout.EndHorizontal();

//                 GUILayout.BeginHorizontal();
//                 GUILayout.Space(20);
//                 System.Enum oldFunc = buildingFunction;
//                 System.Enum newFunc = EditorGUILayout.EnumPopup("Building Function", oldFunc);
//                 if (!oldFunc.Equals(newFunc))
//                 {
//                     buildingFunction = (EnumBuildingFunction)newFunc;
//                 }
//                 GUILayout.EndHorizontal();
//             }

//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

//             GUILayout.Space(5);
//             statusFoldout = EditorGUILayout.Foldout(statusFoldout, "Status", true);
//             if (statusFoldout)
//             {
//                 ItemData itemData = itemsCollection.list[selectedItemIndex];
//                 foreach (var property in itemData.configuration.GetType().GetFields())
//                 {
//                     var val = property.GetValue(itemData.configuration);

//                     if (val.GetType() == typeof(System.Int32))
//                     {
//                         var temp = EditorGUILayout.IntField(property.Name, (int)val);
//                         property.SetValue(itemData.configuration, temp);
//                     }
//                     else if (val.GetType() == typeof(System.Single))
//                     {
//                         var temp = EditorGUILayout.FloatField(property.Name, (float)val);
//                         property.SetValue(itemData.configuration, temp);
//                     }
//                     else if (val.GetType() == typeof(System.Boolean))
//                     {
//                         bool temp = EditorGUILayout.Toggle(property.Name, (bool)val);
//                         property.SetValue(itemData.configuration, temp);
//                     }
//                     else if (val.GetType() == typeof(System.String))
//                     {
//                         string temp = EditorGUILayout.TextField(property.Name, (string)val);
//                         property.SetValue(itemData.configuration, temp);
//                     }
//                     else if (val.GetType() == typeof(System.Enum))
//                     {
//                         var currentEnum = EditorGUILayout.EnumPopup(property.Name, (System.Enum)val);
//                         property.SetValue(itemData.configuration, currentEnum);
//                     }
//                 }
//             }

//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
//             EditorGUILayout.EndScrollView();

//             if (GUI.changed)
//                 UpdateDataValues();
//         }
//         GUILayout.EndArea();
//     }

//     private void UpdateDataValues()
//     {
//         if (selectedItemIndex == -1)
//             return;

//         ItemData itemData = itemsCollection.list[selectedItemIndex];

//         itemData.name = _name;
//         itemData.gridSize = gridSize;
//         itemData.thumb = thumb;
//         itemData.modelPath = modelPath;
//         itemData.model = model;
//         itemData.entityType = entityType;
//         itemData.buildingFunction = buildingFunction;

//         EditorUtility.SetDirty(itemsCollection);
//     }

//     Editor modelview;
//     void RenderPreviewModel()
//     {
//         if (model != null)
//         {
//             if (modelview == null)
//                 modelview = Editor.CreateEditor(model);
//             modelview.OnPreviewGUI(new Rect(WidthOfLetPanel, 0, position.width - WidthOfLetPanel - WidthOfRightPanel, position.height), EditorStyles.whiteLabel);
//         }
//     }
// }
