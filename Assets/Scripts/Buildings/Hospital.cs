using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hospital : Building, IUnitInteractable
{
    public int healCost = 0;
    public int healAmount = 50;

    public bool Interact(PlayerState player, Unit unit)
    {
        if (isAlreadyUsedInTurn)
        {
            return false;
        }

        if(unit.hp == unit.stats.hp)
        {
            return false;
        }

        if (player.gold < healCost)
        {
            return false;
        }

        player.gold -= healCost;
        unit.hp += healAmount;

        if (unit.hp > unit.stats.hp)
        {
            unit.hp = unit.stats.hp;
        }
        return true;
    }
}
