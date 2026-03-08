using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// 2�������ݹ�����
    /// </summary>
    public class BinaryDataMgr
    {
        private static BinaryDataMgr instance = new BinaryDataMgr();
        public static BinaryDataMgr Instance => instance;
        private BinaryDataMgr() { }

        /// <summary>
        /// ���ڴ洢����Excel�����ݵ�����
        /// </summary>
        private Dictionary<string, object> tableDic = new Dictionary<string, object>();

        /// <summary>
        /// ���ݴ洢·��
        /// </summary>
        private static string SAVE_PATH = Application.persistentDataPath + "/Data/";
        /// <summary>
        /// ���������ݴ洢·��
        /// </summary>
        public static string DATA_BINARY_PATH = Application.streamingAssetsPath + "/Bianry/";

        public void InitData()
        {
            //�����Լ��ı����ڴ�
            //LoadTable<TowerInfoContainer, TowerInfo>();
            //LoadTable<PlayerInfo1Container, PlayerInfo1>();
            //LoadTable<TestInfoContainer, TestInfo>();
            //LoadTable<BasicDataContainer, BasicData>();
            LoadTable<DTBasicData>();
            Debug.Log("���ݼ������");
        }
        public void LoadTable<T>()
        {
            string drTableName = "DR" + typeof(T).Name;
            //��ȡExcel����Ӧ��2�����ļ�  �����н���
            using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(T).Name + ".zhou", FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                //���ڼ�¼��ǰ��ȡ�˶����ֽ�
                int index = 0;

                //��ȡ����������
                int count = BitConverter.ToInt32(bytes, index);
                index += 4;

                //��ȡ����������
                int keyNameLength = BitConverter.ToInt32(bytes, index);
                index += 4;
                string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
                index += keyNameLength;

                //�������������
                Type contaninerType = typeof(T);
                object contaninerObj = Activator.CreateInstance(contaninerType);
                //�õ����ݽṹ��Type
                Type classType = Type.GetType( "DataCenter." + drTableName);
                //ͨ������õ����ݽṹ�� �����ֶε���Ϣ
                FieldInfo[] infos = classType.GetFields();
                //��ȡÿһ�е���Ϣ
                for (int i = 0; i < count; i++)
                {
                    //ʵ����һ���ṹ������ ����
                    object dataObj = Activator.CreateInstance(classType);
                    Debug.Log(dataObj);
                    foreach (FieldInfo info in infos)
                    {
                        if (info.FieldType == typeof(int))
                        {
                            //�൱�ڽ�2��������תΪint Ȼ��ֵ����Ӧ�ֶ�
                            info.SetValue(dataObj, BitConverter.ToInt32(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(float))
                        {
                            info.SetValue(dataObj, BitConverter.ToSingle(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(bool))
                        {
                            info.SetValue(dataObj, BitConverter.ToBoolean(bytes, index));
                            index += 1;
                        }
                        else if (info.FieldType == typeof(string))
                        {
                            //��ȡ�ַ����ֽ����鳤��
                            int length = BitConverter.ToInt32(bytes, index);
                            index += 4;
                            info.SetValue(dataObj, Encoding.UTF8.GetString(bytes, index, length));
                            index += length;
                        }
                    }

                    //��ȡ��һ�е����ݺ� ��������ݴ浽����������
                    object dicObj = contaninerType.GetField("dataDic").GetValue(contaninerObj);

                    //�õ������ֶζ���
                    MethodInfo mInfo = dicObj.GetType().GetMethod("Add");
                    //�õ����ݽṹ������� ָ�������ֶε�ֵ
                    object keyValue = classType.GetField(keyName).GetValue(dataObj);
                    mInfo.Invoke(dicObj, new object[] { keyValue, dataObj });
                }

                //�Ѷ�ȡ��ı���¼����
                tableDic.Add(typeof(T).Name, contaninerObj);
                fs.Close();

            }
        }
        /// <summary>
        /// ����Excel����2�������ݵ��ڴ���
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <typeparam name="K">���ݽṹ������</typeparam>
        public void LoadTable<T, K>()
        {
            //��ȡExcel����Ӧ��2�����ļ�  �����н���
            using (FileStream fs = File.Open(DATA_BINARY_PATH + typeof(K).Name + ".zhou", FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                //���ڼ�¼��ǰ��ȡ�˶����ֽ�
                int index = 0;

                //��ȡ����������
                int count = BitConverter.ToInt32(bytes, index);
                index += 4;

                //��ȡ����������
                int keyNameLength = BitConverter.ToInt32(bytes, index);
                index += 4;
                string keyName = Encoding.UTF8.GetString(bytes, index, keyNameLength);
                index += keyNameLength;

                //�������������
                Type contaninerType = typeof(T);
                object contaninerObj = Activator.CreateInstance(contaninerType);
                //�õ����ݽṹ��Type
                Type classType = typeof(K);
                //ͨ������õ����ݽṹ�� �����ֶε���Ϣ
                FieldInfo[] infos = classType.GetFields();
                //��ȡÿһ�е���Ϣ
                for (int i = 0; i < count; i++)
                {
                    //ʵ����һ���ṹ������ ����
                    object dataObj = Activator.CreateInstance(classType);
                    foreach (FieldInfo info in infos)
                    {
                        if (info.FieldType == typeof(int))
                        {
                            //�൱�ڽ�2��������תΪint Ȼ��ֵ����Ӧ�ֶ�
                            info.SetValue(dataObj, BitConverter.ToInt32(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(float))
                        {
                            info.SetValue(dataObj, BitConverter.ToSingle(bytes, index));
                            index += 4;
                        }
                        else if (info.FieldType == typeof(bool))
                        {
                            info.SetValue(dataObj, BitConverter.ToBoolean(bytes, index));
                            index += 1;
                        }
                        else if (info.FieldType == typeof(string))
                        {
                            //��ȡ�ַ����ֽ����鳤��
                            int length = BitConverter.ToInt32(bytes, index);
                            index += 4;
                            info.SetValue(dataObj, Encoding.UTF8.GetString(bytes, index, length));
                            index += length;
                        }
                    }

                    //��ȡ��һ�е����ݺ� ��������ݴ浽����������
                    object dicObj = contaninerType.GetField("dataDic").GetValue(contaninerObj);

                    //�õ������ֶζ���
                    MethodInfo mInfo = dicObj.GetType().GetMethod("Add");
                    //�õ����ݽṹ������� ָ�������ֶε�ֵ
                    object keyValue = classType.GetField(keyName).GetValue(dataObj);
                    mInfo.Invoke(dicObj, new object[] { keyValue, dataObj });
                }

                //�Ѷ�ȡ��ı���¼����
                tableDic.Add(typeof(T).Name, contaninerObj);
                fs.Close();

            }
        }

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
        /// �õ�һ�ű�����Ϣ
        /// </summary>
        /// <typeparam name="T">��������</typeparam>
        /// <returns></returns>
        public T GetTable<T>() where T : class
        {
            string tableName = typeof(T).Name;
            if (!tableName.StartsWith("DT"))
               Debug.LogWarning("���ȡ�ı���������DT��ͷ�����ܲ������ݱ�������");
            if (tableDic.ContainsKey(tableName))
            {
                return tableDic[tableName] as T;
            }
            return null;
        }

        /// <summary>
        /// �洢���������
        /// </summary>
        /// <param name="data"></param>
        /// <param name="fileName"></param>
        public void Save(object data, string fileName)
        {
            if (!Directory.Exists(SAVE_PATH))
            {
                Directory.CreateDirectory(SAVE_PATH);
            }

            using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".zhou", FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, data);

                fs.Flush();
                fs.Close();
            }
        }

        /// <summary>
        /// ��ȡ2��������ת���ɶ���
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public T Load<T>(string fileName) where T : class
        {
            if (!File.Exists(SAVE_PATH + fileName + ".zhou"))
            {
                return default(T);
            }

            T data;
            using (FileStream fs = new FileStream(SAVE_PATH + fileName + ".zhou", FileMode.Open, FileAccess.Read))
            {
                BinaryFormatter bf = new BinaryFormatter();
                data = bf.Deserialize(fs) as T;
                fs.Close();
            }
            return data;
        }

    }
}