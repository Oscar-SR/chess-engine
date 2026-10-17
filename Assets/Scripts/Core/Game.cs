using System;
using System.Text;
using System.Collections.Generic;
using Ajedrez.Utilities;
using Ajedrez.IA;

namespace Ajedrez.Core
{
    public class Game
    {
        private Board board;
        private Player whitePlayer;
        private Player blackPlayer;
        private bool withTime;
        private Timer timer;
        private List<Move> movesMade;
        private string startingFen;
        private GameStatus.Type status;
        private List<Move> lastLegalMoves;
        private bool inCheck;
        /*
        private uint checkCount;
        */

        public Game()
        {
            withTime = false;
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
        }

        public Game(Board board, Player whitePlayer, Player blackPlayer, Timer timer)
        {
            withTime = true;
            this.board = board;
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            this.timer = timer;
            startingFen = board.ToFEN();
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
            EvaluateCurrentStatus();
        }

        public Game(string fen, Player whitePlayer, Player blackPlayer, Timer timer)
        {
            withTime = true;
            board = new Board(fen);
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            this.timer = timer;
            startingFen = fen;
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
            EvaluateCurrentStatus();
        }

        public Game(Board board, Player whitePlayer, Player blackPlayer, float duration, float increment)
        {
            withTime = true;
            this.board = board;
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            timer = new Timer(duration, increment);
            startingFen = board.ToFEN();
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
            EvaluateCurrentStatus();
        }

        public Game(string fen, Player whitePlayer, Player blackPlayer, float duration, float increment)
        {
            withTime = true;
            board = new Board(fen);
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            timer = new Timer(duration, increment);
            startingFen = fen;
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
            EvaluateCurrentStatus();
        }

        public Game(Board board, Player whitePlayer, Player blackPlayer)
        {
            withTime = false;
            this.board = board;
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            startingFen = board.ToFEN();
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
            EvaluateCurrentStatus();
        }

        public Game(string fen, Player whitePlayer, Player blackPlayer)
        {
            withTime = false;
            board = new Board(fen);
            this.whitePlayer = whitePlayer;
            this.blackPlayer = blackPlayer;
            startingFen = fen;
            movesMade = new List<Move>();
            lastLegalMoves = new List<Move>();
            EvaluateCurrentStatus();
        }

        public Board Board
        {
            get
            {
                return this.board;
            }
            set
            {
                this.board = value;
                movesMade.Clear();
                if (board != null)
                {
                    startingFen = board.ToFEN();
                    EvaluateCurrentStatus();
                }
            }
        }

        public Player WhitePlayer
        {
            get
            {
                return this.whitePlayer;
            }
            set
            {
                this.whitePlayer = value;
            }
        }

        public Player BlackPlayer
        {
            get
            {
                return this.blackPlayer;
            }
            set
            {
                this.blackPlayer = value;
            }
        }

        public bool WithTime
        {
            get
            {
                return withTime;
            }
            set
            {
                withTime = value;
            }
        }

        public Timer Timer
        {
            get
            {
                if (!withTime)
                    throw new InvalidOperationException("This method cannot be used in a game without time control.");

                return timer;
            }
            set
            {
                if (!withTime)
                    throw new InvalidOperationException("This method cannot be used in a game without time control.");

                timer = value;

                if (whitePlayer is AIPlayer whiteAIPlayer)
                {
                    whiteAIPlayer.TimeManager = new TimeManager(value, Piece.Color.White);
                }

                if (blackPlayer is AIPlayer blackAIPlayer)
                {
                    blackAIPlayer.TimeManager = new TimeManager(value, Piece.Color.Black);
                }
            }
        }

        public List<Move> MovesMade
        {
            get
            {
                return movesMade;
            }
        }

        public string StartingFen
        {
            get
            {
                return startingFen;
            }
        }

        public GameStatus.Type Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
            }
        }

        public List<Move> LastLegalMoves
        {
            get
            {
                return lastLegalMoves;
            }
        }

        public bool InCheck
        {
            get
            {
                return inCheck;
            }
        }

        public void MakeMove(Move movement)
        {
            board.MakeMove(movement);
            movesMade.Add(movement);
            // Calculate the game status and keep track of it, as well as of the last legal moves and whether there is check
            EvaluateCurrentStatus();
        }

        private void EvaluateCurrentStatus()
        {
            (lastLegalMoves, inCheck) = board.GenerateLegalMoves();
            status = GameStatus.GetGameStatus(board, lastLegalMoves.Count, inCheck);
        }

        public string ToPGN(/*GameStatus.Type situation*/)
        {
            //startingFen = startingFen.Replace("\n", "").Replace("\r", "");

            StringBuilder pgn = new();
            Board initialBoard = new Board(startingFen);

            // Headers
            pgn.AppendLine($"[White \"{whitePlayer.Name}\"]");
            pgn.AppendLine($"[Black \"{blackPlayer.Name}\"]");

            if (startingFen != Board.INITIAL_POSITION_FEN)
            {
                pgn.AppendLine($"[FEN \"{startingFen}\"]");
            }

            if (status is not GameStatus.Type.InProgress)
            {
                pgn.AppendLine($"[Result \"{status}\"]");
            }

            for (int moveIndex = 0; moveIndex < movesMade.Count; moveIndex++)
            {
                string moveSAN = movesMade[moveIndex].ToSAN(initialBoard);
                initialBoard.MakeMove(movesMade[moveIndex]);
                if (moveIndex % 2 == 0)
                {
                    pgn.Append((moveIndex / 2 + 1) + ". ");
                }
                pgn.Append(moveSAN + " ");
            }

            return pgn.ToString();
        }

        public void LoadFEN(string fen)
        {
            board = new Board(fen);
            movesMade.Clear();
            startingFen = board.ToFEN();
            EvaluateCurrentStatus();
        }

        public void SwapPlayerColors()
        {
            // Swap the players
            Player whitePlayerTemp = whitePlayer;
            whitePlayer = blackPlayer;
            blackPlayer = whitePlayerTemp;

            // Invert colors
            whitePlayer.PieceColor = ChessUtils.InverseColor(whitePlayer.PieceColor);
            blackPlayer.PieceColor = ChessUtils.InverseColor(blackPlayer.PieceColor);
        }
    }
}