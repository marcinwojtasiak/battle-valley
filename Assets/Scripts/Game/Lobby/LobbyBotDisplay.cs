using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyBotDisplay : MonoBehaviour
{
    public TMP_Text botName;
    public TMP_Dropdown botDifficulty;
    public Button removeButton;
    private BotInfo botInfo;
    private LobbyUI lobbyUI;

    public void SetBotInfo(LobbyUI lobbyUI, BotInfo info, int index)
    {
        this.lobbyUI = lobbyUI;
        botInfo = info;
        botName.text = $"Bot {index}";
        botDifficulty.value = info.difficulty;
    }

    public void RemoveBot()
    {
        lobbyUI.RemoveBot(botInfo);
    }

    public void SetDifficulty(TMP_Dropdown dropdown)
    {
        botInfo.difficulty = (byte)dropdown.value;
        lobbyUI.SetBotDifficulty(botInfo);
    }
}
