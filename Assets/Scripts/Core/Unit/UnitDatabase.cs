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
        public List<UnitDataSO> units = new List<UnitDataSO>();

        public UnitDataSO GetUnitData(string unitId)
        {
            return units.Find(u => u.id == unitId);
        }

        public List<UnitDataSO> GetUnitsByType(UnitType type)
        {
            return units.FindAll(u => u.unitType == type);
        }
    }
}