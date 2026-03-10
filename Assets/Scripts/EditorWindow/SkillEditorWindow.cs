// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
// using System.IO;
// using System.Linq;
// using Skill;

// public class SkillEditorWindow : EditorWindow
// {
//     private enum EditMode
//     {
//         Skill,
//         Buff,
//         Effect,
//         Condition
//     }
//     private EditMode _editMode = EditMode.Skill;
//     private Vector2 _scrollPosition;
//     private int _selectedIndex = -1;
//     private Vector2 _scrollPositionRight;

//     private List<SkillDataSO> _skills = new List<SkillDataSO>();
//     private SkillDataSO _selectedSkill;
//     private List<BuffDataSO> _buffs = new List<BuffDataSO>();
//     private BuffDataSO _selectedBuff;
//     private List<SkillEffectSO> _effects = new List<SkillEffectSO>();
//     private SkillEffectSO _selectedEffect;
//     private List<SkillConditionSO> _conditions = new List<SkillConditionSO>();
//     private SkillConditionSO _selectedCondition;

//     [MenuItem("Window/SkillEditorWindow")]
//     public static void ShowWindow()
//     {
//         var window = GetWindow<SkillEditorWindow>();
//         window.titleContent = new GUIContent("技能编辑器");
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
//     // private EditMode _currentEditMode;
//     // private int _index_skill,_index_buff,_index_effect,_index_condition;
//     /// <summary>
//     /// 绘制工具栏
//     /// </summary>
//     private void DrawToolbar()
//     {
//         GUILayout.BeginHorizontal(EditorStyles.toolbar);

//         // 切换编辑模式
//         _editMode = (EditMode)GUILayout.Toolbar((int)_editMode, new[] { "Skills", "Buffs", "Effects", "Conditions" }, GUILayout.Width(200));
        
//         // 添加按钮
//         if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
//         {
//             // ShowCreateMenu();
//             CreatItem();
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
//             case EditMode.Skill:
//                 DrawSkillList();
//                 break;
//             case EditMode.Buff:
//                 DrawBuffList();
//                 break;
//             case EditMode.Effect:
//                 DrawEffectList();
//                 break;
//             case EditMode.Condition:
//                 DrawConditionList();
//                 break;
//         }

//         EditorGUILayout.EndScrollView();
//         GUILayout.EndVertical();
//     }
//     #region 资源列表绘制
//     /// <summary>
//     /// 绘制技能列表
//     /// </summary>
//     private void DrawSkillList()
//     {
//         GUILayout.Label("Skills", EditorStyles.boldLabel);
//         for (int i = 0; i < _skills.Count; i++)
//         {
//             SkillDataSO skill = _skills[i];
//             if (skill == null) continue;
//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, skill.skillName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 _selectedSkill = skill;
//             }
//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Skill", $"Are you sure you want to delete {skill.skillName}?", "Yes", "No"))
//                 {
//                     DeleteItem(skill);
//                     _selectedIndex = -1;
//                     _selectedSkill = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     /// <summary>
//     /// 绘制Buff列表
//     /// </summary>
//     private void DrawBuffList()
//     {
//         GUILayout.Label("Buffs", EditorStyles.boldLabel);

//         for (int i = 0; i < _buffs.Count; i++)
//         {
//             BuffDataSO buff = _buffs[i];
//             if (buff == null)
//                 continue;

//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, buff.buffName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 _selectedBuff = buff;
//             }

//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Buff", $"Are you sure you want to delete {buff.buffName}?", "Yes", "No"))
//                 {
//                     DeleteItem(buff);
//                     _selectedIndex = -1;
//                     _selectedBuff = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     /// <summary>
//     /// 绘制效果列表
//     /// </summary>
//     private void DrawEffectList()
//     {
//         GUILayout.Label("Effects", EditorStyles.boldLabel);

//         for (int i = 0; i < _effects.Count; i++)
//         {
//             SkillEffectSO effect = _effects[i];
//             if (effect == null)
//                 continue;

