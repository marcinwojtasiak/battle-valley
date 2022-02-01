using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class OnlineLobbyController : MonoBehaviour, ILobbyController
{
    /// <summary>
    /// Match Controllers listen for this to terminate their match and clean up
    /// </summary>
    private event Action<NetworkConnection> OnPlayerDisconnected;

    /// <summary>
    /// Cross-reference of client that created the corresponding match in openMatches below
    /// </summary>
    private static readonly Dictionary<NetworkConnection, Guid> playerMatches = new Dictionary<NetworkConnection, Guid>();

    /// <summary>
    /// Open matches that are available for joining
    /// </summary>
    private static readonly Dictionary<Guid, MatchInfo> openMatches = new Dictionary<Guid, MatchInfo>();

    /// <summary>
    /// Network Connections of all players in a match
    /// </summary>
    private static readonly Dictionary<Guid, HashSet<NetworkConnection>> matchConnections = new Dictionary<Guid, HashSet<NetworkConnection>>();

    /// <summary>
    /// Bot infos of every match
    /// </summary>
    private static readonly Dictionary<Guid, HashSet<BotInfo>> matchBots = new Dictionary<Guid, HashSet<BotInfo>>();

    /// <summary>
    /// Player informations by Network Connection
    /// </summary>
    private static readonly Dictionary<NetworkConnection, PlayerInfo> playerInfos = new Dictionary<NetworkConnection, PlayerInfo>();

    /// <summary>
    /// Network Connections that have neither started nor joined a match yet
    /// </summary>
    private static readonly List<NetworkConnection> waitingConnections = new List<NetworkConnection>();

    /// <summary>
    /// GUID of a match the local player has created
    /// </summary>
    private Guid localPlayerMatch = Guid.Empty;

    /// <summary>
    /// GUID of a match the local player has joined
    /// </summary>
    private Guid localJoinedMatch = Guid.Empty;

    /// <summary>
    /// GUID of a match the local player has selected in the Toggle Group match list
    /// </summary>
    private Guid selectedMatch = Guid.Empty;

    // Starting ids
    private int playerIndex = 1;

    [Header("GUI References")]
    [SerializeField] private GameController gameControllerPrefab;
    [SerializeField] private GameObject matchChoicesList;
    [SerializeField] private GameObject matchChoicePrefab;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private Image background;
    [SerializeField] private GameObject lobbyView;
    [SerializeField] private GameObject roomView;
    [SerializeField] private GameObject joinPasswordWindow;
    [SerializeField] private TMP_InputField joinPasswordField;
    [SerializeField] private GameObject initialSettings;
    [SerializeField] private TMP_Dropdown initSettingsPlayers;
    [SerializeField] private TMP_InputField initSettingsPassword;
    [SerializeField] private OnlineLobbyUI roomGUI;
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private TMP_Text connectingText;

    #region UI Functions

    // Called from several places to ensure a clean reset
    //  - MatchNetworkManager.Awake
    //  - OnStartServer
    //  - OnStartClient
    //  - OnClientDisconnect
    //  - ResetCanvas
    public void InitializeData()
    {
        playerMatches.Clear();
        openMatches.Clear();
        matchConnections.Clear();
        waitingConnections.Clear();
        localPlayerMatch = Guid.Empty;
        localJoinedMatch = Guid.Empty;
    }

    // Called from OnStopServer and OnStopClient when shutting down
    private void ResetCanvas()
    {
        InitializeData();
        lobbyView.SetActive(false);
        roomView.SetActive(false);
        gameObject.SetActive(false);
    }

    #endregion

    #region Button Calls

    /// <summary>
    /// Called from <see cref="MatchChoiceDisplay.OnToggleClicked"/>
    /// </summary>
    /// <param name="matchId"></param>
    public void SelectMatch(Guid matchId)
    {
        if (!NetworkClient.active) return;

        if (matchId == Guid.Empty)
        {
            selectedMatch = Guid.Empty;
            joinButton.interactable = false;
        }
        else
        {
            if (!openMatches.Keys.Contains(matchId))
            {
                joinButton.interactable = false;
                return;
            }

            selectedMatch = matchId;
            MatchInfo infos = openMatches[matchId];
            joinButton.interactable = infos.players < infos.maxPlayers;
        }
    }

    /// <summary>
    /// Assigned in inspector to Create button
    /// </summary>
    public void RequestExitToMenu()
    {
        if (!NetworkClient.active) return;

        NetworkManager.singleton.StopClient();
    }

    /// <summary>
    /// Assigned in inspector to Create button in lobby view
    /// </summary>
    public void RequestCreateMatch()
    {
        if (!NetworkClient.active) return;

        initialSettings.SetActive(true);
    }

    /// <summary>
    /// Assigned in inspector to Create button in lobby view
    /// </summary>
    public void RequestCancelCreation()
    {
        if (!NetworkClient.active) return;

        initSettingsPassword.text = "";
        initSettingsPlayers.value = 0;
        initialSettings.SetActive(false);
    }

    /// <summary>
    /// Assigned in inspector to Confirm button in initial settings window
    /// </summary>
    public void RequestConfirmMatch()
    {
        if (!NetworkClient.active) return;

        int maxPlayers = int.Parse(initSettingsPlayers.options[initSettingsPlayers.value].text);
        string password = initSettingsPassword.text;

        initSettingsPassword.text = "";
        initSettingsPlayers.value = 0;
        initialSettings.SetActive(false);
        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Create, maxPlayers = maxPlayers, password = password });
    }

    /// <summary>
    /// Assigned in inspector to Join button
    /// </summary>
    public void RequestJoinMatch()
    {
        if (!NetworkClient.active || selectedMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Join, matchId = selectedMatch });
    }

    /// <summary>
    /// Assigned in inspector to Confirm password button
    /// </summary>
    public void EnterPassword()
    {
        if (!NetworkClient.active || selectedMatch == Guid.Empty) return;

        string password = joinPasswordField.text;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.EnteredPassword, matchId = selectedMatch, password = password });
    }

    /// <summary>
    /// Assigned in inspector to Cancel password button
    /// </summary>
    public void CancelPassword()
    {
        if (!NetworkClient.active || selectedMatch == Guid.Empty) return;

        joinPasswordField.text = "";
        joinPasswordWindow.SetActive(false);
    }

    /// <summary>
    /// Assigned in inspector to ShowPasswordToggle in InitialSettings
    /// </summary>
    /// <param name="isVisible"></param>
    public void SetInitPasswordVisibility(bool isVisible)
    {
        if (!NetworkClient.active) return;

        if (isVisible)
        {
            initSettingsPassword.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            initSettingsPassword.contentType = TMP_InputField.ContentType.Password;
        }
        initSettingsPassword.ForceLabelUpdate();
    }

    /// <summary>
    /// Assigned in inspector to ShowPasswordToggle in PasswordWindow
    /// </summary>
    /// <param name="isVisible"></param>
    public void SetJoinPasswordVisibility(bool isVisible)
    {
        if (!NetworkClient.active) return;

        if (isVisible)
        {
            joinPasswordField.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            joinPasswordField.contentType = TMP_InputField.ContentType.Password;
        }
        joinPasswordField.ForceLabelUpdate();
    }

    /// <summary>
    /// Assigned in inspector to Leave button
    /// </summary>
    public void RequestLeaveMatch()
    {
        if (!NetworkClient.active || localJoinedMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Leave, matchId = localJoinedMatch });
    }

    /// <summary>
    /// Assigned in inspector to Cancel button
    /// </summary>
    public void RequestCancelMatch()
    {
        if (!NetworkClient.active || localPlayerMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Cancel });
    }

    /// <summary>
    /// Assigned in inspector to Ready button
    /// </summary>
    public void RequestReadyChange()
    {
        if (!NetworkClient.active || (localPlayerMatch == Guid.Empty && localJoinedMatch == Guid.Empty)) return;

        Guid matchId = localPlayerMatch == Guid.Empty ? localJoinedMatch : localPlayerMatch;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Ready, matchId = matchId });
    }

    /// <summary>
    /// Assigned in inspector to Start button
    /// </summary>
    public void RequestStartMatch()
    {
        if (!NetworkClient.active || localPlayerMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Start });
    }

    /// <summary>
    /// Assigned in inspector to AddBot button
    /// </summary>
    public void RequestAddBot()
    {
        if (!NetworkClient.active || localPlayerMatch == Guid.Empty) return;

        Guid matchId = localPlayerMatch == Guid.Empty ? localJoinedMatch : localPlayerMatch;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.AddBot, matchId = matchId });
    }

    /// <summary>
    /// Called from <see cref="LobbyBotDisplay.RemoveBot"/>
    /// </summary>
    /// <param name="botInfo"></param>
    public void RemoveBot(BotInfo botInfo)
    {
        if (!NetworkClient.active || localPlayerMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.RemoveBot, botInfo = botInfo });
    }

    /// <summary>
    /// Called from <see cref="LobbyBotDisplay.SetDifficulty"/>
    /// </summary>
    /// <param name="botInfo"></param>
    public void SetBotDifficulty(BotInfo botInfo)
    {
        if (!NetworkClient.active || localPlayerMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.BotDifficulty, botInfo = botInfo });
    }

    /// <summary>
    /// Called from <see cref="LobbyPlayerDisplay.RemovePlayer"/>
    /// </summary>
    /// <param name="playerInfo"></param>
    public void RemovePlayer(PlayerInfo playerInfo)
    {
        if (!NetworkClient.active || localPlayerMatch == Guid.Empty) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.Kick, playerInfo = playerInfo });
    }

    /// <summary>
    /// Called from <see cref="LobbyPlayerDisplay.ChangePlayerName"/>
    /// </summary>
    /// <param name="playerInfo"></param>
    public void SetPlayerName(PlayerInfo playerInfo)
    {
        if (!NetworkClient.active) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.PlayerName, name = playerInfo.playerName });
    }

    /// <summary>
    /// Assigned to PlayerName InputField
    /// </summary>
    /// <param name="inputField"></param>
    public void SetPlayerName(TMP_InputField inputField)
    {
        SetPlayerName(new PlayerInfo() { playerName = inputField.text });
    }

    /// <summary>
    /// Called from <see cref="LobbyUI.SetMapOptions"/>
    /// </summary>
    public void SetMapOptions(MapSettings mapSettings)
    {
        if (!NetworkClient.active) return;

        NetworkClient.connection.Send(new ServerMatchMessage { serverMatchOperation = ServerMatchOperation.MapOptions, matchId = localPlayerMatch, mapSettings = mapSettings });
    }

    /// <summary>
    /// Sends updated match list to all waiting connections or just one if specified
    /// </summary>
    /// <param name="conn"></param>
    private void SendMatchList(NetworkConnection conn = null)
    {
        if (!NetworkServer.active) return;

        if (conn != null)
        {
            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.List, matchInfos = openMatches.Values.ToArray() });
        }
        else
        {
            foreach (var waiter in waitingConnections)
            {
                waiter.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.List, matchInfos = openMatches.Values.ToArray() });
            }
        }
    }

    #endregion

    #region Server & Client Callbacks

    // Methods in this section are called from MatchNetworkManager's corresponding methods

    public void OnStartServer()
    {
        if (!NetworkServer.active) return;

        InitializeData();
        connectingText.gameObject.SetActive(false);
        NetworkServer.RegisterHandler<ServerMatchMessage>(OnServerMatchMessage);
    }

    public void OnServerReady(NetworkConnection conn)
    {
        if (!NetworkServer.active) return;

        waitingConnections.Add(conn);
        playerInfos.Add(conn, new PlayerInfo { playerIndex = playerIndex, playerName = $"Player", ready = false });
        playerIndex++;

        SendMatchList();
    }

    public void OnServerDisconnect(NetworkConnection conn)
    {
        if (!NetworkServer.active) return;

        // Invoke OnPlayerDisconnected on all instances of MatchController
        OnPlayerDisconnected?.Invoke(conn);

        Guid matchId;
        if (playerMatches.TryGetValue(conn, out matchId))
        {
            playerMatches.Remove(conn);
            openMatches.Remove(matchId);

            foreach (NetworkConnection playerConn in matchConnections[matchId])
            {
                PlayerInfo _playerInfo = playerInfos[playerConn];
                _playerInfo.ready = false;
                _playerInfo.matchId = Guid.Empty;
                playerInfos[playerConn] = _playerInfo;
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
            }
        }

        foreach (KeyValuePair<Guid, HashSet<NetworkConnection>> kvp in matchConnections)
        {
            kvp.Value.Remove(conn);
        }

        PlayerInfo playerInfo = playerInfos[conn];
        if (playerInfo.matchId != Guid.Empty)
        {
            MatchInfo matchInfo;
            if (openMatches.TryGetValue(playerInfo.matchId, out matchInfo))
            {
                matchInfo.players--;
                openMatches[playerInfo.matchId] = matchInfo;
            }

            HashSet<NetworkConnection> connections;
            if (matchConnections.TryGetValue(playerInfo.matchId, out connections))
            {
                PlayerInfo[] pInfos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();
                BotInfo[] bInfos = matchBots[playerInfo.matchId].ToArray();
                bool full = matchInfo.players == matchInfo.maxPlayers;

                foreach (NetworkConnection playerConn in matchConnections[playerInfo.matchId])
                {
                    if (playerConn != conn)
                    {
                        playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = pInfos, botInfos = bInfos, isFull = full });
                    }
                }
            }
        }

        SendMatchList();
    }

    public void OnStopServer()
    {
        ResetCanvas();
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    public void OnClientConnect(NetworkConnection conn)
    {
        ShowLobbyView();
        createButton.gameObject.SetActive(true);
        joinButton.gameObject.SetActive(true);
        backButton.gameObject.SetActive(true);
        playerNameField.gameObject.SetActive(true);
        connectingText.gameObject.SetActive(false);
    }

    public void OnStartClient()
    {
        if (!NetworkClient.active) return;

        InitializeData();
        NetworkClient.RegisterHandler<ClientMatchMessage>(OnClientMatchMessage);
    }

    public void OnClientDisconnect()
    {
        if (!NetworkClient.active) return;

        InitializeData();
    }

    public void OnStopClient()
    {
        ResetCanvas();
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    #endregion

    #region Server Match Message Handlers

    private void OnServerMatchMessage(NetworkConnection conn, ServerMatchMessage msg)
    {
        if (!NetworkServer.active) return;

        switch (msg.serverMatchOperation)
        {
            case ServerMatchOperation.None:
                {
                    Debug.LogWarning("Missing ServerMatchOperation");
                    break;
                }
            case ServerMatchOperation.Create:
                {
                    OnServerCreateMatch(conn, msg.maxPlayers, msg.password);
                    break;
                }
            case ServerMatchOperation.Cancel:
                {
                    OnServerCancelMatch(conn);
                    break;
                }
            case ServerMatchOperation.Start:
                {
                    OnServerStartMatch(conn);
                    break;
                }
            case ServerMatchOperation.Join:
                {
                    OnServerJoinMatch(conn, msg.matchId, true);
                    break;
                }
            case ServerMatchOperation.Leave:
                {
                    OnServerLeaveMatch(conn, msg.matchId);
                    break;
                }
            case ServerMatchOperation.Ready:
                {
                    OnServerPlayerReady(conn, msg.matchId);
                    break;
                }
            case ServerMatchOperation.AddBot:
                {
                    OnServerAddBot(msg.matchId);
                    break;
                }
            case ServerMatchOperation.RemoveBot:
                {
                    OnServerRemoveBot(msg.botInfo);
                    break;
                }
            case ServerMatchOperation.BotDifficulty:
                {
                    OnServerSetBotDifficulty(conn, msg.botInfo);
                    break;
                }
            case ServerMatchOperation.Kick:
                {
                    OnServerKickPlayer(msg.playerInfo);
                    break;
                }
            case ServerMatchOperation.PlayerName:
                {
                    OnServerChangePlayerName(conn, msg.name);
                    break;
                }
            case ServerMatchOperation.EnteredPassword:
                {
                    OnServerPasswordEntered(conn, msg.matchId, msg.password);
                    break;
                }
            case ServerMatchOperation.MapOptions:
                {
                    OnServerChangeMapSettings(msg.matchId, msg.mapSettings);
                    break;
                }
        }
    }

    private void OnServerPlayerReady(NetworkConnection conn, Guid matchId)
    {
        if (!NetworkServer.active) return;

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.ready = !playerInfo.ready;
        playerInfos[conn] = playerInfo;

        HashSet<NetworkConnection> connections = matchConnections[matchId];
        PlayerInfo[] pInfos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        MatchInfo matchInfo = openMatches[matchId];
        bool full = matchInfo.players == matchInfo.maxPlayers;

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = pInfos, botInfos = bInfos, isFull = full });
        }
    }

    private void OnServerLeaveMatch(NetworkConnection conn, Guid matchId)
    {
        if (!NetworkServer.active) return;

        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.players--;
        openMatches[matchId] = matchInfo;

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.ready = false;
        playerInfo.matchId = Guid.Empty;
        playerInfos[conn] = playerInfo;

        foreach (KeyValuePair<Guid, HashSet<NetworkConnection>> kvp in matchConnections)
        {
            kvp.Value.Remove(conn);
        }

        HashSet<NetworkConnection> connections = matchConnections[matchId];
        PlayerInfo[] pInfos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        bool full = matchInfo.players == matchInfo.maxPlayers;

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = pInfos, botInfos = bInfos, isFull = full });
        }

        SendMatchList();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
    }

    private void OnServerCreateMatch(NetworkConnection conn, int maxPlayers, string passworrd)
    {
        if (!NetworkServer.active || playerMatches.ContainsKey(conn)) return;

        Guid newMatchId = Guid.NewGuid();
        PlayerInfo playerInfo = playerInfos[conn];

        matchConnections.Add(newMatchId, new HashSet<NetworkConnection>());
        matchConnections[newMatchId].Add(conn);
        playerMatches.Add(conn, newMatchId);
        MapSettings mapSettings = new MapSettings() { mapSize = 0 };
        openMatches.Add(newMatchId, new MatchInfo { matchId = newMatchId, maxPlayers = (byte)maxPlayers, players = 1, password = passworrd, matchName = $"{playerInfo.playerName}'s match" , mapSettings = mapSettings });

        playerInfo.ready = false;
        playerInfo.matchId = newMatchId;
        playerInfos[conn] = playerInfo;

        matchBots.Add(newMatchId, new HashSet<BotInfo>());

        PlayerInfo[] infos = matchConnections[newMatchId].Select(playerConn => playerInfos[playerConn]).ToArray();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Created, matchId = newMatchId, playerInfos = infos, botInfos = Array.Empty<BotInfo>() });

        SendMatchList();
    }

    private void OnServerCancelMatch(NetworkConnection conn)
    {
        if (!NetworkServer.active || !playerMatches.ContainsKey(conn)) return;

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Cancelled });

        Guid matchId;
        if (playerMatches.TryGetValue(conn, out matchId))
        {
            playerMatches.Remove(conn);
            openMatches.Remove(matchId);

            foreach (NetworkConnection playerConn in matchConnections[matchId])
            {
                PlayerInfo playerInfo = playerInfos[playerConn];
                playerInfo.ready = false;
                playerInfo.matchId = Guid.Empty;
                playerInfos[playerConn] = playerInfo;
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
            }

            SendMatchList();
        }
    }

    private void OnServerStartMatch(NetworkConnection conn)
    {
        if (!NetworkServer.active || !playerMatches.ContainsKey(conn)) return;

        Guid matchId;
        if (playerMatches.TryGetValue(conn, out matchId))
        {
            GameController matchController = Instantiate(gameControllerPrefab);

            List<NetworkIdentity> identities = new List<NetworkIdentity>();
            List<PlayerInfo> players = new List<PlayerInfo>();

            foreach (NetworkConnection playerConn in matchConnections[matchId])
            {
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Started });

                GameObject player = Instantiate(NetworkManager.singleton.playerPrefab);
                player.GetComponent<NetworkMatch>().matchId = matchId;
                NetworkServer.AddPlayerForConnection(playerConn, player);

                identities.Add(playerConn.identity);
                players.Add(playerInfos[playerConn]);

                // Reset ready state for after the match.
                PlayerInfo playerInfo = playerInfos[playerConn];
                playerInfo.ready = false;
                playerInfos[playerConn] = playerInfo;
            }

            MapSettings mapSettings = openMatches[matchId].mapSettings;

            matchController.CreateGame(matchId, this, identities, players, matchBots[matchId].ToList(), false, mapSettings);

            playerMatches.Remove(conn);
            openMatches.Remove(matchId);
            matchConnections.Remove(matchId);
            SendMatchList();

            OnPlayerDisconnected += matchController.OnPlayerDisconnected;
        }
    }

    private void OnServerJoinMatch(NetworkConnection conn, Guid matchId, bool passwordCheck)
    {
        if (!NetworkServer.active || !openMatches.ContainsKey(matchId)) return;

        MatchInfo matchInfo = openMatches[matchId];

        if (passwordCheck && matchInfo.password != "")
        {
            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.EnterPassword });
            return;
        }

        matchInfo.players++;
        openMatches[matchId] = matchInfo;
        matchConnections[matchId].Add(conn);

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.ready = false;
        playerInfo.matchId = matchId;
        playerInfos[conn] = playerInfo;

        PlayerInfo[] infos = matchConnections[matchId].Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        bool full = matchInfo.players == matchInfo.maxPlayers;
        SendMatchList();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Joined, matchId = matchId, playerInfos = infos, botInfos = bInfos });

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos, botInfos = bInfos, isFull = full });
        }
    }

    private void OnServerPasswordEntered(NetworkConnection conn, Guid matchId, string password)
    {
        if (!NetworkServer.active || !matchConnections.ContainsKey(matchId) || !openMatches.ContainsKey(matchId)) return;

        MatchInfo matchInfo = openMatches[matchId];

        if (matchInfo.password.Equals(password))
        {
            OnServerJoinMatch(conn, matchId, false);
        }
        else
        {
            conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.WrongPassword });
        }
    }

    private void OnServerAddBot(Guid matchId)
    {
        if (!NetworkServer.active || !matchBots.ContainsKey(matchId)) return;

        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.players++;
        openMatches[matchId] = matchInfo;

        matchBots[matchId].Add(new BotInfo { botIndex = playerIndex, difficulty = 0, matchId = matchId });
        playerIndex++;

        PlayerInfo[] infos = matchConnections[matchId].Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        bool full = matchInfo.players == matchInfo.maxPlayers;

        SendMatchList();

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos, botInfos = bInfos, isFull = full });
        }
    }

    private void OnServerRemoveBot(BotInfo botInfo)
    {
        Guid matchId = botInfo.matchId;
        if (!NetworkServer.active || !matchBots.ContainsKey(matchId) || !matchBots[matchId].Contains(botInfo)) return;

        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.players--;
        openMatches[matchId] = matchInfo;

        matchBots[matchId].Remove(botInfo);

        PlayerInfo[] infos = matchConnections[matchId].Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        bool full = matchInfo.players == matchInfo.maxPlayers;

        SendMatchList();

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = infos, botInfos = bInfos, isFull = full });
        }
    }

    private void OnServerSetBotDifficulty(NetworkConnection conn, BotInfo botInfo)
    {
        Guid matchId = botInfo.matchId;
        if (!NetworkServer.active || !matchBots.ContainsKey(matchId)) return;

        matchBots[matchId].RemoveWhere(b => b.botIndex == botInfo.botIndex);
        matchBots[matchId].Add(botInfo);

        MatchInfo matchInfo = openMatches[matchId];
        PlayerInfo[] pInfos = matchConnections[matchId].Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        bool full = matchInfo.players == matchInfo.maxPlayers;

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            if (playerConn != conn)
                playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = pInfos, botInfos = bInfos, isFull = full });
        }
    }

    private void OnServerKickPlayer(PlayerInfo playerInfo)
    {
        if (!NetworkServer.active) return;

        Guid matchId = playerInfo.matchId;
        NetworkConnection conn = playerInfos.First(p => p.Value.playerIndex == playerInfo.playerIndex).Key;

        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.players--;
        openMatches[matchId] = matchInfo;
        
        playerInfo.ready = false;
        playerInfo.matchId = Guid.Empty;
        playerInfos[conn] = playerInfo;

        foreach (KeyValuePair<Guid, HashSet<NetworkConnection>> kvp in matchConnections)
        {
            kvp.Value.Remove(conn);
        }

        HashSet<NetworkConnection> connections = matchConnections[matchId];
        PlayerInfo[] pInfos = connections.Select(playerConn => playerInfos[playerConn]).ToArray();
        BotInfo[] bInfos = matchBots[matchId].ToArray();
        bool full = matchInfo.players == matchInfo.maxPlayers;

        foreach (NetworkConnection playerConn in matchConnections[matchId])
        {
            playerConn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.UpdateRoom, playerInfos = pInfos, botInfos = bInfos, isFull = full });
        }

        SendMatchList();

        conn.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Departed });
    }

    private void OnServerChangePlayerName(NetworkConnection conn, string name)
    {
        if (!NetworkServer.active || !waitingConnections.Contains(conn) || !playerInfos.ContainsKey(conn)) return;

        PlayerInfo playerInfo = playerInfos[conn];
        playerInfo.playerName = name;
        playerInfos[conn] = playerInfo;
    }

    private void OnServerChangeMapSettings(Guid matchId, MapSettings mapSettings)
    {
        if (!NetworkServer.active || !openMatches.ContainsKey(matchId)) return;

        MatchInfo matchInfo = openMatches[matchId];
        matchInfo.mapSettings = mapSettings;
        openMatches[matchId] = matchInfo;
    }

    #endregion

    #region Client Match Message Handler

    private void OnClientMatchMessage(ClientMatchMessage msg)
    {
        if (!NetworkClient.active) return;

        switch (msg.clientMatchOperation)
        {
            case ClientMatchOperation.None:
                {
                    Debug.LogWarning("Missing ClientMatchOperation");
                    break;
                }
            case ClientMatchOperation.List:
                {
                    openMatches.Clear();
                    foreach (MatchInfo matchInfo in msg.matchInfos)
                    {
                        openMatches.Add(matchInfo.matchId, matchInfo);
                    }
                    RefreshMatchList();
                    break;
                }
            case ClientMatchOperation.Created:
                {
                    localPlayerMatch = msg.matchId;
                    ShowRoomView();
                    roomGUI.SetOwner(true);
                    roomGUI.SetCreator(msg.playerInfos[0].playerIndex);
                    roomGUI.RefreshRoom(msg.playerInfos, msg.botInfos);
                    roomGUI.SetFull(false);
                    break;
                }
            case ClientMatchOperation.Cancelled:
                {
                    localPlayerMatch = Guid.Empty;
                    ShowLobbyView();
                    break;
                }
            case ClientMatchOperation.Joined:
                {
                    joinPasswordField.text = "";
                    joinPasswordWindow.SetActive(false);
                    localJoinedMatch = msg.matchId;
                    ShowRoomView();
                    roomGUI.SetOwner(false);
                    roomGUI.RefreshRoom(msg.playerInfos, msg.botInfos);
                    break;
                }
            case ClientMatchOperation.Departed:
                {
                    localJoinedMatch = Guid.Empty;
                    ShowLobbyView();
                    break;
                }
            case ClientMatchOperation.UpdateRoom:
                {
                    roomGUI.RefreshRoom(msg.playerInfos, msg.botInfos);
                    roomGUI.SetFull(msg.isFull);
                    break;
                }
            case ClientMatchOperation.Started:
                {
                    lobbyView.SetActive(false);
                    roomView.SetActive(false);
                    background.gameObject.SetActive(false);
                    break;
                }
            case ClientMatchOperation.Ended:
                {
                    OnMatchEnded();
                    break;
                }
            case ClientMatchOperation.EnterPassword:
                {
                    AskPassword();
                    break;
                }
            case ClientMatchOperation.WrongPassword:
                {
                    WrongPassword();
                    break;
                }
        }
    }

    private void AskPassword()
    {
        joinPasswordWindow.SetActive(true);
        joinPasswordField.text = "";
    }

    private void WrongPassword()
    {
        joinPasswordField.text = "Wrong password!";
    }

    private void ShowLobbyView()
    {
        lobbyView.SetActive(true);
        roomView.SetActive(false);
        background.gameObject.SetActive(true);

        foreach (Transform child in matchChoicesList.transform)
        {
            if (child.gameObject.GetComponent<MatchChoiceDisplay>().GetMatchId() == selectedMatch)
            {
                Toggle toggle = child.gameObject.GetComponent<Toggle>();
                toggle.isOn = true;
                toggle.onValueChanged.Invoke(true);
            }
        }
    }

    private void ShowRoomView()
    {
        lobbyView.SetActive(false);
        roomView.SetActive(true);
        background.gameObject.SetActive(true);
    }

    private void RefreshMatchList()
    {
        foreach (Transform child in matchChoicesList.transform)
        {
            Destroy(child.gameObject);
        }

        joinButton.interactable = false;

        foreach (MatchInfo matchInfo in openMatches.Values)
        {
            GameObject newMatch = Instantiate(matchChoicePrefab, Vector3.zero, Quaternion.identity);
            newMatch.transform.SetParent(matchChoicesList.transform, false);
            newMatch.GetComponent<MatchChoiceDisplay>().SetMatchInfo(matchInfo);
            newMatch.GetComponent<Toggle>().group = toggleGroup;
            if (matchInfo.matchId == selectedMatch)
            {
                newMatch.GetComponent<Toggle>().isOn = true;
            }
        }
    }

    #endregion

    public void EndMatch(GameController gameController)
    {
        StartCoroutine(ServerEndMatch(gameController));
    }

    private IEnumerator ServerEndMatch(GameController gameController)
    {
        OnPlayerDisconnected -= gameController.OnPlayerDisconnected;

        foreach (NetworkIdentity player in gameController.playerIdentities.Keys)
        {
            player.connectionToClient.Send(new ClientMatchMessage { clientMatchOperation = ClientMatchOperation.Ended });
        }

        // Skip a frame to wait for clients to quit
        yield return null;

        foreach (NetworkIdentity player in gameController.playerIdentities.Keys)
        {
            NetworkServer.RemovePlayerForConnection(player.connectionToClient, true);
            waitingConnections.Add(player.connectionToClient);
        }

        // Skip a frame to allow the Removal(s) to complete
        yield return null;

        // Send latest match list
        SendMatchList();

        NetworkServer.Destroy(gameController.gameObject);
    }

    private void OnMatchEnded()
    {
        if (!NetworkClient.active) return;

        localPlayerMatch = Guid.Empty;
        localJoinedMatch = Guid.Empty;
        ShowLobbyView();
    }
}
