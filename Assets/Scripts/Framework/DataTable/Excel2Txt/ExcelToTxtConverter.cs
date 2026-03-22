using System;
using System.IO;
using OfficeOpenXml;
using UnityEngine;

/// <summary>
/// Excel转TXT核心转换类
/// </summary>
public static class ExcelToTxtConverter
{
    /// <summary>
    /// 将Excel文件转换为TXT文本
    /// </summary>
    /// <param name="excelPath">Excel文件路径（.xlsx）</param>
    /// <param name="outputPath">TXT输出路径</param>
    /// <param name="sheetIndex">要转换的Sheet索引（默认0，第一个Sheet）</param>
    /// <returns>是否转换成功</returns>
    public static bool ConvertExcelToTxt(string excelPath, string outputPath, int sheetIndex = 0)
    {
        // 校验输入路径
        if (!File.Exists(excelPath))
        {
            Debug.LogError($"Excel文件不存在：{excelPath}");
            return false;
        }

        // 校验文件格式
        if (!excelPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("仅支持.xlsx格式的Excel文件，请转换后重试");
            return false;
        }

        try
        {
            // 设置EPPlus许可证上下文（4.x版本必需）
            // ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // 读取Excel文件
            using (var package = new ExcelPackage(new FileInfo(excelPath)))
            {
                // 校验Sheet索引
                if (sheetIndex < 0 || sheetIndex >= package.Workbook.Worksheets.Count)
                {
                    Debug.LogError($"Sheet索引无效，当前Excel有{package.Workbook.Worksheets.Count}个Sheet");
                    return false;
                }

                var worksheet = package.Workbook.Worksheets[sheetIndex];
                var sb = new System.Text.StringBuilder();

                // 获取Excel的有效行列范围（避免空行空列冗余）
                int rowCount = worksheet.Dimension?.Rows ?? 0;
                int colCount = worksheet.Dimension?.Columns ?? 0;

                if (rowCount == 0 || colCount == 0)
                {
                    Debug.LogWarning("Excel Sheet中无有效数据");
                    return false;
                }

                // 逐行读取数据
                for (int row = 1; row <= rowCount; row++) // Excel行号从1开始
                {
                    // 逐列读取单元格
                    for (int col = 1; col <= colCount; col++)
                    {
                        // 获取单元格值，空单元格用[空格]替换
                        var cellValue = worksheet.Cells[row, col].Text ?? " ";
                        // 第一行第二行是字段名
                        // if (cellValue.StartsWith("#") || cellValue.StartsWith("$") || cellValue.StartsWith("//"))
                        //     break;
                        // 替换单元格内的换行符（避免破坏TXT布局）
                        cellValue = cellValue.Replace("\n", " ").Replace("\r", "");
                        // 添加单元格值，列之间用制表符分隔
                        sb.Append(cellValue);

                        // 最后一列不添加制表符
                        if (col < colCount)
                        {
                            sb.Append("\t");
                        }
                    }

                    // 最后一行不添加换行符
                    if (row < rowCount)
                    {
                        sb.Append(Environment.NewLine);
                    }
                }

                // 创建输出目录（如果不存在）
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 写入TXT文件（UTF-8编码，确保中文正常显示）
                File.WriteAllText(outputPath, sb.ToString(), System.Text.Encoding.UTF8);

                Debug.Log($"转换成功！TXT文件路径：{outputPath}");
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"转换失败：{e.Message}\n{e.StackTrace}");
            return false;
        }
    }
}