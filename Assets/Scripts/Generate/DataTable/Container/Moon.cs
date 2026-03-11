namespace Top
{

 using UnityEngine;
 using System;

[Serializable]
 public class Moon
 {
    public String Id;
    public String DisplayName;
    public BuildingType buildingType;
    public UnityEngine.Vector2Int size;
    public Int32 buildTime;
    public Single range;

    public Moon() { }

    public Moon(Moon so)
    {
        Id = so.Id;
        DisplayName = so.DisplayName;
        buildingType = so.buildingType;
        size = so.size;
        buildTime = so.buildTime;
        range = so.range;
    }
}
}
