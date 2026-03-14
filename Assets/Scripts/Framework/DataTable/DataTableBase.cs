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
    /// 数据表基类
    /// </summary>
    public abstract class DataTableBase<T> where T : class, new()
    {
        protected List<T> _rows = new List<T>();
        protected bool _isLoaded = false;
        protected bool _useBinary = false;

        public List<T> Rows => _rows;
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// 通过 ID 获取数据 (需要数据行有 Id 字段)
        /// </summary>
        public T GetByID(int id)
        {
            FieldInfo idField = typeof(T).GetField("Id") ?? typeof(T).GetField("id") ?? typeof(T).GetField("ID");
            if (idField == null)
            {
                Debug.LogWarning($"类型 {typeof(T).Name} 没有 Id 字段");
                return null;
            }

            foreach (var row in _rows)
            {
                var rowId = idField.GetValue(row);
                if (rowId is int && (int)rowId == id)
                    return row;
            }
            return null;
        }

        /// <summary>
        /// 通过条件查询
        /// </summary>
        public List<T> Find(Func<T, bool> predicate)
        {
            return _rows.FindAll(item => predicate(item));
        }

        /// <summary>
        /// 加载数据表
        /// </summary>
        public void LoadTable(string tableName, bool useBinary = false)
        {
            if (_isLoaded) return;

            _useBinary = useBinary;
            string extension = useBinary ? ".dat" : ".txt";
            string path = $"DataTables/{tableName}{extension}";

            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogError($"[DataTable] 未找到数据文件：{path}");
                return;
            }

            if (useBinary)
            {
                _rows = ParseBinary(asset.bytes, tableName);
            }
            else
            {
                _rows = JsonConvert.DeserializeObject<List<T>>(asset.text);
            }

            _isLoaded = true;
            Debug.Log($"[DataTable] 加载成功：{tableName} ({_rows.Count} 行) 格式：{(useBinary ? "Binary" : "JSON")}");
        }

        /// <summary>
        /// 异步加载数据表
        /// </summary>
        public async System.Threading.Tasks.Task LoadTableAsync(string tableName, bool useBinary = false)
        {
            if (_isLoaded) return;

            _useBinary = useBinary;
            string extension = useBinary ? ".dat" : ".txt";
            string path = $"DataTables/{tableName}{extension}";

            var request = Resources.LoadAsync<TextAsset>(path);
            while (!request.isDone)
                await System.Threading.Tasks.Task.Yield();

            TextAsset asset = request.asset as TextAsset;
            if (asset == null)
            {
                Debug.LogError($"[DataTable] 未找到数据文件：{path}");
                return;
            }

            if (useBinary)
            {
                _rows = ParseBinary(asset.bytes, tableName);
            }
            else
            {
                _rows = JsonConvert.DeserializeObject<List<T>>(asset.text);
            }

            _isLoaded = true;
            Debug.Log($"[DataTable] 加载成功：{tableName} ({_rows.Count} 行)");
        }

        private List<T> ParseBinary(byte[] bytes, string tableName)
        {
            List<T> result = new List<T>();

            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms))
            {
                // 读取文件头
                byte[] magic = br.ReadBytes(3);
                if (magic[0] != 'D' || magic[1] != 'T' || magic[2] != 'B')
                {
                    throw new Exception("无效的二进制文件格式");
                }

                byte version = br.ReadByte();
                if (version > 1)
                {
                    Debug.LogWarning($"二进制文件版本 {version} 高于支持的版本 1");
                }

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

            return result;
        }

        private object ReadFieldValue(BinaryReader br, Type type)
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

            if (type == typeof(Vector2))
                return new Vector2(br.ReadSingle(), br.ReadSingle());

            if (type == typeof(Vector3))
                return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            if (type == typeof(Vector4))
                return new Vector4(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            if (type == typeof(Color))
                return new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            if (type == typeof(Color32))
                return new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte());

            if (type == typeof(Quaternion))
                return new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            if (type == typeof(Rect))
                return new Rect(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

            if (type == typeof(DateTime))
                return DateTime.FromBinary(br.ReadInt64());

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

            // 自定义类：读取 JSON
            string json = br.ReadString();
            return JsonConvert.DeserializeObject(json, type);
        }

        public void Clear()
        {
            _rows.Clear();
            _isLoaded = false;
        }
    }
}