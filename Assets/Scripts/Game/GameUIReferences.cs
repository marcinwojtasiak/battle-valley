using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUIReferences : MonoBehaviour
{
    public static GameUIReferences instance;

    public GameObject inGameMenu;
    public SettingsUIHandler settings;
    public TMP_Text playerName;
    public Image playerNameBackground;
    public TMP_Text goldText;
    public TMP_Text turnText;
    public Recruitment barracksUI;
    public Recruitment mercenaryCampUI;
    public Button endTurnButton;
    public PopupUI popupUI;
    public GameStateInfo gameStateInfo;
    public BuildingInfo buildingInfo;
    public UnitInfo unitInfo;
    public RectTransform mainUI;
    public TeamPalletes teamPalletes;
    public TMP_Text currentPlayerText;
    public Image currentPlayerBackground;

    private List<IUIWindow> closableWindows;

    private void Awake()
    {
        instance = this;

        closableWindows = new List<IUIWindow>();
        closableWindows.Add(settings);
        closableWindows.Add(barracksUI);
        closableWindows.Add(mercenaryCampUI);
        closableWindows.Add(gameStateInfo);
        closableWindows.Add(buildingInfo);
        closableWindows.Add(unitInfo);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            IUIWindow openedWindow = closableWindows.Find(window => window.IsActive());
            if(openedWindow != null)
            {
                openedWindow.CloseUI();
            }
            else
            {
                inGameMenu.SetActive(!inGameMenu.gameObject.activeSelf);
            }
        }
    }
}
