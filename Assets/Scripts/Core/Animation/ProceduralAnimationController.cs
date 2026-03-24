using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ProceduralAnimationController : MonoBehaviour
{
    public enum AnimationType
    {
        Rotation,
        MoveOffset,
        MoveBetweenPoints
    }

    public enum LoopType
    {
        Restart,
        PingPong
    }

    [Serializable]
    public class ChildAnimation
    {
        public string name;
        public Transform target;
        public AnimationType animationType = AnimationType.Rotation;

        [Range(0f, 1f)]
        public float weight = 1f;

        [Min(0.01f)]
        public float duration = 1f;

        [Min(0f)]
        public float delay = 0f;

        public bool loop = false;
        public LoopType loopType = LoopType.Restart;

        public Vector3 rotationEuler = new Vector3(0f, 90f, 0f);
        public Vector3 moveOffset = new Vector3(0f, 1f, 0f);

        public Vector3 startPosition;
        public Vector3 endPosition;

        [HideInInspector] public Vector3 cachedLocalPosition;
        [HideInInspector] public Quaternion cachedLocalRotation;
        [HideInInspector] public bool cached = false;
    }

    [Header("Preview")]
    [Range(0f, 1f)]
    public float previewProgress = 0f;

    public bool playOnStart = false;

    [Header("Animations")]
    public List<ChildAnimation> animations = new List<ChildAnimation>();

    [NonSerialized] public bool isPlaying = false;
    [NonSerialized] private double editorStartTime;
    [NonSerialized] private float runtimePlayTime;

    private void Start()
    {
        CacheInitialStates();

        if (Application.isPlaying && playOnStart)
        {
            Play();
        }
    }

    private void OnEnable()
    {
        CacheInitialStates();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
        RestoreInitialStates();
    }

    private void OnDestroy()
    {
        RestoreInitialStates();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (!isPlaying) return;

        runtimePlayTime += Time.deltaTime;

        float totalDuration = GetTotalTimelineDuration();
        float timelineProgress = totalDuration > 0f ? runtimePlayTime / totalDuration : 1f;
        previewProgress = Mathf.Clamp01(timelineProgress);

        ApplyByElapsedTime(runtimePlayTime);

        if (runtimePlayTime >= totalDuration)
        {
            if (!AnyAnimationLoops())
            {
                isPlaying = false;
            }
        }
    }

#if UNITY_EDITOR
    private void EditorUpdate()
    {
        if (Application.isPlaying) return;
        if (this == null) return;

        if (isPlaying)
        {
            double elapsed = EditorApplication.timeSinceStartup - editorStartTime;
            runtimePlayTime = (float)elapsed;

            float totalDuration = GetTotalTimelineDuration();
            float timelineProgress = totalDuration > 0f ? runtimePlayTime / totalDuration : 1f;
            timelineProgress = Mathf.Clamp01(timelineProgress);

            if (!Mathf.Approximately(previewProgress, timelineProgress))
            {
                previewProgress = timelineProgress;
                ApplyByElapsedTime(runtimePlayTime);
                EditorUtility.SetDirty(this);
            }

            if (runtimePlayTime >= totalDuration && !AnyAnimationLoops())
            {
                isPlaying = false;
            }
        }
        else
        {
            float totalDuration = GetTotalTimelineDuration();
            float elapsedTime = totalDuration * previewProgress;
            ApplyByElapsedTime(elapsedTime);
        }
    }
#endif

    public void Play()
    {
        CacheInitialStates();
        isPlaying = true;

        float totalDuration = GetTotalTimelineDuration();
        runtimePlayTime = previewProgress * totalDuration;

#if UNITY_EDITOR
        editorStartTime = EditorApplication.timeSinceStartup - runtimePlayTime;
#endif
    }

    public void Stop()
    {
        isPlaying = false;
        runtimePlayTime = 0f;
        previewProgress = 0f;
        ApplyByElapsedTime(0f);
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void ApplyPreview(float normalizedTimelineProgress)
    {
        normalizedTimelineProgress = Mathf.Clamp01(normalizedTimelineProgress);
        float totalDuration = GetTotalTimelineDuration();
        float elapsedTime = totalDuration * normalizedTimelineProgress;
        ApplyByElapsedTime(elapsedTime);
    }

    public void ApplyByElapsedTime(float elapsedTime)
    {
        foreach (var anim in animations)
        {
            if (anim == null || anim.target == null) continue;

            EnsureCached(anim);

            float localT = EvaluateAnimationProgress(anim, elapsedTime);
            float weightedT = localT * anim.weight;

            switch (anim.animationType)
            {
                case AnimationType.Rotation:
                {
                    Quaternion from = anim.cachedLocalRotation;
                    Quaternion to = anim.cachedLocalRotation * Quaternion.Euler(anim.rotationEuler);
                    anim.target.localRotation = Quaternion.SlerpUnclamped(from, to, weightedT);
                    anim.target.localPosition = anim.cachedLocalPosition;
                    break;
                }

                case AnimationType.MoveOffset:
                {
                    anim.target.localRotation = anim.cachedLocalRotation;
                    anim.target.localPosition = Vector3.LerpUnclamped(
                        anim.cachedLocalPosition,
                        anim.cachedLocalPosition + anim.moveOffset,
                        weightedT
                    );
                    break;
                }

                case AnimationType.MoveBetweenPoints:
                {
                    anim.target.localRotation = anim.cachedLocalRotation;
                    anim.target.localPosition = Vector3.LerpUnclamped(
                        anim.startPosition,
                        anim.endPosition,
                        weightedT
                    );
                    break;
                }
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SceneView.RepaintAll();
        }
#endif
    }

    private float EvaluateAnimationProgress(ChildAnimation anim, float elapsedTime)
    {
        if (anim.duration <= 0f) return 1f;

        float localTime = elapsedTime - anim.delay;
        if (localTime <= 0f) return 0f;

        if (!anim.loop)
        {
            return Mathf.Clamp01(localTime / anim.duration);
        }

        switch (anim.loopType)
        {
            case LoopType.Restart:
                return Mathf.Repeat(localTime / anim.duration, 1f);

            case LoopType.PingPong:
                return Mathf.PingPong(localTime / anim.duration, 1f);

            default:
                return Mathf.Clamp01(localTime / anim.duration);
        }
    }

    public float GetTotalTimelineDuration()
    {
        float maxTime = 0f;

        foreach (var anim in animations)
        {
            if (anim == null) continue;

            float endTime = anim.delay + Mathf.Max(0.01f, anim.duration);
            if (endTime > maxTime)
            {
                maxTime = endTime;
            }
        }

        return Mathf.Max(0.01f, maxTime);
    }

    public bool AnyAnimationLoops()
    {
        foreach (var anim in animations)
        {
            if (anim != null && anim.loop)
            {
                return true;
            }
        }
        return false;
    }

    public void CacheInitialStates()
    {
        foreach (var anim in animations)
        {
            if (anim == null || anim.target == null) continue;
            anim.cachedLocalPosition = anim.target.localPosition;
            anim.cachedLocalRotation = anim.target.localRotation;
            anim.cached = true;
        }
    }

    public void RestoreInitialStates()
    {
        foreach (var anim in animations)
        {
            if (anim == null || anim.target == null || !anim.cached) continue;
            anim.target.localPosition = anim.cachedLocalPosition;
            anim.target.localRotation = anim.cachedLocalRotation;
        }
    }

    private void EnsureCached(ChildAnimation anim)
    {
        if (anim.cached || anim.target == null) return;

        anim.cachedLocalPosition = anim.target.localPosition;
        anim.cachedLocalRotation = anim.target.localRotation;
        anim.cached = true;
    }

    public void AddAllChildren()
    {
        foreach (Transform child in transform)
        {
            bool exists = false;
            foreach (var anim in animations)
            {
                if (anim != null && anim.target == child)
                {
                    exists = true;
                    break;
                }
            }

            if (exists) continue;

            animations.Add(new ChildAnimation
            {
                name = child.name,
                target = child,
                animationType = AnimationType.Rotation,
                duration = 1f,
                delay = 0f,
                loop = false,
                loopType = LoopType.Restart,
                rotationEuler = new Vector3(0f, 90f, 0f),
                moveOffset = new Vector3(0f, 1f, 0f),
                startPosition = child.localPosition,
                endPosition = child.localPosition + Vector3.up,
                weight = 1f
            });
        }
    }

    public void AddSingleChild(Transform child)
    {
        if (child == null) return;

        foreach (var anim in animations)
        {
            if (anim != null && anim.target == child)
                return;
        }

        animations.Add(new ChildAnimation
        {
            name = child.name,
            target = child,
            animationType = AnimationType.Rotation,
            duration = 1f,
            delay = 0f,
            loop = false,
            loopType = LoopType.Restart,
            rotationEuler = new Vector3(0f, 90f, 0f),
            moveOffset = new Vector3(0f, 1f, 0f),
            startPosition = child.localPosition,
            endPosition = child.localPosition + Vector3.up,
            weight = 1f
        });
    }

    public void MoveAnimationUp(int index)
    {
        if (index <= 0 || index >= animations.Count) return;

        var temp = animations[index - 1];
        animations[index - 1] = animations[index];
        animations[index] = temp;
    }

    public void MoveAnimationDown(int index)
    {
        if (index < 0 || index >= animations.Count - 1) return;

        var temp = animations[index + 1];
        animations[index + 1] = animations[index];
        animations[index] = temp;
    }
}