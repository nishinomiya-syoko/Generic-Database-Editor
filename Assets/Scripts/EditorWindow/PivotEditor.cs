// using UnityEngine;
// using UnityEditor;
// using Top;
// using System.Collections.Generic;
// using System.Linq;
// using System.IO;

// public class PivotEditor : EditorWindow
// {
//     private enum EditMode
//     {
//         Building,
//         Unit,
//         Ability,
//         Equipment,
//         Quest,

//     }
//     private EditMode _editMode = EditMode.Building;
//     private Vector2 _scrollPosition;
//     private int _selectedIndex = -1;
//     private Vector2 _scrollPositionRight;


//     private List<BuildingDataSO> _buildingDataList = new List<BuildingDataSO>();
//     private List<UnitDataSO> _unitDataList = new List<UnitDataSO>();
//     private List<SkillDataSO> _skillDataList = new List<SkillDataSO>();
//     private List<EquipmentDataSO> _equipmentDataList = new List<EquipmentDataSO>();
//     private List<QuestDataSO> _questDataList = new List<QuestDataSO>();

//     private ScriptableObject _selectedSO;
//     private SerializedObject _serializedItem;
//     [MenuItem("Tools/PivotEditor")]
//     public static void ShowWindow()
//     {
//         var window = GetWindow<PivotEditor>();
//         window.titleContent = new GUIContent("PivotEditor");
//     }
//     private void OnEnable()
//     {
//         LoadAllAssets();
//     }
//     private void OnGUI()
//     {
//         DrawToolbar();
//         DrawMainContent();
//     }
//     /// <summary>
//     /// 绘制工具栏
//     /// </summary>
//     private void DrawToolbar()
//     {
//         GUILayout.BeginHorizontal(EditorStyles.toolbar);

//         // 切换编辑模式
//         _editMode = (EditMode)GUILayout.Toolbar((int)_editMode, new[] { "Building", "Unit", "Ability", "Equipment", "Quest" }, GUILayout.Width(400));
       
        
//         // 添加按钮
//         if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
//         {
//             // ShowCreateMenu();
//             CreateItem();
//         }

//         // 保存按钮
//         if (GUILayout.Button("Save", EditorStyles.toolbarButton))
//         {
//             SaveAllAssets();
//         }

//         GUILayout.EndHorizontal();
//     }
//     /// <summary>
//     /// 绘制主内容区域
//     /// </summary>
//     private void DrawMainContent()
//     {
//         GUILayout.BeginHorizontal();

//         // 左侧列表
//         DrawAssetList();

//         // 右侧编辑区域
//         DrawEditorPanel();

//         GUILayout.EndHorizontal();
//     }
//     /// <summary>
//     /// 绘制资源列表
//     /// </summary>
//     private void DrawAssetList()
//     {
//         GUILayout.BeginVertical(GUILayout.Width(250));

//         _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

//         switch (_editMode)
//         {
//             case EditMode.Building:
//                 DrawBuildingList();
//                 break;
//             case EditMode.Quest:
//                 DrawQuestList();
//                 break;
//             // case EditMode.Equipment:
//             //     DrawEquipmentList();
//             //     break;
//             case EditMode.Ability:
//                 DrawAbilityList();
//                 break;
//             // case EditMode.Unit:
//             //     DrawUnitList();
//             //     break;
//         }

//         EditorGUILayout.EndScrollView();
//         GUILayout.EndVertical();
//     }

//     #region 资源列表绘制
//     /// <summary>
//     /// 绘制技能列表
//     /// </summary>
//     private void DrawAbilityList()
//     {
//         GUILayout.Label("Skills", EditorStyles.boldLabel);
//         for (int i = 0; i < _skillDataList.Count; i++)
//         {
//             SkillDataSO skill = _skillDataList[i];
//             if (skill == null) continue;
//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, skill.skillName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 // _selectedSkill = skill;
//             }
//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Skill", $"Are you sure you want to delete {skill.skillName}?", "Yes", "No"))
//                 {
//                     DeleteItem(skill);
//                     _selectedIndex = -1;
//                     // _selectedSkill = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     private void DrawBuildingList()
//     {
//         GUILayout.Label("Buildings", EditorStyles.boldLabel);
//         for (int i = 0; i < _buildingDataList.Count; i++)
//         {
//             BuildingDataSO building = _buildingDataList[i];
//             if (building == null) continue;
//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, building.DisplayName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 // _selectedBuilding = building;
//             }
//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Building", $"Are you sure you want to delete {building.DisplayName}?", "Yes", "No"))
//                 {
//                     DeleteItem(building);
//                     _selectedIndex = -1;
//                     // _selectedBuilding = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     private void DrawQuestList()
//     {
//         GUILayout.Label("Quests", EditorStyles.boldLabel);
//         for (int i = 0; i < _questDataList.Count; i++)
//         {
//             QuestDataSO quest = _questDataList[i];
//             if (quest == null) continue;
//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, quest.questName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 // _selectedQuest = quest;
//             }
//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Quest", $"Are you sure you want to delete {quest.questName}?", "Yes", "No"))
//                 {
//                     DeleteItem(quest);
//                     _selectedIndex = -1;
//                     // _selectedQuest = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     #endregion
//     #region 编辑区
//     private void DrawEditorPanel()
//     {
//         if (_selectedSO == null)
//         {
//             GUILayout.Label("请选择一个 ScriptableObject");
//             return;
//         }
//         if (_serializedItem == null || _serializedItem.targetObject != _selectedSO)
//             _serializedItem = new SerializedObject(_selectedSO);

