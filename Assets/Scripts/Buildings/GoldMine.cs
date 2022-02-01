using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldMine : Building, IUnitInteractable
{
    public bool Interact(PlayerState player, Unit unit)
    {
        if (!isAlreadyUsedInTurn)
        {
            player.gold += 10; // It's not a bug. It's a feature!
            return true;
        }
        else
        {
            return false;
        }
    }
}
