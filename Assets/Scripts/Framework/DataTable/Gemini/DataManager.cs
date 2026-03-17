// using System;
// using System.Collections.Generic;
// using System.IO;
// using Newtonsoft.Json;

// namespace Gemini
// {
//     public class DataManager
//     {
//         private static DataManager _instance;
//         public static DataManager Instance => _instance ?? (_instance = new DataManager());

//         // 缓存所有表的数据
//         private Dictionary<Type, object> _dataCaches = new Dictionary<Type, object>();

//         /// <summary>
//         /// 获取某张表的所有数据 (例如: DataManager.Instance.GetTable<ItemData>())
//         /// </summary>
//         public List<T> GetTable<T>() where T : class
//         {
//             Type type = typeof(T);
//             if (_dataCaches.ContainsKey(type))
//             {
//                 return _dataCaches[type] as List<T>;
//             }

//             // 加载 TXT (这里以 Txt/Json 为例，也可以根据需求改成读取 Binary)
//             string txtPath = Path.Combine(ExcelImporter.TxtOutputPath, $"{type.Name}.txt");
//             if (File.Exists(txtPath))
//             {
//                 string json = File.ReadAllText(txtPath);
//                 List<T> dataList = JsonConvert.DeserializeObject<List<T>>(json);
//                 _dataCaches[type] = dataList;
//                 return dataList;
//             }

//             return null;
//         }
//     }
// }