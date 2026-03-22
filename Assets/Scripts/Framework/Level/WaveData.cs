[System.Serializable]
public class WaveData
{
    public float startDelay; // 出兵延迟时间
    public UnitSpawnData[] unitsToSpawn;
}
[System.Serializable]
public class UnitSpawnData
{
    public int unitId;
    public int quantity;
    public float spawnDelay; // 当前波次spawn延迟时间
}