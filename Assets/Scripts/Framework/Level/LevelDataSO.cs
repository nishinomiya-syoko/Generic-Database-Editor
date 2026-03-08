using UnityEngine;
// 关卡类型
public enum LevelType
{
    Attack,
    Defense
}
[System.Serializable]
public class CampData
{
    public string campName;

}

// 关卡数据
[CreateAssetMenu(fileName = "LevelData", menuName = "EmpireClash/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("基本信息")]
    public string levelName;
    public int levelId;
    public LevelType levelType;
    public Sprite levelIcon;
    public int requiredLevel;
    public bool isUnlocked = false;
    [Header("关卡信息")]
    public CampData levelCamp;
    public WaveData[] waves;
    [Header("奖励信息")]
    public RewardDataSO rewardData;

    [Header("评级标准")]
    public float threeStarTime = 180f; // 3星时间限制
    public float twoStarTime = 300f; // 2星时间限制
    public int maxUnitsLost = 10; // 最大损失单位数

    public bool IsUnlocked()
    {
        return isUnlocked || GlobalManager.Instance.level >= requiredLevel;
    }

    public int GetStarRating(float completionTime, int unitsLost)
    {
        if (completionTime <= threeStarTime && unitsLost <= maxUnitsLost)
            return 3;
        else if (completionTime <= twoStarTime && unitsLost <= maxUnitsLost * 2)
            return 2;
        else
            return 1;
    }
}