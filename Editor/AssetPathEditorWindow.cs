using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OfficeOpenXml;
using Object = UnityEngine.Object;

namespace NiShiMiYa.GenericEditor
{
    /// <summary>
    /// 如果项目里已经定义过 AssetPathData / AssetPathItem，请删除下面两个类型，避免重复定义。
    /// </summary>
    [Serializable]
    public class AssetPathData
    {
        public List<AssetPathItem> items = new List<AssetPathItem>();
    }

    [Serializable]
    public class AssetPathItem
    {
        public int assetId;

        [NonSerialized]
        public Object asset;

        public string assetPath;
        public string assetName;
    }

    public class AssetPathEditorWindow : EditorWindow
    {
        private static readonly string EXCEL_PATH = Constant.EXCEL_PATH;
        private static readonly string JSON_PATH = Constant.JSON_PATH;

        private const string ConfigFileName = "AssetPathConfig.json";

        private List<AssetPathItem> assetItems = new List<AssetPathItem>();
        private Vector2 scrollPos;

        private int newAssetId = 1;
        private Object newAsset;
        private string exportFileName = "AssetPaths";
        private int nextAutoId = 1;

        private bool isDirty = false;
        private bool drawDirtyBannerForCurrentLayout = false;

        // 关键修复：明确保存拖拽区域 Rect，不再依赖 GUILayoutUtility.GetLastRect()
        private Rect explicitDropAreaRect;
        private Rect listDropAreaRect;

        [MenuItem("Tools/Generic Database/Asset Path Editor")]
        public static void OpenWindow()
        {
            AssetPathEditorWindow window = GetWindow<AssetPathEditorWindow>("资源路径编辑器");
            window.minSize = new Vector2(760, 520);
            window.Show();
        }

        private void OnEnable()
        {
            LoadAssetPaths();
            UpdateNextAutoId();
            newAssetId = nextAutoId;
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                drawDirtyBannerForCurrentLayout = isDirty;
            }

            EditorGUILayout.BeginVertical();

            DrawDirtyBanner(drawDirtyBannerForCurrentLayout);

            // EditorGUILayout.BeginVertical("Box");
            // EditorGUILayout.LabelField("资源路径管理器", EditorStyles.boldLabel);
            // EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            DrawAddAssetSection();

            EditorGUILayout.Space();

            DrawBatchAddSection();

            EditorGUILayout.Space();

            DrawDragDropArea();

            EditorGUILayout.Space();

            DrawAssetList();

            EditorGUILayout.Space();

            // 关键：添加弹性空间，让导出按钮始终靠下
            GUILayout.FlexibleSpace();

            EditorGUILayout.Space();

            DrawExportButtons();

            EditorGUILayout.EndVertical();

            // 关键修复：统一在 OnGUI 最后处理拖拽，但判断的是明确的拖拽区域
            HandleDragAndDrop(Event.current);
        }

