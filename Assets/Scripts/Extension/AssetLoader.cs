using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class AssetLoader
{
    public static List<Object> LoadAllAssetsInFolder(string folderPath)
    {
        // folderPath 必须以 "Assets/" 开头，比如 "Assets/Resources/SO/Skills"
        string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
        var assets = new List<Object>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset != null)
                assets.Add(asset);
        }

        Debug.Log($"在 {folderPath} 中加载到 {assets.Count} 个资源");
        return assets;
    }
}