//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, effect.effectName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 _selectedEffect = effect;
//             }

//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Effect", $"Are you sure you want to delete {effect.effectName}?", "Yes", "No"))
//                 {
//                     DeleteItem(effect);
//                     _selectedIndex = -1;
//                     _selectedEffect = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     /// <summary>
//     /// 绘制条件列表
//     /// </summary>
//     private void DrawConditionList()
//     {
//         GUILayout.Label("Conditions", EditorStyles.boldLabel);

//         for (int i = 0; i < _conditions.Count; i++)
//         {
//             SkillConditionSO condition = _conditions[i];
//             if (condition == null)
//                 continue;

//             GUILayout.BeginHorizontal();

//             bool isSelected = _selectedIndex == i;
//             if (GUILayout.Toggle(isSelected, condition.conditionName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 _selectedCondition = condition;
//             }

//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Condition", $"Are you sure you want to delete {condition.conditionName}?", "Yes", "No"))
//                 {
//                     DeleteItem(condition);
//                     _selectedIndex = -1;
//                     _selectedCondition = null;
//                 }
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
//     #endregion
//     #region 资源编辑
//     /// <summary>
//     /// 绘制编辑器面板
//     /// </summary>
//     private void DrawEditorPanel()
//     {

//         GUILayout.BeginVertical();
//         _scrollPositionRight = GUILayout.BeginScrollView(_scrollPositionRight);

//         switch (_editMode)
//         {
//             case EditMode.Skill:
//                 DrawSkillEditor();
//                 break;
//             case EditMode.Buff:
//                 DrawBuffEditor();
//                 break;
//             case EditMode.Effect:
//                 DrawEffectEditor();
//                 break;
//             case EditMode.Condition:
//                 DrawConditionEditor();
//                 break;
//         }
//         GUILayout.EndScrollView();
//         GUILayout.EndVertical();
//     }

//     /// <summary>
//     /// 绘制技能编辑器
//     /// </summary>
//     private void DrawSkillEditor()
//     {
//         if (_selectedSkill == null)
//         {
//             GUILayout.Label("Select a skill to edit", EditorStyles.centeredGreyMiniLabel);
//             return;
//         }

//         // 基本信息
//         GUILayout.Label("Skill Settings", EditorStyles.boldLabel);
//         _selectedSkill.skillName = EditorGUILayout.TextField("Skill Name", _selectedSkill.skillName);
//         _selectedSkill.skillID = EditorGUILayout.TextField("Skill ID", _selectedSkill.skillID);
//         GUILayout.Label("Description");
//         _selectedSkill.description = EditorGUILayout.TextArea(_selectedSkill.description, GUILayout.Height(60));
//         _selectedSkill.icon = (Sprite)EditorGUILayout.ObjectField("Icon", _selectedSkill.icon, typeof(Sprite), false);
//         GUILayout.Label("Skill Properties");
//         _selectedSkill.cooldown = EditorGUILayout.FloatField("Cooldown", _selectedSkill.cooldown);
//         _selectedSkill.manaCost = EditorGUILayout.IntField("Mana Cost", _selectedSkill.manaCost);
//         _selectedSkill.level = EditorGUILayout.IntField("Level", _selectedSkill.level);
//         _selectedSkill.priority = EditorGUILayout.IntField("Priority", _selectedSkill.priority);
//         _selectedSkill.itemMaxLevel = EditorGUILayout.IntField("Max Level", _selectedSkill.itemMaxLevel);
//         _selectedSkill.checkTiming = (EnumCheckTiming)EditorGUILayout.EnumPopup("Check Timing", _selectedSkill.checkTiming);
//         _selectedSkill.probability = EditorGUILayout.IntField("Probability", _selectedSkill.probability);

//         DrawStatusListEditor(_selectedSkill.skillProperties);
//         DrawConditionListEditor(_selectedSkill.triggerConditions);
//         DrawEffectListEditor(_selectedSkill.effects);
//     }
//     /// <summary>
//     /// 绘制Buff编辑器
//     /// </summary>
//     private void DrawBuffEditor()
//     {
//         if (_selectedBuff == null)
//         {
//             GUILayout.Label("Select a buff to edit", EditorStyles.centeredGreyMiniLabel);
//             return;
//         }

