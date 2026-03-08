// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using System.Runtime.Serialization.Formatters.Binary;
// using System.Text;
// using UnityEngine;

// namespace DataCenter
// {
//     /// <summary>
//     /// 数据加载格式枚举
//     /// </summary>
//     public enum DataLoadFormat
//     {
//         /// <summary>自动检测（优先二进制，回退TXT）</summary>
//         Auto,
//         /// <summary>强制二进制格式</summary>
//         Binary,
//         /// <summary>强制TXT文本格式</summary>
//         Txt
//     }

//     /// <summary>
//     /// 数据格式工具类
//     /// </summary>
//     public static class DataFormatUtil
//     {
//         public const string BINARY_EXTENSION = ".pve";
//         public const string TXT_EXTENSION = ".txt";
        
//         /// <summary>
//         /// 检测指定路径下存在的数据格式
//         /// </summary>
//         public static DataLoadFormat DetectFormat(string basePath, string tableName)
//         {
//             string binaryPath = basePath + tableName + BINARY_EXTENSION;
//             string txtPath = basePath + tableName + TXT_EXTENSION;
            
//             if (File.Exists(binaryPath))
//                 return DataLoadFormat.Binary;
//             if (File.Exists(txtPath))
//                 return DataLoadFormat.Txt;
                
//             return DataLoadFormat.Binary; // 默认回退
//         }
//     }

//     /// <summary>
//     /// 二进制数据管理器
//     /// 支持二进制(.pve)和文本(.txt)两种数据格式加载
//     /// </summary>
//     public class BinaryDataMgr
//     {
//         // 单例实例
//         private static BinaryDataMgr instance = new BinaryDataMgr();
//         public static BinaryDataMgr Instance => instance;
        
//         // 私有构造函数，确保单例
//         private BinaryDataMgr() { }

//         /// <summary>
//         /// 存储已加载的Excel数据表容器（键：容器类型名，值：容器实例）
//         /// </summary>
//         private Dictionary<string, object> tableDic = new Dictionary<string, object>();

//         /// <summary>
//         /// 运行时数据保存路径（PersistentDataPath）
//         /// </summary>
//         private static string SAVE_PATH = Application.persistentDataPath + "/Data/";
        
//         /// <summary>
//         /// 二进制数据表文件根路径（StreamingAssets/Bianry/）
//         /// 注：Bianry为自定义目录名，注意拼写
//         /// </summary>
//         public static string DATA_BINARY_PATH = Application.streamingAssetsPath + "/Bianry/";

//         public static string CUSTOM_NAME_END = ".pve";

//         // ==================== 核心加载接口（新增格式选择） ====================

//         /// <summary>
//         /// 加载数据表（单泛型版本，支持格式选择）
//         /// </summary>
//         /// <typeparam name="T">数据表容器类型（需包含dataDic字典字段）</typeparam>
//         /// <param name="format">加载格式，默认Auto自动检测</param>
//         public void LoadTable<T>(DataLoadFormat format = DataLoadFormat.Auto)
//         {
//             string tableName = typeof(T).Name;
            
//             // 自动检测格式
//             if (format == DataLoadFormat.Auto)
//             {
//                 format = DataFormatUtil.DetectFormat(DATA_BINARY_PATH, tableName);
//                 Debug.Log($"[DataMgr] 表 {tableName} 自动检测到格式: {format}");
//             }

//             switch (format)
//             {
//                 case DataLoadFormat.Txt:
//                     LoadTableFromTxt<T>();
//                     break;
//                 case DataLoadFormat.Binary:
//                 default:
//                     LoadTable<T>();
//                     break;
//             }
//         }

//         /// <summary>
//         /// 加载数据表（双泛型版本，支持格式选择）
//         /// </summary>
//         /// <typeparam name="T">数据表容器类型</typeparam>
//         /// <typeparam name="K">数据结构类型</typeparam>
//         /// <param name="format">加载格式，默认Auto自动检测</param>
//         public void LoadTable<T, K>(DataLoadFormat format = DataLoadFormat.Auto)
//         {
//             string tableName = typeof(K).Name;
            
//             if (format == DataLoadFormat.Auto)
//             {
//                 format = DataFormatUtil.DetectFormat(DATA_BINARY_PATH, tableName);
//                 Debug.Log($"[DataMgr] 表 {tableName} 自动检测到格式: {format}");
//             }

//             switch (format)
//             {
//                 case DataLoadFormat.Txt:
//                     LoadTableFromTxt<T, K>();
//                     break;
//                 case DataLoadFormat.Binary:
//                 default:
//                     LoadTable<T, K>();
//                     break;
//             }
//         }

//         // ==================== 二进制加载（原有逻辑） ====================

