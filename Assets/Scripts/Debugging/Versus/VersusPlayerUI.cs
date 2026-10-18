using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Chess.Debugging.Versus
{
    public class VersusPlayerUI : MonoBehaviour
    {
        [SerializeField] private VersusPlayerManager remotePlayerManager;
        [SerializeField] private Color connectedColor;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text connectedText;
        [SerializeField] private TMP_Text boardText;
        [SerializeField] private TMP_Text pieceColorText;
        [SerializeField] private TMP_Text thinkingTimeText;
        [SerializeField] private TMP_Text receivedMoveText;
        [SerializeField] private TMP_Text sentMoveText;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private string githubCommitLink;

        // Start is called before the first frame update
        void Start()
        {
            // Set up window settings
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Application.runInBackground = true;
        }

        public void ShowConnected()
        {
            connectedText.text = "Connected";
            connectedText.color = connectedColor;
        }

        public void ShowName(string name)
        {
            nameText.text = name;
        }

        public void ShowBoard(string fen)
        {
            boardText.text = fen;
        }

        public void ShowPieceColor(string pieceColor)
        {
            pieceColorText.text = pieceColor;
        }

        public void ShowThinkingTime(string thinkingTime)
        {
            thinkingTimeText.text = thinkingTime;
        }

        public void ShowReceivedMove(string receivedMove)
        {
            receivedMoveText.text = receivedMove;
        }

        public void ShowSentMove(string sentMove)
        {
            sentMoveText.text = sentMove;
        }

        public void OpenCommit()
        {
            Application.OpenURL(githubCommitLink);
        }

        public void WriteDebug(string message)
        {
            debugText.text += message;
        }
    }
}