//         // 基本信息
//         GUILayout.Label("Buff Settings", EditorStyles.boldLabel);
//         _selectedBuff.buffName = EditorGUILayout.TextField("Buff Name", _selectedBuff.buffName);
//         _selectedBuff.icon = (Sprite)EditorGUILayout.ObjectField("Icon", _selectedBuff.icon, typeof(Sprite), false);
//         _selectedBuff.duration = EditorGUILayout.FloatField("Duration", _selectedBuff.duration);
//         _selectedBuff.canStack = EditorGUILayout.Toggle("Can Stack", _selectedBuff.canStack);
//         if (_selectedBuff.canStack)
//         {
//             _selectedBuff.maxStack = EditorGUILayout.IntField("Max Stack", _selectedBuff.maxStack);
//         }
//         _selectedBuff.refreshStack = EditorGUILayout.Toggle("Refresh Stack", _selectedBuff.refreshStack);
//         _selectedBuff.isValueMultiplyLevel = EditorGUILayout.Toggle("Is Value Multiply Level", _selectedBuff.isValueMultiplyLevel);
//         _selectedBuff.buffType = (EnumBuffType)EditorGUILayout.EnumPopup("Buff Type", _selectedBuff.buffType);

//         _selectedBuff.effectGoodOrBad = (EnumGoodOrBad)EditorGUILayout.EnumPopup("Effect Type", _selectedBuff.effectGoodOrBad);
//         DrawPropertiesListEditor(_selectedBuff.itemAdditionalPropertiesList);
//     }

//     /// <summary>
//     /// 绘制效果编辑器
//     /// </summary>
//     private void DrawEffectEditor()
//     {
//         if (_selectedEffect == null)
//         {
//             GUILayout.Label("Select an effect to edit", EditorStyles.centeredGreyMiniLabel);
//             return;
//         }

//         // 基本信息
//         GUILayout.Label("Effect Settings", EditorStyles.boldLabel);
//         _selectedEffect.effectName = EditorGUILayout.TextField("Effect Name", _selectedEffect.effectName);
//         _selectedEffect.description = EditorGUILayout.TextArea(_selectedEffect.description, GUILayout.Height(60));
//         _selectedEffect.delay = EditorGUILayout.FloatField("Delay", _selectedEffect.delay);
//         _selectedEffect.duration = EditorGUILayout.FloatField("Duration", _selectedEffect.duration);
//         _selectedEffect.interval = EditorGUILayout.FloatField("Interval", _selectedEffect.interval);
//         _selectedEffect.stackable = EditorGUILayout.Toggle("Stackable", _selectedEffect.stackable);
//         if (_selectedEffect.stackable)
//         {
//             _selectedEffect.maxStack = EditorGUILayout.IntField("Max Stack", _selectedEffect.maxStack);
//             _selectedEffect.stackrefresh = EditorGUILayout.Toggle("Stack refresh", _selectedEffect.stackrefresh);
//         }
//         _selectedEffect.itemTargetType = (EnumTargetType)EditorGUILayout.EnumPopup("Target Type", _selectedEffect.itemTargetType);
//         _selectedEffect.itemTargetCount = EditorGUILayout.IntField("Target Count", _selectedEffect.itemTargetCount);
//         DrawPropertiesListEditor(_selectedEffect.itemAdditionalPropertiesList);
//         DrawBuffListEditor(_selectedEffect.itemTriggerBuffs);
//     }

