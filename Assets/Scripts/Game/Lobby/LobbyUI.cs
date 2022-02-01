using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class LobbyUI : MonoBehaviour
{
    public GameObject playerList;
    public GameObject mapOptionsPanel;
    public TMP_Dropdown mapSizeDropdown;
    public LobbyPlayerDisplay playerPrefab;
    public LobbyBotDisplay botPrefab;
    public Button startButton;
    public Button addBotButton;
    protected List<LobbyPlayerDisplay> playerDisplays = new List<LobbyPlayerDisplay>();
    protected List<LobbyBotDisplay> botDisplays = new List<LobbyBotDisplay>();
    protected ILobbyController lobbyController;

    private void Start()
    {
        lobbyController = GameObject.FindGameObjectWithTag("LobbyController").GetComponent<ILobbyController>();
    }

    public virtual void RefreshRoom(PlayerInfo[] playerInfos, BotInfo[] botInfos)
    {
        foreach (Transform child in playerList.transform)
        {
            Destroy(child.gameObject);
        }

        playerDisplays.Clear();
        botDisplays.Clear();

        foreach (PlayerInfo playerInfo in playerInfos)
        {
            LobbyPlayerDisplay playerDisplay = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            playerDisplay.transform.SetParent(playerList.transform, false);
            playerDisplay.SetPlayerInfo(this, playerInfo);
            playerDisplays.Add(playerDisplay);
        }

        int i = 1;
        foreach (BotInfo botInfo in botInfos)
        {
            LobbyBotDisplay botDisplay = Instantiate(botPrefab, Vector3.zero, Quaternion.identity);
            botDisplay.transform.SetParent(playerList.transform, false);
            botDisplay.SetBotInfo(this, botInfo, i++);
            botDisplays.Add(botDisplay);
        }
    }

    public virtual void SetFull(bool isFull)
    {
        addBotButton.interactable = !isFull;
    }

    public void SetPlayerName(PlayerInfo playerInfo)
    {
        lobbyController.SetPlayerName(playerInfo);
    }

    public void RemovePlayer(PlayerInfo playerInfo)
    {
        lobbyController.RemovePlayer(playerInfo);
    }

    public void RemoveBot(BotInfo botInfo)
    {
        lobbyController.RemoveBot(botInfo);
    }

    public void SetBotDifficulty(BotInfo botInfo)
    {
        lobbyController.SetBotDifficulty(botInfo);
    }

    /// <summary>
    /// Assigned to MapOptions button in inspector
    /// </summary>
    public void ShowMapOptions()
    {
        mapOptionsPanel.SetActive(true);
    }

    /// <summary>
    /// Assigned to Confirm button in map options
    /// </summary>
    public void HideMapOptions()
    {
        mapOptionsPanel.SetActive(false);
    }

    /// <summary>
    /// Assigned to Confirm button in map options
    /// </summary>
    public void SetMapOptions()
    {
        MapSettings mapSettings = new MapSettings() { mapSize = (byte)mapSizeDropdown.value };
        lobbyController.SetMapOptions(mapSettings);
    }
}
