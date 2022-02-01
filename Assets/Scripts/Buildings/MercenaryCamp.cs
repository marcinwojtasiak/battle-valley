using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MercenaryCamp : Building, IInteractable
{
    public void Interact(PlayerState player)
    {
        Recruitment recruitment = GameUIReferences.instance.mercenaryCampUI;
        recruitment.OpenUI(player, this);
    }
}
