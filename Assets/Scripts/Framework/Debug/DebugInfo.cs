using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class DebugInfo : MonoBehaviour
{
    public static DebugInfo Instance;

    public string info;
    private List<string> logList = new List<string>();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        StartCoroutine(UpdateInfo());
    }
    IEnumerator UpdateInfo()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            info = "FPS:" + (1.0f / Time.deltaTime).ToString("0.0") + "\n" +
                "DeltaTime:" + Time.deltaTime.ToString("0.000") + "\n" +
                "Time:" + Time.time.ToString("0.0") + "\n" +
                "Memory:" + System.GC.GetTotalMemory(false).ToString() + "\n";
            if (logList.Count > 0)
            {
                info += "Logs:\n";
                for(int i = 0; i < logList.Count; i++)
                {
                    info += logList[i];
                }
            }
            logList.Clear();
        }
    }
    public static void Log(string message)
    {
        message += "\n";
        Instance.logList.Add(message);
#if UNITY_EDITOR
        Debug.Log(message);
#endif
    }
    public static void LogWarning(string message)
    {
        message = "<color=yellow>Warning: " + message + "</color>\n";
        Instance.logList.Add(message);
#if UNITY_EDITOR
        Debug.LogWarning(message);
#endif
    }
    public static void LogError(string message)
    {
        message = "<color=red>Error: " + message + "</color>\n";
        Instance.logList.Add(message);
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
    }
    void OnGUI()
    {
        GUILayout.Label(info);
    }
    [Button]
    public void AddLog()
    {
        DebugInfo.Log("AddLog");
    }
}