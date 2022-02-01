using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAiOpponent
{
    PlayerState PlayerState { get; }
    void Initialize(PlayerState playerState, DifficultyType difficulty);
    void StartTurn();
}
