using UnityEngine;
// 奖励数据
    [System.Serializable]
    public class RewardDataSO
    {
        public int goldReward;
        public int elixirReward;
        public int darkElixirReward;
        public int gemReward;
        public int experienceReward;
        public string[] unlockContent; // 解锁的内容ID
    }