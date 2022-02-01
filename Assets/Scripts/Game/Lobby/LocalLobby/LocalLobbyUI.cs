using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalLobbyUI : LobbyUI
{
    public Button addPlayerButton;

    public override void RefreshRoom(PlayerInfo[] playerInfos, BotInfo[] botInfos)
    {
        base.RefreshRoom(playerInfos, botInfos);
        startButton.interactable = (playerInfos.Length + botInfos.Length > 1) && (playerInfos.Length > 0);
    }

    public override void SetFull(bool isFull)
    {
        base.SetFull(isFull);
        addPlayerButton.interactable = !isFull;
    }
}
