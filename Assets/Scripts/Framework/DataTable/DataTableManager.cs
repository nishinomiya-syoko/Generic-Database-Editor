using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;

namespace DataCenter
{
    /// <summary>
    /// 全局数据表管理器
    /// </summary>
    public static class DataTableManager
    {
        public static readonly string DATA_BINARY_NAMEEND = Constant.DATA_BINARY_NAMEEND;
        public static readonly string DATA_BINARY_PATH = Constant.DATA_BINARY_PATH;
        public static readonly string DATA_TXT_PATH = Constant.DATA_TXT_PATH;
        

        private static Dictionary<string, object> _cache = new Dictionary<string, object>();
        private static bool _defaultUseBinary = false;
        public static bool useBinary => _defaultUseBinary;

        public static void SetDefaultFormat(bool useBinary)
        {
            _defaultUseBinary = useBinary;
        }

        public static List<T> GetTable<T>(string tableName, bool? useBinary = null) where T : class, new()
        {
            string cacheKey = $"{typeof(T).FullName}_{(useBinary ?? _defaultUseBinary)}";
            
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return (List<T>)cached;
            }

            List<T> data = LoadTableInternal<T>(tableName, useBinary ?? _defaultUseBinary);
            _cache[cacheKey] = data;
            return data;
        }

        private static List<T> LoadTableInternal<T>(string tableName, bool useBinary) where T : class, new()
        {
            string extension = useBinary ? DATA_BINARY_NAMEEND : ".txt";
            string b = useBinary ? DATA_BINARY_PATH : DATA_TXT_PATH;
            // string path = $"{b}/{tableName}{extension}";
            string path = tableName;

            TextAsset asset = GlobalManager.Instance.dataLoader.LoadAssetAsync<TextAsset>(path); 
            DebugInfo.Log($"[DataTableManager] 加载：{tableName}");
            if (asset == null)
            {
                Debug.LogError($"[DataTableManager] 未找到：{path}");
                return new List<T>();
            }

            if (useBinary)
            {
                return ParseBinary<T>(asset.bytes);
            }
            else
            {
                return JsonConvert.DeserializeObject<List<T>>(asset.text);
            }
        }

        private static List<T> ParseBinary<T>(byte[] bytes) where T : class, new()
        {
            List<T> result = new List<T>();
            
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryReader br = new BinaryReader(ms);
                try
                {
                    // 验证文件头
                    byte[] magic = br.ReadBytes(3);
                    if (magic[0] != 'D' || magic[1] != 'T' || magic[2] != 'B')
                    {
                        // 尝试解压
                        try
                        {
                            bytes = Decompress(bytes);
                            ms.Position = 0;
                            br.Dispose();
                            br = new BinaryReader(ms);
                            magic = br.ReadBytes(3);
                        }
                        catch
                        {
                            throw new Exception("无效的二进制文件格式");
                        }
                    }

                    br.ReadByte(); // version
                    int rowCount = br.ReadInt32();
                    FieldInfo[] fields = typeof(T).GetFields();

                    for (int i = 0; i < rowCount; i++)
                    {
                        T row = new T();
                        foreach (var field in fields)
                        {
                            object val = ReadFieldValue(br, field.FieldType);
                            field.SetValue(row, val);
                        }
                        result.Add(row);
                    }
                }
                finally
                {
                    br.Dispose();
                }
            }
            
            return result;
        }

        private static object ReadFieldValue(BinaryReader br, Type type)
        {
            if (type == typeof(int)) return br.ReadInt32();
            if (type == typeof(long)) return br.ReadInt64();
            if (type == typeof(float)) return br.ReadSingle();
            if (type == typeof(double)) return br.ReadDouble();
            if (type == typeof(bool)) return br.ReadBoolean();
            if (type == typeof(string)) return br.ReadString();
            if (type == typeof(byte)) return br.ReadByte();
            if (type == typeof(short)) return br.ReadInt16();
            if (type == typeof(uint)) return br.ReadUInt32();
            if (type == typeof(ulong)) return br.ReadUInt64();
            if (type == typeof(Vector2)) return new Vector2(br.ReadSingle(), br.ReadSingle());
            if (type == typeof(Vector3)) return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            if (type == typeof(Color)) return new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            if (type == typeof(DateTime)) return DateTime.FromBinary(br.ReadInt64());

            if (type.IsArray)
            {
                Type inner = type.GetElementType();
                int len = br.ReadInt32();
                Array arr = Array.CreateInstance(inner, len);
                for (int i = 0; i < len; i++)
                    arr.SetValue(ReadFieldValue(br, inner), i);
                return arr;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type inner = type.GetGenericArguments()[0];
                int len = br.ReadInt32();
                IList list = (IList)Activator.CreateInstance(type);
                for (int i = 0; i < len; i++)
                    list.Add(ReadFieldValue(br, inner));
                return list;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyT = type.GetGenericArguments()[0];
                Type valT = type.GetGenericArguments()[1];
                int len = br.ReadInt32();
                IDictionary dict = (IDictionary)Activator.CreateInstance(type);
                for (int i = 0; i < len; i++)
                {
                    var k = ReadFieldValue(br, keyT);
                    var v = ReadFieldValue(br, valT);
                    dict.Add(k, v);
                }
                return dict;
            }

            string json = br.ReadString();
            return JsonConvert.DeserializeObject(json, type);
        }

        private static byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    deflate.CopyTo(output);
                }
                return output.ToArray();
            }
        }

        public static void ClearCache()
        {
            _cache.Clear();
        }

        public static void ClearCache(string tableName)
        {
            var keysToRemove = _cache.Keys.Where(k => k.StartsWith(tableName)).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }
        }
    }
}