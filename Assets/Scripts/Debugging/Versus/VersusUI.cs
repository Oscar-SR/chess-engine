using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chess.Debugging.Versus
{
    public class VersusUI : MonoBehaviour
    {
        [SerializeField] private VersusManager versusManager;
        [SerializeField] private TMP_Text playersText;
        [SerializeField] private TMP_Text statisticsText;
        [SerializeField] private TMP_Text whitePlayerNameText;
        [SerializeField] private TMP_Text blackPlayerNameText;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private TMP_InputField maxThinkingTime;
        [SerializeField] private TMP_InputField maxMovesPerGame;
        [SerializeField] private Button startButton;
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
                versusManager.MaxThinkingTime = value;
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
                versusManager.MaxMovesPerGame = value;
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
            versusManager.StartNewGame();
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
