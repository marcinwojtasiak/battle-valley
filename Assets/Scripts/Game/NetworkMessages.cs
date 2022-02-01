using Mirror;
using System;

/// <summary>
/// Match message to be sent to the server
/// </summary>
public struct ServerMatchMessage : NetworkMessage
{
    public ServerMatchOperation serverMatchOperation;
    public Guid matchId;
    public PlayerInfo playerInfo;
    public BotInfo botInfo;
    public MapSettings mapSettings;
    public string name;
    public int maxPlayers;
    public string password;
}

/// <summary>
/// Match message to be sent to the client
/// </summary>
public struct ClientMatchMessage : NetworkMessage
{
    public ClientMatchOperation clientMatchOperation;
    public Guid matchId;
    public MatchInfo[] matchInfos;
    public PlayerInfo[] playerInfos;
    public BotInfo[] botInfos;
    public bool isFull;
    public string password;
}

/// <summary>
/// Information about a match
/// </summary>
[Serializable]
public struct MatchInfo
{
    public Guid matchId;
    public byte players;
    public byte maxPlayers;
    public string password;
    public string matchName;
    public MapSettings mapSettings;
}

/// <summary>
/// Information about the map
/// </summary>
[Serializable]
public struct MapSettings
{
    public byte mapSize;
}

/// <summary>
/// Information about a player
/// </summary>
[Serializable]
public struct PlayerInfo
{
    public int playerIndex;
    public string playerName;
    public bool ready;
    public Guid matchId;
}

/// <summary>
/// Information about a bot
/// </summary>
[Serializable]
public struct BotInfo
{
    public int botIndex;
    public Guid matchId;
    public byte difficulty;
}

/// <summary>
/// Match operation to execute on the server
/// </summary>
public enum ServerMatchOperation : byte
{
    None,
    Create,
    Cancel,
    Start,
    Join,
    Leave,
    Ready,
    AddBot,
    RemoveBot,
    Kick,
    BotDifficulty,
    PlayerName,
    EnteredPassword,
    MapOptions
}

/// <summary>
/// Match operation to execute on the client
/// </summary>
public enum ClientMatchOperation : byte
{
    None,
    List,
    Created,
    Cancelled,
    Joined,
    Departed,
    UpdateRoom,
    Started,
    Ended,
    EnterPassword,
    WrongPassword
}