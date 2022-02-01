using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Colors/TeamPalletes", order = 1)]
public class TeamPalletes : ScriptableObject
{
    public List<Palette> palettes = new List<Palette>();
}
