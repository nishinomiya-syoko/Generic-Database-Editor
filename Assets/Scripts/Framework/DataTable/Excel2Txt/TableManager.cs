using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.VersionControl;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Linq;

namespace DataCenter
{

    /// <summary>
    /// 数据表管理器（单例模式）
    /// 负责加载TXT数据表并提供数据查询接口
    /// </summary>
    public class TableManager : MonoBehaviour
    {
        // 单例实例
        
        private bool preWarmed = false;

        // 数据缓存：key=数据表名称（类名），value=数据字典（key=ID，value=数据实体）
        private Dictionary<string, Dictionary<int, object>> _tableDataCache = new Dictionary<string, Dictionary<int, object>>();

        private void Awake()
        {

        }
        
        public void PreWarm()
        {
            if (preWarmed)
                return;
            preWarmed = true;
            LoadTable<AssetPath>(Constant.ASSET_PATH_CONFIG);
        }
        public async UniTask LoadAllTables()
        {
            PreWarm();
            await UniTask.WaitForSeconds(0.1f);

            LoadTable<BuildingData>();
            LoadTable<BuildingLevelData>();
        }
        public bool LoadTable<T>() where T : class,new()
        {
            var p = GetAllData<AssetPath>();
            var path = p.Any(x => x.DisplayName == typeof(T).Name) ? p.First(x => x.DisplayName == typeof(T).Name).Path : null;
            return LoadTable<T>(path);
        }
        public bool LoadTable<T>(int id) where T : class, new()
        {
            var p = GetDataById<AssetPath>(id);
            if (p == null)
            {
                return false;
            }
            return LoadTable<T>(p.Path);
        }

        /// <summary>
        /// 加载指定的TXT数据表 路径格式：（Assets/  ******  .txt ）
        /// </summary>
        /// <typeparam name="T">数据实体类类型</typeparam>
        /// <param name="txtPath">TXT文件路径（Assets/  ******  .txt ）</param>
        /// <returns>是否加载成功</returns>
        public bool LoadTable<T>(string txtPath) where T : class, new()
        {
            string tableName = typeof(T).Name;
            // 避免重复加载
            if (_tableDataCache.ContainsKey(tableName))
            {
                Debug.LogWarning($"数据表{tableName}已加载，无需重复加载");
                return true;
            }

            try
            {
                // 读取Resources中的TXT文件（需将TXT放入Resources目录）
                // TextAsset txtAsset = Resources.Load<TextAsset>(txtPath);
                TextAsset txtAsset = GlobalManager.Instance.AssetLoader.LoadAssetAsync<TextAsset>(txtPath).Result;
                if (txtAsset == null)
                {
                    Debug.LogError($"未找到TXT文件：{txtPath}");
                    return false;
                }

                // 写入临时文件（方便复用解析逻辑）
                string tempPath = Path.Combine(Application.temporaryCachePath, $"{tableName}.txt");
                File.WriteAllText(tempPath, txtAsset.text, System.Text.Encoding.UTF8);

                // 解析表头
                if (!TxtTableParser.ParseTableHeader(tempPath, out List<string> fieldNames, out List<string> fieldTypes, out _))
                {
                    return false;
                }

                // 解析数据行
                List<Dictionary<string, object>> dataList = TxtTableParser.ParseTableData(tempPath, fieldNames, fieldTypes);
                if (dataList.Count == 0)
                {
                    Debug.LogWarning($"数据表{tableName}无有效数据");
                    // return false;
                }

                // 转换为实体类并缓存（以ID为键）
                Dictionary<int, object> dataDict = new Dictionary<int, object>();
                foreach (var dict in dataList)
                {
                    T entity = new T();
                    int id = 0;

                    // 给实体类赋值
                    foreach (var fieldName in fieldNames)
                    {
                        if (fieldName == string.Empty)
                            continue;
                        var property = typeof(T).GetProperty(fieldName);
                        if (property == null)
                        {
                            Debug.LogWarning($"实体类{tableName}未找到属性{fieldName}");
                            continue;
                        }

                        object value = dict[fieldName];
                        property.SetValue(entity, value);

                        // 记录ID
                        if (fieldName.ToLower() == "id")
                        {
                            id = (int)value;
                        }
                    }

                    // 检查ID唯一性
                    if (dataDict.ContainsKey(id))
                    {
                        Debug.LogError($"数据表{tableName}存在重复ID：{id}");
                        continue;
                    }

                    dataDict.Add(id, entity);
                }

                // 加入缓存
                _tableDataCache.Add(tableName, dataDict);
                Debug.Log($"数据表{tableName}加载成功，共{dataDict.Count}条数据");

                // 删除临时文件
                File.Delete(tempPath);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载数据表{tableName}失败：{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 根据ID查询数据
        /// </summary>
        /// <typeparam name="T">数据实体类类型</typeparam>
        /// <param name="id">数据ID</param>
        /// <returns>对应的数据实体，不存在则返回null</returns>
        public T GetDataById<T>(int id) where T : class
        {
            string tableName = typeof(T).Name;
            if (!_tableDataCache.ContainsKey(tableName))
            {
                Debug.LogError($"数据表{tableName}未加载，请先调用LoadTable");
                return null;
            }

            if (_tableDataCache[tableName].TryGetValue(id, out object data))
            {
                return data as T;
            }
            else
            {
                Debug.LogWarning($"数据表{tableName}中未找到ID={id}的数据");
                return null;
            }
        }

        /// <summary>
        /// 获取数据表的所有数据
        /// </summary>
        /// <typeparam name="T">数据实体类类型</typeparam>
        /// <returns>所有数据实体列表</returns>
        public List<T> GetAllData<T>() where T : class
        {
            string tableName = typeof(T).Name;
            if (!_tableDataCache.ContainsKey(tableName))
            {
                Debug.LogError($"数据表{tableName}未加载，请先调用LoadTable");
                return new List<T>();
            }

            List<T> dataList = new List<T>();
            foreach (var data in _tableDataCache[tableName].Values)
            {
                dataList.Add(data as T);
            }
            return dataList;
        }

        /// <summary>
        /// 清空指定数据表缓存
        /// </summary>
        /// <typeparam name="T">数据实体类类型</typeparam>
        public void ClearTable<T>()
        {
            string tableName = typeof(T).Name;
            if (_tableDataCache.ContainsKey(tableName))
            {
                _tableDataCache.Remove(tableName);
                Debug.Log($"数据表{tableName}缓存已清空");
            }
        }

        /// <summary>
        /// 清空所有数据表缓存
        /// </summary>
        public void ClearAllTables()
        {
            _tableDataCache.Clear();
            Debug.Log("所有数据表缓存已清空");
        }

        // 编辑器扩展：一键加载所有数据表
        [Sirenix.OdinInspector.Button("加载所有数据表")]
        public static void LoadTestTables()
        {
            // 示例：加载角色数据表（需根据实际类名和路径修改）
            // Instance.LoadTable<RoleTable>("Tables/RoleTable");
            GlobalManager.Instance.DataTableManager.LoadTable<BuildingData>("Assets/Data/TXT/BuildingData.txt");
            // var p = Instance.GetDataById<BuildingData>(111);
        }
        [Sirenix.OdinInspector.Button("读取")]
        public static void Read()
        {
            var p = GlobalManager.Instance.DataTableManager.GetDataById<BuildingData>(111);
            Debug.Log(p.Id);
            Debug.Log(p.buildingType);
        }   
    }
}