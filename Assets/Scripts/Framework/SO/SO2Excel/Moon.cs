using UnityEngine;

[CreateAssetMenu(fileName = "Moon", menuName = "Moon")]
public class Moon : ExcelableSO
{
    [Header("基础信息")]
    public string Id;
    public string DisplayName;
    public BuildingType buildingType; // 枚举
    public Vector2Int size = new Vector2Int(2, 2);
    public int buildTime = 60;
    public float range = 5f;

    // 重写Excel文件名（可选）
    public override string GetExcelFileName()
    {
        return "Moon.xlsx";
    }
}