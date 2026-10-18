using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Chess.Core;
using Chess.UI;
using Chess.Utilities;

namespace Chess.Debugging.Multiplayer
{
    public class RemotePlayer : Player
    {
        private TcpClient client;
        private int winCount;
        private int lossCount;
        private int drawCount;
        private int moveCount;
        private int totalSearchDepth;

        public RemotePlayer(string name, Piece.Color pieceColor, TcpClient client)
            : base(name, pieceColor)
        {
            this.client = client;
        }

        public TcpClient Client
        {
            get => client;
            set => client = value;
        }

        public int WinCount
        {
            get => winCount;
            set => winCount = value;
        }

        public int LossCount
        {
            get => lossCount;
            set => lossCount = value;
        }

        public int DrawCount
        {
            get => drawCount;
            set => drawCount = value;
        }

        public int MoveCount
        {
            get => moveCount;
            set => moveCount = value;
        }

        public int TotalSearchDepth
        {
            get => totalSearchDepth;
            set => totalSearchDepth = value;
        }
    }

    public class MultiplayerManager : ServerTCP
    {
        [SerializeField] private BoardUI boardUI;
        [SerializeField] private MultiplayerUI matchUI;
        [SerializeField] private int maxThinkingTime = 1000;
        [SerializeField] private int maxMovesPerGame = 100;
        [SerializeField] private TextAsset initialPositions;
        [SerializeField] private bool bothPlayEachPosition = true;
        [SerializeField] private string savePath;

        private Game game;
        private RemotePlayer player1;
        private RemotePlayer player2;
        private int matchIndex;
        private int matchCount;
        private string[] initialPositionFens;

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
                matchUI.WriteDebug("[ERROR] " + logString + "\n" + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                matchUI.WriteDebug("[WARNING] " + logString);
            }
            else
            {
                matchUI.WriteDebug(logString);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            initialPositionFens = initialPositions.text.Split('\n');
            matchIndex = -1;
            matchCount = bothPlayEachPosition ? initialPositionFens.Length * 2 : initialPositionFens.Length;
            game = new Game();
            boardUI.Init(null);
            matchUI.Init(maxThinkingTime, maxMovesPerGame);
            matchUI.UpdateStatistics(matchCount, matchIndex, player1, player2);
            StartServer();
        }

        public void StartNewMatch()
        {
            matchIndex++;
            int fenIndex = bothPlayEachPosition ? matchIndex / 2 : matchIndex;

            if (fenIndex < initialPositionFens.Length)
            {
                // Load board position
                game.LoadFEN(initialPositionFens[fenIndex]);

                if (matchIndex > 0)
                {
                    // Swap colors
                    game.SwapPlayerColors();
                }

                // Update UI
                boardUI.ChangePosition(game.Board);
                matchUI.SetWhitePlayerName(game.WhitePlayer.Name);
                matchUI.SetBlackPlayerName(game.BlackPlayer.Name);

                string whiteMessage = MatchMessage.CreateNewGameMessage(game.StartingFen, Piece.Color.White, maxThinkingTime).ToJsonString();
                string blackMessage = MatchMessage.CreateNewGameMessage(game.StartingFen, Piece.Color.Black, maxThinkingTime).ToJsonString();

                SendMessageToClient(((RemotePlayer)game.WhitePlayer).Client, whiteMessage);
                SendMessageToClient(((RemotePlayer)game.BlackPlayer).Client, blackMessage);
            }

            matchUI.UpdateStatistics(matchCount, matchIndex, player1, player2);
        }

        protected override void OnClientConnected(TcpClient client)
        {
            base.OnClientConnected(client);
            matchUI.WriteDebug("Client connected\n");
        }

        protected override void MessageReceived(TcpClient client, string message)
        {
            MatchMessage matchMessage = MatchMessage.CreateFromJson(message);
            switch (matchMessage.messageType)
            {
                case MatchMessage.MessageType.RegisterPlayer:
                    RegisterPlayer(client, matchMessage.playerName);
                    break;
                case MatchMessage.MessageType.MakeMove:
                    MoveReceived(matchMessage.moveName, matchMessage.iterativeSearchDepth);
                    break;
            }
        }

