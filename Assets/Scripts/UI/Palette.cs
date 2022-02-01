using UnityEngine;

[CreateAssetMenu(menuName = "Colors/Palette")]
public class Palette : ScriptableObject
{
    public Color[] color = new Color[6];
}
