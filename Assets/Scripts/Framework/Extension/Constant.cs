using UnityEngine;
public static class Constant
{
    // 数据表路径
    public static readonly string EXCEL_PATH = Application.dataPath + "/Data/Excel/";
    public static readonly string DATA_CLASS_PATH = Application.dataPath + "/Scripts/Generate/DataTable/";
    public static readonly string DATA_CONTAINER_PATH = Application.dataPath + "/Scripts/Generate/DataTable/Container/";
    // public static readonly string DATA_BINARY_PATH = Application.streamingAssetsPath + "/Bianry/";
    public static readonly string DATA_BINARY_PATH = Application.dataPath + "/Data/Binary/";
    public static readonly string DATA_TXT_PATH = Application.dataPath + "/Data/TXT/";
    public static readonly string JSON_PATH = Application.dataPath + "/Res/Database";
    // 跳过的文件名前缀
    public static readonly string[] SKIP_PREFIXES = { "~", "$", "_" };

    // 技能
    public const string SKILL_PATH = "Assets/Res/SO/Skills/";
    public const string BUILDING_PATH = "Assets/Res/SO/Buildings/";
    public const string UNIT_PATH = "Assets/Res/SO/Units/";
    public const string EQUIPMENT_PATH = "Assets/Res/SO/Equipments/";
    public const string TECH_PATH = "Assets/Res/SO/Techs/";
    public const string ITEM_PATH = "Assets/Res/SO/Items/";
    public const string QUEST_PATH = "Assets/Res/SO/Quests/";
    public static readonly string SO_PATH = "Assets/Res/SO/";

   
    // 实体
    public const string ENTITY_PATH = "Assets/Res/SO/Entities/";

    public static float DEFAULT_DELTA_TIME = 0.1f;
    public static float LONG_DELTA_TIME = 0.5f;

    public static int PLACED_PATH_COST = 100;
}