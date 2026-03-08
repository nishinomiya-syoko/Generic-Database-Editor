using UnityEngine;
using System.Collections;

namespace GameData
{
    public class PlayerManager : MonoBehaviour
    {
        public PlayerData Data { get; private set; } = new PlayerData();

        private const string SAVE_SLOT = "player_main";

        void Start()
        {
            LoadGame();
        }

        void OnApplicationQuit()
        {
            SaveGame();
        }

        public void SaveGame()
        {
            // 调用生成的 Archive() 方法
            var archive = Data.Archive();
            ArchiveSystem.Save(SAVE_SLOT, archive);
            DebugInfo.Log($"存档成功: Level={archive.Level}");
        }

        public void LoadGame()
        {
            var archive = ArchiveSystem.Load<PlayerData.ArchiveData>(SAVE_SLOT);
            if (archive != null)
            {
                Data.Restore(archive);
                DebugInfo.Log($"读档成功: Level={Data.Level}");
            }
            else
            {
                Data.ResetToDefault(); // 新玩家
            }
        }

        [ContextMenu("手动存档")]
        void ManualSave() => SaveGame();
    }
}