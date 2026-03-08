using Logic;
using UnityEngine;
using Top;
public class GlobalManager : MonoBehaviour
{

    public static GlobalManager Instance;
    [Header("地图系统")]
    public GridManager GridManager;
    public MapManager MapManager;
    public AudioManager AudioManager;

    [Header("关卡系统")]
    public LevelManager LevelManager;
    public EntityManager entityManager;
    public WaveManager waveManager;
    public PoolManager poolManager;

    public EventManager EventManager;
    public ResourceManager ResourceManager;
    public QuestManager QuestManager;
    public UIManager UIManager;


    public DataLoader dataLoader;

    public int level = 1;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LevelManager = GetComponentInChildren<LevelManager>();
        entityManager = GetComponentInChildren<EntityManager>();
        waveManager = GetComponentInChildren<WaveManager>();
        poolManager = GetComponentInChildren<PoolManager>();
        dataLoader = GetComponentInChildren<DataLoader>();
    }
}