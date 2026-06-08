using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace NiShiMiYa.GenericEditor
{
    /// <summary>
    /// 通用数据序列化/反序列化工具
    /// </summary>
    public static class GenericDataPersistence
    {
        // 默认保存路径（Assets/Resources/EditableData/）
        private static readonly string BasePath = Constant.JSON_PATH;

        /// <summary>
        /// 获取指定类型+实例名的保存路径
        /// </summary>
        public static string GetSavePath<T>(string instanceName = "Default") where T : class, new()
        {
            string className = typeof(T).Name;
            string directory = Path.Combine(BasePath, className);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, $"{instanceName}.json");
        }

        /// <summary>
        /// 保存数据（自动处理路径和序列化）
        /// </summary>
        public static bool SaveData<T>(T data, string instanceName = "Default") where T : class, new()
        {
            try
            {
                string path = GetSavePath<T>(instanceName);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented,
                    new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                File.WriteAllText(path, json);
                Debug.Log($"数据保存成功：{path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"保存{typeof(T).Name}失败：{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载数据（不存在则返回新实例）
        /// </summary>
        public static T LoadData<T>(string instanceName = "Default") where T : class, new()
        {
            try
            {
                string path = GetSavePath<T>(instanceName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                Debug.LogWarning($"未找到{typeof(T).Name}的{instanceName}实例，创建新实例");
                return new T();
            }
            catch (Exception e)
            {
                Debug.LogError($"加载{typeof(T).Name}失败：{e.Message}");
                return new T();
            }
        }

        /// <summary>
        /// 获取指定类型的所有实例名（用于下拉选择）
        /// </summary>
        public static string[] GetAllInstanceNames<T>() where T : class, new()
        {
            string className = typeof(T).Name;
            string directory = Path.Combine(BasePath, className);
            if (!Directory.Exists(directory))
            {
                return new[] { "Default" };
            }

            var files = Directory.GetFiles(directory, "*.json");
            var instanceNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                instanceNames[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return instanceNames.Length == 0 ? new[] { "Default" } : instanceNames;
        }
    }
}