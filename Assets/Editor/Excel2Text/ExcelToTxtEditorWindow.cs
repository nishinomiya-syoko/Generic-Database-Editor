using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

/// <summary>
/// Excel转TXT编辑器窗口
/// </summary>
public class ExcelToTxtEditorWindow : EditorWindow
{
    private static string _excelFilePath = Constant.EXCEL_PATH;
    private static string _outputTxtPath = Constant.DATA_TXT_PATH;
    private int _sheetIndex = 0;   // 要转换的Sheet索引

    // 批量处理统计
        private int totalFiles = 0;
        private int successFiles = 0;
        private int failedFiles = 0;
        private int totalSheets = 0;
        private int successSheets = 0;

    [MenuItem("Tools/DataTable/编辑文件路径")]
    public static void OpenConstant()
    {
        IDEOpenHelper.OpenScriptAtLine(Application.dataPath +"/Scripts/Framework/Extension/Constant.cs");
    }
    // 在Unity编辑器菜单中添加入口
    [MenuItem("Tools/DataTable/Excel To TXT Converter")]
    public static void ShowWindow()
    {
        // 打开编辑器窗口
        GetWindow<ExcelToTxtEditorWindow>("Excel转TXT工具");
    }
    /// <summary>
    /// 批量转换文件夹下所有Excel文件为TXT
    /// </summary>
    /// <param name="_excelFilePath">Excel根文件夹</param>
    /// <param name="_outputTxtPath">TXT输出根文件夹</param>
    [MenuItem("Tools/DataTable/Excel批量处理")]
    public static void BatchConvertExcelToTxt()
    {
        try
        {
            if (!Directory.Exists(_excelFilePath))
            {
                EditorUtility.DisplayDialog("错误", "Excel源文件夹不存在！", "确定");
                return;
            }

            // 查找所有.xlsx文件（包括子文件夹）
            string[] excelFiles = Directory.GetFiles(_excelFilePath, "*.xlsx", SearchOption.AllDirectories);
            if (excelFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "未找到任何.xlsx文件！", "确定");
                return;
            }
            if (!Directory.Exists(_outputTxtPath))
            {
                Directory.CreateDirectory(_outputTxtPath);
            }
            string[] txtFiles = Directory.GetFiles(_outputTxtPath, "*.txt", SearchOption.AllDirectories);
            foreach (string txtFile in txtFiles)
            {
                File.Delete(txtFile);
            }

            // 显示进度条
            int successCount = 0;
            int failCount = 0;
            EditorUtility.DisplayProgressBar("批量转换Excel", "开始处理...", 0);

            for (int i = 0; i < excelFiles.Length; i++)
            {
                string excelPath = excelFiles[i];
                string relativePath = Path.GetRelativePath(_excelFilePath, excelPath);
                string txtPath = Path.Combine(_outputTxtPath, Path.ChangeExtension(relativePath, "txt"));

                // 更新进度条
                float progress = (float)(i + 1) / excelFiles.Length;
                EditorUtility.DisplayProgressBar("批量转换Excel", $"处理：{Path.GetFileName(excelPath)}", progress);

                // 调用原有转换逻辑
                bool success = ExcelToTxtConverter.ConvertExcelToTxt(excelPath, txtPath);
                if (success)
                    successCount++;
                else
                    failCount++;
            }

            // 关闭进度条
            EditorUtility.ClearProgressBar();

            // 显示结果
            string resultMsg = $"批量转换完成！\n成功：{successCount} 个\n失败：{failCount} 个";
            EditorUtility.DisplayDialog("批量转换结果", resultMsg, "确定");

            // 打开输出目录
            EditorUtility.RevealInFinder(_outputTxtPath);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", "批量处理出错：" + e.Message, "确定");
        }
    }


    private void OnGUI()
    {
        // 窗口标题
        GUILayout.Label("Excel转TXT配置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 1. 选择Excel文件
        GUILayout.Label("1. 选择Excel文件（.xlsx）");
        GUILayout.BeginHorizontal();
        _excelFilePath = EditorGUILayout.TextField("文件路径", _excelFilePath);
        if (GUILayout.Button("选择文件", GUILayout.Width(80)))
        {
            // 打开文件选择对话框，仅显示.xlsx文件
            string path = EditorUtility.OpenFilePanel("选择Excel文件", "", "xlsx");
            if (!string.IsNullOrEmpty(path))
            {
                _excelFilePath = path;
                // 自动填充输出路径（与Excel同目录，同名.txt）
                _outputTxtPath = Path.ChangeExtension(path, "txt");
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // 2. 设置输出路径
        GUILayout.Label("2. 设置TXT输出路径");
        GUILayout.BeginHorizontal();
        _outputTxtPath = EditorGUILayout.TextField("输出路径", _outputTxtPath);
        if (GUILayout.Button("选择路径", GUILayout.Width(80)))
        {
            // 打开保存对话框
            string path = EditorUtility.SaveFilePanel("保存TXT文件", "", Path.GetFileNameWithoutExtension(_excelFilePath) + ".txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                _outputTxtPath = path;
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // 3. 设置Sheet索引
        GUILayout.Label("3. Excel Sheet索引（从0开始）");
        _sheetIndex = EditorGUILayout.IntField("Sheet索引", _sheetIndex);
        GUILayout.Space(20);

        // 4. 转换按钮
        GUI.enabled = !string.IsNullOrEmpty(_excelFilePath) && !string.IsNullOrEmpty(_outputTxtPath);
        if (GUILayout.Button("开始转换", GUILayout.Height(40)))
        {
            bool success = ExcelToTxtConverter.ConvertExcelToTxt(_excelFilePath, _outputTxtPath, _sheetIndex);
            if (success)
            {
                EditorUtility.DisplayDialog("成功", "Excel转TXT完成！", "确定");
                // 转换成功后打开输出目录
                EditorUtility.RevealInFinder(_outputTxtPath);
            }
            else
            {
                EditorUtility.DisplayDialog("失败", "转换出错，请查看控制台日志", "确定");
            }
        }
        GUI.enabled = true;
    }
}