using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ajedrez.Managers;
using Ajedrez.IA;
using Ajedrez.Debugging;
using System.Diagnostics;

namespace Ajedrez.Core
{
    public class AIPlayer : Player
    {
        private AIConfiguration aiConfiguration;
        private Board board;
        private Search search;
        private OpeningBook openingBook;
        private TimeManager timeManager;

        public AIPlayer(string name, Piece.Color pieceColor, Board board, Timer timer, AIConfiguration aiConfiguration)
            : base(name, pieceColor)
        {
            this.board = board;
            this.aiConfiguration = aiConfiguration;

            if (timer != null)
                timeManager = new TimeManager(timer, pieceColor);

            ConfigureAI();
        }

        public AIPlayer(Piece.Color pieceColor, Board board, Timer timer, AIConfiguration aiConfiguration)
            : base("AI " + aiConfiguration.Difficulty, pieceColor)
        {
            this.board = board;
            this.aiConfiguration = aiConfiguration;

            if (timer != null)
                timeManager = new TimeManager(timer, pieceColor);

            ConfigureAI();
        }

        private void ConfigureAI()
        {
            if (aiConfiguration.UseOpeningBook)
                openingBook = new OpeningBook(aiConfiguration.OpeningBook.text);

            search = new Search(board, aiConfiguration.SearchLimit, aiConfiguration.Limit);
        }

        public AIConfiguration AIConfiguration
        {
            get
            {
                return aiConfiguration;
            }
            set
            {
                aiConfiguration = value;
                base.Name = "AI " + aiConfiguration.Difficulty;
                ConfigureAI();
            }
        }

        public override Piece.Color PieceColor
        {
            get => base.PieceColor;
            set
            {
                if (timeManager != null)
                    timeManager.PieceColor = value;

                base.PieceColor = value;
            }
        }

        public Search Search
        {
            get
            {
                return search;
            }
        }

        public TimeManager TimeManager
        {
            set
            {
                timeManager = value;
            }
        }

        public Board Board
        {
            set
            {
                board = value;
                search = new Search(board, aiConfiguration.SearchLimit, aiConfiguration.Limit);
            }
        }

        public async Task<Move> FindBestMove(List<Move> legalMoves = null)
        {
            Move bestMove;

            if (aiConfiguration.UseOpeningBook && board.TotalMoveCount <= aiConfiguration.MaxBookMovement && openingBook.TryGetValue(board.ToFEN(includeEnPassant: false), out string moveLAN))
            {
                bestMove = new Move(moveLAN, board);
            }
            else
            {
                CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

                if (aiConfiguration.SearchLimit == Search.SearchType.ByTime)
                {
                    // Launch a task that cancels the search after the required search time
                    int searchTime = aiConfiguration.Limit == AIConfiguration.DYNAMIC_TIME ? timeManager.CalculateSearchTime(board.TotalMoveCount) : aiConfiguration.Limit;
                    Task delayTask = Task.Delay(searchTime, cancelTokenSource.Token)
                    .ContinueWith(_ =>
                    {
                        if (!cancelTokenSource.IsCancellationRequested)
                            search.StopSearch();
                    });
                }

                // Execute the search as a background task and wait for it to finish
                bestMove = await Task.Run(() =>
                {
                    return search.StartSearch(legalMoves);
                });

                // Cancel the timer if the search finishes first
                cancelTokenSource.Cancel();

                // Write the diagnostic result to the debugger
                SearchDebugger.Instance?.DebugSearch(search.Diagnostic.ToString());
            }

            return bestMove;
        }
    }
}
