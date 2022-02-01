using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LocalLobbyController : MonoBehaviour, ILobbyController
{
    private readonly List<PlayerInfo> players = new List<PlayerInfo>();
    private readonly List<BotInfo> bots = new List<BotInfo>();

    private Guid matchId;
    private int maxPlayers;
    private MapSettings mapSettings;

    // Starting ids
    private int playerIndex = 1;

    [Header("GUI References")]
    [SerializeField] private GameController gameControllerPrefab;
    [SerializeField] private LocalLobbyUI lobbyUI;
    [SerializeField] private Image background;

    private void Start()
    {
        matchId = Guid.NewGuid();
        maxPlayers = 4;
        mapSettings = new MapSettings() { mapSize = 0 };
    }

    public void OnStopServer()
    {
        Destroy(gameObject);
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    #region Button Calls

    /// <summary>
    /// Assigned in inspector to Start button
    /// </summary>
    public void StartMatch()
    {
        if (!NetworkServer.active) return;

        GameController matchController = Instantiate(gameControllerPrefab);

        GameObject player = Instantiate(NetworkManager.singleton.playerPrefab);
        player.GetComponent<NetworkMatch>().matchId = matchId;
        NetworkServer.AddPlayerForConnection(NetworkServer.connections[NetworkConnection.LocalConnectionId], player);

        lobbyUI.gameObject.SetActive(false);
        background.gameObject.SetActive(false);

        matchController.CreateGame(matchId, this, new List<NetworkIdentity>(), players, bots, true, mapSettings);
    }

    /// <summary>
    /// Assigned in inspector to Cancel button
    /// </summary>
    public void CancelMatch()
    {
        NetworkManager.singleton.StopHost();
    }

    /// <summary>
    /// Assigned in inspector to AddPlayer button
    /// </summary>
    public void AddPlayer()
    {
        PlayerInfo newPlayer = new PlayerInfo() { playerIndex = playerIndex, playerName = $"Player {playerIndex}" };
        playerIndex++;
        players.Add(newPlayer);

        lobbyUI.RefreshRoom(players.ToArray(), bots.ToArray());
        lobbyUI.SetFull(players.Count + bots.Count == maxPlayers);
    }

    /// <summary>
    /// Called from <see cref="LobbyPlayerDisplay.RemovePlayer"/>
    /// </summary>
    /// <param name="playerInfo"></param>
    public void RemovePlayer(PlayerInfo playerInfo)
    {
        players.Remove(playerInfo);

        lobbyUI.RefreshRoom(players.ToArray(), bots.ToArray());
        lobbyUI.SetFull(players.Count + bots.Count == maxPlayers);
    }

    /// <summary>
    /// Called from <see cref="LobbyPlayerDisplay.SetPlayerName"/>
    /// </summary>
    /// <param name="playerInfo"></param>
    public void SetPlayerName(PlayerInfo playerInfo)
    {
        players.RemoveAll(p => p.playerIndex == playerInfo.playerIndex);
        players.Add(playerInfo);
    }

    /// <summary>
    /// Assigned in inspector to AddBot button
    /// </summary>
    public void AddBot()
    {
        BotInfo newBot = new BotInfo() { botIndex = playerIndex, difficulty = 0 };
        playerIndex++;
        bots.Add(newBot);

        lobbyUI.RefreshRoom(players.ToArray(), bots.ToArray());
        lobbyUI.SetFull(players.Count + bots.Count == maxPlayers);
    }

    /// <summary>
    /// Called from <see cref="LobbyBotDisplay.RemoveBot"/>
    /// </summary>
    /// <param name="botInfo"></param>
    public void RemoveBot(BotInfo botInfo)
    {
        bots.Remove(botInfo);

        lobbyUI.RefreshRoom(players.ToArray(), bots.ToArray());
        lobbyUI.SetFull(players.Count + bots.Count == maxPlayers);
    }

    /// <summary>
    /// Called from <see cref="LobbyBotDisplay.SetDifficulty"/>
    /// </summary>
    /// <param name="botInfo"></param>
    public void SetBotDifficulty(BotInfo botInfo)
    {
        bots.RemoveAll(b => b.botIndex == botInfo.botIndex);
        bots.Add(botInfo);
    }

    /// <summary>
    /// Called from <see cref="LobbyUI.SetMapOptions"/>
    /// </summary>
    public void SetMapOptions(MapSettings mapSettings)
    {
        this.mapSettings = mapSettings;
    }

    #endregion

    public void EndMatch(GameController gameController)
    {
        NetworkManager.singleton.StopHost();
    }
}