        private void DrawDirtyBanner(bool shouldDraw)
        {
            if (!shouldDraw) return;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("当前配置有未保存修改", EditorStyles.boldLabel);

            if (GUILayout.Button("立即保存", GUILayout.Width(100)))
            {
                SaveAssetPaths();
                isDirty = false;
                Repaint();
            }

            if (GUILayout.Button("放弃修改并重载", GUILayout.Width(120)))
            {
                bool confirm = EditorUtility.DisplayDialog("确认", "是否放弃当前未保存修改并重新读取配置？", "放弃修改", "取消");
                if (confirm)
                {
                    LoadAssetPaths();
                    isDirty = false;
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void MarkDirty()
        {
            isDirty = true;
        }

        private void DrawAddAssetSection()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("添加单个资源", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("资源ID:", GUILayout.Width(60));
            newAssetId = EditorGUILayout.IntField(newAssetId, GUILayout.Width(90));

            EditorGUILayout.LabelField("资源:", GUILayout.Width(40));
            EditorGUI.BeginChangeCheck();
            newAsset = EditorGUILayout.ObjectField(newAsset, typeof(Object), false);
            if (EditorGUI.EndChangeCheck() && newAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(newAsset);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning("请选择 Project 窗口中的资源，Scene 对象无法生成 asset 路径。 ");
                    newAsset = null;
                }
            }

            if (GUILayout.Button("添加", GUILayout.Width(70)))
            {
                AddSingleAsset();
            }

            EditorGUILayout.EndHorizontal();

            if (newAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(newAsset);
                EditorGUILayout.LabelField("Asset Path:", path);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBatchAddSection()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("添加 Project 中选中的资源", GUILayout.Width(180)))
            {
                AddSelectedAssets();
            }

            EditorGUILayout.LabelField("下一个ID:", GUILayout.Width(65));
            int editedId = EditorGUILayout.IntField(nextAutoId, GUILayout.Width(90));
            if (editedId != nextAutoId)
            {
                nextAutoId = Mathf.Max(1, editedId);
                newAssetId = nextAutoId;
            }

            if (GUILayout.Button("重新计算ID", GUILayout.Width(100)))
            {
                UpdateNextAutoId();
                newAssetId = nextAutoId;
            }

            EditorGUILayout.LabelField("拖拽/批量添加时自动递增", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            // EditorGUILayout.HelpBox("支持从 Project 窗口拖拽一个或多个资源到下方虚线区域；拖入文件夹时会递归添加文件夹内资源。", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawDragDropArea()
        {
            explicitDropAreaRect = GUILayoutUtility.GetRect(0f, 58f, GUILayout.ExpandWidth(true));

            bool hovering = explicitDropAreaRect.Contains(Event.current.mousePosition)
                            && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform);

            GUIStyle style = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            string text = hovering
                ? "松开鼠标添加资源并自动生成 Asset Path + ID"
                : "拖拽 Project 资源到这里批量添加，支持文件夹（会递归添加文件夹内资源）";

            GUI.Box(explicitDropAreaRect, text, style);
        }

        private void AddSingleAsset()
        {
            if (newAsset == null)
            {
                Debug.LogWarning("请选择一个资源");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(newAsset);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning("无法获取资源路径，请确认资源来自 Project 窗口，而不是 Hierarchy 或 Scene。");
                return;
            }

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                Debug.LogWarning("单个添加不支持文件夹，请使用拖拽或批量添加。 ");
                return;
            }

            if (assetItems.Any(item => item.assetId == newAssetId))
            {
                Debug.LogWarning($"资源ID {newAssetId} 已存在");
                return;
            }

            if (assetItems.Any(item => item.assetPath == assetPath))
            {
                Debug.LogWarning($"资源路径已存在：{assetPath}");
                return;
            }

            AddAssetInternal(newAsset, assetPath, newAssetId);

            Debug.Log($"已添加资源: {newAsset.name} (ID: {newAssetId}, Path: {assetPath})");

            UpdateNextAutoId();
            newAssetId = nextAutoId;
            newAsset = null;
            Repaint();
        }

        private void AddSelectedAssets()
        {
            Object[] selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("请在 Project 窗口中选择资源");
                return;
            }

            AddObjectsBatch(selectedObjects, "批量添加");
        }

        private void HandleDragAndDrop(Event evt)
        {
            if (evt == null) return;

            bool isDragEvent = evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform;
            if (!isDragEvent) return;

            bool inDropArea = explicitDropAreaRect.Contains(evt.mousePosition) || listDropAreaRect.Contains(evt.mousePosition);
            if (!inDropArea) return;

            bool hasProjectAssets = DragAndDrop.objectReferences != null
                                    && DragAndDrop.objectReferences.Any(obj => obj != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)));