//         /// <summary>
//         /// 加载二进制数据表（单泛型版本）
//         /// </summary>
//         public void LoadTable<T>()
//         {
//             string RowTableName = "Row" + typeof(T).Name;
            
//             using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(T).Name + ".pve", FileMode.Open, FileAccess.Read))
//             {
//                 byte[] bytes = new byte[fs.Length];
//                 fs.Read(bytes, 0, bytes.Length);
                
//                 int index = 0;
//                 int count = BitConverter.ToInt32(bytes, index);
//                 index += 4;

//                 int keyNameLength = BitConverter.ToInt32(bytes, index);
//                 index += 4;
//                 string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
//                 index += keyNameLength;

//                 Type containerType = typeof(T);
//                 object containerObj = Activator.CreateInstance(containerType);
                
//                 Type classType = Type.GetType("DataCenter." + RowTableName);
//                 if (classType == null)
//                 {
//                     Debug.LogError($"未找到数据结构类型：DataCenter.{RowTableName}");
//                     return;
//                 }
                
//                 FieldInfo[] infos = classType.GetFields();
                
//                 for (int i = 0; i < count; i++)
//                 {
//                     object dataObj = Activator.CreateInstance(classType);
                    
//                     foreach (FieldInfo info in infos)
//                     {
//                         ReadBinaryField(bytes, ref index, info, dataObj);
//                     }

//                     AddToContainer(containerType, containerObj, classType, keyName, dataObj);
//                 }

//                 tableDic.Add(typeof(T).Name, containerObj);
//                 fs.Close();
//             }
//         }

//         /// <summary>
//         /// 加载二进制数据表（双泛型版本）
//         /// </summary>
//         public void LoadTable<T, K>()
//         {
//             using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(K).Name + ".pve", FileMode.Open, FileAccess.Read))
//             {
//                 byte[] bytes = new byte[fs.Length];
//                 fs.Read(bytes, 0, bytes.Length);
//                 fs.Close();
                
//                 int index = 0;
//                 int count = BitConverter.ToInt32(bytes, index);
//                 index += 4;

//                 int keyNameLength = BitConverter.ToInt32(bytes, index);
//                 index += 4;
//                 string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
//                 index += keyNameLength;

//                 Type containerType = typeof(T);
//                 object containerObj = Activator.CreateInstance(containerType);
                
//                 Type classType = typeof(K);
//                 FieldInfo[] infos = classType.GetFields();
                
//                 for (int i = 0; i < count; i++)
//                 {
//                     object dataObj = Activator.CreateInstance(classType);
                    
//                     foreach (FieldInfo info in infos)
//                     {
//                         ReadBinaryField(bytes, ref index, info, dataObj);
//                     }

//                     AddToContainer(containerType, containerObj, classType, keyName, dataObj);
//                 }

//                 tableDic.Add(typeof(T).Name, containerObj);
//                 fs.Close();
//             }
//         }

//         // ==================== TXT文本加载（新增） ====================

//         /// <summary>
//         /// 从TXT加载数据表（单泛型版本）
//         /// TXT格式：第一行表头(字段名)，第二行主键名，后续数据行（制表符分隔）
//         /// </summary>
//         public void LoadTableFromTxt<T>()
//         {
//             string tableName = typeof(T).Name;
//             string filePath = DATA_BINARY_PATH + tableName + ".txt";
            
//             if (!File.Exists(filePath))
//             {
//                 Debug.LogError($"[DataMgr] TXT文件不存在: {filePath}");
//                 return;
//             }

//             string RowTableName = "Row" + tableName;
//             Type containerType = typeof(T);
//             object containerObj = Activator.CreateInstance(containerType);
            
//             Type classType = Type.GetType("DataCenter." + RowTableName);
//             if (classType == null)
//             {
//                 Debug.LogError($"未找到数据结构类型：DataCenter.{RowTableName}");
//                 return;
//             }

//             FieldInfo[] infos = classType.GetFields();
            
//             string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
//             if (lines.Length < 2)
//             {
//                 Debug.LogError($"[DataMgr] TXT文件格式错误，至少需要2行（表头+主键名）: {filePath}");
//                 return;
//             }

//             // 解析表头获取字段顺序
//             string[] headers = lines[0].Split('\t');
//             string keyName = lines[1].Trim(); // 第二行是主键名
            
//             // 建立字段名到FieldInfo的映射
//             Dictionary<string, FieldInfo> fieldMap = new Dictionary<string, FieldInfo>();
//             foreach (var info in infos)
//             {
//                 fieldMap[info.Name] = info;
//             }

