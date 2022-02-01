using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class LobbyPlayerDisplay : MonoBehaviour
{
    public Button kickButton;
    protected PlayerInfo playerInfo;
    protected LobbyUI lobbyUI;

    public virtual void SetPlayerInfo(LobbyUI lobbyUI, PlayerInfo info)
    {
        this.lobbyUI = lobbyUI;
        playerInfo = info;
    }

    public PlayerInfo GetPlayerInfo()
    {
        return playerInfo;
    }

    public void SetPlayerName(TMP_InputField inputField)
    {
        playerInfo.playerName = inputField.text;
        lobbyUI.SetPlayerName(playerInfo);
    }

    public void RemovePlayer()
    {
        lobbyUI.RemovePlayer(playerInfo);
    }
}
