using System;
using UnityEngine;

// 示例1：玩家数据类（标记可编辑，自定义显示名称）
[EditableData("玩家配置")]
[Serializable]
public class PlayerData
{
    [Header("基础属性")]
    public string playerName = "默认玩家";
    public int level = 1;
    public float maxHP = 100f;
    public bool isVIP = false;

    [Header("位置/颜色")]
    public Vector3 spawnPos = Vector3.zero;
    public Color playerColor = Color.blue;
}

// 示例2：游戏配置类（标记可编辑，使用默认类名）
[EditableData]
[Serializable]
public class GameConfig
{
    public float musicVolume = 0.8f;
    public float sfxVolume = 1.0f;
    public int maxPlayerCount = 4;
    public Vector2 screenResolution = new Vector2(1920, 1080);
}