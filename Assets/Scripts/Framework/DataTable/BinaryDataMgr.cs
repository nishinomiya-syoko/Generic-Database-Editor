using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// 二进制数据管理器
    /// 负责加载StreamingAssets下的二进制数据表文件，以及运行时数据的保存/读取
    /// </summary>
    public class BinaryDataMgr
    {
        // 单例实例
        private static BinaryDataMgr instance = new BinaryDataMgr();
        public static BinaryDataMgr Instance => instance;
        
        // 私有构造函数，确保单例
        private BinaryDataMgr() { }

        /// <summary>
        /// 存储已加载的Excel数据表容器（键：容器类型名，值：容器实例）
        /// </summary>
        private Dictionary<string, object> tableDic = new Dictionary<string, object>();

        /// <summary>
        /// 运行时数据保存路径（PersistentDataPath）
        /// </summary>
        private static string SAVE_PATH = Application.persistentDataPath + "/Data/";
        
        /// <summary>
        /// 二进制数据表文件根路径（StreamingAssets/Bianry/）
        /// 注：Bianry为自定义目录名，注意拼写
        /// </summary>
        public static string DATA_BINARY_PATH = Application.streamingAssetsPath + "/Bianry/";

        public static string CUSTOM_NAME_END = ".pve";

        /// <summary>
        /// 加载二进制数据表（单泛型版本）
        /// </summary>
        /// <typeparam name="T">数据表容器类型（需包含dataDic字典字段）</typeparam>
        public void LoadTable<T>()
        {
            // 拼接Row前缀的数据结构类型名（例如 DTBasicData -> RowDTBasicData）
            string RowTableName = "Row" + typeof(T).Name;
            
            // 打开对应名称的二进制文件（.pve为自定义后缀）
            using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(T).Name + ".pve", FileMode.Open, FileAccess.Read))
            {
                // 读取文件所有字节
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                
                // 读取偏移量（初始为0）
                int index = 0;

                // 1. 读取数据总条数（4字节int）
                int count = BitConverter.ToInt32(bytes, index);
                index += 4;

                // 2. 读取主键字段名（先读长度，再读字符串）
                int keyNameLength = BitConverter.ToInt32(bytes, index);
                index += 4;
                string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
                index += keyNameLength;

                // 3. 创建数据表容器实例
                Type containerType = typeof(T);
                object containerObj = Activator.CreateInstance(containerType);
                
                // 4. 获取对应的数据结构类型（DataCenter命名空间下的Row开头类型）
                Type classType = Type.GetType("DataCenter." + RowTableName);
                if (classType == null)
                {
                    Debug.LogError($"未找到数据结构类型：DataCenter.{RowTableName}");
                    return;
                }
                
                // 5. 获取数据结构的所有字段信息
                FieldInfo[] infos = classType.GetFields();
                
                // 6. 遍历读取每条数据记录
                for (int i = 0; i < count; i++)
                {
                    // 创建数据结构实例
                    object dataObj = Activator.CreateInstance(classType);
                    Debug.Log(dataObj);

                    // 遍历字段，按类型读取字节并赋值
                    foreach (FieldInfo info in infos)
                    {
                        if (info.FieldType == typeof(int))
                        {
                            // int类型：4字节
                            info.SetValue(dataObj, BitConverter.ToInt32(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(float))
                        {
                            // float类型：4字节
                            info.SetValue(dataObj, BitConverter.ToSingle(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(bool))
                        {
                            // bool类型：1字节
                            info.SetValue(dataObj, BitConverter.ToBoolean(bytes, index));
                            index += 1;
                        }
                        else if (info.FieldType == typeof(string))
                        {
                            // string类型：先读长度（4字节），再读字符串内容
                            int length = BitConverter.ToInt32(bytes, index);
                            index += 4;
                            info.SetValue(dataObj, Encoding.UTF8.GetString(bytes, index, length));
                            index += length;
                        }
                    }

                    // 7. 将数据记录添加到容器的dataDic字典中
                    // 获取容器的dataDic字段实例
                    object dicObj = containerType.GetField("dataDic").GetValue(containerObj);
                    // 获取字典的Add方法
                    MethodInfo addMethod = dicObj.GetType().GetMethod("Add");
                    // 获取当前数据的主键值
                    object keyValue = classType.GetField(keyName).GetValue(dataObj);
                    // 调用Add方法添加数据到字典
                    addMethod.Invoke(dicObj, new object[] { keyValue, dataObj });
                }

                // 8. 将容器添加到全局表字典
                tableDic.Add(typeof(T).Name, containerObj);
                fs.Close();
            }
        }

        /// <summary>
        /// 加载二进制数据表（双泛型版本）
        /// </summary>
        /// <typeparam name="T">数据表容器类型（需包含dataDic字典字段）</typeparam>
        /// <typeparam name="K">数据结构类型（对应Excel行数据）</typeparam>
        public void LoadTable<T, K>()
        {
            // 打开对应数据结构名称的二进制文件（.pve为自定义后缀）
            using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(K).Name + ".pve", FileMode.Open, FileAccess.Read))
            {
                // 读取文件所有字节
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                
                // 读取偏移量（初始为0）
                int index = 0;

                // 1. 读取数据总条数（4字节int）
                int count = BitConverter.ToInt32(bytes, index);
                index += 4;

                // 2. 读取主键字段名（先读长度，再读字符串）
                int keyNameLength = BitConverter.ToInt32(bytes, index);
                index += 4;
                string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
                index += keyNameLength;

                // 3. 创建数据表容器实例
                Type containerType = typeof(T);
                object containerObj = Activator.CreateInstance(containerType);
                
                // 4. 获取数据结构类型
                Type classType = typeof(K);
                // 获取数据结构的所有字段信息
                FieldInfo[] infos = classType.GetFields();
                
                // 5. 遍历读取每条数据记录
                for (int i = 0; i < count; i++)
                {
                    // 创建数据结构实例
                    object dataObj = Activator.CreateInstance(classType);
                    
                    // 遍历字段，按类型读取字节并赋值
                    foreach (FieldInfo info in infos)
                    {
                        if (info.FieldType == typeof(int))
                        {
                            // int类型：4字节
                            info.SetValue(dataObj, BitConverter.ToInt32(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(float))
                        {
                            // float类型：4字节
                            info.SetValue(dataObj, BitConverter.ToSingle(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(bool))
                        {
                            // bool类型：1字节
                            info.SetValue(dataObj, BitConverter.ToBoolean(bytes, index));
                            index += 1;
                        }
                        else if (info.FieldType == typeof(string))
                        {
                            // string类型：先读长度（4字节），再读字符串内容
                            int length = BitConverter.ToInt32(bytes, index);
                            index += 4;
                            info.SetValue(dataObj, Encoding.UTF8.GetString(bytes, index, length));
                            index += length;
                        }
                    }

                    // 6. 将数据记录添加到容器的dataDic字典中
                    object dicObj = containerType.GetField("dataDic").GetValue(containerObj);
                    MethodInfo addMethod = dicObj.GetType().GetMethod("Add");
                    object keyValue = classType.GetField(keyName).GetValue(dataObj);
                    addMethod.Invoke(dicObj, new object[] { keyValue, dataObj });
                }

                // 7. 将容器添加到全局表字典
                tableDic.Add(typeof(T).Name, containerObj);
                fs.Close();
            }
        }

        /// <summary>
        /// 获取数据表容器（泛型约束：DataTable）
        /// </summary>
        /// <typeparam name="T">数据表容器类型（继承自DataTable）</typeparam>
        /// <returns>数据表容器实例，不存在则返回null</returns>
        public T GetData<T>() where T : DataTable
        {
            string tableName = typeof(T).Name;
            Debug.Log(tableName);
            if (tableDic.ContainsKey(tableName))
            {
                return tableDic[tableName] as T;
            }
            return null;
        }

        /// <summary>
        /// 获取数据表容器（无基类约束版本）
        /// 注：建议数据表容器以DT开头命名，便于识别
        /// </summary>
        /// <typeparam name="T">数据表容器类型</typeparam>
        /// <returns>数据表容器实例，不存在则返回null</returns>
        public T GetTable<T>() where T : class
        {
            string tableName = typeof(T).Name;
            
            // 警告：非DT开头的表名可能不符合命名规范
            if (!tableName.StartsWith("DT"))
                Debug.LogWarning($"获取的表名{tableName}不以DT开头，可能无法正确获取数据");
            
            if (tableDic.ContainsKey(tableName))
            {
                return tableDic[tableName] as T;
            }
            return null;
        }

        /// <summary>
        /// 保存运行时数据到本地（二进制序列化）
        /// </summary>
        /// <param name="data">要保存的数据对象（需可序列化）</param>
        /// <param name="fileName">保存的文件名（无需后缀）</param>
        public void Save(object data, string fileName)
        {
            // 确保保存目录存在
            if (!Directory.Exists(SAVE_PATH))
            {
                Directory.CreateDirectory(SAVE_PATH);
            }

            // 写入二进制文件（.pve后缀）
            using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".pve", FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, data);

                fs.Flush();
                fs.Close();
            }
        }

        /// <summary>
        /// 从本地加载运行时数据（二进制反序列化）
        /// </summary>
        /// <typeparam name="T">要加载的数据类型</typeparam>
        /// <param name="fileName">文件名（无需后缀）</param>
        /// <returns>加载的数据对象，文件不存在则返回默认值</returns>
        public T Load<T>(string fileName) where T : class
        {
            // 检查文件是否存在
            if (!File.Exists(SAVE_PATH + fileName + ".pve"))
            {
                return default(T);
            }

            T data;
            using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".pve", FileMode.Open, FileAccess.Read))
            {
                BinaryFormatter bf = new BinaryFormatter();
                data = bf.Deserialize(fs) as T;
                fs.Close();
            }
            return data;
        }
    }
}