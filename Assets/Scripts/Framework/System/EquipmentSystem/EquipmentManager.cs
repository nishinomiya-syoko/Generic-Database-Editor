using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 装备管理器
    public class EquipmentManager : MonoBehaviour
    {
        [Header("装备设置")]
        public EquipmentDatabase equipmentDatabase;
        public int maxInventorySize = 100;

        private List<PlayerEquipment> inventory = new List<PlayerEquipment>();
        private Dictionary<EquipmentType, string> equippedItems = new Dictionary<EquipmentType, string>();

        // 事件
        public event Action<PlayerEquipment> OnEquipmentObtained;
        public event Action<PlayerEquipment> OnEquipmentEquipped;
        public event Action<PlayerEquipment> OnEquipmentUnequipped;
        public event Action<PlayerEquipment> OnEquipmentEnhanced;

        void Start()
        {
            LoadEquipmentData();
        }

        #region 装备获取

        public void AddEquipment(string equipmentId)
        {
            if (inventory.Count >= maxInventorySize)
            {
                Debug.LogWarning("装备背包已满！");
                return;
            }

            var equipmentData = equipmentDatabase?.GetEquipmentData(equipmentId);
            if (equipmentData == null)
                return;

            var playerEquipment = new PlayerEquipment(equipmentId);

            // 随机附加技能
            if (equipmentData.attachedSkills != null && equipmentData.attachedSkills.Length > 0)
            {
                if (UnityEngine.Random.Range(0f, 1f) <= equipmentData.skillAttachProbability)
                {
                    string randomSkill = equipmentData.attachedSkills[UnityEngine.Random.Range(0, equipmentData.attachedSkills.Length)];
                    playerEquipment.AttachSkill(randomSkill);
                }
            }

            inventory.Add(playerEquipment);
            OnEquipmentObtained?.Invoke(playerEquipment);

            GlobalManager.Instance?.EventManager?.OnEquipmentGet?.Invoke(equipmentData);
            // GlobalManager.Instance?.UIManager?.ShowNotification($"获得装备: {equipmentData.equipmentName}");
        }

        public void AddEquipmentWithQuality(EquipmentType type, EquipmentQuality quality)
        {
            var equipmentList = equipmentDatabase?.GetEquipmentByTypeAndQuality(type, quality);
            if (equipmentList != null && equipmentList.Count > 0)
            {
                var randomEquipment = equipmentList[UnityEngine.Random.Range(0, equipmentList.Count)];
                AddEquipment(randomEquipment.id);
            }
        }

        #endregion

        #region 装备穿戴

        public bool EquipItem(string equipmentId)
        {
            var equipment = inventory.Find(e => e.equipmentId == equipmentId);
            if (equipment == null || equipment.isEquipped)
                return false;

            var equipmentData = equipmentDatabase.GetEquipmentData(equipmentId);
            if (equipmentData == null)
                return false;

            // 检查等级要求
            int playerLevel = GlobalManager.Instance?.LevelManager?.playerLevel ?? 1;
            if (playerLevel < equipmentData.requiredLevel)
            {
                GlobalManager.Instance?.UIManager?.ShowNotification("等级不足，无法装备");
                return false;
            }

            // 卸下同类型装备
            if (equippedItems.ContainsKey(equipmentData.equipmentType))
            {
                UnequipItem(equippedItems[equipmentData.equipmentType]);
            }

            // 装备新物品
            equipment.isEquipped = true;
            equippedItems[equipmentData.equipmentType] = equipmentId;

            OnEquipmentEquipped?.Invoke(equipment);
            // ApplyEquipmentStats(equipmentData);

            return true;
        }

        public bool UnequipItem(string equipmentId)
        {
            var equipment = inventory.Find(e => e.equipmentId == equipmentId);
            if (equipment == null || !equipment.isEquipped)
                return false;

            var equipmentData = equipmentDatabase.GetEquipmentData(equipmentId);
            if (equipmentData == null)
                return false;

            equipment.isEquipped = false;
            equippedItems.Remove(equipmentData.equipmentType);

            OnEquipmentUnequipped?.Invoke(equipment);
            // RemoveEquipmentStats(equipmentData);

            return true;
        }

        #endregion

        #region 属性应用

        // private void ApplyEquipmentStats(EquipmentData equipmentData)
        // {
        //     // 应用装备属性到玩家
        //     var unitManager = GlobalManager.Instance?.entityManager;
        //     if (unitManager != null)
        //     {
        //         // 这里可以应用到英雄单位或全局属性
        //         foreach (var unit in unitManager.ActiveUnits)
        //         {
        //             if (unit.data.unitType == UnitType.Hero)
        //             {
        //                 unit.AddEquipmentStats(equipmentData);
        //             }
        //         }
        //     }
        // }

        // private void RemoveEquipmentStats(EquipmentData equipmentData)
        // {
        //     // 移除装备属性
        //     var unitManager = GlobalManager.Instance?.UnitManager;
        //     if (unitManager != null)
        //     {
        //         foreach (var unit in unitManager.ActiveUnits)
        //         {
        //             if (unit.data.unitType == UnitType.Hero)
        //             {
        //                 unit.RemoveEquipmentStats(equipmentData);
        //             }
        //         }
        //     }
        // }

        #endregion

        #region 装备强化

        public bool EnhanceEquipment(string equipmentId)
        {
            var equipment = inventory.Find(e => e.equipmentId == equipmentId);
            if (equipment == null)
                return false;

            var equipmentData = equipmentDatabase.GetEquipmentData(equipmentId);
            if (equipmentData == null)
                return false;

            // 检查强化材料
            var resourceManager = GlobalManager.Instance?.ResourceManager;
            if (resourceManager != null)
            {
                int enhanceCost = GetEnhancementCost(equipment.enhancementLevel + 1);
                if (!resourceManager.HasEnoughResource(ResourceType.Gold, enhanceCost))
                {
                    GlobalManager.Instance?.UIManager?.ShowNotification("金币不足");
                    return false;
                }

                resourceManager.SpendResource(ResourceType.Gold, enhanceCost);
            }

            equipment.Enhance();
            OnEquipmentEnhanced?.Invoke(equipment);

            GlobalManager.Instance?.UIManager?.ShowNotification($"装备强化成功！+{equipment.enhancementLevel}");

            return true;
        }

        private int GetEnhancementCost(int level)
        {
            return 100 * level * level; // 强化成本随等级平方增长
        }

        #endregion

        #region 套装效果

        public bool CheckSetBonus()
        {
            var equippedSets = new Dictionary<string, int>();

            foreach (var equipmentId in equippedItems.Values)
            {
                var equipment = inventory.Find(e => e.equipmentId == equipmentId);
                if (equipment != null)
                {
                    var equipmentData = equipmentDatabase.GetEquipmentData(equipmentId);
                    if (!string.IsNullOrEmpty(equipmentData.setEffectId))
                    {
                        if (!equippedSets.ContainsKey(equipmentData.setEffectId))
                            equippedSets[equipmentData.setEffectId] = 0;
                        equippedSets[equipmentData.setEffectId]++;
                    }
                }
            }

            // 检查套装效果
            foreach (var set in equippedSets)
            {
                if (set.Value >= 2) // 2件套效果
                {
                    ApplySetBonus(set.Key, set.Value);
                }
            }

            return true;
        }

        private void ApplySetBonus(string setEffectId, int pieceCount)
        {
            // 应用套装效果
            Debug.Log($"激活套装效果: {setEffectId}, 件数: {pieceCount}");
        }

        #endregion

        #region 数据持久化

        public void SaveEquipmentData()
        {
            var saveData = new EquipmentSaveData
            {
                inventory = inventory,
                equippedItems = new Dictionary<EquipmentType, string>(equippedItems)
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("EquipmentData", json);
        }

        public void LoadEquipmentData()
        {
            if (PlayerPrefs.HasKey("EquipmentData"))
            {
                string json = PlayerPrefs.GetString("EquipmentData");
                var saveData = JsonUtility.FromJson<EquipmentSaveData>(json);

                if (saveData != null)
                {
                    inventory = saveData.inventory ?? new List<PlayerEquipment>();
                    equippedItems = saveData.equippedItems ?? new Dictionary<EquipmentType, string>();
                }
            }
        }

        #endregion

        #region 查询方法

        public List<PlayerEquipment> GetInventory()
        {
            return new List<PlayerEquipment>(inventory);
        }

        public List<PlayerEquipment> GetEquippedItems()
        {
            var equipped = new List<PlayerEquipment>();
            foreach (var equipmentId in equippedItems.Values)
            {
                var equipment = inventory.Find(e => e.equipmentId == equipmentId);
                if (equipment != null)
                {
                    equipped.Add(equipment);
                }
            }
            return equipped;
        }

        public PlayerEquipment GetEquipment(string equipmentId)
        {
            return inventory.Find(e => e.equipmentId == equipmentId);
        }

        public bool IsEquipped(string equipmentId)
        {
            var equipment = inventory.Find(e => e.equipmentId == equipmentId);
            return equipment != null && equipment.isEquipped;
        }

        #endregion

        [System.Serializable]
        private class EquipmentSaveData
        {
            public List<PlayerEquipment> inventory;
            public Dictionary<EquipmentType, string> equippedItems;
        }
    }
}