using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource movementSource;
    public AudioSource uiSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip inGameMusic;
    [SerializeField] private AudioClip fanfars;

    private void Awake()
    {
        // Singleton
        if (instance)
            DestroyImmediate(gameObject);
        else
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
    }

    private void Start()
    {
        musicSource.volume = SettingsManager.instance.VolumeMusicSound * SettingsManager.instance.VolumeMasterSound;
        sfxSource.volume = SettingsManager.instance.VolumeSoundFX * SettingsManager.instance.VolumeMasterSound;
        movementSource.volume = SettingsManager.instance.VolumeSoundFX * SettingsManager.instance.VolumeMasterSound;
        uiSource.volume = SettingsManager.instance.VolumeSoundFX * SettingsManager.instance.VolumeMasterSound;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            uiSource.Play();
        }
    }

    public void PlayMenuMusic()
    {
        musicSource.clip = menuMusic;
        musicSource.Play();
    }
    

    public void PlayInGameMusic()
    {
        musicSource.clip = inGameMusic;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayFootstepsSFX()
    {
        movementSource.Play();
    }

    public void StopFootstepsSFX()
    {
        movementSource.Stop();
    }

    public void PlayAttackSFX(AudioClip attackSFX)
    {
        sfxSource.PlayOneShot(attackSFX);
    }

    public void PlayFanfarsSFX()
    {
        sfxSource.PlayOneShot(fanfars);
    }
}
