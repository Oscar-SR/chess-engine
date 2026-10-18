using static System.Math;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Chess.Core;
using Chess.Debugging;

namespace Chess.AI
{
    public class Search
    {
        public enum SearchType : byte
        {
            ByDepth,
            ByTime,
            ByNodeCount
        }

        public const int INFINITY = 9999999;
        public const int CHECKMATE_EVALUATION = 100000;
        public const int TRANSPOSITION_TABLE_SIZE_MB = 64;
        private const int MAX_DEPTH = 256;

        private readonly SearchType searchType;
        private readonly int searchLimit;

        private Board board;
        private Evaluation evaluation;
        private MoveOrderer moveOrderer;
        private TranspositionTable transpositionTable;
        private Move bestMoveThisIteration;
        private int bestEvaluationThisIteration;
        private bool searchCanceled;
        private SearchDiagnostic diagnostic;

        [Serializable]
        public struct SearchDiagnostic
        {
            private float searchTime;
            private int maxDepth;
            private int bestEvaluation;
            private Move bestMove;
            private float transpositionTableOccupancy;

            public float SearchTime
            {
                get { return searchTime; }
                set { searchTime = value; }
            }

            public int MaxDepth
            {
                get { return maxDepth; }
                set { maxDepth = value; }
            }

            public int BestEvaluation
            {
                get { return bestEvaluation; }
                set { bestEvaluation = value; }
            }

            public Move BestMove
            {
                get { return bestMove; }
                set { bestMove = value; }
            }

            public float TranspositionTableOccupancy
            {
                get { return transpositionTableOccupancy; }
                set { transpositionTableOccupancy = value; }
            }

            public override string ToString()
            {
                const string DARK_RED = "#CD6666";
                const string ORANGE = "#DDB267";
                const string LILAC = "#C48AEB";
                const string PASTEL_BLUE = "#8DD2EC";
                const string GREEN = "#699A33";

                return $"Depth searched: <color={DARK_RED}>{maxDepth}</color>\n" +
                       $"Evaluation: <color={ORANGE}>{(IsCheckmate(bestEvaluation) ? "#" + GetNumPlysToMate(bestEvaluation) : bestEvaluation)}</color>\n" +
                       $"Move: <color={LILAC}>{(bestMove == Move.Null ? "N/A" : bestMove.ToLAN())}</color>\n" +
                       $"Time: <color={PASTEL_BLUE}>{searchTime} ms</color>\n" +
                       $"Transposition table: <color={GREEN}>{transpositionTableOccupancy.ToString("F2")} %</color>\n";
            }
        }

        public SearchDiagnostic Diagnostic
        {
            get
            {
                return diagnostic;
            }
        }

        public Search(Board board, SearchType searchType, int searchLimit = 0)
        {
            this.board = board;
            this.searchType = searchType;
            this.searchLimit = searchLimit;
            evaluation = new Evaluation(board);
            moveOrderer = new MoveOrderer(board);
            transpositionTable = new TranspositionTable(TRANSPOSITION_TABLE_SIZE_MB);
        }

        public Move StartSearch(List<Move> initialMoves = null)
        {
            // Initialize diagnostic data
            diagnostic = new SearchDiagnostic();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            SearchDebugger.Instance?.Log("Starting search\n");

            // Generate the initial moves if there are none
            initialMoves ??= board.GenerateLegalMoves(shortenGeneration: true).legalMoves;
            int bestEvaluation; Move bestMove;

            switch (searchType)
            {
                case SearchType.ByDepth:
                    {
                        (bestEvaluation, bestMove) = FixedDepthSearch(initialMoves, searchLimit);
                        break;
                    }
                case SearchType.ByTime:
                    {
                        searchCanceled = false;
                        (bestEvaluation, bestMove) = IterativeDeepeningSearch(initialMoves);
                        break;
                    }
                case SearchType.ByNodeCount:
                    {
                        (bestEvaluation, bestMove) = FixedDepthSearch(initialMoves, searchLimit);
                        break;
                    }
                default:
                    {
                        bestEvaluation = 0;
                        bestMove = Move.Null;
                        break;
                    }
            }

            if (bestMove == Move.Null)
            {
                bestMove = initialMoves[0];
            }

            // Save diagnostic data
            SearchDebugger.Instance?.Log("Search finished with: " + bestMove.ToLAN() + " (" + bestEvaluation + ")\n\n");
            stopwatch.Stop();
            diagnostic.SearchTime = stopwatch.ElapsedMilliseconds;
            diagnostic.BestEvaluation = bestEvaluation;
            diagnostic.BestMove = bestMove;
            diagnostic.TranspositionTableOccupancy = transpositionTable.OccupancyPercentage;

            return bestMove;
        }

