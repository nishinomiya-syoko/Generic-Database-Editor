using UnityEditor;
using UnityEngine;
using System.IO;

public static class SOTExcelMenu
{
    // 生成BuildingDataSO的Excel
    [MenuItem("Tools/SO Excel/Generate Building Excel")]
    public static void GenerateBuildingExcel()
    {
        SOTExcelTool.GenerateExcelFromSO<Moon>();
    }

    // 从Excel加载数据到BuildingDataSO
    [MenuItem("Tools/SO Excel/Load Building From Excel")]
    public static void LoadBuildingFromExcel()
    {
        // SO保存路径（可自定义）
        string soSavePath = Path.Combine(Application.dataPath, "Resources/Buildings");
        SOTExcelTool.LoadSOFromExcel<Moon>(soSavePath);
    }
}