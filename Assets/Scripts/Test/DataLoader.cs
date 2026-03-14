using UnityEngine;
using DataCenter;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using Sirenix.OdinInspector;

namespace Test
{
    public class DataLoader : MonoBehaviour
    {
        [Button]
        public void LoadData()
        {
            DebugInfo.Log("开始加载数据...");
            // DataTableManager.GetTable<BuildingDataTable>()
            Addressables.LoadAssetAsync<AudioClip>("Assets/Res/Audio/BaseTheme.mp3").Completed += (handle) =>
            {
                DebugInfo.Log("加载完成: " + handle.Result);
                AudioSource.PlayClipAtPoint(handle.Result, Vector3.zero);
            };
        }
        public T LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            return Addressables.LoadAssetAsync<T>(path).WaitForCompletion();
        }
        [Button]
        public void LoadTable()
        {
            var bu = DataTableManager.GetTable<BuildingDataTable>("Assets/Data/TXT/BuildingData.txt");
            Debug.Log(bu.Count.ToString());
        }
    }
}