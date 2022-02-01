using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ILobbyController
{
    void EndMatch(GameController gameController);
    void RemovePlayer(PlayerInfo player);
    void SetPlayerName(PlayerInfo player);
    void RemoveBot(BotInfo bot);
    void SetBotDifficulty(BotInfo bot);
    void SetMapOptions(MapSettings mapSettings);
}
