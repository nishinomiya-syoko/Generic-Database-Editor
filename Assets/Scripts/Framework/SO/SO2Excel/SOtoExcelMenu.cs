using UnityEditor;
using UnityEngine;
using System.IO;
using Top;

public static class SOTExcelMenu
{
    // 生成BuildingDataSO的Excel
    [MenuItem("Tools/SO Excel/Generate Building Excel")]
    public static void GenerateBuildingExcel()
    {
        SOTExcelTool.GenerateExcelFromSO<BuildingDataSO>();
        // SOTExcelTool.GenerateExcelFromSO<UnitDataSO>();
        // SOTExcelTool.GenerateExcelFromSO
        // SOTExcelTool.GenerateExcelFromSO
        // SOTExcelTool.GenerateExcelFromSO
        // SOTExcelTool.GenerateExcelFromSO
    }

    // 从Excel加载数据到BuildingDataSO
    [MenuItem("Tools/SO Excel/Load Building From Excel")]
    public static void LoadBuildingFromExcel()
    {
        // SO保存路径（可自定义）
        string soSavePath = Path.Combine(Application.dataPath, "Resources/Buildings");
        // SOTExcelTool.LoadSOFromExcel<Moon>(soSavePath);
    }
    [MenuItem("Tools/SO Excel/Generate Moon Excel")]
    public static void GenerateMoonExcel()
    {
    }
}