using UnityEngine;
using UnityEditor;
using Top;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// 确保窗口只存在一个实例，避免重复创建
[InitializeOnLoad]
public class PivotEditor : EditorWindow
{
    private enum EditMode
    {
        Building,
        Unit,
        Ability,
        Equipment,
        Quest
    }

    private EditMode _editMode = EditMode.Building;
    private Vector2 _scrollPosition;
    private int _selectedIndex = -1;
    private Vector2 _scrollPositionRight;

    private List<BuildingDataSO> _buildingDataList = new List<BuildingDataSO>();
    private List<UnitDataSO> _unitDataList = new List<UnitDataSO>();
    private List<SkillDataSO> _skillDataList = new List<SkillDataSO>();
    private List<EquipmentDataSO> _equipmentDataList = new List<EquipmentDataSO>();
    private List<QuestDataSO> _questDataList = new List<QuestDataSO>();

    private ScriptableObject _selectedSO;
    private SerializedObject _serializedItem;
    
    [MenuItem("Tools/SO Tool/Pivot Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<PivotEditor>();
        window.titleContent = new GUIContent("Pivot Editor");
        window.minSize = new Vector2(800, 600); // 设置最小窗口尺寸，避免编辑区域过小
    }

    private void OnEnable()
    {
        LoadAllAssets();
        ResetSelectedState(); // 加载资源后重置选中状态
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawMainContent();
    }

    /// <summary>
    /// 绘制工具栏
    /// </summary>
    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        // 切换编辑模式
        _editMode = (EditMode)GUILayout.Toolbar((int)_editMode, 
            new[] { "Building", "Unit", "Ability", "Equipment", "Quest" }, 
            GUILayout.Width(400));

        // 添加按钮
        if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
        {
            CreateItem();
        }

        // 保存按钮
        if (GUILayout.Button("Save", EditorStyles.toolbarButton))
        {
            SaveAllAssets();
        }

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制主内容区域
    /// </summary>
    private void DrawMainContent()
    {
        GUILayout.BeginHorizontal();

        // 左侧列表
        DrawAssetList();

        // 右侧编辑区域
        DrawEditorPanel();

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 绘制资源列表
    /// </summary>
    private void DrawAssetList()
    {
        GUILayout.BeginVertical(GUILayout.Width(250));

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        switch (_editMode)
        {
            case EditMode.Building:
                DrawBuildingList();
                break;
            case EditMode.Quest:
                DrawQuestList();
                break;
            case EditMode.Ability:
                DrawAbilityList();
                break;
            case EditMode.Equipment:
                DrawEquipmentList(); // 补全缺失的方法
                break;
            case EditMode.Unit:
                DrawUnitList(); // 补全缺失的方法
                break;
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    #region 资源列表绘制
    /// <summary>
    /// 绘制技能列表
    /// </summary>
    private void DrawAbilityList()
    {
        GUILayout.Label("Skills", EditorStyles.boldLabel);
        for (int i = 0; i < _skillDataList.Count; i++)
        {
            SkillDataSO skill = _skillDataList[i];
            if (skill == null) continue;

            GUILayout.BeginHorizontal();

            bool isSelected = _selectedIndex == i;
            if (GUILayout.Toggle(isSelected, skill.skillName, EditorStyles.radioButton, GUILayout.Width(180)))
            {
                _selectedIndex = i;
                SetSelectedSO(skill); // 关键修复：赋值选中的SO
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Skill", $"Are you sure you want to delete {skill.skillName}?", "Yes", "No"))
                {
                    DeleteItem(skill);
                    ResetSelectedState(); // 删除后重置选中状态
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawBuildingList()
    {
        GUILayout.Label("Buildings", EditorStyles.boldLabel);
        for (int i = 0; i < _buildingDataList.Count; i++)
        {
            BuildingDataSO building = _buildingDataList[i];
            if (building == null) continue;

            GUILayout.BeginHorizontal();

            bool isSelected = _selectedIndex == i;
            if (GUILayout.Toggle(isSelected, building.DisplayName, EditorStyles.radioButton, GUILayout.Width(180)))
            {
                _selectedIndex = i;
                SetSelectedSO(building); // 关键修复：赋值选中的SO
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Building", $"Are you sure you want to delete {building.DisplayName}?", "Yes", "No"))
                {
                    DeleteItem(building);
                    ResetSelectedState(); // 删除后重置选中状态
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawQuestList()
    {
        GUILayout.Label("Quests", EditorStyles.boldLabel);
        for (int i = 0; i < _questDataList.Count; i++)
        {
            QuestDataSO quest = _questDataList[i];
            if (quest == null) continue;

            GUILayout.BeginHorizontal();

            bool isSelected = _selectedIndex == i;
            if (GUILayout.Toggle(isSelected, quest.questName, EditorStyles.radioButton, GUILayout.Width(180)))
            {
                _selectedIndex = i;
                SetSelectedSO(quest); // 关键修复：赋值选中的SO
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Quest", $"Are you sure you want to delete {quest.questName}?", "Yes", "No"))
                {
                    DeleteItem(quest);
                    ResetSelectedState(); // 删除后重置选中状态
                }
            }

            GUILayout.EndHorizontal();
        }
    }

    // 补全缺失的Equipment列表绘制
    private void DrawEquipmentList()
    {
        GUILayout.Label("Equipment", EditorStyles.boldLabel);
        for (int i = 0; i < _equipmentDataList.Count; i++)
        {
            EquipmentDataSO equipment = _equipmentDataList[i];
            if (equipment == null) continue;

            GUILayout.BeginHorizontal();
            bool isSelected = _selectedIndex == i;
            if (GUILayout.Toggle(isSelected, equipment.equipmentName, EditorStyles.radioButton, GUILayout.Width(180)))
            {
                _selectedIndex = i;
                SetSelectedSO(equipment);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Equipment", $"Are you sure you want to delete {equipment.equipmentName}?", "Yes", "No"))
                {
                    DeleteItem(equipment);
                    ResetSelectedState();
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    // 补全缺失的Unit列表绘制
    private void DrawUnitList()
    {
        GUILayout.Label("Units", EditorStyles.boldLabel);
        for (int i = 0; i < _unitDataList.Count; i++)
        {
            UnitDataSO unit = _unitDataList[i];
            if (unit == null) continue;

            GUILayout.BeginHorizontal();
            bool isSelected = _selectedIndex == i;
            if (GUILayout.Toggle(isSelected, unit.unitName, EditorStyles.radioButton, GUILayout.Width(180)))
            {
                _selectedIndex = i;
                SetSelectedSO(unit);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                if (EditorUtility.DisplayDialog("Delete Unit", $"Are you sure you want to delete {unit.unitName}?", "Yes", "No"))
                {
                    DeleteItem(unit);
                    ResetSelectedState();
                }
            }
            GUILayout.EndHorizontal();
        }
    }
    #endregion

    #region 编辑区
    private void DrawEditorPanel()
    {
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        if (_selectedSO == null)
        {
            EditorGUILayout.LabelField("请选择一个 ScriptableObject 进行编辑", EditorStyles.centeredGreyMiniLabel);
            GUILayout.EndVertical();
            return;
        }

        // 确保SerializedObject与选中的SO关联
        if (_serializedItem == null || _serializedItem.targetObject != _selectedSO)
        {
            _serializedItem = new SerializedObject(_selectedSO);
        }

        _serializedItem.Update();

        SerializedProperty iterator = _serializedItem.GetIterator();
        bool enterChildren = true;

        EditorGUILayout.BeginVertical("box");
        _scrollPositionRight = EditorGUILayout.BeginScrollView(_scrollPositionRight);

        // 修复嵌套字段（数组、自定义类）的显示问题
        while (iterator.NextVisible(enterChildren))
        {
            if (iterator.name == "m_Script") // 跳过脚本引用字段
            {
                enterChildren = false;
                continue;
            }
            EditorGUILayout.PropertyField(iterator, true); // true = 自动展开嵌套字段
            enterChildren = false;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 应用所有修改
        if (_serializedItem.hasModifiedProperties)
        {
            _serializedItem.ApplyModifiedProperties();
        }

        GUILayout.EndVertical();
    }
    #endregion

    #region 核心功能方法
    private void CreateItem()
    {
        ScriptableObject obj = null;
        string defaultName = "";
        string folderPath = "";

        switch (_editMode)
        {
            case EditMode.Building:
                defaultName = "New Building";
                folderPath = Constant.BUILDING_PATH;
                obj = ScriptableObject.CreateInstance<BuildingDataSO>();
                break;
            case EditMode.Ability:
                // 关键修复：Ability模式创建SkillDataSO，路径改为SKILL_PATH
                defaultName = "New Ability";
                folderPath = Constant.SKILL_PATH;
                obj = ScriptableObject.CreateInstance<SkillDataSO>();
                break;
            case EditMode.Quest:
                defaultName = "New Quest";
                folderPath = Constant.QUEST_PATH;
                obj = ScriptableObject.CreateInstance<QuestDataSO>();
                break;
            case EditMode.Equipment:
                defaultName = "New Equipment";
                folderPath = Constant.EQUIPMENT_PATH;
                obj = ScriptableObject.CreateInstance<EquipmentDataSO>();
                break;
            case EditMode.Unit:
                defaultName = "New Unit";
                folderPath = Constant.UNIT_PATH;
                obj = ScriptableObject.CreateInstance<UnitDataSO>();
                break;
        }

        if (obj == null) return;

        obj.name = defaultName;

        // 确保文件夹存在
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh(); // 刷新数据库，避免路径识别错误
        }

        // 生成唯一路径（避免重名）
        string path = $"{folderPath}{obj.name}.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        // 创建并保存资源
        AssetDatabase.CreateAsset(obj, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 重新加载资源列表
        LoadAllAssets();
        ResetSelectedState();

        Debug.Log($"Created new {_editMode}: {path}");
    }

    private void DeleteItem(Object obj)
    {
        if (obj == null) return;

        // 删除资源文件
        string path = AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 从对应列表中移除
        if (obj is SkillDataSO skill) _skillDataList.Remove(skill);
        else if (obj is BuildingDataSO building) _buildingDataList.Remove(building);
        else if (obj is QuestDataSO quest) _questDataList.Remove(quest);
        else if (obj is EquipmentDataSO equipment) _equipmentDataList.Remove(equipment);
        else if (obj is UnitDataSO unit) _unitDataList.Remove(unit);

        Debug.Log($"Deleted Item: {obj.name}");
    }

    private void LoadAllAssets()
    {
        // 清空所有列表
        _buildingDataList.Clear();
        _questDataList.Clear();
        _equipmentDataList.Clear();
        _skillDataList.Clear();
        _unitDataList.Clear();

        // 加载资源（兼容AssetLoader和默认加载方式）
        _buildingDataList = LoadAssets<BuildingDataSO>(Constant.BUILDING_PATH);
        _questDataList = LoadAssets<QuestDataSO>(Constant.QUEST_PATH);
        _equipmentDataList = LoadAssets<EquipmentDataSO>(Constant.EQUIPMENT_PATH);
        _skillDataList = LoadAssets<SkillDataSO>(Constant.SKILL_PATH);
        _unitDataList = LoadAssets<UnitDataSO>(Constant.UNIT_PATH);
    }

    /// <summary>
    /// 通用资源加载方法（兼容自定义AssetLoader和Unity默认加载）
    /// </summary>
    private List<T> LoadAssets<T>(string folderPath) where T : ScriptableObject
    {
        if (!Directory.Exists(folderPath)) return new List<T>();

        // 如果项目中有AssetLoader，优先使用；否则用Unity默认方式
        try
        {
            return AssetLoader.LoadAllAssetsInFolder(folderPath).OfType<T>().ToList();
        }
        catch
        {
            // Unity默认加载方式
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) assets.Add(asset);
            }
            return assets;
        }
    }

    /// <summary>
    /// 保存所有资源（修复无效保存问题）
    /// </summary>
    private void SaveAllAssets()
    {
        // 标记所有加载的SO为脏（确保修改被保存）
        MarkAssetsDirty(_buildingDataList);
        MarkAssetsDirty(_unitDataList);
        MarkAssetsDirty(_skillDataList);
        MarkAssetsDirty(_equipmentDataList);
        MarkAssetsDirty(_questDataList);

        // 保存并刷新
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("保存成功", "所有资源已保存", "确定");
        Debug.Log("All assets saved");
    }

    /// <summary>
    /// 标记列表中的SO为脏（需要保存）
    /// </summary>
    private void MarkAssetsDirty<T>(List<T> assets) where T : ScriptableObject
    {
        foreach (var asset in assets)
        {
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
            }
        }
    }

    /// <summary>
    /// 设置选中的SO（统一处理）
    /// </summary>
    private void SetSelectedSO(ScriptableObject so)
    {
        _selectedSO = so;
        _serializedItem = new SerializedObject(so); // 重新创建SerializedObject
    }

    /// <summary>
    /// 重置选中状态（避免空引用/索引越界）
    /// </summary>
    private void ResetSelectedState()
    {
        _selectedIndex = -1;
        _selectedSO = null;
        _serializedItem = null;
    }
    #endregion
}
