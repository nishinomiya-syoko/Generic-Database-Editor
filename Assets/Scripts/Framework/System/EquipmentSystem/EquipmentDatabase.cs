using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 装备数据库
    [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Top/Equipment Database")]
    public class EquipmentDatabase : ScriptableObject
    {
        public List<EquipmentDataSO> equipment = new List<EquipmentDataSO>();

        public EquipmentDataSO GetEquipmentData(string equipmentId)
        {
            return equipment.Find(e => e.id == equipmentId);
        }

        public List<EquipmentDataSO> GetEquipmentByType(EquipmentType type)
        {
            return equipment.FindAll(e => e.equipmentType == type);
        }

        public List<EquipmentDataSO> GetEquipmentByTypeAndQuality(EquipmentType type, EquipmentQuality quality)
        {
            return equipment.FindAll(e => e.equipmentType == type && e.quality == quality);
        }
    }
}