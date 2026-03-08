using UnityEngine;
public static class Constant
{
    // 数据表路径
    public static string EXCEL_PATH = Application.dataPath + "/Data/Excel/";
    public static string DATA_CLASS_PATH = Application.dataPath + "/Scripts/DataTable/DataClass/";
    public static string DATA_CONTAINER_PATH = Application.dataPath + "/Scripts/DataTable/Container/";

    // 技能
    public const string SKILL_PATH = "Assets/ConfigData/Resources/SO/Skills/";
   
    // 实体
    public const string ENTITY_PATH = "Assets/ConfigData/Resources/SO/Entities/";

    public static float DEFAULT_DELTA_TIME = 0.1f;
    public static float LONG_DELTA_TIME = 0.5f;

    public static int PLACED_PATH_COST = 100;
}