using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using Chess.Core;
using Chess.AI;

namespace Chess.Debugging.Versus
{
    public class VersusPlayerManager : ClientTCP
    {
        [SerializeField] private VersusPlayerUI versusPlayerUI;
        [SerializeField] private TextAsset openingBook;
        [SerializeField] private string playerName;
        private bool connected;
        private Board board;
        private AIPlayer player;

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                versusPlayerUI.WriteDebug("[ERROR] " + logString + "\n" + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                versusPlayerUI.WriteDebug("[WARNING] " + logString);
            }
            else
            {
                versusPlayerUI.WriteDebug(logString);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            board = new Board();
            CreatePlayer();
            versusPlayerUI.ShowName(playerName);
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            string message = VersusMessage.CreateRegisterPlayerMessage(playerName).ToJsonString();
            SendMessageToServer(message);
            connected = true;
            versusPlayerUI.ShowConnected();
        }

        protected override void MessageReceived(TcpClient client, string message)
        {
            base.MessageReceived(client, message);

            // Parse message
            VersusMessage versusMessage = VersusMessage.CreateFromJson(message);
            switch (versusMessage.messageType)
            {
                case VersusMessage.MessageType.NewGame:
                    StartNewGame(versusMessage.initialFen, versusMessage.pieceColor, versusMessage.maxThinkingTimeMs);
                    break;
                case VersusMessage.MessageType.MakeMove:
                    OpponentMoveReceived(versusMessage.moveName);
                    break;
            }
        }

        private void StartNewGame(string fen, Piece.Color pieceColor, int maxThinkingTime)
        {
            board = new Board(fen);

            if (player == null)
            {
                player = new AIPlayer(playerName, pieceColor, board, null, AIConfiguration.CreateCustom(Search.SearchType.ByTime, maxThinkingTime, true, 8, openingBook));
            }
            else
            {
                player.PieceColor = pieceColor;
                player.Board = board;
            }

            // Update UI
            versusPlayerUI.ShowBoard(fen);
            versusPlayerUI.ShowPieceColor(pieceColor.ToString());
            versusPlayerUI.ShowThinkingTime(maxThinkingTime.ToString());

            if (board.Turn == pieceColor)
            {
                ChooseMove();
            }
        }

        private void OpponentMoveReceived(string moveName)
        {
            versusPlayerUI.ShowReceivedMove(moveName);
            Move move = new Move(moveName, board);
            board.MakeMove(move);
            ChooseMove();
        }

        private async void ChooseMove()
        {
            Move move = await player.FindBestMove();
            board.MakeMove(move);
            string moveLan = move.ToLAN();
            VersusMessage versusMessage = VersusMessage.CreateMoveMessage(moveLan);

            versusMessage.iterativeSearchDepth = player.Search.Diagnostic.MaxDepth;

            string message = versusMessage.ToJsonString();
            SendMessageToServer(message);
            versusPlayerUI.ShowSentMove(moveLan);
        }
    }
}