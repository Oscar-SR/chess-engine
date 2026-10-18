using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Chess.Debugging.Versus
{
    [CustomEditor(typeof(VersusManager))]
    public class VersusEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Button to open the folder containing the saved game files
            if (GUILayout.Button("Open saved games folder"))
            {
                System.IO.Directory.CreateDirectory(VersusManager.SavedGamesFolder);
                EditorUtility.RevealInFinder(VersusManager.SavedGamesFolder);
            }
        }
    }
}