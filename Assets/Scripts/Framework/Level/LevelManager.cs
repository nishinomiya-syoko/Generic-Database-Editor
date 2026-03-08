using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;


public class LevelManager : MonoBehaviour
{
    public enum LevelState
    {
        NotStarted,
        InProgress,
        Completed,
        Failed
    }

    [Header("关卡数据库")]
    public LevelDatabase levelDatabase;

    [Header("玩家进度")]
    public int playerLevel = 1;
    public int playerExperience = 0;
    public List<int> completedLevels = new List<int>();
    public Dictionary<int, int> levelStars = new Dictionary<int, int>(); // levelId -> stars

    [Header("当前关卡")]
    public LevelDataSO currentLevel;
    public LevelState currentLevelState = LevelState.NotStarted;
    private float levelStartTime;
    private int unitsLostInLevel = 0;

    private WaveManager waveManager;
    void Start()
    {
        if (levelDatabase == null)
        {
            var levelData = Resources.LoadAll<LevelDataSO>("SO");
            levelDatabase = new LevelDatabase();
            for (int i = 0; i < levelData.Length; i++)
            {
                levelDatabase.levels.Add(levelData[i]);
            }
        }

        waveManager = GlobalManager.Instance.waveManager;
    }
    public void StartLevel(int levelId)
    {
        LevelDataSO levelData = levelDatabase.GetLevelData(levelId);
        if (levelData == null)
        {
            // Debug.LogError("Level data not found for level ID: " + levelId);
            DebugInfo.LogError("关卡数据未找到，关卡ID: " + levelId);
            return;
        }
        if (!levelData.IsUnlocked())
        {
            DebugInfo.LogError("关卡已锁定: " + levelId);
            return;
        }
        currentLevel = levelData;
        currentLevelState = LevelState.InProgress;
        levelStartTime = Time.time;
        unitsLostInLevel = 0;

        if (levelData.levelType == LevelType.Attack)
        {
            StartAttackLevel(levelData);
        }
        else
        {
            StartDefenseLevel(levelData);
        }
    }
    private void StartAttackLevel(LevelDataSO levelData)
    { 
    }
    private void StartDefenseLevel(LevelDataSO levelData)
    {
        waveManager.StartWave(levelData.waves[0], 0);
    }
    [Button("测试关卡1")]
    public void StartTestLevel()
    {
        StartLevel(1);
    }
}