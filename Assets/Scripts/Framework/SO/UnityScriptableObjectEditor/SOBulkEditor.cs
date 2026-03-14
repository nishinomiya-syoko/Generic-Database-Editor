using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SOEditor.Editor
{
    /// <summary>
    /// 批量编辑器工具
    /// </summary>
    public class SOBulkEditor : EditorWindow
    {
        private Vector2 _scrollPosition;
        private string _selectedSOType = "Item";
        private readonly string[] _soTypes = new[] { "Item", "Character", "Skill", "Quest" };
        
        // 批量编辑数据
        private Dictionary<string, bool> _selectedItems = new Dictionary<string, bool>();
        private bool _selectAll = false;
        
        // 批量修改字段
        private bool _modifyField1 = false;
        private bool _modifyField2 = false;
        private bool _modifyField3 = false;
        
        // 字段值
        private int _intValue1;
        private int _intValue2;
        private float _floatValue1;
        private string _stringValue1 = "";
        
        [MenuItem("Tools/SO Tool/Bulk Editor")]
        public static void ShowWindow()
        {
            SOBulkEditor window = GetWindow<SOBulkEditor>("Bulk Editor");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadItems();
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawItemList();
            DrawBulkOperations();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("批量编辑器", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("类型:", GUILayout.Width(40));
            int selectedIndex = EditorGUILayout.Popup(
                System.Array.IndexOf(_soTypes, _selectedSOType),
                _soTypes,
                GUILayout.Width(100)
            );
            
            if (selectedIndex != System.Array.IndexOf(_soTypes, _selectedSOType))
            {
                _selectedSOType = _soTypes[selectedIndex];
                LoadItems();
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                LoadItems();
            }
            
            if (GUILayout.Button("全选", GUILayout.Width(60)))
            {
                ToggleSelectAll();
            }
            
            if (GUILayout.Button("取消全选", GUILayout.Width(80)))
            {
                ClearSelection();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        private void DrawItemList()
        {
            EditorGUILayout.LabelField($"选择要编辑的项目 (已选择: {GetSelectedCount()})", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box, GUILayout.Height(250));
            
            foreach (var kvp in _selectedItems.ToList())
            {
                EditorGUILayout.BeginHorizontal();
                
                _selectedItems[kvp.Key] = EditorGUILayout.Toggle(kvp.Value, GUILayout.Width(20));
                
                GUILayout.Label(kvp.Key, GUILayout.Width(80));
                
                // 获取显示名称
                string displayName = GetDisplayName(kvp.Key);
                GUILayout.Label(displayName);
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);
        }
        
        private void DrawBulkOperations()
        {
            EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            switch (_selectedSOType)
            {
                case "Item":
                    DrawItemBulkOperations();
                    break;
                case "Character":
                    DrawCharacterBulkOperations();
                    break;
                case "Skill":
                    DrawSkillBulkOperations();
                    break;
                case "Quest":
                    DrawQuestBulkOperations();
                    break;
            }
            
            EditorGUILayout.Space(10);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("应用批量修改", GUILayout.Height(40)))
            {
                ApplyBulkChanges();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawItemBulkOperations()
        {
            // 价格修改
            _modifyField1 = EditorGUILayout.ToggleLeft("修改购买价格", _modifyField1);
            if (_modifyField1)
            {
                _intValue1 = EditorGUILayout.IntField("购买价格", _intValue1);
            }
            
            _modifyField2 = EditorGUILayout.ToggleLeft("修改出售价格", _modifyField2);
            if (_modifyField2)
            {
                _intValue2 = EditorGUILayout.IntField("出售价格", _intValue2);
            }
            
            // 稀有度修改
            _modifyField3 = EditorGUILayout.ToggleLeft("修改稀有度", _modifyField3);
            if (_modifyField3)
            {
                _intValue1 = EditorGUILayout.IntSlider("稀有度", _intValue1, 0, 5);
            }
        }
        
        private void DrawCharacterBulkOperations()
        {
            _modifyField1 = EditorGUILayout.ToggleLeft("修改等级", _modifyField1);
            if (_modifyField1)
            {
                _intValue1 = EditorGUILayout.IntField("等级", _intValue1);
            }
            
            _modifyField2 = EditorGUILayout.ToggleLeft("修改生命值", _modifyField2);
            if (_modifyField2)
            {
                _intValue2 = EditorGUILayout.IntField("生命值", _intValue2);
            }
            
            _modifyField3 = EditorGUILayout.ToggleLeft("修改移动速度", _modifyField3);
            if (_modifyField3)
            {
                _floatValue1 = EditorGUILayout.FloatField("移动速度", _floatValue1);
            }
        }
        
        private void DrawSkillBulkOperations()
        {
            _modifyField1 = EditorGUILayout.ToggleLeft("修改法力消耗", _modifyField1);
            if (_modifyField1)
            {
                _intValue1 = EditorGUILayout.IntField("法力消耗", _intValue1);
            }
            
            _modifyField2 = EditorGUILayout.ToggleLeft("修改冷却时间", _modifyField2);
            if (_modifyField2)
            {
                _floatValue1 = EditorGUILayout.FloatField("冷却时间", _floatValue1);
            }
            
            _modifyField3 = EditorGUILayout.ToggleLeft("修改需求等级", _modifyField3);
            if (_modifyField3)
            {
                _intValue2 = EditorGUILayout.IntField("需求等级", _intValue2);
            }
        }
        
        private void DrawQuestBulkOperations()
        {
            _modifyField1 = EditorGUILayout.ToggleLeft("修改需求等级", _modifyField1);
            if (_modifyField1)
            {
                _intValue1 = EditorGUILayout.IntField("需求等级", _intValue1);
            }
            
            _modifyField2 = EditorGUILayout.ToggleLeft("修改经验奖励", _modifyField2);
            if (_modifyField2)
            {
                _intValue2 = EditorGUILayout.IntField("经验奖励", _intValue2);
            }
            
            _modifyField3 = EditorGUILayout.ToggleLeft("修改金币奖励", _modifyField3);
            if (_modifyField3)
            {
                _intValue1 = EditorGUILayout.IntField("金币奖励", _intValue1);
            }
        }
        
        private void ApplyBulkChanges()
        {
            List<string> selectedIds = GetSelectedIds();
            
            if (selectedIds.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择要修改的项目", "确定");
                return;
            }
            
            if (!EditorUtility.DisplayDialog("确认", $"确定要修改选中的 {selectedIds.Count} 个项目吗？", "确定", "取消"))
            {
                return;
            }
            
            int successCount = 0;
            
            foreach (string id in selectedIds)
            {
                bool success = ApplyChangesToItem(id);
                if (success) successCount++;
            }
            
            EditorUtility.DisplayDialog("完成", $"成功修改 {successCount}/{selectedIds.Count} 个项目", "确定");
            LoadItems();
        }
        
        private bool ApplyChangesToItem(string id)
        {
            switch (_selectedSOType)
            {
                case "Item":
                    return ApplyItemChanges(id);
                case "Character":
                    return ApplyCharacterChanges(id);
                case "Skill":
                    return ApplySkillChanges(id);
                case "Quest":
                    return ApplyQuestChanges(id);
                default:
                    return false;
            }
        }
        
        private bool ApplyItemChanges(string id)
        {
            ItemData data = SODatabase.LoadData<ItemData>("Item", id);
            if (data == null) return false;
            
            if (_modifyField1) data.BuyPrice = _intValue1;
            if (_modifyField2) data.SellPrice = _intValue2;
            if (_modifyField3) data.Rarity = _intValue1;
            
            // 保存修改
            ItemSO tempSO = ScriptableObject.CreateInstance<ItemSO>();
            tempSO.LoadFromSerializableData(data);
            return SODatabase.SaveScriptableObject(tempSO);
        }
        
        private bool ApplyCharacterChanges(string id)
        {
            CharacterData data = SODatabase.LoadData<CharacterData>("Character", id);
            if (data == null) return false;
            
            if (_modifyField1) data.Level = _intValue1;
            if (_modifyField2) data.MaxHealth = _intValue2;
            if (_modifyField3) data.MoveSpeed = _floatValue1;
            
            CharacterSO tempSO = ScriptableObject.CreateInstance<CharacterSO>();
            tempSO.LoadFromSerializableData(data);
            return SODatabase.SaveScriptableObject(tempSO);
        }
        
        private bool ApplySkillChanges(string id)
        {
            SkillData data = SODatabase.LoadData<SkillData>("Skill", id);
            if (data == null) return false;
            
            if (_modifyField1) data.ManaCost = _intValue1;
            if (_modifyField2) data.Cooldown = _floatValue1;
            if (_modifyField3) data.RequiredLevel = _intValue2;
            
            SkillSO tempSO = ScriptableObject.CreateInstance<SkillSO>();
            tempSO.LoadFromSerializableData(data);
            return SODatabase.SaveScriptableObject(tempSO);
        }
        
        private bool ApplyQuestChanges(string id)
        {
            QuestData data = SODatabase.LoadData<QuestData>("Quest", id);
            if (data == null) return false;
            
            if (_modifyField1) data.RequiredLevel = _intValue1;
            if (_modifyField2) data.Reward.Experience = _intValue2;
            if (_modifyField3) data.Reward.Gold = _intValue1;
            
            QuestSO tempSO = ScriptableObject.CreateInstance<QuestSO>();
            tempSO.LoadFromSerializableData(data);
            return SODatabase.SaveScriptableObject(tempSO);
        }
        
        private void LoadItems()
        {
            _selectedItems.Clear();
            
            List<string> ids = SODatabase.GetAllSOIds(_selectedSOType);
            foreach (string id in ids)
            {
                _selectedItems[id] = false;
            }
        }
        
        private void ToggleSelectAll()
        {
            _selectAll = !_selectAll;
            foreach (var key in _selectedItems.Keys.ToList())
            {
                _selectedItems[key] = _selectAll;
            }
        }
        
        private void ClearSelection()
        {
            _selectAll = false;
            foreach (var key in _selectedItems.Keys.ToList())
            {
                _selectedItems[key] = false;
            }
        }
        
        private int GetSelectedCount()
        {
            return _selectedItems.Count(kvp => kvp.Value);
        }
        
        private List<string> GetSelectedIds()
        {
            return _selectedItems.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
        
        private string GetDisplayName(string id)
        {
            // 从数据库加载显示名称
            switch (_selectedSOType)
            {
                case "Item":
                    var itemData = SODatabase.LoadData<ItemData>("Item", id);
                    return itemData?.DisplayName ?? id;
                case "Character":
                    var charData = SODatabase.LoadData<CharacterData>("Character", id);
                    return charData?.DisplayName ?? id;
                case "Skill":
                    var skillData = SODatabase.LoadData<SkillData>("Skill", id);
                    return skillData?.DisplayName ?? id;
                case "Quest":
                    var questData = SODatabase.LoadData<QuestData>("Quest", id);
                    return questData?.DisplayName ?? id;
                default:
                    return id;
            }
        }
    }
}
