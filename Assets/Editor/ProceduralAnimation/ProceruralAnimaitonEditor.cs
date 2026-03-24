using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralAnimationController))]
public class ProceduralAnimationControllerEditor : Editor
{
    private ProceduralAnimationController controller;

    private SerializedProperty previewProgressProp;
    private SerializedProperty playOnStartProp;
    private SerializedProperty animationsProp;

    private Transform childToAdd;

    private void OnEnable()
    {
        controller = (ProceduralAnimationController)target;

        previewProgressProp = serializedObject.FindProperty("previewProgress");
        playOnStartProp = serializedObject.FindProperty("playOnStart");
        animationsProp = serializedObject.FindProperty("animations");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("程序化动画控制器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(playOnStartProp, new GUIContent("运行时自动播放"));

        EditorGUILayout.Space();
        DrawPlaybackControls();

        EditorGUILayout.Space();
        DrawAddControls();

        EditorGUILayout.Space();
        DrawAnimationList();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(controller);
        }
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.LabelField("预览控制", EditorStyles.boldLabel);

        float totalDuration = controller.GetTotalTimelineDuration();
        EditorGUILayout.LabelField("总预览时长", $"{totalDuration:F2}s");

        EditorGUI.BeginChangeCheck();
        float newProgress = EditorGUILayout.Slider("进度", previewProgressProp.floatValue, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            previewProgressProp.floatValue = newProgress;
            serializedObject.ApplyModifiedProperties();

            controller.Pause();
            controller.ApplyPreview(newProgress);

            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("播放", GUILayout.Height(28)))
        {
            serializedObject.ApplyModifiedProperties();
            controller.Play();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("停止", GUILayout.Height(28)))
        {
            serializedObject.ApplyModifiedProperties();
            controller.Stop();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAddControls()
    {
        EditorGUILayout.LabelField("添加子物体动画", EditorStyles.boldLabel);

        if (GUILayout.Button("添加所有直接子物体"))
        {
            Undo.RecordObject(controller, "Add All Child Animations");
            controller.AddAllChildren();
            EditorUtility.SetDirty(controller);
        }

        childToAdd = (Transform)EditorGUILayout.ObjectField("单独添加子物体", childToAdd, typeof(Transform), true);

        if (GUILayout.Button("添加选中的子物体"))
        {
            if (childToAdd != null)
            {
                Undo.RecordObject(controller, "Add Single Child Animation");
                controller.AddSingleChild(childToAdd);
                EditorUtility.SetDirty(controller);
            }
        }
    }

    private void DrawAnimationList()
    {
        EditorGUILayout.LabelField("子物体动画列表", EditorStyles.boldLabel);

        for (int i = 0; i < animationsProp.arraySize; i++)
        {
            SerializedProperty item = animationsProp.GetArrayElementAtIndex(i);

            SerializedProperty nameProp = item.FindPropertyRelative("name");
            SerializedProperty targetProp = item.FindPropertyRelative("target");
            SerializedProperty typeProp = item.FindPropertyRelative("animationType");
            SerializedProperty weightProp = item.FindPropertyRelative("weight");
            SerializedProperty durationProp = item.FindPropertyRelative("duration");
            SerializedProperty delayProp = item.FindPropertyRelative("delay");
            SerializedProperty loopProp = item.FindPropertyRelative("loop");
            SerializedProperty loopTypeProp = item.FindPropertyRelative("loopType");
            SerializedProperty rotationEulerProp = item.FindPropertyRelative("rotationEuler");
            SerializedProperty moveOffsetProp = item.FindPropertyRelative("moveOffset");
            SerializedProperty startPosProp = item.FindPropertyRelative("startPosition");
            SerializedProperty endPosProp = item.FindPropertyRelative("endPosition");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(nameProp, new GUIContent("名称"));

            if (GUILayout.Button("↑", GUILayout.Width(28)))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(controller, "Move Animation Up");
                controller.MoveAnimationUp(i);
                EditorUtility.SetDirty(controller);
                break;
            }

            if (GUILayout.Button("↓", GUILayout.Width(28)))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(controller, "Move Animation Down");
                controller.MoveAnimationDown(i);
                EditorUtility.SetDirty(controller);
                break;
            }

            if (GUILayout.Button("删除", GUILayout.Width(60)))
            {
                animationsProp.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(targetProp, new GUIContent("目标子物体"));
            EditorGUILayout.PropertyField(typeProp, new GUIContent("动画类型"));
            EditorGUILayout.Slider(weightProp, 0f, 1f, new GUIContent("权重"));
            EditorGUILayout.PropertyField(durationProp, new GUIContent("持续时间"));
            EditorGUILayout.PropertyField(delayProp, new GUIContent("延迟启动"));
            EditorGUILayout.PropertyField(loopProp, new GUIContent("是否循环"));

            if (loopProp.boolValue)
            {
                EditorGUILayout.PropertyField(loopTypeProp, new GUIContent("循环类型"));
            }

            ProceduralAnimationController.AnimationType animType =
                (ProceduralAnimationController.AnimationType)typeProp.enumValueIndex;

            switch (animType)
            {
                case ProceduralAnimationController.AnimationType.Rotation:
                    EditorGUILayout.PropertyField(rotationEulerProp, new GUIContent("旋转角度"));
                    break;

                case ProceduralAnimationController.AnimationType.MoveOffset:
                    EditorGUILayout.PropertyField(moveOffsetProp, new GUIContent("移动距离"));
                    DrawCaptureButtons(targetProp, startPosProp, endPosProp, false);
                    break;

                case ProceduralAnimationController.AnimationType.MoveBetweenPoints:
                    EditorGUILayout.PropertyField(startPosProp, new GUIContent("初始位置"));
                    EditorGUILayout.PropertyField(endPosProp, new GUIContent("结束位置"));
                    DrawCaptureButtons(targetProp, startPosProp, endPosProp, true);
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    private void DrawCaptureButtons(
        SerializedProperty targetProp,
        SerializedProperty startPosProp,
        SerializedProperty endPosProp,
        bool showStartEndFields)
    {
        Transform target = targetProp.objectReferenceValue as Transform;
        if (target == null) return;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("将当前位置设为起点"))
        {
            Undo.RecordObject(controller, "Set Start Position");
            startPosProp.vector3Value = target.localPosition;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("将当前位置设为终点"))
        {
            Undo.RecordObject(controller, "Set End Position");
            endPosProp.vector3Value = target.localPosition;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.EndHorizontal();

        if (!showStartEndFields)
        {
            EditorGUILayout.HelpBox(
                "当前为“移动距离”模式。按钮可记录当前位置作为参考。若需要严格从起点到终点插值，请切换到 MoveBetweenPoints。",
                MessageType.Info
            );
        }
    }
}