//     private void DrawConditionEditor()
//     {
//         if (_selectedCondition == null)
//         {
//             GUILayout.Label("Select a condition to edit", EditorStyles.centeredGreyMiniLabel);
//             return;
//         }
//         // 基本信息
//         GUILayout.Label("Condition Settings", EditorStyles.boldLabel);
//         _selectedCondition.conditionName = EditorGUILayout.TextField("Condition Name", _selectedCondition.conditionName);
//         _selectedCondition.description = EditorGUILayout.TextArea(_selectedCondition.description, GUILayout.Height(60));
//         _selectedCondition.propertyType_1 = (EnumProperty)EditorGUILayout.EnumPopup("Property Type 1", _selectedCondition.propertyType_1);
//         _selectedCondition.targetType_1 = (EnumTargetType)EditorGUILayout.EnumPopup("Target Type 1", _selectedCondition.targetType_1);

//         _selectedCondition.mathematicalJudgmentType = (EnumMathematicalJudgmentType)EditorGUILayout.EnumPopup("Mathematical Judgment Type", _selectedCondition.mathematicalJudgmentType);
//         _selectedCondition.triggerValue = EditorGUILayout.FloatField("Trigger Value", _selectedCondition.triggerValue);
//         _selectedCondition.isPercentage = EditorGUILayout.Toggle("Is Percentage", _selectedCondition.isPercentage);
//         _selectedCondition.propertyType_2 = (EnumProperty)EditorGUILayout.EnumPopup("Property Type 2", _selectedCondition.propertyType_2);
//         _selectedCondition.targetType_2 = (EnumTargetType)EditorGUILayout.EnumPopup("Target Type 2", _selectedCondition.targetType_2);
//     }

//     #endregion

//     #region 选择列表

//     /// <summary>
//     /// 绘制效果列表编辑器
//     /// </summary>
//     private void DrawEffectListEditor(List<SkillEffectSO> effects)
//     {
//         GUILayout.Space(10);
//         GUILayout.Label("Effects", EditorStyles.boldLabel);

//         if (effects == null)
//             return;

//         for (int i = 0; i < effects.Count; i++)
//         {
//             GUILayout.BeginHorizontal();

//             effects[i] = (SkillEffectSO)EditorGUILayout.ObjectField(effects[i], typeof(SkillEffectSO), false);

//             if (GUILayout.Button("Remove", GUILayout.Width(60)))
//             {
//                 effects.RemoveAt(i);
//                 i--;
//             }

//             GUILayout.EndHorizontal();
//         }

//         if (GUILayout.Button("Add Effect"))
//         {
//             ShowAddEffectMenu(effects);
//         }
//     }

//     /// <summary>
//     /// 绘制Buff列表编辑器
//     /// </summary>
//     private void DrawBuffListEditor(List<BuffDataSO> buffs)
//     {
//         GUILayout.Space(10);
//         GUILayout.Label("Buffs", EditorStyles.boldLabel);

//         if (buffs == null)
//             return;

//         for (int i = 0; i < buffs.Count; i++)
//         {
//             GUILayout.BeginHorizontal();

//             buffs[i] = (BuffDataSO)EditorGUILayout.ObjectField(buffs[i], typeof(BuffDataSO), false);

//             if (GUILayout.Button("Remove", GUILayout.Width(60)))
//             {
//                 buffs.RemoveAt(i);
//                 i--;
//             }

//             GUILayout.EndHorizontal();
//         }

//         if (GUILayout.Button("Add Buff"))
//         {
//             ShowAddBuffMenu(buffs);
//             // CreatItem();

//         }
//     }

//     /// <summary>
//     /// 绘制条件列表编辑器
//     /// </summary>
//     private void DrawConditionListEditor(List<SkillConditionSO> conditions)
//     {
//         GUILayout.Space(10);
//         GUILayout.Label("Conditions", EditorStyles.boldLabel);

//         if (conditions == null)
//             return;

//         for (int i = 0; i < conditions.Count; i++)
//         {
//             GUILayout.BeginHorizontal();

//             conditions[i] = (SkillConditionSO)EditorGUILayout.ObjectField(conditions[i], typeof(SkillConditionSO), false);

//             if (GUILayout.Button("Remove", GUILayout.Width(60)))
//             {
//                 conditions.RemoveAt(i);
//                 i--;
//             }

//             GUILayout.EndHorizontal();
//         }

