using UnityEditor;
using UnityEngine;
using System.IO;

namespace DataCenter
{
    /// <summary>
    /// Excel转TXT编辑器窗口
    /// </summary>
    public class ExcelToTextWindow : EditorWindow
    {
        private string _excelFilePath; // 选择的Excel文件路径
        private string _outputTxtPath; // 输出TXT路径
        private int _sheetIndex = 0;   // 要转换的Sheet索引

        // 在Unity编辑器菜单中添加入口
        [MenuItem("Tools/DataTable/Convert Excel to TXT")]
        public static void ShowWindow()
        {
            // 打开编辑器窗口
            GetWindow<ExcelToTextWindow>("Excel转TXT工具");
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
                bool success = ConvertExcelToText.ConvertExcelToTxt(_excelFilePath, _outputTxtPath, _sheetIndex);
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
}