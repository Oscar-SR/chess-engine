using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Ajedrez.Core;

namespace Ajedrez.UI
{
    public class GameUI : MonoBehaviour
    {
        private const float RESULT_WAIT_TIME = 3;

        [SerializeField] private Text boardStatusText;
        [SerializeField] private Text resultTypeText;
        [SerializeField] private Image dividerLine;
        [SerializeField] private TextMeshProUGUI bottomPlayerTimeText;
        [SerializeField] private TextMeshProUGUI topPlayerTimeText;
        [SerializeField] private TextMeshProUGUI bottomPlayerNameText;
        [SerializeField] private TextMeshProUGUI topPlayerNameText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Color timeTextColor;
        [SerializeField] private Color lowTimeTextColor;

        private TextMeshProUGUI whitePlayerTimeText;
        private TextMeshProUGUI blackPlayerTimeText;
        private TextMeshProUGUI whitePlayerNameText;
        private TextMeshProUGUI blackPlayerNameText;

        public void Init(string whitePlayerName, string blackPlayerName, float duration, bool whiteAtBottom = true)
        {
            if (whiteAtBottom)
            {
                whitePlayerNameText = bottomPlayerNameText;
                blackPlayerNameText = topPlayerNameText;
                whitePlayerTimeText = bottomPlayerTimeText;
                blackPlayerTimeText = topPlayerTimeText;
            }
            else
            {
                whitePlayerNameText = topPlayerNameText;
                blackPlayerNameText = bottomPlayerNameText;
                whitePlayerTimeText = topPlayerTimeText;
                blackPlayerTimeText = bottomPlayerTimeText;
            }

            FormatName(whitePlayerName, bottomPlayerNameText);
            FormatName(blackPlayerName, topPlayerNameText);
            FormatTime(duration, bottomPlayerTimeText);
            FormatTime(duration, topPlayerTimeText);
        }

        public void FormatWhitePlayerTime(float time)
        {
            FormatTime(time, whitePlayerTimeText);
        }

        public void FormatBlackPlayerTime(float time)
        {
            FormatTime(time, blackPlayerTimeText);
        }

        public void FormatWhitePlayerName(string name)
        {
            FormatName(name, whitePlayerNameText);
        }

        public void FormatBlackPlayerName(string name)
        {
            FormatName(name, blackPlayerNameText);
        }

        private void FormatTime(float time, TextMeshProUGUI timeText)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            string formattedTime;

            // If time is less than 10 seconds, show milliseconds
            if (time < 10f)
            {
                // Get only one millisecond digit
                int milliseconds = Mathf.FloorToInt((time % 1) * 10);
                // Format: MM:SS.m
                formattedTime = $"{minutes:00}:{seconds:00}.{milliseconds}";

                timeText.color = lowTimeTextColor;
            }
            else
            {
                // Format: MM:SS without milliseconds
                formattedTime = $"{minutes:00}:{seconds:00}";

                timeText.color = timeTextColor;
            }

            timeText.text = formattedTime;
        }

        private void FormatName(string name, TextMeshProUGUI playerText)
        {
            playerText.text = name;
        }

        public void FadeWhitePlayerTime()
        {
            whitePlayerTimeText.color = new Color(whitePlayerTimeText.color.r, whitePlayerTimeText.color.g, whitePlayerTimeText.color.b, 0.5f);
        }

        public void FadeBlackPlayerTime()
        {
            blackPlayerTimeText.color = new Color(blackPlayerTimeText.color.r, blackPlayerTimeText.color.g, blackPlayerTimeText.color.b, 0.5f);
        }

        public void FinishGame(GameStatus.Type result)
        {
            dividerLine.enabled = false;
            boardStatusText.text = result.GetDescription();
            StartCoroutine(ShowResultAfterDelay(result));
        }

        private IEnumerator ShowResultAfterDelay(GameStatus.Type result)
        {
            yield return new WaitForSeconds(RESULT_WAIT_TIME);

            Piece.Color winner = GameStatus.GetWinner(result);
            if (winner == Piece.Color.None)
            {
                resultTypeText.text = "Draw";
            }
            else
            {
                resultTypeText.text = $"{winner} wins";
            }

            resultPanel.SetActive(true);
        }

        public void ExitButton()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}