using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Chess.Core;
using Chess.UI;
using Chess.Utilities;

namespace Chess.Debugging.Versus
{
    public class VersusPlayer : Player
    {
        private TcpClient client;
        private int winCount;
        private int lossCount;
        private int drawCount;
        private int moveCount;
        private int totalSearchDepth;

        public VersusPlayer(string name, Piece.Color pieceColor, TcpClient client)
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

    public class VersusManager : ServerTCP
    {
        [SerializeField] private BoardUI boardUI;
        [SerializeField] private VersusUI versusUI;
        [SerializeField] private int maxThinkingTime = 1000;
        [SerializeField] private int maxMovesPerGame = 100;
        [SerializeField] private TextAsset initialPositions;
        [SerializeField] private bool bothPlayEachPosition = true;
        [SerializeField] private string savePath;

        private Game game;
        private VersusPlayer player1;
        private VersusPlayer player2;
        private int gameIndex;
        private int gameCount;
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
                versusUI.WriteDebug("[ERROR] " + logString + "\n" + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                versusUI.WriteDebug("[WARNING] " + logString);
            }
            else
            {
                versusUI.WriteDebug(logString);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            initialPositionFens = initialPositions.text.Split('\n');
            gameIndex = -1;
            gameCount = bothPlayEachPosition ? initialPositionFens.Length * 2 : initialPositionFens.Length;
            game = new Game();
            boardUI.Init(null);
            versusUI.Init(maxThinkingTime, maxMovesPerGame);
            versusUI.UpdateStatistics(gameCount, gameIndex, player1, player2);
            StartServer();
        }

        public void StartNewGame()
        {
            gameIndex++;
            int fenIndex = bothPlayEachPosition ? gameIndex / 2 : gameIndex;

            if (fenIndex < initialPositionFens.Length)
            {
                // Load board position
                game.LoadFEN(initialPositionFens[fenIndex]);

                if (gameIndex > 0)
                {
                    // Swap colors
                    game.SwapPlayerColors();
                }

                // Update UI
                boardUI.ChangePosition(game.Board);
                versusUI.SetWhitePlayerName(game.WhitePlayer.Name);
                versusUI.SetBlackPlayerName(game.BlackPlayer.Name);

                string whiteMessage = VersusMessage.CreateNewGameMessage(game.StartingFen, Piece.Color.White, maxThinkingTime).ToJsonString();
                string blackMessage = VersusMessage.CreateNewGameMessage(game.StartingFen, Piece.Color.Black, maxThinkingTime).ToJsonString();

                SendMessageToClient(((VersusPlayer)game.WhitePlayer).Client, whiteMessage);
                SendMessageToClient(((VersusPlayer)game.BlackPlayer).Client, blackMessage);
            }

            versusUI.UpdateStatistics(gameCount, gameIndex, player1, player2);
        }

        protected override void OnClientConnected(TcpClient client)
        {
            base.OnClientConnected(client);
            versusUI.WriteDebug("Client connected\n");
        }

        protected override void MessageReceived(TcpClient client, string message)
        {
            VersusMessage versusMessage = VersusMessage.CreateFromJson(message);
            switch (versusMessage.messageType)
            {
                case VersusMessage.MessageType.RegisterPlayer:
                    RegisterPlayer(client, versusMessage.playerName);
                    break;
                case VersusMessage.MessageType.MakeMove:
                    MoveReceived(versusMessage.moveName, versusMessage.iterativeSearchDepth);
                    break;
            }
        }

        private void RegisterPlayer(TcpClient client, string playerName)
        {
            versusUI.WriteDebug("Register " + playerName + "\n");

            if (player1 == null)
            {
                player1 = new VersusPlayer(playerName, Piece.Color.White, client);
                game.WhitePlayer = player1;
            }
            else if (player2 == null)
            {
                player2 = new VersusPlayer(playerName, Piece.Color.Black, client);

                game.BlackPlayer = player2;

                // Both players have been registered
                versusUI.EnableStartButton();
            }
            else
            {
                versusUI.WriteDebug("Unexpected registration: two players are already registered. Name: " + playerName + "\n");
            }

            versusUI.WritePlayerNames(player1, player2);
            versusUI.UpdateStatistics(gameCount, gameIndex, player1, player2);
        }

        private void MoveReceived(string moveLan, int iterativeSearchDepth)
        {
            versusUI.WriteDebug("Move: " + moveLan + "\n");

            VersusPlayer currentPlayer = ChessUtils.SameColor(game.Board.Turn, Piece.Color.White) ? (VersusPlayer)game.WhitePlayer : (VersusPlayer)game.BlackPlayer;
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
                VersusPlayer nextPlayer = ChessUtils.SameColor(game.Board.Turn, Piece.Color.White) ? (VersusPlayer)game.WhitePlayer : (VersusPlayer)game.BlackPlayer;
                string json = VersusMessage.CreateMoveMessage(moveLan).ToJsonString();
                SendMessageToClient(nextPlayer.Client, json);
            }
            else
            {
                FinishGame();
            }
        }

        private void FinishGame()
        {
            SaveGamePgn();
            UpdateStatistics();
            versusUI.UpdateStatistics(gameCount, gameIndex, player1, player2);
            StartNewGame();

            void SaveGamePgn()
            {
                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = Path.Combine(SavedGamesFolder, $"Games_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
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
                string path = Path.Combine(saveDirectory, $"Game {gameIndex}.txt");
                StreamWriter writer = new StreamWriter(path);
                writer.Write(pgn);
                writer.Close();
            }

            void UpdateStatistics()
            {
                if (game.Status == GameStatus.Type.WhiteCheckmate)
                {
                    ((VersusPlayer)game.BlackPlayer).WinCount++;
                    ((VersusPlayer)game.WhitePlayer).LossCount++;
                }
                else if (game.Status == GameStatus.Type.BlackCheckmate)
                {
                    ((VersusPlayer)game.WhitePlayer).WinCount++;
                    ((VersusPlayer)game.BlackPlayer).LossCount++;
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

        // Folder name kept in Spanish on purpose: renaming it would leave every
        // previously saved game unreachable.
        public static string SavedGamesFolder => Path.Combine(Application.persistentDataPath, "Partidas enfrentamiento");
    }
}
