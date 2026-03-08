// ArchiveSystem.cs (Runtime)
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class ArchiveSystem 
{
    private static string SavePath => Application.persistentDataPath + "/SaveData/";
    
    public static void Save<T>(string slotId, T data) where T : class
    {
        if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
        
        var json = JsonUtility.ToJson(data, true);
        // 可选：加密 json
        File.WriteAllText(GetFilePath(slotId), json);
    }

    public static T Load<T>(string slotId) where T : class
    {
        string path = GetFilePath(slotId);
        if (!File.Exists(path)) return null;
        
        string json = File.ReadAllText(path);
        // 可选：解密
        return JsonUtility.FromJson<T>(json);
    }

    public static void Delete(string slotId)
    {
        string path = GetFilePath(slotId);
        if (File.Exists(path)) File.Delete(path);
    }

    private static string GetFilePath(string slotId) => $"{SavePath}{slotId}.json";
}