using UnityEngine;
using TMPro;
using System.IO;
using Chess.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Chess.Debugging
{
    public class SearchDebugger : MonoBehaviour
    {
        public static SearchDebugger Instance { get; private set; }

        [SerializeField] 
        private TextMeshProUGUI searchDebugText;

        private bool isVisible = false;
        private string log;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void OnEnable()
        {
            // Beware of script execution order
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnToggleSearchDebug += ToggleSearchDebug;
            }

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnToggleSearchDebug -= ToggleSearchDebug;
            }

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        }

        private void ToggleSearchDebug()
        {
            isVisible = !isVisible;
            searchDebugText.enabled = isVisible;
        }

        public void DebugSearch(string debug)
        {
            searchDebugText.text = debug;
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SaveLog();
            }
        }
#endif

        public void SaveLog(string name = "search")
        {
            string logsFolder = Path.Combine(Application.persistentDataPath, "Search logs");
            logsFolder = Path.GetFullPath(logsFolder); // Normalize the path

            if (!Directory.Exists(logsFolder))
                Directory.CreateDirectory(logsFolder);

            string filePath = Path.Combine(logsFolder, $"{name}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            File.WriteAllText(filePath, log);

            Debug.Log($"[SearchDebugger] Log saved at: {filePath}");
        }

        public void Log(string message)
        {
            this.log += message;
        }
    }
}