        public void StopSearch()
        {
            searchCanceled = true;
        }

        private (int bestEvaluation, Move bestMove) IterativeDeepeningSearch(List<Move> moves)
        {
            int bestEvaluation = -INFINITY;
            Move bestMove = Move.Null;

            /// CLEARING THE TRANSPOSITION TABLE BEFORE A SEARCH IS NOT CORRECT. HOWEVER, THERE IS SOME ERROR
            /// IN SOME PART OF THE CODE AND THIS IMPROVES ENDGAME PLAY
            // transpositionTable.Clear();

            for (int searchDepth = 1; searchDepth <= MAX_DEPTH; searchDepth++)
            {
                SearchDebugger.Instance?.Log("Starting iteration: " + searchDepth + "\n");

                bestEvaluationThisIteration = -INFINITY;
                bestMoveThisIteration = Move.Null;
                // hasSearchedForAMove = false;

                RootAlphaBeta(moves, searchDepth);

                if (searchCanceled)
                {
                    /*
                    //if (hasSearchedForAMove && bestEvaluationThisIteration > bestEvaluation)
                    if (hasSearchedForAMove)
                    {
                        bestEvaluation = bestEvaluationThisIteration;
                        bestMove = bestMoveThisIteration;
                        SearchDebugger.Instance.Log("Using result of a partial search: " + bestMove.ToLAN() + " with evaluation " + bestEvaluation + "\n");
                    }
                    */

                    SearchDebugger.Instance?.Log("Search canceled\n");
                    break;
                }
                else
                {
                    diagnostic.MaxDepth = searchDepth;
                    bestEvaluation = bestEvaluationThisIteration;
                    bestMove = bestMoveThisIteration;
                    SearchDebugger.Instance?.Log("Iteration result: " + bestMove.ToLAN() + " (" + bestEvaluation + ")\n");
                }
            }

            return (bestEvaluation, bestMove);
        }

        private (int bestEvaluation, Move bestMove) FixedDepthSearch(List<Move> moves, int searchDepth)
        {
            RootAlphaBeta(moves, searchDepth);
            diagnostic.MaxDepth = searchDepth;
            return (bestEvaluationThisIteration, bestMoveThisIteration);
        }

        // Perform a search by node count

        private void RootAlphaBeta(List<Move> moves, int searchDepth)
        {
            if (searchCanceled)
                return;

            int alpha = -INFINITY; // Stores maximum evaluation values
            int beta = INFINITY; // Stores minimum evaluation values

            int evaluationValue = transpositionTable.SearchEvaluation(board.CurrentState.ZobristHash, searchDepth, 0, alpha, beta);
            if (evaluationValue != TranspositionTable.SEARCH_FAILED)
            {
                bestMoveThisIteration = transpositionTable.GetStoredMove(board.CurrentState.ZobristHash);
                bestEvaluationThisIteration = evaluationValue;
            }

            TranspositionTable.EvaluationType evaluationType = TranspositionTable.EvaluationType.UpperBound;

            OrderMoves(moves);
            foreach (Move move in moves)
            {
                board.MakeMove(move, inSearch: true);
                evaluationValue = -AlphaBeta(searchDepth - 1, 1, -beta, -alpha);
                board.UndoMove(move);

                if (searchCanceled)
                    return;

                if (evaluationValue > alpha)
                {
                    // A new best move was found at this position
                    evaluationType = TranspositionTable.EvaluationType.Exact;
                    bestMoveThisIteration = move;
                    bestEvaluationThisIteration = evaluationValue;
                    // hasSearchedForAMove = true;
                    alpha = evaluationValue;
                }
            }

            // Save the evaluated position (with alpha, since it stores maximum evaluations, i.e., the best evaluation)
            transpositionTable.SaveEvaluation(board.CurrentState.ZobristHash, /*maxProfundidad*/ searchDepth, 0, alpha, evaluationType, bestMoveThisIteration);
        }

