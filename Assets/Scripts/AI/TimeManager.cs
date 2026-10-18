using System;
using Chess.Core;

namespace Chess.AI
{
    public class TimeManager
    {
        public const float INCREMENT_FACTOR = 0.6f;
        private Timer timer;
        private Piece.Color pieceColor;
        private readonly float employedIncrement;

        public TimeManager(Timer timer, Piece.Color pieceColor)
        {
            this.timer = timer;
            this.pieceColor = pieceColor;
            employedIncrement = timer.IncrementPerMove * INCREMENT_FACTOR;
        }

        public Piece.Color PieceColor
        {
            get { return pieceColor; }
            set { pieceColor = value; }
        }

        public int CalculateSearchTime(uint moveNumber)
        {
            const float MAX_TIME_FRACTION = 0.1f; // 10%
            const float MIN_SEARCH_SECONDS = 0.05f; // 50 ms

            int expectedMoves = EstimateRemainingMoves(moveNumber);
            float remainingTime = RemainingTime;
            float optimalTime = RemainingTime / expectedMoves + employedIncrement;
            float maxTime = RemainingTime * MAX_TIME_FRACTION;

            // Limits the time so it never uses more than 10% of the remaining time
            optimalTime = Math.Min(optimalTime, maxTime);

            // Guarantees a minimum
            optimalTime = Math.Max(optimalTime, MIN_SEARCH_SECONDS);

            // Converts to milliseconds and rounds
            // UnityEngine.Debug.Log(remainingTime + " " + optimalTime);
            return (int)Math.Round(optimalTime * 1000);
        }

        private int EstimateRemainingMoves(uint moveNumber)
        {
            if (moveNumber < 20)
                return 40;
            else if (moveNumber < 40)
                return 20;
            else
                return 10;
        }

        private float RemainingTime
        {
            get
            {
                if (pieceColor == Piece.Color.White)
                {
                    return timer.RemainingTimeWhite;
                }
                else
                {
                    return timer.RemainingTimeBlack;
                }
            }
        }
    }
}
