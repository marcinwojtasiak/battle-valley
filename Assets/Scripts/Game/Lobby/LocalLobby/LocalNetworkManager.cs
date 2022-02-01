using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalNetworkManager : NetworkManager
{
    [Header("Lobby Controls - assign in inspector")]
    [SerializeField] private LocalLobbyController lobbyController;

    public override void Start()
    {
        base.Start();
        StartHost();
    }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer()
    {
        lobbyController.OnStopServer();
    }
}
