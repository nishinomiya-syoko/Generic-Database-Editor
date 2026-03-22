using UnityEditor;
using UnityEngine;

// 仅编辑器生效的扩展类
[CustomEditor(typeof(AssetLoader))]
public class AssetLoaderEditor : Editor
{
    private AssetLoader _target;
    private bool editorMode = false;

    void OnEnable()
    {
        _target = (AssetLoader)target;
        editorMode = AssetLoader.editorMode;
    }
    public void ChangeEditorMode()
    {
        editorMode = !editorMode;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (editorMode)
            //修改文字颜色
            GUI.color = Color.red;
        else
            GUI.color = Color.green;

        EditorGUILayout.HelpBox($"UseEditorMode:{editorMode}", MessageType.Info);
        GUI.color = Color.white;

        if (GUILayout.Button("ChangeEditorMode"))
        {
            ChangeEditorMode();
        }
    }

    // 重写加载逻辑（仅编辑器生效）
    public T LoadAssetEditor<T>(string path) where T : Object
    {
        if (AssetLoader.editorMode)
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"编辑器模式加载资源失败：路径[{path}]不存在！");
            }
            return asset;
        }
        // 非编辑器模式，调用原方法
        return _target.LoadAsset<T>(path);
    }
}