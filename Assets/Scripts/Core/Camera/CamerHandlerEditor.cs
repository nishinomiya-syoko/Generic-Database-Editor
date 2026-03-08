using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraHandler))]
public class CameraMovementLimitsEditor : Editor
{
    private CameraHandler handler;
    private SerializedProperty mapSizeProp;
    private Rect mapSize;
    private bool showHandles;

    private void OnEnable()
    {
        handler = (CameraHandler)target;
        mapSizeProp = serializedObject.FindProperty("mapSize");
        mapSize = handler.mapSize;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();

        EditorGUILayout.LabelField("=== Map Boundary Editor ===", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(mapSizeProp);

        // showHandles = EditorGUILayout.Toggle("Show Handles in Scene", showHandles);
        showHandles = handler.showHandles;

        if (showHandles)
        {
            SceneView.duringSceneGui -= OnSceneGUI; // 避免重复注册
            SceneView.duringSceneGui += OnSceneGUI;
        }
        else
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
        Handles.color = Color.cyan;

        float floorY = handler.floorY;

        Vector3 topLeft = new Vector3(mapSize.xMin, floorY, mapSize.yMax);
        Vector3 topRight = new Vector3(mapSize.xMax, floorY, mapSize.yMax);
        Vector3 bottomRight = new Vector3(mapSize.xMax, floorY, mapSize.yMin);
        Vector3 bottomLeft = new Vector3(mapSize.xMin, floorY, mapSize.yMin);

        Handles.DrawLine(topLeft, topRight);
        Handles.DrawLine(topRight, bottomRight);
        Handles.DrawLine(bottomRight, bottomLeft);
        Handles.DrawLine(bottomLeft, topLeft);

        float handleSize = HandleUtility.GetHandleSize(handler.transform.position) * 0.1f;

        EditorGUI.BeginChangeCheck();

        // 支持自由双轴拖动（XZ平面）
        topLeft = DrawFreeHandle(topLeft, handleSize, "TopLeft");
        topRight = DrawFreeHandle(topRight, handleSize, "TopRight");
        bottomRight = DrawFreeHandle(bottomRight, handleSize, "BottomRight");
        bottomLeft = DrawFreeHandle(bottomLeft, handleSize, "BottomLeft");

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(handler, "Adjust Map Boundary");

            // 计算新的矩形尺寸（不强制取Min/Max）
            float newXMin = (bottomLeft.x + topLeft.x) / 2f;
            float newXMax = (bottomRight.x + topRight.x) / 2f;
            float newYMin = (bottomLeft.z + bottomRight.z) / 2f;
            float newYMax = (topLeft.z + topRight.z) / 2f;

            // 允许反向调整
            if (newXMin > newXMax) (newXMin, newXMax) = (newXMax, newXMin);
            if (newYMin > newYMax) (newYMin, newYMax) = (newYMax, newYMin);

            mapSize.xMin = newXMin;
            mapSize.xMax = newXMax;
            mapSize.yMin = newYMin;
            mapSize.yMax = newYMax;

            handler.mapSize = mapSize;
            mapSizeProp.rectValue = mapSize;
            serializedObject.ApplyModifiedProperties();

        }

        // 显示标签
        Handles.Label(topLeft + Vector3.up * 0.3f, "TopLeft");
        Handles.Label(topRight + Vector3.up * 0.3f, "TopRight");
        Handles.Label(bottomLeft + Vector3.up * 0.3f, "BottomLeft");
        Handles.Label(bottomRight + Vector3.up * 0.3f, "BottomRight");

        SceneView.RepaintAll();
    }

    private Vector3 DrawFreeHandle(Vector3 pos, float size, string name)
    {
        Handles.color = Color.green;
        Vector3 newPos = Handles.Slider2D(
            pos,
            Vector3.up,        // 限制在XZ平面
            Vector3.right,
            Vector3.forward,
            size,
            Handles.RectangleHandleCap,
            0f
        );
        newPos.y = handler.floorY;

       
        return newPos;
    }
}
