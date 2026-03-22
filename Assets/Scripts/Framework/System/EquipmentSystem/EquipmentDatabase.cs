using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 装备数据库
    // [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Top/Equipment Database")]
    public class EquipmentDatabase 
    {
        public List<EquipmentData> equipment = new List<EquipmentData>();

        public EquipmentData GetEquipmentData(string equipmentId)
        {
            return equipment.Find(e => e.id == equipmentId);
        }

        public List<EquipmentData> GetEquipmentByType(EquipmentType type)
        {
            return equipment.FindAll(e => e.equipmentType == type);
        }

        public List<EquipmentData> GetEquipmentByTypeAndQuality(EquipmentType type, EquipmentQuality quality)
        {
            return equipment.FindAll(e => e.equipmentType == type && e.quality == quality);
        }
    }
}