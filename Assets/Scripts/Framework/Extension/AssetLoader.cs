using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class AssetLoader : MonoBehaviour
{
    public static bool editorMode = false;

    public void InitData() { }

    // 基础加载方法（仅运行时逻辑）
    public virtual T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"加载资源失败：路径为空！");
            return null;
        }

        try
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Debug.LogError($"Addressables加载资源失败：{path}，状态：{handle.Status}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Addressables加载资源异常：{path}，错误：{e.Message}");
            return null;
        }
        // finally
        // {
        //     Addressables.Release(handle);
        // }
    }
   
    

    public async Task<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError($"加载资源失败：路径为空！");
            return null;
        }

        try
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            else
            {
                Debug.LogError($"Addressables加载资源失败：{path}，状态：{handle.Status}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Addressables加载资源异常：{path}，错误：{e.Message}");
            return null;
        }
    }
}