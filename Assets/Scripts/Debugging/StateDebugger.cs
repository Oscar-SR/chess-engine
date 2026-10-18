using UnityEngine;
using TMPro;
using Chess.UI;

namespace Chess.Debugging
{
    public class StateDebugger : MonoBehaviour
    {
        public static StateDebugger Instance { get; private set; }

        [SerializeField] 
        private TextMeshProUGUI boardStateDebugText;

        private bool isVisible = false;

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
                InputManager.Instance.OnToggleBoardState += ToggleBoardState;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnToggleBoardState -= ToggleBoardState;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void ToggleBoardState()
        {
            isVisible = !isVisible;
            boardStateDebugText.enabled = isVisible;
        }

        public void LogBoardState(string log)
        {
            boardStateDebugText.text = log;
        }
    }
}