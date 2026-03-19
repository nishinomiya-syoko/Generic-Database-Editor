using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using UnityEditorInternal;

public static class IDEOpenHelper
{
    public static void OpenScriptAtLine(string relativeFilePath)
    {
        // 组合并规范化路径（自动处理跨平台差异）
        string fullPath = Path.Combine(Application.dataPath, relativeFilePath);
        fullPath = Path.GetFullPath(fullPath); // 解析相对路径并统一格式
        InternalEditorUtility.OpenFileAtLineExternal(fullPath, 0);
    }
}