//             // 从第3行开始读取数据（索引2）
//             for (int i = 2; i < lines.Length; i++)
//             {
//                 if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
//                 string[] values = lines[i].Split('\t');
//                 object dataObj = Activator.CreateInstance(classType);
                
//                 for (int j = 0; j < headers.Length && j < values.Length; j++)
//                 {
//                     string fieldName = headers[j].Trim();
//                     if (fieldMap.TryGetValue(fieldName, out FieldInfo info))
//                     {
//                         ParseAndSetValue(info, dataObj, values[j].Trim());
//                     }
//                 }

//                 AddToContainer(containerType, containerObj, classType, keyName, dataObj);
//             }

//             tableDic.Add(tableName, containerObj);
//             Debug.Log($"[DataMgr] 从TXT加载完成: {tableName}, 数据行数: {lines.Length - 2}");
//         }

//         /// <summary>
//         /// 从TXT加载数据表（双泛型版本）
//         /// </summary>
//         public void LoadTableFromTxt<T, K>()
//         {
//             string tableName = typeof(K).Name;
//             string filePath = DATA_BINARY_PATH + tableName + ".txt";
            
//             if (!File.Exists(filePath))
//             {
//                 Debug.LogError($"[DataMgr] TXT文件不存在: {filePath}");
//                 return;
//             }

//             Type containerType = typeof(T);
//             object containerObj = Activator.CreateInstance(containerType);
            
//             Type classType = typeof(K);
//             FieldInfo[] infos = classType.GetFields();
            
//             string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
//             if (lines.Length < 2)
//             {
//                 Debug.LogError($"[DataMgr] TXT文件格式错误: {filePath}");
//                 return;
//             }

//             string[] headers = lines[0].Split('\t');
//             string keyName = lines[1].Trim();
            
//             Dictionary<string, FieldInfo> fieldMap = new Dictionary<string, FieldInfo>();
//             foreach (var info in infos)
//             {
//                 fieldMap[info.Name] = info;
//             }

//             for (int i = 2; i < lines.Length; i++)
//             {
//                 if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
//                 string[] values = lines[i].Split('\t');
//                 object dataObj = Activator.CreateInstance(classType);
                
//                 for (int j = 0; j < headers.Length && j < values.Length; j++)
//                 {
//                     string fieldName = headers[j].Trim();
//                     if (fieldMap.TryGetValue(fieldName, out FieldInfo info))
//                     {
//                         ParseAndSetValue(info, dataObj, values[j].Trim());
//                     }
//                 }

//                 AddToContainer(containerType, containerObj, classType, keyName, dataObj);
//             }

//             tableDic.Add(typeof(T).Name, containerObj);
//             Debug.Log($"[DataMgr] 从TXT加载完成: {tableName}");
//         }

//         // ==================== 辅助方法 ====================

//         /// <summary>
//         /// 从二进制读取单个字段
//         /// </summary>
//         private void ReadBinaryField(byte[] bytes, ref int index, FieldInfo info, object dataObj)
//         {
//             if (info.FieldType == typeof(int))
//             {
//                 info.SetValue(dataObj, BitConverter.ToInt32(bytes, index));
//                 index += 4;
//             }
//             else if (info.FieldType == typeof(float))
//             {
//                 info.SetValue(dataObj, BitConverter.ToSingle(bytes, index));
//                 index += 4;
//             }
//             else if (info.FieldType == typeof(bool))
//             {
//                 info.SetValue(dataObj, BitConverter.ToBoolean(bytes, index));
//                 index += 1;
//             }
//             else if (info.FieldType == typeof(string))
//             {
//                 int length = BitConverter.ToInt32(bytes, index);
//                 index += 4;
//                 info.SetValue(dataObj, Encoding.UTF8.GetString(bytes, index, length));
//                 index += length;
//             }
//         }

//         /// <summary>
//         /// 解析字符串并设置字段值（支持int/float/bool/string）
//         /// </summary>
//         private void ParseAndSetValue(FieldInfo info, object dataObj, string valueStr)
//         {
//             try
//             {
//                 if (info.FieldType == typeof(int))
//                 {
//                     info.SetValue(dataObj, int.Parse(valueStr));
//                 }
//                 else if (info.FieldType == typeof(float))
//                 {
//                     // 支持中文输入法的逗号容错
//                     info.SetValue(dataObj, float.Parse(valueStr.Replace(',', '.')));
//                 }
//                 else if (info.FieldType == typeof(bool))
//                 {
//                     // 支持多种bool表示
//                     bool boolVal = valueStr == "1" || valueStr.ToLower() == "true";
//                     info.SetValue(dataObj, boolVal);
//                 }
//                 else if (info.FieldType == typeof(string))
//                 {
//                     info.SetValue(dataObj, valueStr);
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[DataMgr] 字段解析失败 {info.Name}={valueStr}: {e.Message}");
//             }
//         }

