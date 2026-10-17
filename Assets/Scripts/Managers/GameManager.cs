using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Ajedrez.UI;
using Ajedrez.IA;
using Ajedrez.Core;
using Ajedrez.Systems;
using Ajedrez.Utilities;
using Ajedrez.Debugging;

namespace Ajedrez.Managers
{
    public class GameManager : MonoBehaviour
    {
        private const float ResultWaitTime = 3;

        [SerializeField] private GameUI gameUI;
        [SerializeField] private BoardUI boardUI;
        [SerializeField] private PromotionUI promotionUI;

        private Game game;
        private Player currentPlayer;
        private Piece.Color turn;
        private bool gameActive = false;
        private bool firstTurn = true;
        private List<Move> legalMovesOfPiece;

        // Start is called before the first frame update
        void Start()
        {
            if (!PersistenceSystem.Instance.HasGame())
            {
                SceneManager.LoadScene("ConfiguracionesPrevias");
            }

            game = PersistenceSystem.Instance.game;
            bool whiteOnBottom = PersistenceSystem.Instance.whiteOnBottom;

            /*
            // Create board instance
            Board board = new Board("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1");

            TextAsset textAsset = Resources.Load<TextAsset>("Aperturas");
            string openingBook = textAsset.text;

            float duration = 10f;
            float increment = 0f;
            Timer timer = new Timer(duration, increment);

            // Create player instances
            // HumanPlayer whitePlayer = new HumanPlayer("Player 1");
            AIPlayer whitePlayer = new AIPlayer(AIPlayer.TipoDificultad.Dificil, board, openingBook, timer, Piece.Color.White);
            AIPlayer blackPlayer = new AIPlayer(AIPlayer.TipoDificultad.Dificil, board, openingBook, timer, Piece.Color.Black);

            // Create game instance
            game = new Game(board, whitePlayer, blackPlayer, timer);

            // Define board orientation
            bool whiteOnBottom = true;
            */

            gameUI.Init(game.WhitePlayer.Name, game.BlackPlayer.Name, game.Timer.InitialDuration, whiteOnBottom);
            boardUI.Init(game.Board, whiteOnBottom);
            promotionUI.Init(whiteOnBottom);
            BitboardDebugger.Instance.Init(game.Board);
            UpdateTurn();
        }

        // Update is called once per frame
        void Update()
        {
            if (!gameActive)
                return;

            if (turn == Piece.Color.White)
            {
                game.Timer.ConsumeTimeWhite(Time.deltaTime);
                gameUI.FormatWhitePlayerTime(game.Timer.RemainingTimeWhite);

                if (game.Timer.TimeOutWhite)
                {
                    game.Status = GameStatus.Type.WhiteTimeout;
                    EndGame();
                }
            }
            else
            {
                game.Timer.ConsumeTimeBlack(Time.deltaTime);
                gameUI.FormatBlackPlayerTime(game.Timer.RemainingTimeBlack);

                if (game.Timer.TimeOutBlack)
                {
                    game.Status = GameStatus.Type.BlackTimeout;
                    EndGame();
                }
            }
        }

        private void UpdateGameState()
        {
            // Add the time increment to the corresponding player
            if (gameActive)
            {
                if (turn == Piece.Color.Black)
                {
                    game.Timer.ApplyIncrementWhite();
                    gameUI.FormatWhitePlayerTime(game.Timer.RemainingTimeWhite);
                }
                else
                {
                    game.Timer.ApplyIncrementBlack();
                    gameUI.FormatBlackPlayerTime(game.Timer.RemainingTimeBlack);
                }
            }

            if (ChessUtils.SameColor(turn, Piece.Color.White))
            {
                // Fade the black player timer
                gameUI.FadeBlackPlayerTime();
            }
            else
            {
                // Fade the white player timer
                gameUI.FadeWhitePlayerTime();
            }

            if (firstTurn)
            {
                gameActive = true;
                firstTurn = false;
            }

            // Start the corresponding player's turn
            StartTurn();
        }

        private void EndGame()
        {
            gameActive = false;
            InputManager.Instance.InteractableColor = Piece.Color.None;
            gameUI.FinishGame(game.Status);
        }

        private async void StartTurn()
        {
            currentPlayer = turn == Piece.Color.White ? game.WhitePlayer : game.BlackPlayer;

            if (currentPlayer is AIPlayer aiPlayer)
            {
                Move move = await aiPlayer.FindBestMove(legalMoves: game.LastLegalMoves);
                StartCoroutine(MakeMove(move, isAI: true));
            }
            else if (currentPlayer is HumanPlayer humanPlayer)
            {
                // Enable interaction with the corresponding player's pieces
                InputManager.Instance.InteractableColor = turn;
            }
        }

