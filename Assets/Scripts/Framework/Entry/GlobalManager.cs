using Logic;
using UnityEngine;
using Top;
using DataCenter;
using RTS.TargetSearch;
using Cysharp.Threading.Tasks;

public class GlobalManager : Singleton<GlobalManager>
{
    [Header("资源加载系统")]
    public AssetLoader AssetLoader;

    [Header("地图系统")]
    public GridManager GridManager;
    public MapManager MapManager;
    public AudioManager AudioManager;
    [Header("UI")]
    public UIManager UIManager;

    [Header("流程")]
    public ProcedureManager ProcedureManager;
    [Header("模块")]
    public ModuleManager ModuleManager;

    [Header("数据表")]
    public TableManager DataTableManager;

    [Header("关卡系统")]
    public LevelManager LevelManager;
    public EntityManager EntityManager;
    public WaveManager WaveManager;
    public PoolManager PoolManager;
    public BuildingManager BuildingManager;
    public UnitManager UnitManager;

    [Header("fight")]
    public BatchedSearchManager BatchedSearchManager;

    public EventManager EventManager;
    public ResourceManager ResourceManager;
    public QuestManager QuestManager;



    public int level = 1;

    public override void Awake()
    {
        base.Awake();
        AssetLoader = GetComponentInChildren<AssetLoader>();

        GridManager = GetComponentInChildren<GridManager>();
        MapManager = GetComponentInChildren<MapManager>();
        AudioManager = GetComponentInChildren<AudioManager>();
        UIManager = GetComponentInChildren<UIManager>();

        ProcedureManager = GetComponentInChildren<ProcedureManager>();
        ModuleManager = GetComponentInChildren<ModuleManager>();

        DataTableManager = GetComponentInChildren<TableManager>();

        LevelManager = GetComponentInChildren<LevelManager>();
        EntityManager = GetComponentInChildren<EntityManager>();
        WaveManager = GetComponentInChildren<WaveManager>();
        PoolManager = GetComponentInChildren<PoolManager>();
        BuildingManager = GetComponentInChildren<BuildingManager>();
        UnitManager = GetComponentInChildren<UnitManager>();

        BatchedSearchManager = GetComponentInChildren<BatchedSearchManager>();

        EventManager = GetComponentInChildren<EventManager>();
        ResourceManager = GetComponentInChildren<ResourceManager>();
        QuestManager = GetComponentInChildren<QuestManager>();
    }
    void Start()
    {
        Init().Forget();
    }
    public async UniTask Init()
    {
        DataTableManager.PreWarm();
        await UniTask.WaitForSeconds(1);
        PoolManager.PreWarm();

    }
}