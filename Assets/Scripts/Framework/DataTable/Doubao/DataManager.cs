using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public class DataManager
{
    // 线程安全单例 (Unity主线程使用)
    private static readonly object _lockObj = new object();
    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lockObj)
                {
                    if (_instance == null)
                    {
                        _instance = new DataManager();
                    }
                }
            }
            return _instance;
        }
    }

    // 缓存所有表的数据
    private Dictionary<Type, object> _dataCaches = new Dictionary<Type, object>();

    /// <summary>
    /// 获取某张表的所有数据 (支持Txt/Binary加载)
    /// </summary>
    /// <param name="useBinary">是否使用二进制加载（性能更高）</param>
    public List<T> GetTable<T>(bool useBinary = false) where T : class, new()
    {
        Type type = typeof(T);
        // 优先返回缓存
        if (_dataCaches.ContainsKey(type))
        {
            return _dataCaches[type] as List<T>;
        }

        List<T> dataList = null;
        if (useBinary)
        {
            // 加载二进制
            dataList = LoadFromBinary<T>();
        }
        else
        {
            // 加载Txt(Json)
            dataList = LoadFromTxt<T>();
        }

        // 缓存数据
        if (dataList != null)
        {
            _dataCaches[type] = dataList;
        }
        return dataList;
    }

    /// <summary>
    /// 从Txt(Json)加载数据
    /// </summary>
    private List<T> LoadFromTxt<T>() where T : class, new()
    {
        Type type = typeof(T);
        string txtPath = Path.Combine(ExcelImporter.TxtOutputPath, $"{type.Name}.txt");
        if (!File.Exists(txtPath))
        {
            Debug.LogWarning($"Txt文件不存在：{txtPath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(txtPath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<List<T>>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载Txt失败: {txtPath}, error={ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从Binary加载数据 (补全二进制读取逻辑)
    /// </summary>
    private List<T> LoadFromBinary<T>() where T : class, new()
    {
        Type type = typeof(T);
        string binaryPath = Path.Combine(ExcelImporter.BinaryOutputPath, $"{type.Name}.bytes");
        if (!File.Exists(binaryPath))
        {
            Debug.LogWarning($"Binary文件不存在：{binaryPath}");
            return null;
        }

        try
        {
            List<T> dataList = new List<T>();
            using (FileStream fs = new FileStream(binaryPath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs, Encoding.UTF8))
            {
                // 读取数据总数
                int count = br.ReadInt32();

                // 获取所有字段信息 (需要和Excel导入时的字段顺序一致)
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                for (int i = 0; i < count; i++)
                {
                    T item = new T();
                    foreach (var field in fields)
                    {
                        object fieldValue = ReadBinaryData(br, field.FieldType);
                        if (fieldValue != null)
                        {
                            field.SetValue(item, fieldValue);
                        }
                    }
                    dataList.Add(item);
                }
            }
            return dataList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载Binary失败: {binaryPath}, error={ex.Message}");
            return null;
        }
    }

    // 二进制读取辅助方法
    private object ReadBinaryData(BinaryReader br, Type fieldType)
    {
        // 读取非空标记
        bool hasValue = br.ReadBoolean();
        if (!hasValue) return null;

        if (fieldType == typeof(int))
            return br.ReadInt32();
        else if (fieldType == typeof(float))
            return br.ReadSingle();
        else if (fieldType == typeof(string))
            return br.ReadString();
        else if (fieldType == typeof(int[]))
        {
            int length = br.ReadInt32();
            int[] arr = new int[length];
            for (int i = 0; i < length; i++) arr[i] = br.ReadInt32();
            return arr;
        }
        else if (fieldType == typeof(List<string>))
        {
            int length = br.ReadInt32();
            List<string> list = new List<string>();
            for (int i = 0; i < length; i++) list.Add(br.ReadString());
            return list;
        }
        else if (fieldType == typeof(Dictionary<int, string>))
        {
            int length = br.ReadInt32();
            Dictionary<int, string> dict = new Dictionary<int, string>();
            for (int i = 0; i < length; i++)
            {
                int key = br.ReadInt32();
                string value = br.ReadString();
                dict.Add(key, value);
            }
            return dict;
        }
        else
        {
            Debug.LogWarning($"不支持的二进制读取类型: {fieldType.Name}");
            return null;
        }
    }

    /// <summary>
    /// 清空指定表的缓存
    /// </summary>
    public void ClearCache<T>()
    {
        Type type = typeof(T);
        if (_dataCaches.ContainsKey(type))
        {
            _dataCaches.Remove(type);
        }
    }
    public void ClearCache(string tableName)
    {
        Type type = Type.GetType(tableName);
        if (_dataCaches.ContainsKey(type))
        {
            _dataCaches.Remove(type);
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        _dataCaches.Clear();
    }
}