//         _serializedItem.Update();

//         SerializedProperty iterator = _serializedItem.GetIterator();
//         bool enterChildren = true;

//         EditorGUILayout.BeginVertical("box");
//         _scrollPositionRight = EditorGUILayout.BeginScrollView(_scrollPositionRight);
//         // 遍历所有可序列化字段（包括List、Serializable类）
//         while (iterator.NextVisible(enterChildren))
//         {
//             if (iterator.name == "m_Script") continue; // 不显示脚本引用
//             EditorGUILayout.PropertyField(iterator, true); // 自动绘制所有字段
//             enterChildren = false;
//         }

//         EditorGUILayout.EndScrollView();

//         EditorGUILayout.EndVertical();

//         // 应用修改
//         _serializedItem.ApplyModifiedProperties();
//     }
//     #endregion
//     private void CreateItem()
//     {
//         ScriptableObject obj = null;
//         string defaultName = "";
//         string folderPath = "";
//         switch (_editMode)
//         {
//             case EditMode.Building:
//                 defaultName = "New Building";
//                 folderPath = Constant.BUILDING_PATH;
//                 obj = ScriptableObject.CreateInstance<BuildingDataSO>();
//                 break;
//             case EditMode.Ability:
//                 defaultName = "New Ability";
//                 folderPath = Constant.QUEST_PATH;
//                 obj = ScriptableObject.CreateInstance<QuestDataSO>();
//                 break;
//             case EditMode.Quest:
//                 defaultName = "New Quest";
//                 folderPath = Constant.QUEST_PATH;
//                 obj = ScriptableObject.CreateInstance<QuestDataSO>();
//                 break;
//             case EditMode.Equipment:
//                 defaultName = "New Equipment";
//                 folderPath = Constant.EQUIPMENT_PATH;
//                 obj = ScriptableObject.CreateInstance<EquipmentDataSO>();
//                 break;
//             case EditMode.Unit:
//                 defaultName = "New Unit";
//                 folderPath = Constant.UNIT_PATH;
//                 obj = ScriptableObject.CreateInstance<UnitDataSO>();
//                 break;
//         }
//         if (obj != null)
//         {
//             obj.name = defaultName;
            
//             if (!Directory.Exists(folderPath))
//                 Directory.CreateDirectory(folderPath);

//             string path = $"{folderPath}{obj.name}.asset";
//             path = AssetDatabase.GenerateUniqueAssetPath(path);
//             AssetDatabase.CreateAsset(obj, path);
//             AssetDatabase.SaveAssets();

//             // _items.Add(obj);
//             // ChangeMode();
//             OnEnable();
//             // _selectedItem = obj;
//             Debug.Log($"Created new {_editMode}: {path}");
//         }
//     }
//     private void DeleteItem(Object obj)
//     {
//         if (obj == null)
//             return;
//         Debug.Log($"Deleted Item: {obj.name}");

//         // 删除资源
//         string path = AssetDatabase.GetAssetPath(obj);
//         AssetDatabase.DeleteAsset(path);
//         AssetDatabase.SaveAssets();

//         // 从列表中移除
//         if (obj is SkillDataSO skill)
//         {
//             _skillDataList.Remove(skill);
//         }
//         else if (obj is BuildingDataSO building)
//         {
//             _buildingDataList.Remove(building);
//         }
//         else if (obj is QuestDataSO quest)
//         {
//             _questDataList.Remove(quest);
//         }
//         else if (obj is EquipmentDataSO equipment)
//         {
//             _equipmentDataList.Remove(equipment);
//         }
//         else if (obj is UnitDataSO unit)
//         {
//             _unitDataList.Remove(unit);
//         }
//     }
//     // 保持原有保存、加载、删除、创建方法不变...
//     private void LoadAllAssets()
//     {
//         _buildingDataList.Clear();
//         _questDataList.Clear();
//         _equipmentDataList.Clear();
//         _skillDataList.Clear();
//         _unitDataList.Clear();
//         _buildingDataList = AssetLoader.LoadAllAssetsInFolder(Constant.BUILDING_PATH).OfType<BuildingDataSO>().ToList();
//         _questDataList = AssetLoader.LoadAllAssetsInFolder(Constant.QUEST_PATH).OfType<QuestDataSO>().ToList();
//         _equipmentDataList = AssetLoader.LoadAllAssetsInFolder(Constant.EQUIPMENT_PATH).OfType<EquipmentDataSO>().ToList();
//         _skillDataList = AssetLoader.LoadAllAssetsInFolder(Constant.SKILL_PATH).OfType<SkillDataSO>().ToList();
//         _unitDataList = AssetLoader.LoadAllAssetsInFolder(Constant.UNIT_PATH).OfType<UnitDataSO>().ToList();
//     }

//     /// <summary>
//     /// 保存所有资源
//     /// </summary>
//     private void SaveAllAssets()
//     {
//         EditorUtility.SetDirty(this);
//         AssetDatabase.SaveAssets();
//         AssetDatabase.Refresh();

//         Debug.Log("All assets saved");
//     }
// }