using Logic;
using UnityEngine;
using Top;
using DataCenter;
public class GlobalManager : MonoBehaviour
{

    public static GlobalManager Instance;
    [Header("地图系统")]
    public GridManager GridManager;
    public MapManager MapManager;
    public AudioManager AudioManager;

    [Header("游戏系统")]
    public DataTableManager DataTableManager;

    [Header("关卡系统")]
    public LevelManager LevelManager;
    public EntityManager EntityManager;
    public WaveManager WaveManager;
    public PoolManager PoolManager;

    public EventManager EventManager;
    public ResourceManager ResourceManager;
    public QuestManager QuestManager;
    public UIManager UIManager;


    public DataLoader DataLoader;

    public int level = 1;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LevelManager = GetComponentInChildren<LevelManager>();
        EntityManager = GetComponentInChildren<EntityManager>();
        WaveManager = GetComponentInChildren<WaveManager>();
        PoolManager = GetComponentInChildren<PoolManager>();
        DataLoader = GetComponentInChildren<DataLoader>();
        EventManager = GetComponentInChildren<EventManager>();
        ResourceManager = GetComponentInChildren<ResourceManager>();
        QuestManager = GetComponentInChildren<QuestManager>();
        UIManager = GetComponentInChildren<UIManager>();
        MapManager = GetComponentInChildren<MapManager>();
        GridManager = GetComponentInChildren<GridManager>();
        AudioManager = GetComponentInChildren<AudioManager>();
        DataTableManager = GetComponentInChildren<DataTableManager>();

    }
}