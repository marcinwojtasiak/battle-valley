using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stronghold : Building, IInteractable
{
    public void Interact(PlayerState player)
    {
        Recruitment recruitment = GameUIReferences.instance.barracksUI;
        recruitment.OpenUI(player, this);
    }
}
