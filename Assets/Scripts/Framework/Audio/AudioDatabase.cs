using UnityEngine;
using System;
using System.Collections;

namespace Top
{
    // 音频数据库
    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Top/Audio Database")]
    public class AudioDatabase : ScriptableObject
    {
        [Header("背景音乐")]
        public AudioClip mainTheme;
        public AudioClip battleTheme;
        public AudioClip baseTheme;

        [Header("音效")]
        public AudioClip buttonClick;
        public AudioClip buildingSound;
        public AudioClip resourceCollect;
        public AudioClip explosion;
        public AudioClip trainingComplete;
        public AudioClip victory;
        public AudioClip defeat;

        public AudioClip GetMusicClip(string musicId)
        {
            switch (musicId)
            {
                case "MainTheme":
                    return mainTheme;
                case "BattleTheme":
                    return battleTheme;
                case "BaseTheme":
                    return baseTheme;
                default:
                    return null;
            }
        }

        public AudioClip GetSFXClip(string sfxId)
        {
            switch (sfxId)
            {
                case "ButtonClick":
                    return buttonClick;
                case "BuildingSound":
                    return buildingSound;
                case "ResourceCollect":
                    return resourceCollect;
                case "Explosion":
                    return explosion;
                case "TrainingComplete":
                    return trainingComplete;
                case "Victory":
                    return victory;
                case "Defeat":
                    return defeat;
                default:
                    return null;
            }
        }
    }
}