using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Chess.Debugging.Versus
{
    [CustomEditor(typeof(VersusHostManager))]
    public class VersusHostEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();

            // Button to open the folder containing the saved game files
            if (GUILayout.Button("Open saved games folder"))
            {
                System.IO.Directory.CreateDirectory(VersusHostManager.SavedGamesFolder);
                EditorUtility.RevealInFinder(VersusHostManager.SavedGamesFolder);
            }
        }
    }
}