using UnityEngine;
using System;
using System.Collections;

namespace Top
{
    // 音频管理器
    public class AudioManager : MonoBehaviour
    {
        [Header("音频源")]
        public AudioSource musicSource;
        public AudioSource sfxSource;

        [Header("音量设置")]
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;
        [Range(0f, 1f)]
        public float sfxVolume = 0.8f;

        [Header("音频数据库")]
        public AudioDatabase audioDatabase;

        void Start()
        {
            // 创建音频源
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.volume = musicVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.volume = sfxVolume;
            }

            // 加载音频数据库
            if (audioDatabase == null)
            {
                // audioDatabase = Resources.Load<AudioDatabase>("AudioDatabase");
            }
        }

        public void PlayMusic(string musicId)
        {
            if (audioDatabase != null)
            {
                AudioClip clip = audioDatabase.GetMusicClip(musicId);
                if (clip != null)
                {
                    musicSource.clip = clip;
                    musicSource.Play();
                }
            }
        }

        public void PlaySFX(string sfxId)
        {
            if (audioDatabase != null)
            {
                AudioClip clip = audioDatabase.GetSFXClip(sfxId);
                if (clip != null)
                {
                    sfxSource.PlayOneShot(clip);
                }
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }
    }
}