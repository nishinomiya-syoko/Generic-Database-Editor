// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;
// using Equip;
// using System.IO;

// public class ItemEditorWindow : EditorWindow
// {
//     // 保持原有枚举和字段定义不变...
//     private enum ItemEditMode
//     {
//         Equip,
//         Consumable,
//         Material,
//         Quest,
//         Rune
//     }
//     private ItemEditMode _editMode = ItemEditMode.Equip;
//     private ItemEditMode _curMode = ItemEditMode.Equip;
//     private Vector2 _scrollPosition;
//     private int _selectedIndex = -1;
//     private Vector2 _scrollPositionRight;

//     private BaseInventoryItemSO _selectedItem;
//     private List<BaseInventoryItemSO> _items = new List<BaseInventoryItemSO>();
//     private List<BaseInventoryItemSO> _itemsMatched = new List<BaseInventoryItemSO>();

//     [MenuItem("Window/ItemEditorWindow")]
//     public static void ShowWindow()
//     {
//         var window = GetWindow<ItemEditorWindow>();
//         window.titleContent = new GUIContent("InventoryItemEditorWindow");
//     }

//     private void OnEnable()
//     {
//         LoadAllAssets();
//         ChangeMode();
//     }

//     private void OnGUI()
//     {
//         DrawToolbar();
//         DrawMainContent();
//     }

//     // 保持原有工具栏、模式切换等方法不变...
//     private void DrawToolbar()
//     {
//         GUILayout.BeginHorizontal(EditorStyles.toolbar);

//         _editMode = (ItemEditMode)GUILayout.Toolbar((int)_editMode,
//             new[] { "Equip", "Consumable", "Material", "Quest", "Rune" },
//             GUILayout.Width(400));

//         if (_editMode != _curMode)
//         {
//             ChangeMode();
//         }

//         if (GUILayout.Button("Create New", EditorStyles.toolbarButton))
//         {
//             CreateItem();
//         }
//         Color originalColor = GUI.color;
//         GUI.color = Color.green;
//         if (GUILayout.Button("Save", EditorStyles.toolbarButton))
//         {
//             SaveAllAssets();
//         }
//         GUI.color = originalColor;

//         GUILayout.EndHorizontal();
//     }

//     private void ChangeMode()
//     {
//         _curMode = _editMode;
//         _itemsMatched.Clear();
//         _selectedIndex = -1;
//         _selectedItem = null;

//         foreach (var item in _items)
//         {
//             if (item == null) continue;

//             switch (_curMode)
//             {
//                 case ItemEditMode.Equip:
//                     if (item is EquipmentInventoryItemSO)
//                         _itemsMatched.Add(item);
//                     break;
//                 case ItemEditMode.Consumable:
//                     if (item is ConsumableInventoryItemSO)
//                         _itemsMatched.Add(item);
//                     break;
//                 case ItemEditMode.Material:
//                     if (item is MaterialInventoryItemSO)
//                         _itemsMatched.Add(item);
//                     break;
//                 case ItemEditMode.Quest:
//                     if (item is TaskInventoryItemSO)
//                         _itemsMatched.Add(item);
//                     break;
//                 case ItemEditMode.Rune:
//                     if (item is RuneInventoryItemSO)
//                         _itemsMatched.Add(item);
//                     break;
//             }
//         }
//     }

//     private void DrawMainContent()
//     {
//         GUILayout.BeginHorizontal();
//         DrawAssetList();
//         DrawItemPanel();
//         GUILayout.EndHorizontal();
//     }

//     private void DrawAssetList()
//     {
//         GUILayout.BeginVertical(GUILayout.Width(250));
//         _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

//         for (int i = 0; i < _itemsMatched.Count; i++)
//         {
//             var item = _itemsMatched[i];
//             if (item == null) continue;

//             GUILayout.BeginHorizontal();
//             bool isSelected = item == _selectedItem;
//             if (GUILayout.Toggle(isSelected, item.itemName, EditorStyles.radioButton, GUILayout.Width(180)))
//             {
//                 _selectedIndex = i;
//                 _selectedItem = item;
//             }
//             if (GUILayout.Button("X", GUILayout.Width(20)))
//             {
//                 if (EditorUtility.DisplayDialog("Delete Item", $"Are you sure you want to delete {item.itemName}?", "Yes", "No"))
//                 {
//                     DeleteItem(item);
//                     _selectedItem = _itemsMatched[_itemsMatched.Count - 1];
//                 }
//             }
//             GUILayout.EndHorizontal();
//         }

