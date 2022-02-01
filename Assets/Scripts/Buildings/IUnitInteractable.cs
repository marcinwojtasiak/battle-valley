using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUnitInteractable
{
    public bool Interact(PlayerState player, Unit unit);
}
