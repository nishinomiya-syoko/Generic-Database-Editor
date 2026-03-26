namespace Top
{
    public static class GM
    {
        public static GlobalManager GlobalManager => GlobalManager.Instance;

        public static AssetLoader Asset => GlobalManager.AssetLoader;

        public static GridManager GridManager => GlobalManager.GridManager;
        public static MapManager MapManager => GlobalManager.MapManager;
        public static AudioManager AudioManager => GlobalManager.AudioManager;

        public static UIManager UIManager => GlobalManager.UIManager;

        public static ProcedureManager ProcedureManager => GlobalManager.ProcedureManager;

        public static ModuleManager ModuleManager => GlobalManager.ModuleManager;


        public static DataCenter.TableManager DataTableManager => GlobalManager.DataTableManager;

        public static LevelManager LevelManager => GlobalManager.LevelManager;
        public static EntityManager EntityManager => GlobalManager.EntityManager;
        public static WaveManager WaveManager => GlobalManager.WaveManager;
        public static Logic.PoolManager PoolManager => GlobalManager.PoolManager;
        public static BuildingManager BuildingManager => GlobalManager.BuildingManager;
        public static UnitManager UnitManager => GlobalManager.UnitManager;

        public static RTS.TargetSearch.BatchedSearchManager BatchedSearchManager => GlobalManager.BatchedSearchManager;

        public static EventManager EventManager => GlobalManager.EventManager;
        public static ResourceManager ResourceManager => GlobalManager.ResourceManager;
        public static QuestManager QuestManager => GlobalManager.QuestManager;
    }
}
// public static class GM
//     {
//         public static GlobalManager Global => GlobalManager.Instance;

//         public static AssetLoader Asset => Global.AssetLoader;

//         public static GridManager Grid => Global.GridManager;
//         public static MapManager Map => Global.MapManager;
//         public static AudioManager Audio => Global.AudioManager;

//         public static UIManager UI => Global.UIManager;

//         public static ProcedureManager Procedure => Global.ProcedureManager;

//         public static ModuleManager Module => Global.ModuleManager;


//         public static DataCenter.TableManager DataTable => Global.DataTableManager;

//         public static LevelManager LevelManager     => Global.LevelManager;
//         public static EntityManager Entity => Global.EntityManager;
//         public static WaveManager Wave => Global.WaveManager;
//         public static Logic.PoolManager Pool => Global.PoolManager;
//         public static BuildingManager Building => Global.BuildingManager;
//         public static UnitManager Unit => Global.UnitManager;

//         public static RTS.TargetSearch.BatchedSearchManager BatchedSearch => Global.BatchedSearchManager;

//         public static EventManager Event => Global.EventManager;
//         public static ResourceManager Resource => Global.ResourceManager;
//         public static QuestManager Quest => Global.QuestManager;
//     }