//         if (GUILayout.Button("Add Condition"))
//         {
//             ShowAddConditionMenu(conditions);
//         }
//     }

//     /// <summary>
//     /// 绘制技能列表编辑器
//     /// </summary>
//     private void DrawSkillListEditor(List<SkillDataSO> skills)
//     {
//         GUILayout.Space(10);
//         GUILayout.Label("Triggered Skills", EditorStyles.boldLabel);

//         if (skills == null)
//             return;

//         for (int i = 0; i < skills.Count; i++)
//         {
//             GUILayout.BeginHorizontal();

//             skills[i] = (SkillDataSO)EditorGUILayout.ObjectField(skills[i], typeof(SkillDataSO), false);

//             if (GUILayout.Button("Remove", GUILayout.Width(60)))
//             {
//                 skills.RemoveAt(i);
//                 i--;
//             }

//             GUILayout.EndHorizontal();
//         }

//         if (GUILayout.Button("Add Skill"))
//         {

//         }
//     }
//     #endregion
//     #region 添加附加效果

//     /// <summary>
//     /// 显示添加Buff菜单
//     /// </summary>
//     private void ShowAddBuffMenu(List<BuffDataSO> buffs)
//     {
//         if (_buffs.Count == 0)
//         {
//             EditorUtility.DisplayDialog("No Buffs Available", "Please create some buffs first.", "OK");
//             return;
//         }

//         GenericMenu menu = new GenericMenu();

//         foreach (var buff in _buffs)
//         {
//             if (buff != null)
//             {
//                 menu.AddItem(new GUIContent(buff.buffName), false, () => buffs.Add(buff));
//             }
//         }

//         menu.ShowAsContext();
//     }
//     // <summary>
//     /// 显示添加Buff菜单
//     /// </summary>
//     private void ShowAddEffectMenu(List<SkillEffectSO> buffs)
//     {
//         if (_effects.Count == 0)
//         {
//             EditorUtility.DisplayDialog("No Buffs Available", "Please create some buffs first.", "OK");
//             return;
//         }

//         GenericMenu menu = new GenericMenu();

//         foreach (var buff in _effects)
//         {
//             if (buff != null)
//             {
//                 menu.AddItem(new GUIContent(buff.effectName), false, () => buffs.Add(buff));
//             }
//         }

//         menu.ShowAsContext();
//     }

//     /// <summary>
//     /// 显示添加条件菜单
//     /// </summary>
//     private void ShowAddConditionMenu(List<SkillConditionSO> conditions)
//     {
//         if (_conditions.Count == 0)
//         {
//             EditorUtility.DisplayDialog("No Conditions Available", "Please create some conditions first.", "OK");
//             return;
//         }

//         GenericMenu menu = new GenericMenu();

//         foreach (var condition in _conditions)
//         {
//             if (condition != null)
//             {
//                 menu.AddItem(new GUIContent(condition.conditionName), false, () => conditions.Add(condition));
//             }
//         }

//         menu.ShowAsContext();
//     }
//     #endregion
//     #region 创建与删除物品
//     private void CreatItem()
//     {

//         switch (_editMode)
//         {
//             case EditMode.Skill:
//                 {
//                     var newSkill = ScriptableObject.CreateInstance<SkillDataSO>();
//                     newSkill.skillName = "New Skill";
//                     if (!Directory.Exists(Constant.SKILL_PATH))
//                         Directory.CreateDirectory(Constant.SKILL_PATH);
//                     string path = $"{Constant.SKILL_PATH}{newSkill.skillName}.asset";
//                     path = GetUnusedPath(path);

//                     AssetDatabase.CreateAsset(newSkill, path);
//                     AssetDatabase.SaveAssets();

//                     // 添加到列表
//                     _skills.Add(newSkill);
//                     _selectedIndex = _skills.Count - 1;
//                     _selectedSkill = newSkill;
//                     Debug.Log($"Created new : {_editMode.ToString()}");
//                 }

