using UnityEditor;
using UnityEngine;
using System.IO;
using Top;

public static class SOTExcelMenu
{
    // 从Excel加载数据到BuildingDataSO
    [MenuItem("Tools/SO2Excel/Load SO From Excel")]
    public static void LoadSOFromExcel()
    {
        // SO保存路径（可自定义）
        // string soSavePath = Path.Combine(Application.dataPath, "Resources/Buildings");
        // SOTExcelTool.LoadSOFromExcel<Moon>(soSavePath);
        string SOpath = Constant.SO_PATH + "Moon";
        SOTExcelTool.LoadSOFromExcel<Moon>(SOpath);
    }
    [MenuItem("Tools/SO2Excel/Generate Moon Excel")]
    public static void GenerateMoonExcel()
    {
        SOTExcelTool.GenerateExcelFromSO<Moon>();
        
    }
}