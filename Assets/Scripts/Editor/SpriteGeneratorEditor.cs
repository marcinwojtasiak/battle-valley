using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteGenerator))]
public class SpriteGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate sprites"))
        {
            SpriteGenerator generator = (SpriteGenerator)target;
            generator.GenrateSprites();
        }
    }
}