//                 break;
//             case EditMode.Buff:
//                 {
//                     var newItem = ScriptableObject.CreateInstance<BuffDataSO>();
//                     newItem.buffName = "New Buff";
//                     if (!Directory.Exists(Constant.BUFF_PATH))
//                         Directory.CreateDirectory(Constant.BUFF_PATH);
//                     string path = $"{Constant.BUFF_PATH}{newItem.buffName}.asset";
//                     path = GetUnusedPath(path);

//                     AssetDatabase.CreateAsset(newItem, path);
//                     AssetDatabase.SaveAssets();

//                     // 添加到列表
//                     _buffs.Add(newItem);
//                     _selectedIndex = _buffs.Count - 1;
//                     _selectedBuff = newItem;
//                     Debug.Log($"Created new : {_editMode.ToString()}");
//                 }
//                 break;
//             case EditMode.Effect:
//                 {
//                     var newItem = ScriptableObject.CreateInstance<SkillEffectSO>();
//                     newItem.effectName = "New Effect";
//                     if (!Directory.Exists(Constant.EFFECT_PATH))
//                         Directory.CreateDirectory(Constant.EFFECT_PATH);
//                     string path = $"{Constant.EFFECT_PATH}{newItem.effectName}.asset";
//                     path = GetUnusedPath(path);

//                     AssetDatabase.CreateAsset(newItem, path);
//                     AssetDatabase.SaveAssets();

//                     // 添加到列表
//                     _effects.Add(newItem);
//                     _selectedIndex = _effects.Count - 1;
//                     _selectedEffect = newItem;
//                     Debug.Log($"Created new : {_editMode.ToString()}");
//                 }
//                 break;
//             case EditMode.Condition:
//                 {
//                     var newItem = ScriptableObject.CreateInstance<SkillConditionSO>();
//                     newItem.conditionName = "New Condition";
//                     if (!Directory.Exists(Constant.CONDITION_PATH))
//                         Directory.CreateDirectory(Constant.CONDITION_PATH);
//                     string path = $"{Constant.CONDITION_PATH}{newItem.conditionName}.asset";
//                     path = GetUnusedPath(path);
//                     AssetDatabase.CreateAsset(newItem, path);
//                     AssetDatabase.SaveAssets();

//                     // 添加到列表
//                     _conditions.Add(newItem);
//                     _selectedIndex = _conditions.Count - 1;
//                     _selectedCondition = newItem;
//                     Debug.Log($"Created new : {_editMode.ToString()}");
//                 }
//                 break;
//         }
//     }
//     private string GetUnusedPath(string path)
//     {
//         return AssetDatabase.GenerateUniqueAssetPath(path);
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
//         if (obj is BuffDataSO buff)
//         {
//             _buffs.Remove(buff);
//         }
//         else if (obj is SkillDataSO skill)
//         {
//             _skills.Remove(skill);
//         }
//         else if (obj is SkillEffectSO effect)
//         {
//             _effects.Remove(effect);
//         }
//         else if (obj is SkillConditionSO condition)
//         {
//             _conditions.Remove(condition);
//         }

//     }
//     #endregion
//     private void LoadAllAssets()
//     {
//         _skills.Clear();
//         _buffs.Clear();
//         _effects.Clear();
//         _conditions.Clear();
//         _skills = AssetLoader.LoadAllAssetsInFolder(Constant.SKILL_PATH).OfType<SkillDataSO>().ToList();
//         _buffs = AssetLoader.LoadAllAssetsInFolder(Constant.BUFF_PATH).OfType<BuffDataSO>().ToList();
//         _effects = AssetLoader.LoadAllAssetsInFolder(Constant.EFFECT_PATH).OfType<SkillEffectSO>().ToList();
//         _conditions = AssetLoader.LoadAllAssetsInFolder(Constant.CONDITION_PATH).OfType<SkillConditionSO>().ToList();
//         Debug.Log($"Loaded {_skills.Count} skills, {_buffs.Count} buffs, {_effects.Count} effects, {_conditions.Count} conditions");

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
//     private void DrawStatusListEditor(List<PropertiesStruct> properties)
//     {
//         statusFoldout = EditorGUILayout.Foldout(statusFoldout, "Properties", true);
//         if (statusFoldout)
//         {
//             GUILayout.Label("Properties", EditorStyles.boldLabel);

