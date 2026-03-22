using Logic;
using UnityEngine;
using Top;
using DataCenter;
using RTS.TargetSearch;
using Cysharp.Threading.Tasks;

public class GlobalManager : Singleton<GlobalManager>
{
    [Header("地图系统")]
    public GridManager GridManager;
    public MapManager MapManager;
    public AudioManager AudioManager;
    [Header("流程")]
    public ProcedureManager ProcedureManager;
    [Header("模块")]
    public ModuleManager ModuleManager;

    [Header("游戏系统")]
    public TableManager DataTableManager;

    [Header("关卡系统")]
    public LevelManager LevelManager;
    public EntityManager EntityManager;
    public WaveManager WaveManager;
    public PoolManager PoolManager;
    public BuildingManager BuildingManager;

    [Header("")]
    public BatchedSearchManager BatchedSearchManager;

    public EventManager EventManager;
    public ResourceManager ResourceManager;
    public QuestManager QuestManager;
    public UIManager UIManager;


    public AssetLoader AssetLoader;

    public int level = 1;

    public override void Awake()
    {
        base.Awake();
        ModuleManager = GetComponentInChildren<ModuleManager>();
        ProcedureManager = GetComponentInChildren<ProcedureManager>();

        LevelManager = GetComponentInChildren<LevelManager>();
        EntityManager = GetComponentInChildren<EntityManager>();
        WaveManager = GetComponentInChildren<WaveManager>();
        PoolManager = GetComponentInChildren<PoolManager>();

        AssetLoader = GetComponentInChildren<AssetLoader>();
        EventManager = GetComponentInChildren<EventManager>();
        ResourceManager = GetComponentInChildren<ResourceManager>();
        QuestManager = GetComponentInChildren<QuestManager>();
        UIManager = GetComponentInChildren<UIManager>();
        MapManager = GetComponentInChildren<MapManager>();
        GridManager = GetComponentInChildren<GridManager>();
        AudioManager = GetComponentInChildren<AudioManager>();
        DataTableManager = GetComponentInChildren<TableManager>();

    }
    void Start()
    {
        Init().Forget();
    }
    public async UniTask Init()
    {
        await DataTableManager.LoadAllTables();
        await UniTask.WaitForSeconds(1);
        PoolManager.PreWarm();

    }
}