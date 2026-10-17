using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;

namespace Ajedrez.Debugging.Enfrentamiento
{
    public class MultiplayerUI : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("enfrentamientoManager")] private MultiplayerManager matchManager;
        [SerializeField] [FormerlySerializedAs("textoJugadores")] private TMP_Text playersText;
        [SerializeField] [FormerlySerializedAs("textoEstadisticas")] private TMP_Text statisticsText;
        [SerializeField] [FormerlySerializedAs("textoNombreBlancas")] private TMP_Text whitePlayerNameText;
        [SerializeField] [FormerlySerializedAs("textoNombreNegras")] private TMP_Text blackPlayerNameText;
        [SerializeField] [FormerlySerializedAs("textoDebug")] private TMP_Text debugText;
        [SerializeField] [FormerlySerializedAs("maxTiempoPensar")] private TMP_InputField maxThinkingTime;
        [SerializeField] [FormerlySerializedAs("maxMovimientosPartida")] private TMP_InputField maxMovesPerGame;
        [SerializeField] [FormerlySerializedAs("botonEmpezar")] private Button startButton;
        private const string greenColor = "#26DA6F";

        // Start is called before the first frame update
        void Start()
        {
            // Set up window settings
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 1;
            Application.runInBackground = true;
            playersText.text = $"<color={greenColor}>?</color> vs <color={greenColor}>?</color>";
        }

        public void Init(int initialMaxThinkingTime, int initialMaxMovesPerGame)
        {
            TMP_Text maxThinkingTimePlaceholder = (TMP_Text)maxThinkingTime.placeholder;
            maxThinkingTimePlaceholder.text = initialMaxThinkingTime.ToString();
            TMP_Text maxMovesPerGamePlaceholder = (TMP_Text)maxMovesPerGame.placeholder;
            maxMovesPerGamePlaceholder.text = initialMaxMovesPerGame.ToString();
        }

        public void WritePlayerNames(RemotePlayer player1, RemotePlayer player2)
        {
            string name1 = player1 != null ? player1.Name : "?";
            string name2 = player2 != null ? player2.Name : "?";
            playersText.text = $"<color={greenColor}>{name1}</color> vs <color={greenColor}>{name2}</color>";
        }

        public void UpdateStatistics(int gameCount, int gameIndex, RemotePlayer player1, RemotePlayer player2)
        {
            statisticsText.text = $"Game number: {Mathf.Min(gameIndex + 1, gameCount)} / {gameCount}";

            UpdatePlayerInformation(player1);
            UpdatePlayerInformation(player2);

            void UpdatePlayerInformation(RemotePlayer player)
            {
                if (player != null)
                {
                    float averageDepth = player.TotalSearchDepth / (float)Mathf.Max(1, player.MoveCount);
                    statisticsText.text += $"\n{player.Name}: Wins: {player.WinCount} Losses: {player.LossCount} Draws: {player.DrawCount} Average depth: {averageDepth:0.00}";
                }
                else
                {
                    statisticsText.text += "\nPlayer not connected";
                }
            }
        }

        public void ValidateMaxThinkingTime(string text)
        {
            if (int.TryParse(text, out int value) && value > 0)
            {
                matchManager.MaxThinkingTime = value;
            }
            else
            {
                maxThinkingTime.text = "";
            }
        }

        public void ValidateMaxMovesPerGame(string text)
        {
            if (int.TryParse(text, out int value) && value > 0)
            {
                matchManager.MaxMovesPerGame = value;
            }
            else
            {
                maxMovesPerGame.text = "";
            }
        }

        public void EnableStartButton()
        {
            startButton.interactable = true;
        }

        public void StartButton()
        {
            startButton.interactable = false;
            matchManager.StartNewMatch();
        }

        public void WriteDebug(string message)
        {
            debugText.text += message;
        }

        public void SetWhitePlayerName(string name)
        {
            whitePlayerNameText.text = name;
        }

        public void SetBlackPlayerName(string name)
        {
            blackPlayerNameText.text = name;
        }
    }
}
