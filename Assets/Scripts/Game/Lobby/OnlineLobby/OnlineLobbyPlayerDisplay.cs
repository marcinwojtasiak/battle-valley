using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnlineLobbyPlayerDisplay : LobbyPlayerDisplay
{
    public TMP_Text playerName;

    public override void SetPlayerInfo(LobbyUI lobbyUI, PlayerInfo info)
    {
        base.SetPlayerInfo(lobbyUI, info);
        playerName.text = info.playerName;
        playerName.color = info.ready ? new Color(0, 0.4705882f, 0) : new Color(0.627451f, 0, 0);
    }
}
