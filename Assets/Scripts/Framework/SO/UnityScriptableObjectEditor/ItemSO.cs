using System;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material,
        QuestItem,
        Miscellaneous
    }
    
    /// <summary>
    /// 物品 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "SO Editor/Item")]
    public class ItemSO : ScriptableObjectBase
    {
        [Header("物品属性")]
        [SerializeField] private ItemType _itemType = ItemType.Miscellaneous;
        [SerializeField] private int _maxStackSize = 99;
        [SerializeField] private int _buyPrice;
        [SerializeField] private int _sellPrice;
        [SerializeField] private bool _isUsable;
        [SerializeField] private bool _isEquipable;
        [SerializeField] private int _rarity; // 0-5, 越高越稀有
        
        [Header("装备属性")]
        [SerializeField] private int _attackBonus;
        [SerializeField] private int _defenseBonus;
        [SerializeField] private int _healthBonus;
        [SerializeField] private int _manaBonus;
        
        // 属性访问器
        public ItemType ItemType => _itemType;
        public int MaxStackSize => _maxStackSize;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;
        public bool IsUsable => _isUsable;
        public bool IsEquipable => _isEquipable;
        public int Rarity => _rarity;
        public int AttackBonus => _attackBonus;
        public int DefenseBonus => _defenseBonus;
        public int HealthBonus => _healthBonus;
        public int ManaBonus => _manaBonus;
        
        public override string GetSOType() => "Item";
        
        public override object GetSerializableData()
        {
            return new ItemData
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                IconPath = Icon != null ? Icon.name : "",
                ItemType = _itemType,
                MaxStackSize = _maxStackSize,
                BuyPrice = _buyPrice,
                SellPrice = _sellPrice,
                IsUsable = _isUsable,
                IsEquipable = _isEquipable,
                Rarity = _rarity,
                AttackBonus = _attackBonus,
                DefenseBonus = _defenseBonus,
                HealthBonus = _healthBonus,
                ManaBonus = _manaBonus
            };
        }
        
        public override void LoadFromSerializableData(object data)
        {
            if (data is ItemData itemData)
            {
                SetId(itemData.Id);
                SetDisplayName(itemData.DisplayName);
                SetDescription(itemData.Description);
                _itemType = itemData.ItemType;
                _maxStackSize = itemData.MaxStackSize;
                _buyPrice = itemData.BuyPrice;
                _sellPrice = itemData.SellPrice;
                _isUsable = itemData.IsUsable;
                _isEquipable = itemData.IsEquipable;
                _rarity = itemData.Rarity;
                _attackBonus = itemData.AttackBonus;
                _defenseBonus = itemData.DefenseBonus;
                _healthBonus = itemData.HealthBonus;
                _manaBonus = itemData.ManaBonus;
            }
        }
        
        // 设置方法（用于编辑器）
        public void SetItemType(ItemType type) => _itemType = type;
        public void SetMaxStackSize(int size) => _maxStackSize = size;
        public void SetBuyPrice(int price) => _buyPrice = price;
        public void SetSellPrice(int price) => _sellPrice = price;
        public void SetIsUsable(bool usable) => _isUsable = usable;
        public void SetIsEquipable(bool equipable) => _isEquipable = equipable;
        public void SetRarity(int rarity) => _rarity = rarity;
        public void SetAttackBonus(int bonus) => _attackBonus = bonus;
        public void SetDefenseBonus(int bonus) => _defenseBonus = bonus;
        public void SetHealthBonus(int bonus) => _healthBonus = bonus;
        public void SetManaBonus(int bonus) => _manaBonus = bonus;
    }
    
    /// <summary>
    /// 物品序列化数据
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string IconPath;
        public ItemType ItemType;
        public int MaxStackSize;
        public int BuyPrice;
        public int SellPrice;
        public bool IsUsable;
        public bool IsEquipable;
        public int Rarity;
        public int AttackBonus;
        public int DefenseBonus;
        public int HealthBonus;
        public int ManaBonus;
    }
}
