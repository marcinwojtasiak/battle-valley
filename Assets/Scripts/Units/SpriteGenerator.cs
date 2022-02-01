using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SpriteGenerator : MonoBehaviour
{
    [SerializeField] private Palette basicPalette;
    [SerializeField] private List<Texture2D> sprites = new List<Texture2D>();
    [SerializeField] private TeamPalletes teamPalletes;

    public void GenrateSprites()
    {
        for(int i = 0; i < teamPalletes.palettes.Count; i++)
        {
            foreach(Texture2D sprite in sprites)
            {
                GenerateSingleSprite(sprite, teamPalletes.palettes[i], sprite.name + i, i);
            }
        }
    }


    private void GenerateSingleSprite(Texture2D baseTexture, Palette palette, string name, int team)
    {
        Texture2D newTexture = new Texture2D(baseTexture.width, baseTexture.height);
        newTexture.filterMode = FilterMode.Point;
        Color[] baseColors = baseTexture.GetPixels();
        Color[] newColors = new Color[baseColors.Length];
        for (int i = 0; i < baseColors.Length; i++)
        {
            newColors[i] = baseColors[i];
            for (int j = 0; j < palette.color.Length; j++)
            {
                if (baseColors[i].Equals(basicPalette.color[j]))
                {
                    newColors[i] = palette.color[j];
                    break;
                }
            }
        }
        newTexture.SetPixels(newColors);
        newTexture.Apply();
        File.WriteAllBytes($"Assets/Sprites/Resources/GeneratedSprites/Team{team}/{name}.png", newTexture.EncodeToPNG());
    }
}