//             foreach (var property in properties)
//             {
//                 EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

//                 DrawStatusEditor(property);
//             }
//         }
//         GUILayout.BeginHorizontal();
//         if (GUILayout.Button("Add Property"))
//         {
//             properties.Add(new PropertiesStruct());
//         }
//         if (GUILayout.Button("Remove property"))
//         {
//             if (properties.Count > 0)
//                 properties.RemoveAt(properties.Count - 1);
//         }
//         GUILayout.EndHorizontal();
//     }
//     private void DrawPropertiesListEditor(List<AdditionalPropertiesStruct> properties)
//     {
//         statusFoldout = EditorGUILayout.Foldout(statusFoldout, "Properties", true);
//         if (statusFoldout)
//         {
//             // GUILayout.Label("Properties", EditorStyles.boldLabel);

//             foreach (var property in properties)
//             {
//                 EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

//                 DrawStatusEditor(property);
//             }
//         }
//         GUILayout.BeginHorizontal();
//         if (GUILayout.Button("Add Property"))
//         {
//             properties.Add(new AdditionalPropertiesStruct());
//         }
//         if (GUILayout.Button("Remove property"))
//         {
//             if (properties.Count > 0)
//                 properties.RemoveAt(properties.Count - 1);
//         }
//         GUILayout.EndHorizontal();
//     }
//     bool statusFoldout = false;
//     private void DrawStatusEditor(object properties)
//     {
//         if (properties == null) return;

//         var fields = properties.GetType().GetFields();

//         foreach (var field in fields)
//         {
//             GUILayout.BeginHorizontal();
//             GUILayout.Space(20);
//             // 显示字段名
//             GUILayout.Label(field.Name, GUILayout.Width(150));

//             object val = field.GetValue(properties);
//             var fieldType = field.FieldType;

//             // 防护 null
//             if (val == null)
//             {
//                 // // 如果是值类型可以用默认值，否则显示空字符串
//                 // if (fieldType.IsValueType)
//                 //     val = Activator.CreateInstance(fieldType);
//                 // else
//                 //     val = "";
//                 continue;
//             }

//             // 整数
//             if (fieldType == typeof(int))
//             {
//                 int current = (int)val;
//                 int newValue = EditorGUILayout.IntField(current);
//                 if (newValue != current)
//                     field.SetValue(properties, newValue);
//             }
//             // 浮点
//             else if (fieldType == typeof(float) || fieldType == typeof(System.Single))
//             {
//                 float current = (float)val;
//                 float newValue = EditorGUILayout.FloatField(current);
//                 if (!Mathf.Approximately(newValue, current))
//                     field.SetValue(properties, newValue);
//             }
//             // 布尔
//             else if (fieldType == typeof(bool))
//             {
//                 bool current = (bool)val;
//                 bool newValue = EditorGUILayout.Toggle(current);
//                 if (newValue != current)
//                     field.SetValue(properties, newValue);
//             }
//             // 字符串
//             else if (fieldType == typeof(string))
//             {
//                 string current = (string)val;
//                 string newValue = EditorGUILayout.TextField(current ?? "");
//                 if (newValue != current)
//                     field.SetValue(properties, newValue);
//             }
//             // 枚举类型
//             else if (fieldType.IsEnum)
//             {
//                 System.Enum current = (System.Enum)val;
//                 System.Enum newEnum = EditorGUILayout.EnumPopup(current);
//                 if (!current.Equals(newEnum))
//                     field.SetValue(properties, newEnum);
//             }
//             // 其它类型，尝试显示为只读字符串（避免调用不安全的 ToString）
//             else
//             {
//                 string display = val != null ? val.ToString() : "(null)";
//                 GUILayout.Label(display);
//             }

//             GUILayout.EndHorizontal();
//         }
//     }
// }