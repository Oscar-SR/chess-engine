using UnityEngine;
using TMPro;
using System.IO;
using Ajedrez.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ajedrez.Debugging
{
    public class BusquedaDebugger : MonoBehaviour
    {
        public static BusquedaDebugger Instancia { get; private set; }
        [SerializeField] private TextMeshProUGUI textoDebugBusqueda;
        private bool visualizar = false;
        private string log;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instancia = this;
            }
        }

        private void OnEnable()
        {
            // Cuidado con el script execution order
            if (InputManager.Instancia != null)
            {
                InputManager.Instancia.OnToggleDebugBusqueda += ToogleDebugBusqueda;
            }

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnCambioModoDeJuego;
#endif
        }

        private void OnDisable()
        {
            if (InputManager.Instancia != null)
            {
                InputManager.Instancia.OnToggleDebugBusqueda -= ToogleDebugBusqueda;
            }

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnCambioModoDeJuego;
#endif
        }

        private void ToogleDebugBusqueda()
        {
            visualizar = !visualizar;
            textoDebugBusqueda.enabled = visualizar;
        }

        public void DebugBusqueda(string debug)
        {
            textoDebugBusqueda.text = debug;
        }

#if UNITY_EDITOR
        private void OnCambioModoDeJuego(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                GuardarLog();
            }
        }
#endif

        public void GuardarLog(string nombre = "busqueda")
        {
            string carpetaLogs = Path.Combine(Application.persistentDataPath, "Logs Busqueda");
            carpetaLogs = Path.GetFullPath(carpetaLogs); // Normalizar la ruta

            if (!Directory.Exists(carpetaLogs))
                Directory.CreateDirectory(carpetaLogs);

            string rutaArchivo = Path.Combine(carpetaLogs, $"{nombre}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            File.WriteAllText(rutaArchivo, log);

            Debug.Log($"[EditorLogger] Log guardado en: {rutaArchivo}");
        }

        public void Log(string log)
        {
            this.log += log;
        }
    }
}