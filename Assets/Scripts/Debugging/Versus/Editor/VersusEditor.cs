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

            // Button to open the folder containing the saved match games
            if (GUILayout.Button("Open match games folder"))
            {
                System.IO.Directory.CreateDirectory(VersusManager.SavedMatchesFolder);
                EditorUtility.RevealInFinder(VersusManager.SavedMatchesFolder);
            }
        }
    }
}