//         /// <summary>
//         /// 将数据对象添加到容器的dataDic中
//         /// </summary>
//         private void AddToContainer(Type containerType, object containerObj, Type classType, string keyName, object dataObj)
//         {
//             object dicObj = containerType.GetField("dataDic").GetValue(containerObj);
//             MethodInfo addMethod = dicObj.GetType().GetMethod("Add");
//             object keyValue = classType.GetField(keyName).GetValue(dataObj);
//             addMethod.Invoke(dicObj, new object[] { keyValue, dataObj });
//         }

//         // ==================== 查询接口（保持不变） ====================

//         public T GetData<T>() where T : DataTable
//         {
//             string tableName = typeof(T).Name;
//             Debug.Log(tableName);
//             if (tableDic.ContainsKey(tableName))
//             {
//                 return tableDic[tableName] as T;
//             }
//             return null;
//         }

//         public T GetTable<T>() where T : class
//         {
//             string tableName = typeof(T).Name;
            
//             if (!tableName.StartsWith("DT"))
//                 Debug.LogWarning($"获取的表名{tableName}不以DT开头，可能无法正确获取数据");
            
//             if (tableDic.ContainsKey(tableName))
//             {
//                 return tableDic[tableName] as T;
//             }
//             return null;
//         }

//         // ==================== 存档接口（保持不变） ====================

//         public void Save(object data, string fileName)
//         {
//             if (!Directory.Exists(SAVE_PATH))
//             {
//                 Directory.CreateDirectory(SAVE_PATH);
//             }

//             using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".pve", FileMode.OpenOrCreate, FileAccess.Write))
//             {
//                 BinaryFormatter bf = new BinaryFormatter();
//                 bf.Serialize(fs, data);
//                 fs.Flush();
//                 fs.Close();
//             }
//         }

//         public T Load<T>(string fileName) where T : class
//         {
//             if (!File.Exists(SAVE_PATH + fileName + ".pve"))
//             {
//                 return default(T);
//             }

//             T data;
//             using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".pve", FileMode.Open, FileAccess.Read))
//             {
//                 BinaryFormatter bf = new BinaryFormatter();
//                 data = bf.Deserialize(fs) as T;
//                 fs.Close();
//             }
//             return data;
//         }

//         // ==================== 编辑器工具：格式转换（可选） ====================

//         /// <summary>
//         /// 将已加载的二进制表导出为TXT（编辑器工具）
//         /// </summary>
//         public void ExportToTxt<T>(string outputPath = null) where T : class
//         {
//             string tableName = typeof(T).Name;
//             if (!tableDic.ContainsKey(tableName))
//             {
//                 Debug.LogError($"[DataMgr] 表未加载: {tableName}");
//                 return;
//             }

//             if (string.IsNullOrEmpty(outputPath))
//                 outputPath = DATA_BINARY_PATH + tableName + ".txt";

//             var container = tableDic[tableName];
//             var dicField = container.GetType().GetField("dataDic");
//             var dic = dicField.GetValue(container) as System.Collections.IDictionary;
            
//             if (dic == null || dic.Count == 0)
//             {
//                 Debug.LogWarning($"[DataMgr] 表为空: {tableName}");
//                 return;
//             }

//             StringBuilder sb = new StringBuilder();
            
//             // 获取第一行数据以确定字段
//             System.Collections.IEnumerator enumerator = dic.GetEnumerator();
//             enumerator.MoveNext();
//             var firstPair = (System.Collections.DictionaryEntry)enumerator.Current;
//             var firstData = firstPair.Value;
//             var fields = firstData.GetType().GetFields();
            
//             // 写入表头
//             for (int i = 0; i < fields.Length; i++)
//             {
//                 sb.Append(fields[i].Name);
//                 if (i < fields.Length - 1) sb.Append('\t');
//             }
//             sb.AppendLine();
            
//             // 写入主键名（假设第一个字段是主键，或需要外部指定）
//             sb.AppendLine(fields[0].Name);
            
//             // 写入数据
//             enumerator.Reset();
//             while (enumerator.MoveNext())
//             {
//                 var pair = (System.Collections.DictionaryEntry)enumerator.Current;
//                 var data = pair.Value;
                
//                 for (int i = 0; i < fields.Length; i++)
//                 {
//                     var val = fields[i].GetValue(data);
//                     sb.Append(val?.ToString() ?? "");
//                     if (i < fields.Length - 1) sb.Append('\t');
//                 }
//                 sb.AppendLine();
//             }

//             File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
//             Debug.Log($"[DataMgr] 导出TXT完成: {outputPath}");
//         }
//     }
// }