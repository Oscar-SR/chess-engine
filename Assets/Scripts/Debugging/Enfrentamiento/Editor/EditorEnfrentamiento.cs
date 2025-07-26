using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ajedrez.Debugging.Enfrentamiento
{
    [CustomEditor(typeof(EnfrentamientoManager))]
    public class EditorEnfrentamiento : Editor
    {
        public override void OnInspectorGUI()
        {
            // Dibuja el inspector predeterminado
            DrawDefaultInspector();

            // Botón para abrir la carpeta de partidas guardadas
            if (GUILayout.Button("Abrir carpeta de partidas"))
            {
                System.IO.Directory.CreateDirectory(EnfrentamientoManager.CarpetaGuardadoPartidas);
                EditorUtility.RevealInFinder(EnfrentamientoManager.CarpetaGuardadoPartidas);
            }
        }
    }
}