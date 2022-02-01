using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barracks : Building, IInteractable
{
    public void Interact(PlayerState player)
    {
        Recruitment recruitment = GameUIReferences.instance.barracksUI;
        recruitment.OpenUI(player, this);
    }
}
