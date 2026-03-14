using UnityEngine;

// 所有可导出Excel的SO基类
public class ExcelableSO : ScriptableObject
{
    // 可选：定义Excel文件名称（子类可重写）
    public virtual string GetExcelFileName()
    {
        return GetType().Name + "Data.xlsx";
    }
}

// // 示例：你的BuildingDataSO（继承基类）
// [CreateAssetMenu(fileName = "BuildingData", menuName = "Top/Building Data")]
// public class BuildingDataSO : ExcelableSO
// {
//     [Header("基础信息")]
//     public string Id;
//     public string DisplayName;
//     public BuildingType buildingType; // 枚举
//     public Vector2Int size = new Vector2Int(2, 2);
//     public int buildTime = 60;
//     public float range = 5f;

//     // 重写Excel文件名（可选）
//     public override string GetExcelFileName()
//     {
//         return "BuildingData.xlsx";
//     }
// }

// 辅助枚举/类（示例）
public enum BuildingType { House, Factory, Defense }
[System.Serializable]
public class ResourceCost // 嵌套类示例
{
    public string resourceType;
    public int amount;
}