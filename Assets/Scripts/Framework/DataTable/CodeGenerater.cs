using ExcelDataReader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DataCenter
{
    public class ExcelTool
    {
        /// <summary>
        /// excel文件存放路径
        /// </summary>
        public static string EXCEL_PATH = Constant.EXCEL_PATH;

        /// <summary>
        /// 数据结构类脚本存储路径
        /// </summary>
        public static string DATA_CLASS_PATH = Constant.DATA_CLASS_PATH;

        /// <summary>
        /// 容器类脚本存储路径
        /// </summary>
        public static string DATA_CONTAINER_PATH = Constant.DATA_CONTAINER_PATH;

        /// <summary>
        /// 二进制数据存储路径
        /// </summary>
        //public static string DATA_BINARY_PATH = Application.streamingAssetsPath + "/Bianry/";


        public static string CUSTOM_NAME_END = ".pve";
        public static string CLASS_NAMESPACE = "Top";
        public static string ROW_NAME_HEAD = "DR";
        public static string CONTAINER_NAME_HEAD = "Container";

        /// <summary>
        /// 真正内容开始行号
        /// </summary>
        public static int BEGIN_INDEX = 4;

        [MenuItem("Tools/Excel2Datatable/Generate Excel to Binary Class")]
        private static void GenerateExcelInfo()
        {
            //加载指定路径中的所有Excel文件 用于生成对应的3个文件
            // if (!Directory.Exists(EXCEL_PATH))
            //     Directory.CreateDirectory(EXCEL_PATH);
            //得到指定路径中的所有文件信息 相当于就是得到所有的Exce1表
            FileInfo[] files = Directory.CreateDirectory(EXCEL_PATH).GetFiles();
            //数据容器
            DataTableCollection tableCollection;
            for (int i = 0; i < files.Length; i++)
            {
                //如果不是Excel文件就不要处理
                if (files[i].Extension != ".xlsx" && files[i].Extension != ".xls")
                    continue;
                else if (files[i].Name.StartsWith("~$"))
                    continue;
                //打开一个excel文件得到其中所有表的数据
                using (FileStream fs = files[i].Open(FileMode.Open, FileAccess.Read))
                {
                    IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(fs);
                    tableCollection = excelReader.AsDataSet().Tables;
                    fs.Close();
                }
                //遍历文件中的所有表的信息
                foreach (System.Data.DataTable table in tableCollection)
                {
                    Debug.Log(table.TableName);
                    //生成数据结构类
                    GenerateDataClass(table);
                    //生成容器类
                    GenerateDataContainer(table);
                    //生成2进制数据
                    ExchangeExcelToBinary(table);
                }
            }
        }

        /// <summary>
        /// 生成Excel表对应的数据结构类
        /// </summary>
        /// <param name="table"></param>
        private static void GenerateDataClass(System.Data.DataTable table)
        {
            //字段名行
            DataRow rowName = GetVariableNameRow(table);
            //字段类型行
            DataRow rowType = GetVariableTypeRow(table);
            //注释
            DataRow rowComment = GetVariableCommentRow(table);
            //判断路径是否存在 没有的话 就创建文件夹
            if (!Directory.Exists(DATA_CLASS_PATH))
                Directory.CreateDirectory(DATA_CLASS_PATH);
            string[] files = Directory.GetFiles(DATA_CLASS_PATH);
            foreach (string file in files)
            {
                File.Delete(file);
            }
            string fileName = ROW_NAME_HEAD + table.TableName;
             // 文件头
             StringBuilder sb = new StringBuilder();
            sb.AppendLine("// ============================================================");
            sb.AppendLine($"// 自动生成的数据表类 - {table.TableName}");
            sb.AppendLine($"// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// 请勿手动修改，修改会被覆盖");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine($"namespace {CLASS_NAMESPACE}");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic class {fileName}");
            sb.AppendLine("\t{");

            //变量进行字符串拼接
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (rowType[i].ToString() == string.Empty || rowName[i].ToString() == string.Empty)
                    continue;

                // str += "\t\t/// <summary>" + rowComment[i].ToString() + "</summary>\n";
                sb.AppendLine($"\t\t/// <summary>{rowComment[i].ToString()}</summary>");
                sb.AppendLine($"\t\tpublic {rowType[i].ToString()} {rowName[i].ToString()};");
                sb.AppendLine();
                //拼接字段类型和变量名 比如 int id;
            }    
            sb.AppendLine("\t}");
            sb.AppendLine("}");
        

            //把拼接好的字符串存进占地文件中
            File.WriteAllText(DATA_CLASS_PATH + fileName + ".cs", sb.ToString());
            //刷新Project窗口
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 生成Excel表对应的数据容器类
        /// </summary>
        /// <param name="table"></param>
        private static void GenerateDataContainer(System.Data.DataTable table)
        {
            string fullName = CONTAINER_NAME_HEAD + table.TableName;
            //得到主键索引
            int keyIndex = GetKeyIndex(table);
            //得到字段类型行
            DataRow rowType = GetVariableTypeRow(table);
            //没有路径就创建路径
            if (!Directory.Exists(DATA_CONTAINER_PATH))
                Directory.CreateDirectory(DATA_CONTAINER_PATH);

            string[] files = Directory.GetFiles(DATA_CONTAINER_PATH);
            foreach (string file in files)
            {
                File.Delete(file);
            }

            // string str = string.Empty;
            StringBuilder sb = new StringBuilder();
              // 文件头
            sb.AppendLine("// ============================================================");
            sb.AppendLine($"// 自动生成的数据表类 - {table.TableName}");
            sb.AppendLine($"// 生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// 请勿手动修改，修改会被覆盖");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine($"namespace {CLASS_NAMESPACE}");
            sb.AppendLine("{");
            sb.AppendLine("\t\t/// <summary>数据表容器类 " + table.TableName + "</summary>");
            sb.AppendLine($"\t\tpublic class {CONTAINER_NAME_HEAD}" + table.TableName);
            sb.AppendLine("\t\t{");
            sb.AppendLine($"\t\t\tpublic Dictionary<{rowType[keyIndex].ToString()}, {fullName}> dataDic = new Dictionary<{rowType[keyIndex].ToString()}, {fullName}>();");
            sb.AppendLine("\t\t}");
            sb.AppendLine("}");
            

            File.WriteAllText(DATA_CONTAINER_PATH + fullName + ".cs", sb.ToString());

            //刷新Project窗口
            AssetDatabase.Refresh();

        }

        /// <summary>
        /// 生成Excel 2进制数据
        /// </summary>
        /// <param name="table"></param>
        private static void ExchangeExcelToBinary(System.Data.DataTable table)
        {
            //没有路径创建路径
            if (!Directory.Exists(BinaryDataMgr.DATA_BINARY_PATH))
                Directory.CreateDirectory(BinaryDataMgr.DATA_BINARY_PATH);
            string[] files = Directory.GetFiles(BinaryDataMgr.DATA_BINARY_PATH);
            foreach (string file in files)
            {
                File.Delete(file);
            }
            //创建一个2进制文件进行写入
            using (FileStream fs = new FileStream(BinaryDataMgr.DATA_BINARY_PATH + table.TableName + CUSTOM_NAME_END, FileMode.OpenOrCreate, FileAccess.Write))
            {
                //存储具体的Excel对应的2进制信息
                //1.先要存储需要写的行数
                //-4 因为前面4行是配置规则 不是需要记录的数据内容
                //fs.Write(BitConverter.GetBytes(table.Rows.Count - 4), 0, 4);

                // 修改后：
                int valiROW_NAME_HEADowCount = 0;
                for (int i = BEGIN_INDEX; i < table.Rows.Count; i++)
                {
                    if (table.Rows[i][0] == null || table.Rows[i][0].ToString() == string.Empty)
                        continue;
                    ++valiROW_NAME_HEADowCount;
                }
                fs.Write(BitConverter.GetBytes(valiROW_NAME_HEADowCount), 0, 4);

                //2.存储主键的变量名
                string keyName = GetVariableNameRow(table)[GetKeyIndex(table)].ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(keyName);
                //存储字符串字节数据的长度
                fs.Write(BitConverter.GetBytes(keyName.Length), 0, 4);
                //存储字符串字节数组
                fs.Write(bytes, 0, bytes.Length);

                //遍历所有内容的行 进行2进制的写入
                DataRow row;
                //得到类型行 根据类型来决定应该如何写入
                DataRow rowType = GetVariableTypeRow(table);
                for (int i = BEGIN_INDEX; i < table.Rows.Count; i++)
                {
                    //得到一行的数据
                    row = table.Rows[i];
                    //检查
                    if (row[0] == null || row[0].ToString() == string.Empty)
                        continue;
                    for (int j = 0; j < table.Columns.Count; j++)
                    {
                        switch (rowType[j].ToString())
                        {
                            case "int":
                                fs.Write(BitConverter.GetBytes(int.Parse(row[j].ToString())), 0, 4);
                                break;
                            case "float":
                                fs.Write(BitConverter.GetBytes(float.Parse(row[j].ToString())), 0, 4);
                                break;
                            case "bool":
                                fs.Write(BitConverter.GetBytes(bool.Parse(row[j].ToString())), 0, 1);
                                break;
                            case "string":
                                bytes = Encoding.UTF8.GetBytes(row[j].ToString());
                                //写入字节数据的长度
                                fs.Write(BitConverter.GetBytes(bytes.Length), 0, 4);
                                //写入字符串字节数组
                                fs.Write(bytes, 0, bytes.Length);
                                break;
                            // 在GenerateExcelBinary的switch中添加：
                            case "int[]":
                                int[] intArr = Array.ConvertAll(row[j].ToString().Split(','), int.Parse);
                                fs.Write(BitConverter.GetBytes(intArr.Length), 0, 4);
                                foreach (var num in intArr)
                                    fs.Write(BitConverter.GetBytes(num), 0, 4);
                                break;
                        }
                    }
                }

                fs.Close();

            }

            AssetDatabase.Refresh();
        }
        

        /// <summary>
        /// 获取变量名所在行
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static DataRow GetVariableNameRow(System.Data.DataTable table)
        {
            return table.Rows[0];
        }

        /// <summary>
        /// 获取变量类型所在行
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static DataRow GetVariableTypeRow(System.Data.DataTable table)
        {
            return table.Rows[1];
        }
        /// <summary>
        /// 获取变量注释所在行
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static DataRow GetVariableCommentRow(System.Data.DataTable table)
        {
            return table.Rows[2];
        }

        /// <summary>
        /// 获取主键索引
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private static int GetKeyIndex(System.Data.DataTable table)
        {
            DataRow row = table.Rows[3];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (row[i].ToString() == "key")
                    return i;
            }

            return 0;
        }
        
    }
}