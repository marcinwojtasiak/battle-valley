using UnityEngine;
using UnityEngine.UI;

public class OnlineLobbyUI : LobbyUI
{
    public Button leaveButton;
    public Button cancelButton;
    public Button mapOptionsButton;
    private bool isOwner;
    private int creatorId;

    public override void RefreshRoom(PlayerInfo[] playerInfos, BotInfo[] botInfos)
    {
        base.RefreshRoom(playerInfos, botInfos);

        startButton.interactable = false;
        bool everyoneReady = true;

        foreach (LobbyPlayerDisplay playerDisplay in playerDisplays)
        {
            playerDisplay.kickButton.gameObject.SetActive(isOwner);
            if (!playerDisplay.GetPlayerInfo().ready)
            {
                everyoneReady = false;
            }
            if (playerDisplay.GetPlayerInfo().playerIndex == creatorId)
            {
                playerDisplay.kickButton.gameObject.SetActive(false);
            }
        }

        foreach (LobbyBotDisplay botDisplay in botDisplays)
        {
            botDisplay.botDifficulty.interactable = isOwner;
            botDisplay.removeButton.gameObject.SetActive(isOwner);
        }

        startButton.interactable = everyoneReady && isOwner && (playerInfos.Length > 1);
    }

    public void SetCreator(int creatorId)
    {
        this.creatorId = creatorId;
    }

    public void SetOwner(bool isOwner)
    {
        this.isOwner = isOwner;
        cancelButton.gameObject.SetActive(isOwner);
        leaveButton.gameObject.SetActive(!isOwner);
        addBotButton.gameObject.SetActive(isOwner);
        mapOptionsButton.gameObject.SetActive(isOwner);
    }
}
