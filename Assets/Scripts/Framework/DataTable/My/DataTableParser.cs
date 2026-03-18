using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DataCenter
{
    /// <summary>
    /// TXT数据表通用解析工具
    /// </summary>
    public static class DataTableParser
    {
        /// <summary>
        /// 解析TXT表头信息（变量名、类型、注释）
        /// </summary>
        /// <param name="txtPath">TXT文件路径</param>
        /// <param name="fieldNames">输出：变量名列表</param>
        /// <param name="fieldTypes">输出：变量类型列表</param>
        /// <param name="fieldComments">输出：变量注释列表</param>
        /// <returns>是否解析成功</returns>
        public static bool ParseTableHeader(string txtPath, out List<string> fieldNames, out List<string> fieldTypes, out List<string> fieldComments)
        {
            fieldNames = new List<string>();
            fieldTypes = new List<string>();
            fieldComments = new List<string>();

            if (!File.Exists(txtPath))
            {
                Debug.LogError($"TXT文件不存在：{txtPath}");
                return false;
            }

            try
            {
                // 按行读取TXT（UTF-8编码）
                string[] allLines = File.ReadAllLines(txtPath, Encoding.UTF8);

                // 校验表头行数（至少3行）
                if (allLines.Length < 3)
                {
                    Debug.LogError("TXT数据表表头不完整（至少需要3行：变量名、类型、注释）");
                    return false;
                }

                // 解析第一行：变量名（制表符分隔）
                fieldNames = SplitLineByTab(allLines[0]);
                // 解析第二行：变量类型
                fieldTypes = SplitLineByTab(allLines[1]);
                // 解析第三行：变量注释
                fieldComments = SplitLineByTab(allLines[2]);

                // 校验列数一致
                if (fieldNames.Count != fieldTypes.Count || fieldNames.Count != fieldComments.Count)
                {
                    Debug.LogError("表头行的列数不一致，请检查TXT文件");
                    return false;
                }

                // 校验必填的ID字段（建议数据表以ID为唯一键）
                if (!fieldNames.Contains("ID"))
                {
                    Debug.LogWarning("数据表未包含ID字段，建议添加ID作为唯一标识");
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"解析表头失败：{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 解析TXT数据行（第四行及以后）
        /// </summary>
        /// <param name="txtPath">TXT文件路径</param>
        /// <param name="fieldNames">变量名列表</param>
        /// <param name="fieldTypes">变量类型列表</param>
        /// <returns>数据字典列表（key=变量名，value=对应类型的值）</returns>
        public static List<Dictionary<string, object>> ParseTableData(string txtPath, List<string> fieldNames, List<string> fieldTypes)
        {
            List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();

            if (!File.Exists(txtPath))
            {
                Debug.LogError($"TXT文件不存在：{txtPath}");
                return dataList;
            }

            try
            {
                string[] allLines = File.ReadAllLines(txtPath, Encoding.UTF8);

                // 从第四行开始解析数据
                for (int i = 3; i < allLines.Length; i++)
                {
                    string line = allLines[i].Trim();
                    // 跳过空行
                    if (string.IsNullOrEmpty(line)) continue;

                    List<string> cellValues = SplitLineByTab(line);
                    Dictionary<string, object> dataDict = new Dictionary<string, object>();

                    // 逐列转换数据类型
                    for (int j = 0; j < fieldNames.Count; j++)
                    {
                        string fieldName = fieldNames[j];
                        string fieldType = fieldTypes[j];
                        string cellValue = j < cellValues.Count ? cellValues[j].Trim() : string.Empty;

                        // 类型转换
                        object value = ConvertValueToType(cellValue, fieldType);
                        dataDict.Add(fieldName, value);
                    }

                    dataList.Add(dataDict);
                }

                Debug.Log($"解析数据完成，共加载{dataList.Count}行数据");
                return dataList;
            }
            catch (Exception e)
            {
                Debug.LogError($"解析数据行失败：{e.Message}\n{e.StackTrace}");
                return dataList;
            }
        }

        /// <summary>
        /// 按制表符分割行内容（处理连续制表符、末尾制表符）
        /// </summary>
        private static List<string> SplitLineByTab(string line)
        {
            List<string> parts = new List<string>();
            // SplitOptions.None 保留空值（对应空单元格）
            string[] splitParts = line.Split(new[] { '\t' }, StringSplitOptions.None);
            parts.AddRange(splitParts);
            return parts;
        }

        /// <summary>
        /// 将字符串值转换为指定类型
        /// </summary>
        private static object ConvertValueToType(string value, string targetType)
        {
            try
            {
                switch (targetType.ToLower())
                {
                    case "int":
                        return string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
                    case "float":
                        return string.IsNullOrEmpty(value) ? 0f : float.Parse(value);
                    case "bool":
                        return string.IsNullOrEmpty(value) ? false : bool.Parse(value);
                    case "string":
                        return value ?? string.Empty;
                    default:
                        Debug.LogWarning($"不支持的类型：{targetType}，默认按string处理");
                        return value ?? string.Empty;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"类型转换失败：值={value}，目标类型={targetType}，错误：{e.Message}");
                // 返回对应类型的默认值
                return GetDefaultValue(targetType);
            }
        }

        /// <summary>
        /// 获取指定类型的默认值
        /// </summary>
        private static object GetDefaultValue(string type)
        {
            switch (type.ToLower())
            {
                case "int": return 0;
                case "float": return 0f;
                case "bool": return false;
                default: return string.Empty;
            }
        }
    }
}