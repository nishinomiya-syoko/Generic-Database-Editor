using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SOEditor
{
    /// <summary>
    /// ScriptableObject 数据库 - 管理所有 SO 的序列化和反序列化
    /// </summary>
    public static class SODatabase
    {
        private const string DATABASE_FOLDER = "SO_Database";
        private const string FILE_EXTENSION = ".json";
        
        private static string GetDatabasePath()
        {
            // 在编辑器中使用 Assets 文件夹，在运行时使用 persistentDataPath
            #if UNITY_EDITOR
            string path = Path.Combine(Application.dataPath, "..", DATABASE_FOLDER);
            #else
            string path = Path.Combine(Application.persistentDataPath, DATABASE_FOLDER);
            #endif
            return path;
        }
        
        /// <summary>
        /// 确保数据库文件夹存在
        /// </summary>
        public static void EnsureDatabaseExists()
        {
            string path = GetDatabasePath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[SODatabase] 创建数据库文件夹: {path}");
            }
        }
        
        /// <summary>
        /// 获取指定类型的保存路径
        /// </summary>
        public static string GetTypePath(string soType)
        {
            return Path.Combine(GetDatabasePath(), soType);
        }
        
        /// <summary>
        /// 确保类型文件夹存在
        /// </summary>
        public static void EnsureTypeFolderExists(string soType)
        {
            string path = GetTypePath(soType);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// 获取 SO 文件的完整路径
        /// </summary>
        public static string GetSOFilePath(string soType, string id)
        {
            return Path.Combine(GetTypePath(soType), $"{id}{FILE_EXTENSION}");
        }
        
        /// <summary>
        /// 保存 ScriptableObject 到 JSON
        /// </summary>
        public static bool SaveScriptableObject(ScriptableObjectBase so)
        {
            try
            {
                EnsureDatabaseExists();
                string soType = so.GetSOType();
                EnsureTypeFolderExists(soType);
                
                string filePath = GetSOFilePath(soType, so.Id);
                object data = so.GetSerializableData();
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(filePath, json);
                
                Debug.Log($"[SODatabase] 已保存: {so.DisplayName} ({so.Id}) 到 {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SODatabase] 保存失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 加载指定类型的所有 ScriptableObject 数据
        /// </summary>
        public static List<T> LoadAllData<T>(string soType) where T : class
        {
            List<T> dataList = new List<T>();
            
            try
            {
                string typePath = GetTypePath(soType);
                if (!Directory.Exists(typePath))
                {
                    return dataList;
                }
                
                string[] files = Directory.GetFiles(typePath, $"*{FILE_EXTENSION}");
                foreach (string file in files)
                {
                    string json = File.ReadAllText(file);
                    T data = JsonUtility.FromJson<T>(json);
                    if (data != null)
                    {
                        dataList.Add(data);
                    }
                }
                
                Debug.Log($"[SODatabase] 已加载 {dataList.Count} 个 {soType}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SODatabase] 加载失败: {e.Message}");
            }
            
            return dataList;
        }
        
        /// <summary>
        /// 加载单个 ScriptableObject 数据
        /// </summary>
        public static T LoadData<T>(string soType, string id) where T : class
        {
            try
            {
                string filePath = GetSOFilePath(soType, id);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SODatabase] 文件不存在: {filePath}");
                    return null;
                }
                
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SODatabase] 加载失败: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 删除 ScriptableObject 文件
        /// </summary>
        public static bool DeleteScriptableObject(string soType, string id)
        {
            try
            {
                string filePath = GetSOFilePath(soType, id);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SODatabase] 已删除: {id}");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SODatabase] 删除失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有可用的 SO 类型
        /// </summary>
        public static List<string> GetAllSOTypes()
        {
            List<string> types = new List<string>();
            string dbPath = GetDatabasePath();
            
            if (!Directory.Exists(dbPath))
            {
                return types;
            }
            
            string[] directories = Directory.GetDirectories(dbPath);
            foreach (string dir in directories)
            {
                types.Add(Path.GetFileName(dir));
            }
            
            return types;
        }
        
        /// <summary>
        /// 获取指定类型的所有 SO ID
        /// </summary>
        public static List<string> GetAllSOIds(string soType)
        {
            List<string> ids = new List<string>();
            string typePath = GetTypePath(soType);
            
            if (!Directory.Exists(typePath))
            {
                return ids;
            }
            
            string[] files = Directory.GetFiles(typePath, $"*{FILE_EXTENSION}");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                ids.Add(fileName);
            }
            
            return ids;
        }
        
        /// <summary>
        /// 导出所有数据到指定路径
        /// </summary>
        public static bool ExportDatabase(string exportPath)
        {
            try
            {
                string dbPath = GetDatabasePath();
                if (!Directory.Exists(dbPath))
                {
                    Debug.LogWarning("[SODatabase] 数据库为空，无法导出");
                    return false;
                }
                
                if (Directory.Exists(exportPath))
                {
                    Directory.Delete(exportPath, true);
                }
                
                CopyDirectory(dbPath, exportPath);
                Debug.Log($"[SODatabase] 数据库已导出到: {exportPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SODatabase] 导出失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从指定路径导入数据库
        /// </summary>
        public static bool ImportDatabase(string importPath)
        {
            try
            {
                if (!Directory.Exists(importPath))
                {
                    Debug.LogError("[SODatabase] 导入路径不存在");
                    return false;
                }
                
                string dbPath = GetDatabasePath();
                if (Directory.Exists(dbPath))
                {
                    Directory.Delete(dbPath, true);
                }
                
                CopyDirectory(importPath, dbPath);
                Debug.Log($"[SODatabase] 数据库已从 {importPath} 导入");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SODatabase] 导入失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 复制文件夹
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
        
        /// <summary>
        /// 清空数据库
        /// </summary>
        public static void ClearDatabase()
        {
            string dbPath = GetDatabasePath();
            if (Directory.Exists(dbPath))
            {
                Directory.Delete(dbPath, true);
                Debug.Log("[SODatabase] 数据库已清空");
            }
        }
    }
}