        /*
        private async void StartTurnAI(AIPlayer aiPlayer)
        {
            Move bestMove;

            if (game.Board.TotalMoveCount <= OpeningBook.MAX_BOOK_MOVES && aiPlayer.OpeningBook.TryGetValue(game.Board.ToFEN(includeEnPassant: false), out string moveLAN))
            {
                bestMove = new Move(moveLAN, game.Board);
            }
            else
            {
                CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

                // Launch a task that cancels the search after the required search time
                Task delayTask = Task.Delay(aiPlayer.TimeManager.CalculateSearchTime(game.Board.TotalMoveCount), cancelTokenSource.Token)
                .ContinueWith(_ =>
                {
                    if (!cancelTokenSource.IsCancellationRequested)
                        aiPlayer.Search.StopSearch();
                });

                // Run the search as a background task and wait for it to finish
                bestMove = await Task.Run(() =>
                {
                    return aiPlayer.Search.StartSearch(game.LastLegalMoves);
                });

                // Cancel the timer if the search finishes first
                cancelTokenSource.Cancel();

                // Write the diagnosis result to the debugger
                SearchDebugger.Instance.DebugSearch(aiPlayer.Search.Diagnostic.ToString());
            }

            // Make the best move
            StartCoroutine(MakeMove(bestMove, isAI: true));
        }
        */

        private void UpdateTurn()
        {
            turn = game.Board.Turn;

            // Highlight the king square if in check
            boardUI.ColorCheckSquare(game.InCheck, game.Board.GetKingSquare(turn));

            // Analyze board situation
            if (game.Status == GameStatus.Type.InProgress)
            {
                // Communicate the turn change
                UpdateGameState();
            }
            else
            {
                // Communicate the end of the game
                EndGame();
            }

            // Write board state content to the debugger
            StateDebugger.Instance.LogBoardState(game.Board.CurrentState.ToString());

            // Update bitboard content in the debugger
            BitboardDebugger.Instance.UpdateDebugBitboardMode();
        }

        private void MoveChosen(Move move)
        {
            StartCoroutine(MakeMove(move, currentPlayer is AIPlayer));
        }

        public IEnumerator MakeMove(Move move, bool isAI = false)
        {
            if (!gameActive)
                yield break;

            if (isAI)
            {
                // Animate the move
                yield return StartCoroutine(boardUI.MakeMove(move));
            }
            else
            {
                boardUI.UpdateBoard(move);
            }

            // Update the model
            game.MakeMove(move);

            // Update the turn
            UpdateTurn();
        }

        public Piece GetPiece(int square)
        {
            return game.Board.GetPiece(square);
        }

        public void ShowLegalMovesForPiece(int square)
        {
            legalMovesOfPiece = game.LastLegalMoves.FindAll(m => m.From == square);
            boardUI.ShowLegalMoves(legalMovesOfPiece);
        }

        public void HideLegalMoves()
        {
            boardUI.HideLegalMoves(legalMovesOfPiece);
            legalMovesOfPiece.Clear();
        }

        public Move GetLegalMove(int destination)
        {
            foreach (Move move in legalMovesOfPiece)
            {
                if (move.To == destination)
                {
                    return move;
                }
            }

            return Move.Null;
        }

        public void DragPiece(int pieceSquare, Vector2 cursor)
        {
            boardUI.DragPiece(pieceSquare, cursor);
        }

        public bool GetSquareFromCoordinate(Vector2 worldPos, out int square)
        {
            return boardUI.GetSquareFromCoordinate(worldPos, out square);
        }

        public void DrawPieceAbove(int square)
        {
            boardUI.DrawPieceAbove(square);
        }

        public void HighlightLastMoveSquare(int square)
        {
            boardUI.ColorLastMoveSquare(square);
        }

        public void PositionPiece(int origin, int destination, int moveFlag)
        {
            boardUI.PositionPiece(origin, destination, moveFlag);
        }

        public void ReturnPieceToOrigin(int square)
        {
            boardUI.ReturnPieceToOrigin(square);
        }

        public void ReturnPieceToLayer(int square)
        {
            boardUI.ReturnPieceToLayer(square);
        }

        public void RestoreLastMoveSquareColors()
        {
            boardUI.RestoreColorsLastMoveSquares();
        }

        public void RestoreSquareColor(int square)
        {
            boardUI.RestoreSquareColor(square);
        }
    }
}