//         EditorGUILayout.EndScrollView();
//         GUILayout.EndVertical();
//     }
//     private SerializedObject _serializedItem;

//     private void DrawItemPanel()
//     {
//         if (_selectedItem == null)
//         {
//             GUILayout.Label("Select an item to edit");
//             return;
//         }
//         if (_serializedItem == null || _serializedItem.targetObject != _selectedItem)
//             _serializedItem = new SerializedObject(_selectedItem);

//         _serializedItem.Update();

//         SerializedProperty iterator = _serializedItem.GetIterator();
//         bool enterChildren = true;

//         EditorGUILayout.BeginVertical("box");
//         _scrollPositionRight = EditorGUILayout.BeginScrollView(_scrollPositionRight);
//         _selectedItem.itemIcon = (Sprite)EditorGUILayout.ObjectField("Item Icon", _selectedItem.itemIcon, typeof(Sprite), false);
//         GUILayout.Label("Item Properties");
//         _selectedItem.levelTypeEnum = (ItemLevelTypeEnum)GUILayout.Toolbar((int)_selectedItem.levelTypeEnum, new[] { "General", "Good", "Excellent", "Uncommon", "Tale", "Epic", "DarkGold" });


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


//     // 保持原有保存、加载、删除、创建方法不变...
//     private void SaveAllAssets()
//     {
//         foreach (var item in _items)
//         {
//             if (item != null)
//                 EditorUtility.SetDirty(item);
//         }
//         AssetDatabase.SaveAssets();
//         AssetDatabase.Refresh();
//         Debug.Log("All assets saved");
//     }

//     private void LoadAllAssets()
//     {
//         _items.Clear();
//         _itemsMatched.Clear();
//         if (Directory.Exists(Constant.ITEM_PATH))
//         {
//             _items = AssetLoader.LoadAllAssetsInFolder(Constant.ITEM_PATH).OfType<BaseInventoryItemSO>().ToList();
//             Debug.Log($"Loaded {_items.Count} items from {Constant.ITEM_PATH}");
//         }
//         else
//         {
//             Debug.LogError($"Item path not found: {Constant.ITEM_PATH}");
//         }
//     }

//     private void DeleteItem(BaseInventoryItemSO obj)
//     {
//         if (obj == null) return;

//         string path = AssetDatabase.GetAssetPath(obj);
//         if (!string.IsNullOrEmpty(path))
//         {
//             AssetDatabase.DeleteAsset(path);
//             AssetDatabase.SaveAssets();
//             AssetDatabase.Refresh();
//         }
//         _items.Remove(obj);
//         _itemsMatched.Remove(obj);
//         Debug.Log($"Deleted Item: {obj.itemName}");
//     }

//     private void CreateItem()
//     {
//         BaseInventoryItemSO obj = null;
//         string defaultName = "";

//         switch (_curMode)
//         {
//             case ItemEditMode.Equip:
//                 obj = ScriptableObject.CreateInstance<EquipmentInventoryItemSO>();
//                 defaultName = "New Equip";
//                 break;
//             case ItemEditMode.Consumable:
//                 obj = ScriptableObject.CreateInstance<ConsumableInventoryItemSO>();
//                 defaultName = "New Consumable";
//                 break;
//             case ItemEditMode.Rune:
//                 obj = ScriptableObject.CreateInstance<RuneInventoryItemSO>();
//                 defaultName = "New Rune";
//                 break;
//             case ItemEditMode.Quest:
//                 obj = ScriptableObject.CreateInstance<TaskInventoryItemSO>();
//                 defaultName = "New Quest";
//                 break;
//             case ItemEditMode.Material:
//                 obj = ScriptableObject.CreateInstance<MaterialInventoryItemSO>();
//                 defaultName = "New Material";
//                 break;
//         }

//         if (obj != null)
//         {
//             obj.itemName = defaultName;
//             if (!Directory.Exists(Constant.ITEM_PATH))
//                 Directory.CreateDirectory(Constant.ITEM_PATH);

//             string path = $"{Constant.ITEM_PATH}{obj.itemName}.asset";
//             path = AssetDatabase.GenerateUniqueAssetPath(path);
//             AssetDatabase.CreateAsset(obj, path);
//             AssetDatabase.SaveAssets();

//             _items.Add(obj);
//             ChangeMode();
//             _selectedItem = obj;
//             Debug.Log($"Created new {_curMode}: {path}");
//         }
//     }
// }