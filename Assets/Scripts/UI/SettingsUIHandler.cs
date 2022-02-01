using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUIHandler : MonoBehaviour, IUIWindow
{
    [SerializeField] private TMP_Dropdown fullScreenDropdown;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundFXSlider;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Toggle gridToggle;

    private IGameController gameController;
    private int windowMode;

    void Start()
    {
        fullScreenDropdown.ClearOptions();
        fullScreenDropdown.AddOptions(SettingsManager.instance.fullScreenModes.ConvertAll<string>(fsm => fsm.ToString()));
        UpdateSettings();
    }

    public void OpenUI()
    {
        gameObject.SetActive(true);

        GameUIReferences.instance.inGameMenu.SetActive(false);

        gameController = GameController.instance;
    }

    public void CloseUI()
    {
        gameObject.SetActive(false);
    }

    private void UpdateSettings()
    {
        masterSlider.value = SettingsManager.instance.VolumeMasterSound;
        soundFXSlider.value = SettingsManager.instance.VolumeSoundFX;
        musicSlider.value = SettingsManager.instance.VolumeMusicSound;
        gridToggle.isOn = SettingsManager.instance.GridEnabled;
        fullScreenDropdown.value = SettingsManager.instance.FullScreenModeIdx;
    }

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    public void OnWindowModeChanged(int windowMode)
    {
        SettingsManager.instance.FullScreenModeIdx = windowMode;
    }

    public void OnGridToggle(bool gridToggle)
    {
        SettingsManager.instance.GridEnabled = gridToggle;
    }
    public void OnMusicSliderChange(float value)
    {
        SettingsManager.instance.VolumeMusicSound = value;
    }

    public void OnSoundFXSliderChange(float value)
    {
        SettingsManager.instance.VolumeSoundFX = value;
    }

    public void OnMasterSoundSliderChange(float value)
    {
        SettingsManager.instance.VolumeMasterSound = value;
    }

    public void OnExitToMainMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

}
