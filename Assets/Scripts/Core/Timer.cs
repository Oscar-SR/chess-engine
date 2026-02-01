using System;

namespace Ajedrez.Core
{
    public class Timer
    {
        private readonly float initialDuration;
        private readonly float incrementPerMove;
        private float remainingTimeWhite;
        private float remainingTimeBlack;

        public Timer(float initialDuration, float increment)
        {
            this.initialDuration = remainingTimeWhite = remainingTimeBlack = initialDuration;
            incrementPerMove = increment;
        }

        public float InitialDuration
        {
            get => initialDuration;
        }

        public float IncrementPerMove
        {
            get => incrementPerMove;
        }

        public float RemainingTimeWhite
        {
            get => remainingTimeWhite;
            private set => remainingTimeWhite = Math.Max(0, value);
        }

        public float RemainingTimeBlack
        {
            get => remainingTimeBlack;
            private set => remainingTimeBlack = Math.Max(0, value);
        }

        public void ConsumeTimeWhite(float consumedTime)
        {
            RemainingTimeWhite -= consumedTime;
        }

        public void ApplyIncrementWhite()
        {
            RemainingTimeWhite += incrementPerMove;
        }

        public void ConsumeTimeBlack(float consumedTime)
        {
            RemainingTimeBlack -= consumedTime;
        }

        public void ApplyIncrementBlack()
        {
            RemainingTimeBlack += incrementPerMove;
        }

        public bool TimeOutWhite => remainingTimeWhite <= 0;
        public bool TimeOutBlack => remainingTimeBlack <= 0;
    }
}