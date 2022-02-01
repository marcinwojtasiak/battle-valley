using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LocalLobbyPlayerDisplay : LobbyPlayerDisplay
{
    public TMP_InputField playerName;

    public override void SetPlayerInfo(LobbyUI lobbyUI, PlayerInfo info)
    {
        base.SetPlayerInfo(lobbyUI, info);
        playerName.text = info.playerName;
    }
}
