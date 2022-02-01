using Mirror;
using UnityEngine;

public class OnlineNetworkManager : NetworkManager
{
    [Header("Lobby Controls - assign in inspector")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private OnlineLobbyController lobbyController;
    [SerializeField] private bool serverBuild;

    #region Unity Callbacks

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        lobbyController.InitializeData();
    }

    public override void Start()
    {
        base.Start();
        if (serverBuild)
        {
            StartServer();
        }
        else
        {
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "ServerAddress.txt");
            networkAddress = System.IO.File.ReadAllText(filePath).Trim();
            StartClient();
        }
    }

    #endregion

    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a client is ready.
    /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
        lobbyController.OnServerReady(conn);
    }

    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        lobbyController.OnServerDisconnect(conn);
        base.OnServerDisconnect(conn);
    }

    #endregion

    #region Client System Callbacks

    /// <summary>
    /// Called on the client when connected to a server.
    /// <para>The default implementation of this function sets the client as ready and adds a player. Override the function to dictate what happens when the client connects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        lobbyController.OnClientConnect(conn);
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    /// <param name="conn">Connection to the server.</param>
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        lobbyController.OnClientDisconnect();
        base.OnClientDisconnect(conn);
    }

    #endregion

    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked when a server is started - including when a host is started.
    /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartServer()
    {
        if (mode == NetworkManagerMode.ServerOnly)
            canvas.SetActive(true);

        lobbyController.OnStartServer();
    }

    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient()
    {
        canvas.SetActive(true);
        lobbyController.OnStartClient();
    }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer()
    {
        lobbyController.OnStopServer();
        canvas.SetActive(false);
    }

    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient()
    {
        lobbyController.OnStopClient();
    }

    #endregion
}