            if (!hasProjectAssets)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.Use();
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                AddObjectsBatch(DragAndDrop.objectReferences, "拖拽添加");
            }

            evt.Use();
        }

        private void AddObjectsBatch(IEnumerable<Object> sourceObjects, string operationName)
        {
            if (sourceObjects == null) return;

            List<Object> assets = ExpandAssets(sourceObjects).ToList();
            if (assets.Count == 0)
            {
                Debug.LogWarning($"{operationName}失败：没有可添加的 Project 资源。 ");
                return;
            }

            int startId = nextAutoId;
            int addedCount = 0;
            int skippedCount = 0;

            HashSet<string> pathsInThisBatch = new HashSet<string>();

            foreach (Object obj in assets)
            {
                if (obj == null)
                {
                    skippedCount++;
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                {
                    skippedCount++;
                    continue;
                }

                if (!pathsInThisBatch.Add(assetPath))
                {
                    skippedCount++;
                    continue;
                }

                if (assetItems.Any(item => item.assetPath == assetPath))
                {
                    skippedCount++;
                    continue;
                }

                while (assetItems.Any(item => item.assetId == nextAutoId))
                {
                    nextAutoId++;
                }

                AddAssetInternal(obj, assetPath, nextAutoId);
                nextAutoId++;
                addedCount++;
            }

            UpdateNextAutoId();
            newAssetId = nextAutoId;

            if (addedCount > 0)
            {
                MarkDirty();
                Debug.Log($"{operationName}完成：成功添加 {addedCount} 个资源，跳过 {skippedCount} 个，起始ID: {startId}");
                Repaint();
            }
            else
            {
                Debug.LogWarning($"{operationName}未添加任何资源，可能全部重复、无效或不是 Project 资源。 ");
            }
        }

        private IEnumerable<Object> ExpandAssets(IEnumerable<Object> sourceObjects)
        {
            foreach (Object obj in sourceObjects)
            {
                if (obj == null) continue;

                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                if (!AssetDatabase.IsValidFolder(path))
                {
                    yield return obj;
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("", new[] { path });
                foreach (string guid in guids)
                {
                    string childPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(childPath) || AssetDatabase.IsValidFolder(childPath))
                    {
                        continue;
                    }

                    Object childAsset = AssetDatabase.LoadAssetAtPath<Object>(childPath);
                    if (childAsset != null)
                    {
                        yield return childAsset;
                    }
                }
            }
        }

        private void AddAssetInternal(Object obj, string assetPath, int assetId)
        {
            AssetPathItem newItem = new AssetPathItem
            {
                assetId = assetId,
                asset = obj,
                assetPath = assetPath,
                assetName = obj != null ? obj.name : Path.GetFileNameWithoutExtension(assetPath)
            };

            assetItems.Add(newItem);
            MarkDirty();
        }

        private void DrawAssetList()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField($"资源列表 ({assetItems.Count})", EditorStyles.boldLabel);

            // 关键修复：记录整个列表区域 Rect，允许拖到列表区域也生效
            Rect listHeaderRect = GUILayoutUtility.GetLastRect();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            if (assetItems.Count == 0)
            {
                EditorGUILayout.HelpBox("暂无资源，请添加资源", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < assetItems.Count; i++)
                {
                    AssetPathItem item = assetItems[i];
                    EditorGUILayout.BeginHorizontal("box");

                    EditorGUILayout.LabelField($"ID: {item.assetId}", GUILayout.Width(80));

                    EditorGUI.BeginChangeCheck();
                    Object changedAsset = EditorGUILayout.ObjectField(item.asset, typeof(Object), false, GUILayout.Width(160));
                    if (EditorGUI.EndChangeCheck())
                    {
                        ApplyChangedAsset(item, changedAsset);
                    }

                    EditorGUILayout.SelectableLabel(item.assetPath ?? string.Empty, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.MinWidth(330));
                    EditorGUILayout.LabelField(item.assetName ?? string.Empty, GUILayout.Width(120));

                    if (GUILayout.Button("定位", GUILayout.Width(50)))
                    {
                        PingAsset(item);
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(60)))
                    {
                        assetItems.RemoveAt(i);
                        MarkDirty();
                        UpdateNextAutoId();
                        newAssetId = nextAutoId;
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();

            // 当前 Vertical 还没 End，GetLastRect() 是 ScrollView 的最后布局矩形附近；
            // 这里用 GUILayoutUtility.GetLastRect() 合并 Header 区域，给拖拽命中更大容错。
            Rect afterScrollRect = GUILayoutUtility.GetLastRect();
            listDropAreaRect = new Rect(
                0,
                listHeaderRect.y,
                position.width,
                Mathf.Max(0, afterScrollRect.yMax - listHeaderRect.y)
            );

            EditorGUILayout.EndVertical();
        }

        private void ApplyChangedAsset(AssetPathItem item, Object changedAsset)
        {
            if (item == null) return;

            if (changedAsset == null)
            {
                item.asset = null;
                item.assetPath = string.Empty;
                item.assetName = string.Empty;
                MarkDirty();
                return;
            }

            string path = AssetDatabase.GetAssetPath(changedAsset);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("只能指定 Project 资源，不能指定 Scene 或 Hierarchy 对象。 ");
                return;
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                Debug.LogWarning("列表中的资源项不能直接指定为文件夹。 ");
                return;
            }

            bool duplicatePath = assetItems.Any(x => !ReferenceEquals(x, item) && x.assetPath == path);
            if (duplicatePath)
            {
                Debug.LogWarning($"资源路径已存在：{path}");
                return;
            }

            item.asset = changedAsset;
            item.assetPath = path;
            item.assetName = changedAsset.name;
            MarkDirty();
        }

        private void PingAsset(AssetPathItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.assetPath)) return;

            Object obj = item.asset != null ? item.asset : AssetDatabase.LoadAssetAtPath<Object>(item.assetPath);
            if (obj == null)
            {
                Debug.LogWarning($"资源不存在或路径无效：{item.assetPath}");
                return;
            }

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private void DrawExportButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("导出文件名:", GUILayout.Width(100));
            exportFileName = EditorGUILayout.TextField(exportFileName, GUILayout.Width(160));

            if (GUILayout.Button("导出到Excel", GUILayout.Width(120)))
            {
                ExportToExcel();
            }

            if (GUILayout.Button("导出到JSON", GUILayout.Width(120)))
            {
                ExportToJson();
            }

            if (GUILayout.Button("保存配置", GUILayout.Width(100)))
            {
                SaveAssetPaths();
                isDirty = false;
            }

            if (GUILayout.Button("加载配置", GUILayout.Width(100)))
            {
                if (isDirty)
                {
                    bool confirm = EditorUtility.DisplayDialog("确认", "当前有未保存修改，是否放弃修改并重新加载？", "放弃修改", "取消");
                    if (!confirm)
                    {
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }

                LoadAssetPaths();
                isDirty = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void UpdateNextAutoId()
        {
            if (assetItems == null || assetItems.Count == 0)
            {
                nextAutoId = Mathf.Max(1, nextAutoId);
                return;
            }

            nextAutoId = Mathf.Max(1, assetItems.Max(item => item.assetId) + 1);
        }

        private void ExportToExcel()
        {
            if (assetItems.Count == 0)
            {
                Debug.LogWarning("没有资源可导出");
                return;
            }

            string excelPath = Path.Combine(EXCEL_PATH, $"{SanitizeFileName(exportFileName)}.xlsx");
            string dir = Path.GetDirectoryName(excelPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("AssetPaths");

                    ws.Cells[1, 1].Value = "#AssetPath";
                    ws.Cells[2, 1].Value = "#实例名";
                    ws.Cells[3, 1].Value = "#实例类型";
                    ws.Cells[4, 1].Value = "#注释";

                    ws.Cells[2, 2].Value = "AssetId";
                    ws.Cells[2, 3].Value = "AssetPath";
                    ws.Cells[2, 4].Value = "AssetName";

                    ws.Cells[3, 2].Value = "int";
                    ws.Cells[3, 3].Value = "string";
                    ws.Cells[3, 4].Value = "string";

                    ws.Cells[4, 2].Value = "资源唯一标识";
                    ws.Cells[4, 3].Value = "资源在项目中的路径";
                    ws.Cells[4, 4].Value = "资源名称";

                    List<AssetPathItem> sortedItems = assetItems.OrderBy(item => item.assetId).ToList();
                    for (int i = 0; i < sortedItems.Count; i++)
                    {
                        var item = sortedItems[i];
                        ws.Cells[5 + i, 1].Value = item.assetName;
                        ws.Cells[5 + i, 2].Value = item.assetId;
                        ws.Cells[5 + i, 3].Value = item.assetPath;
                        ws.Cells[5 + i, 4].Value = item.assetName;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    package.SaveAs(new FileInfo(excelPath));
                }

                AssetDatabase.Refresh();
                Debug.Log($"导出成功：{excelPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"导出失败：{e}");
            }
        }

        private void ExportToJson()
        {
            if (assetItems.Count == 0)
            {
                Debug.LogWarning("没有资源可导出");
                return;
            }

            string jsonPath = Path.Combine(JSON_PATH, $"{SanitizeFileName(exportFileName)}.json");
            string dir = Path.GetDirectoryName(jsonPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                AssetPathData exportData = new AssetPathData
                {
                    items = assetItems
                        .OrderBy(item => item.assetId)
                        .Select(item => new AssetPathItem
                        {
                            assetId = item.assetId,
                            assetPath = item.assetPath,
                            assetName = item.assetName
                        })
                        .ToList()
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(jsonPath, json);

                AssetDatabase.Refresh();
                Debug.Log($"导出成功：{jsonPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"导出失败：{e}");
            }
        }

        private void SaveAssetPaths()
        {
            string savePath = Path.Combine(JSON_PATH, ConfigFileName);
            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                AssetPathData saveData = new AssetPathData
                {
                    items = assetItems
                        .OrderBy(item => item.assetId)
                        .Select(item => new AssetPathItem
                        {
                            assetId = item.assetId,
                            assetPath = item.assetPath,
                            assetName = item.assetName
                        })
                        .ToList()
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(saveData, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(savePath, json);

                AssetDatabase.Refresh();
                Debug.Log($"配置保存成功：{savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存失败：{e}");
            }
        }

        private void LoadAssetPaths()
        {
            string loadPath = Path.Combine(JSON_PATH, ConfigFileName);

            if (!File.Exists(loadPath))
            {
                assetItems = new List<AssetPathItem>();
                UpdateNextAutoId();
                newAssetId = nextAutoId;
                return;
            }

            try
            {
                string json = File.ReadAllText(loadPath);

                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        args.ErrorContext.Handled = true;
                        Debug.LogWarning($"反序列化警告：{args.ErrorContext.Error.Message}");
                    }
                };

                AssetPathData loadData = Newtonsoft.Json.JsonConvert.DeserializeObject<AssetPathData>(json, settings);

                assetItems = new List<AssetPathItem>();
                if (loadData != null && loadData.items != null)
                {
                    foreach (AssetPathItem item in loadData.items)
                    {
                        if (string.IsNullOrEmpty(item.assetPath))
                        {
                            continue;
                        }

                        Object loadedAsset = AssetDatabase.LoadAssetAtPath<Object>(item.assetPath);
                        assetItems.Add(new AssetPathItem
                        {
                            assetId = item.assetId,
                            assetPath = item.assetPath,
                            assetName = string.IsNullOrEmpty(item.assetName)
                                ? Path.GetFileNameWithoutExtension(item.assetPath)
                                : item.assetName,
                            asset = loadedAsset
                        });
                    }
                }

                assetItems = assetItems
                    .GroupBy(item => item.assetPath)
                    .Select(group => group.First())
                    .OrderBy(item => item.assetId)
                    .ToList();

                UpdateNextAutoId();
                newAssetId = nextAutoId;
                Debug.Log("配置加载成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"加载失败：{e}");
                assetItems = new List<AssetPathItem>();
                UpdateNextAutoId();
                newAssetId = nextAutoId;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "AssetPaths";
            }

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }
}