        private int AlphaBeta(int remainingDepth, int depthFromRoot, int alpha, int beta)
        {
            if (searchCanceled)
                return 0;

            int evaluationValue = transpositionTable.SearchEvaluation(board.CurrentState.ZobristHash, remainingDepth, depthFromRoot, alpha, beta);
            if (evaluationValue != TranspositionTable.SEARCH_FAILED)
            {
                return evaluationValue;
            }

            if (remainingDepth == 0)
            {
                evaluationValue = QuiescenceSearch(alpha, beta);
                return evaluationValue;
            }

            (List<Move> moves, bool check) = board.GenerateLegalMoves(shortenGeneration: true);
            if (moves.Count == 0)
            {
                if (check)
                {
                    // Defeat of the current player; penalize defeats that are many moves away
                    return -CHECKMATE_EVALUATION + depthFromRoot;
                }

                // Draw
                return 0;
            }

            TranspositionTable.EvaluationType evaluationType = TranspositionTable.EvaluationType.UpperBound;
            Move bestMoveThisPosition = Move.Null;

            OrderMoves(moves);
            foreach (Move move in moves)
            {
                board.MakeMove(move, inSearch: true);
                evaluationValue = -AlphaBeta(remainingDepth - 1, depthFromRoot + 1, -beta, -alpha);
                board.UndoMove(move);

                if (searchCanceled)
                    return 0;

                if (evaluationValue >= beta)
                {
                    // Prune
                    transpositionTable.SaveEvaluation(board.CurrentState.ZobristHash, remainingDepth, depthFromRoot, evaluationValue, TranspositionTable.EvaluationType.LowerBound, move);
                    return beta;
                }

                if (evaluationValue > alpha)
                {
                    // A new best move was found at this position
                    evaluationType = TranspositionTable.EvaluationType.Exact;
                    bestMoveThisPosition = move;
                    alpha = evaluationValue;
                }
            }

            transpositionTable.SaveEvaluation(board.CurrentState.ZobristHash, remainingDepth, depthFromRoot, alpha, evaluationType, bestMoveThisPosition);
            return alpha;
        }

        /// <summary>
        /// Performs a capture search ("quiescence search") to avoid the instability problem in
        /// the evaluation caused by pending tactical captures.
        /// </summary>
        /// <param name="board">Current board state.</param>
        /// <param name="alpha">Alpha value for alpha-beta pruning.</param>
        /// <param name="beta">Beta value for alpha-beta pruning.</param>
        /// <returns>The updated evaluation value after exploring possible captures.</returns>
        /// <remarks>
        /// The quiescence search extends the search only to capture moves to stabilize the evaluation.
        /// Source: <see href="https://www.chessprogramming.org/Quiescence_Search">Chess Programming - Quiescence Search</see>
        /// </remarks>
        private int QuiescenceSearch(int alpha, int beta)
        {
            if (searchCanceled)
                return 0;

            int evaluationValue = evaluation.Evaluate();
            if (evaluationValue >= beta)
                return beta;
            alpha = Max(alpha, evaluationValue);

            (List<Move> captureMoves, _) = board.GenerateLegalMoves(shortenGeneration: true, capturesOnly: true);
            OrderMoves(captureMoves);

            foreach (Move move in captureMoves)
            {
                board.MakeMove(move, inSearch: true);
                evaluationValue = -QuiescenceSearch(-beta, -alpha);
                board.UndoMove(move);

                if (evaluationValue >= beta)
                    return beta;
                alpha = Max(alpha, evaluationValue);
            }

            return alpha;
        }

        public static bool IsCheckmate(int evaluation)
        {
            if (evaluation == int.MinValue)
            {
                return false;
            }

            const int maxCheckmateDepth = 1000;
            return Abs(evaluation) > (CHECKMATE_EVALUATION - maxCheckmateDepth);
        }

        public static int GetNumPlysToMate(int evaluation)
        {
            return CHECKMATE_EVALUATION - Abs(evaluation);
        }

        private void OrderMoves(List<Move> moves)
        {
            int count = moves.Count;
            int[] scores = new int[count];

            // Calculate scores
            for (int i = 0; i < count; i++)
            {
                Move move = moves[i];
                int score = 0;
                Piece sourcePiece = board.GetPiece(move.From);
                Piece capturedPiece = board.GetPiece(move.To);

                // Prioritize capturing more valuable pieces than the one moved
                if (sourcePiece.PieceType != Piece.Type.None)
                {
                    score = 10 * Evaluation.GetPieceValue(capturedPiece.PieceType) - Evaluation.GetPieceValue(sourcePiece.PieceType);
                }

                // Prioritize promotions
                if (move.IsPromotion())
                {
                    score += Evaluation.GetPromotionValue(move.Flag);
                }

                // Penalize moving to a square attacked by an enemy pawn
                if (board.IsSquareAttackedByEnemyPawn(move.To))
                {
                    score -= Evaluation.GetPieceValue(sourcePiece.PieceType);
                }

                scores[i] = score;
            }

            // Sort the scores array
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => scores[b].CompareTo(scores[a]));

            // Reorder the original list based on the scores array
            List<Move> orderedMoves = new List<Move>(count);
            for (int i = 0; i < count; i++)
            {
                orderedMoves.Add(moves[indices[i]]);
            }

            moves.Clear();
            moves.AddRange(orderedMoves);
        }
    }
}