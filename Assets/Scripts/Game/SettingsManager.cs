using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager instance;

    public List<FullScreenMode> fullScreenModes;
    [SerializeField] private int defaultFullScreenModeIndex;
    [SerializeField] private float defaultMasterSoundVolume;
    [SerializeField] private float defaultSoundFXVolume;
    [SerializeField] private float defaultMusicVolume;
    [SerializeField] private bool defaultGridToggle;

    private const string KeyMasterSound = "Settings.MasterSoundVolume";
    private const string KeySoundFX = "Settings.SoundFXVolume";
    private const string KeyMusicSound = "Settings.MusicSoundVolume";
    private const string KeyGridToggle = "Settings.GridToggle";
    private const string KeyWindowMode = "Settings.WindowMode";

    public float volumeMasterSound;
    public float VolumeMasterSound
    { 
        get 
        { 
            return volumeMasterSound; 
        }
        set
        {
            volumeMasterSound = value;
            AudioManager.instance.musicSource.volume = VolumeMusicSound * value;
            AudioManager.instance.sfxSource.volume = VolumeSoundFX * value;
            AudioManager.instance.movementSource.volume = VolumeSoundFX * value;
            AudioManager.instance.uiSource.volume = VolumeSoundFX *value;
        }
    }
    private float volumeSoundFX;
    public float VolumeSoundFX
    {
        get
        {
            return volumeSoundFX;
        }
        set
        {
            volumeSoundFX = value;
            AudioManager.instance.sfxSource.volume = value * VolumeMasterSound;
            AudioManager.instance.movementSource.volume = value * VolumeMasterSound;
            AudioManager.instance.uiSource.volume = value * VolumeMasterSound;
        }
    }
    private float volumeMusicSound;
    public float VolumeMusicSound
    {
        get
        {
            return volumeMusicSound;
        }
        set
        {
            volumeMusicSound = value;
            AudioManager.instance.musicSource.volume = value * VolumeMasterSound;
        }
    }
    private bool gridEnabled;
    public bool GridEnabled
    {
        get
        {
            return gridEnabled;
        }
        set
        {
            gridEnabled = value;
            if (GameController.instance != null)
            {
                GameController.instance.GetMapManager().SetGridOutlineActive(value);
            }
        }
    }
    private int fullScreenMode;
    public int FullScreenModeIdx
    {
        get
        {
            return fullScreenMode;
        }
        set
        {
            fullScreenMode = value%fullScreenModes.Count;

            //Resolution resolution = Screen.resolutions[Screen.resolutions.Length - 2];
            Resolution resolution;
            if (fullScreenModes[fullScreenMode] == FullScreenMode.FullScreenWindow)
            {
                resolution = Screen.resolutions[Screen.resolutions.Length - 1];
            }
            else
            {
                resolution = Screen.resolutions[0];
            }
            Screen.SetResolution(resolution.width, resolution.height, fullScreenModes[fullScreenMode]);
        }
    }

    private void Awake()
    {
        // Singleton
        if (instance)
            DestroyImmediate(gameObject);
        else
        {
            instance = this;
            DontDestroyOnLoad(this);

            LoadSettings();
        }
    }

    private void Start()
    {
        FullScreenModeIdx = fullScreenMode;
    }

    private void LoadSettings()
    {
        volumeMasterSound = PlayerPrefs.GetFloat(KeyMasterSound, defaultMasterSoundVolume);
        volumeMusicSound = PlayerPrefs.GetFloat(KeyMusicSound, defaultMusicVolume);
        volumeSoundFX = PlayerPrefs.GetFloat(KeySoundFX, defaultSoundFXVolume);
        gridEnabled = PlayerPrefs.GetInt(KeyGridToggle, defaultGridToggle ? 1 : 0) == 1;
        fullScreenMode = PlayerPrefs.GetInt(KeyWindowMode, defaultFullScreenModeIndex);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(KeyMasterSound, VolumeMasterSound);
        PlayerPrefs.SetFloat(KeySoundFX, VolumeSoundFX);
        PlayerPrefs.SetFloat(KeyMusicSound, VolumeMusicSound);
        PlayerPrefs.SetInt(KeyGridToggle, GridEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KeyWindowMode, FullScreenModeIdx);

        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Saving Settings...");
        SaveSettings();
    }
}
