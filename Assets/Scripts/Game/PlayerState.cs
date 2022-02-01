using Mirror;
using System;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    public static readonly int startingGold = 0;

    [SyncVar]
    public int playerID;
    [SyncVar]
    public string playerName;
    [SyncVar]
    public bool isAi;
    [SyncVar]
    public int gold;
    [SyncVar]
    public int colorIndex;
    [SyncVar]
    public bool hasLost;

    public void Initialize(int playerID, string playerName, bool isAi, int colorIndex)
    {
        this.playerID = playerID;
        this.playerName = playerName;
        this.isAi = isAi;
        gold = startingGold;
        this.colorIndex = colorIndex;
    }
    
    public override string ToString()
    {
        return $"{playerID} {playerName}";
    }

    public override bool Equals(object obj)
    {
        PlayerState other = obj as PlayerState;

        if (other == null)
            return false;

        return playerID == other.playerID;
    }

    public override int GetHashCode()
    {
        return playerID.GetHashCode();
    }
}