        private void RegisterPlayer(TcpClient client, string playerName)
        {
            matchUI.WriteDebug("Register " + playerName + "\n");

            if (player1 == null)
            {
                player1 = new RemotePlayer(playerName, Piece.Color.White, client);
                game.WhitePlayer = player1;
            }
            else if (player2 == null)
            {
                player2 = new RemotePlayer(playerName, Piece.Color.Black, client);

                game.BlackPlayer = player2;

                // Both players have been registered
                matchUI.EnableStartButton();
            }
            else
            {
                matchUI.WriteDebug("Unexpected registration: two players are already registered. Name: " + playerName + "\n");
            }

            matchUI.WritePlayerNames(player1, player2);
            matchUI.UpdateStatistics(matchCount, matchIndex, player1, player2);
        }

        private void MoveReceived(string moveLan, int iterativeSearchDepth)
        {
            matchUI.WriteDebug("Move: " + moveLan + "\n");

            RemotePlayer currentPlayer = ChessUtils.SameColor(game.Board.Turn, Piece.Color.White) ? (RemotePlayer)game.WhitePlayer : (RemotePlayer)game.BlackPlayer;
            currentPlayer.MoveCount++;
            currentPlayer.TotalSearchDepth += Mathf.Min(iterativeSearchDepth, 15);

            // Make the move
            Move move = new Move(moveLan, game.Board);
            game.MakeMove(move);
            StartCoroutine(boardUI.MakeMove(move, animate: false));

            if (game.MovesMade.Count / 2 >= maxMovesPerGame)
            {
                game.Status = GameStatus.Type.DrawByArbiter;
            }

            if (game.Status == GameStatus.Type.InProgress)
            {
                RemotePlayer nextPlayer = ChessUtils.SameColor(game.Board.Turn, Piece.Color.White) ? (RemotePlayer)game.WhitePlayer : (RemotePlayer)game.BlackPlayer;
                string json = MatchMessage.CreateMoveMessage(moveLan).ToJsonString();
                SendMessageToClient(nextPlayer.Client, json);
            }
            else
            {
                FinishMatch();
            }
        }

        private void FinishMatch()
        {
            SaveGamePgn();
            UpdateStatistics();
            matchUI.UpdateStatistics(matchCount, matchIndex, player1, player2);
            StartNewMatch();

            void SaveGamePgn()
            {
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = Path.Combine(SavedMatchesFolder, $"Matches_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                    Directory.CreateDirectory(savePath);
                }

                string resultFolder = game.Status switch
                {
                    GameStatus.Type.WhiteCheckmate => game.WhitePlayer == player1 ? "Loss" : "Win",
                    GameStatus.Type.BlackCheckmate => game.WhitePlayer == player1 ? "Win" : "Loss",
                    _ => "Draw"
                };
                string saveDirectory = Path.Combine(savePath, resultFolder);
                Directory.CreateDirectory(saveDirectory);

                string pgn = game.ToPGN();
                string path = Path.Combine(saveDirectory, $"Game {matchIndex}.txt");
                StreamWriter writer = new StreamWriter(path);
                writer.Write(pgn);
                writer.Close();
            }

            void UpdateStatistics()
            {
                if (game.Status == GameStatus.Type.WhiteCheckmate)
                {
                    ((RemotePlayer)game.BlackPlayer).WinCount++;
                    ((RemotePlayer)game.WhitePlayer).LossCount++;
                }
                else if (game.Status == GameStatus.Type.BlackCheckmate)
                {
                    ((RemotePlayer)game.WhitePlayer).WinCount++;
                    ((RemotePlayer)game.BlackPlayer).LossCount++;
                }
                else
                {
                    player1.DrawCount++;
                    player2.DrawCount++;
                }
            }
        }

        public int MaxThinkingTime
        {
            set
            {
                maxThinkingTime = value;
            }
        }

        public int MaxMovesPerGame
        {
            set
            {
                maxMovesPerGame = value;
            }
        }

        public static string SavedMatchesFolder => Path.Combine(Application.persistentDataPath, "Partidas enfrentamiento");
    }
}
