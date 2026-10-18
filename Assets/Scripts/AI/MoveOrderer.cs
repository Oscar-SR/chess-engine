using System;
using System.Collections.Generic;
using System.Linq;
using Chess.Core;

namespace Chess.AI
{
    public class MoveOrderer
    {
        public const int MAX_NUM_MOVES = 218;

        private Board board;
        private int[] moveScores;

        public MoveOrderer(Board board)
        {
            this.board = board;
            moveScores = new int[MAX_NUM_MOVES];
        }

        public void OrderMoves(List<Move> moves)
        {
            int count = moves.Count;

            // Calculate the scores
            for (int i = 0; i < count; i++)
            {
                Move move = moves[i];
                int score = 0;
                Piece originPiece = board.GetPiece(move.From);
                Piece capturedPiece = board.GetPiece(move.To);

                // Prioritize capturing pieces that are more valuable than the moving piece
                if (originPiece.PieceType != Piece.Type.None)
                {
                    score = 10 * Evaluation.GetPieceValue(capturedPiece.PieceType) - Evaluation.GetPieceValue(originPiece.PieceType);
                }

                // Prioritize promotions
                if (move.IsPromotion())
                {
                    score += Evaluation.GetPromotionValue(move.Flag);
                }

                // Penalize moving to a square attacked by an enemy pawn
                if (board.IsSquareAttackedByEnemyPawn(move.To))
                {
                    score -= Evaluation.GetPieceValue(originPiece.PieceType);
                }

                moveScores[i] = score;
            }

            Sort(moves, count);
        }

        private void Sort(List<Move> moves, int count)
        {
            moveScores = moveScores.Take(count).ToArray();
            // Sort the scores array
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => moveScores[b].CompareTo(moveScores[a]));

            // Reorder the original list based on the scores array
            List<Move> sortedMoves = new List<Move>(count);
            for (int i = 0; i < count; i++)
            {
                sortedMoves.Add(moves[indices[i]]);
            }

            moves.Clear();
            moves.AddRange(sortedMoves);
        }
    }
}