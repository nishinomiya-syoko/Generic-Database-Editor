using UnityEngine;
using System;

[Serializable]
public class ItemData
{
    [SerializeField] SOEditor.ItemType _itemType;
    [SerializeField] Int32 _maxStackSize;
    [SerializeField] Int32 _buyPrice;
    [SerializeField] Int32 _sellPrice;
    [SerializeField] Boolean _isUsable;
    [SerializeField] Boolean _isEquipable;
    [SerializeField] Int32 _rarity;
    [SerializeField] Int32 _attackBonus;
    [SerializeField] Int32 _defenseBonus;
    [SerializeField] Int32 _healthBonus;
    [SerializeField] Int32 _manaBonus;

    public ItemData() { }

    public ItemData(ItemData so)
    {
        _itemType = so._itemType;
        _maxStackSize = so._maxStackSize;
        _buyPrice = so._buyPrice;
        _sellPrice = so._sellPrice;
        _isUsable = so._isUsable;
        _isEquipable = so._isEquipable;
        _rarity = so._rarity;
        _attackBonus = so._attackBonus;
        _defenseBonus = so._defenseBonus;
        _healthBonus = so._healthBonus;
        _manaBonus = so._manaBonus;
    }
}
