using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Top
{
    // 建筑数据库
    // [CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Top/Building Database")]
    public class BuildingDatabase
    {
        public List<BuildingData> buildings = new List<BuildingData>();

        public BuildingData GetBuildingData(string buildingId)
        {
            return buildings.Find(b => b.Id == buildingId);
        }

        public List<BuildingData> GetBuildingsByType(BuildingType type)
        {
            return buildings.FindAll(b => b.buildingType == type);
        }
    }
}