using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject playMenu;
    [SerializeField] private GameObject optionsMenu;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenu.SetActive(true);
        playMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    public void ShowPlayMenu()
    {
        mainMenu.SetActive(false);
        playMenu.SetActive(true);
    }

    public void PlayOffline()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }

    public void PlayOnline()
    {
        SceneManager.LoadScene(2, LoadSceneMode.Single);
    }

    public void ShowOptions()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);

    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
