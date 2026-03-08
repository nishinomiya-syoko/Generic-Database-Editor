   using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Top
{ 
    // 单位数据库
    [CreateAssetMenu(fileName = "UnitDatabase", menuName = "Top/Unit Database")]
    public class UnitDatabase : ScriptableObject
    {
        public List<UnitData> units = new List<UnitData>();

        public UnitData GetUnitData(string unitId)
        {
            return units.Find(u => u.id == unitId);
        }

        public List<UnitData> GetUnitsByType(UnitType type)
        {
            return units.FindAll(u => u.unitType == type);
        }
    }
}