using UnityEngine;
using UnityEngine.AddressableAssets;

public class DataLoader : MonoBehaviour
{
    /// <summary>
    /// 初始化数据表（按需加载指定表）
    /// </summary>
    public void InitData()
    {

    }
    public T LoadAssetAsync<T>(string path) where T : UnityEngine.Object
    {
        return Addressables.LoadAssetAsync<T>(path).WaitForCompletion();
    }
}