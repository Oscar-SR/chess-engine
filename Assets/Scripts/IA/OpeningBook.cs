using System;
using System.Collections.Generic;
using Ajedrez.Core;

namespace Ajedrez.IA
{
    public class OpeningBook
    {
        public const int MAX_BOOK_MOVES = 8;

        readonly Dictionary<string, BookMove[]> movesByPosition;
        readonly Random rng;

        public readonly struct BookMove
        {
            private readonly string moveLAN;
            private readonly int timesPlayed;

            public BookMove(string moveLAN, int timesPlayed)
            {
                this.moveLAN = moveLAN;
                this.timesPlayed = timesPlayed;
            }

            public string MoveLAN => moveLAN;
            public int TimesPlayed => timesPlayed;
        }

        public OpeningBook(string content)
        {
            rng = new Random();
            Span<string> entries = content.Trim(new char[] { ' ', '\n' }).Split("pos").AsSpan(1);
            movesByPosition = new Dictionary<string, BookMove[]>(entries.Length);

            for (int i = 0; i < entries.Length; i++)
            {
                string[] entry = entries[i].Trim('\n').Split('\n');
                string positionFen = entry[0].Trim();
                Span<string> moveData = entry.AsSpan(1);

                BookMove[] bookMoves = new BookMove[moveData.Length];

                for (int moveIndex = 0; moveIndex < bookMoves.Length; moveIndex++)
                {
                    string[] moveDatum = moveData[moveIndex].Split(' ');
                    bookMoves[moveIndex] = new BookMove(moveDatum[0], int.Parse(moveDatum[1]));
                }

                movesByPosition.Add(positionFen, bookMoves);
            }
        }

        public bool HasMove(string positionFen)
        {
            return movesByPosition.ContainsKey(RemoveFenCounters(positionFen));
        }

        private string RemoveFenCounters(string fen)
        {
            string fenA = fen.Substring(0, fen.LastIndexOf(' '));
            return fenA.Substring(0, fenA.LastIndexOf(' '));
        }

        // weightPower is a value between 0 and 1.
        // 0 => all moves have equal probability
        // 1 => probability is proportional to the number of times played
        public bool TryGetValue(string positionFen, out string chosenMove, double weightPower = 0.5)
        {
            // Look up the possible moves for the position (without the FEN move counters)
            if (movesByPosition.TryGetValue(RemoveFenCounters(positionFen), out BookMove[] bookMoves))
            {
                // Ensure weightPower stays within the allowed range (0 to 1)
                weightPower = Math.Clamp(weightPower, 0, 1);

                // Calculate the total weight (weighted moves)
                int totalTimesPlayed = 0;
                foreach (BookMove bookMove in bookMoves)
                {
                    totalTimesPlayed += CalculateWeight(bookMove.TimesPlayed);
                }

                // Calculate the normalized weight of each move and sum the weights
                double[] weights = new double[bookMoves.Length];
                double weightSum = 0;
                for (int i = 0; i < bookMoves.Length; i++)
                {
                    double weight = CalculateWeight(bookMoves[i].TimesPlayed) / (double)totalTimesPlayed;
                    weightSum += weight;
                    weights[i] = weight;
                }

                // Calculate the cumulative probability of each move
                double[] cumulativeProbability = new double[bookMoves.Length];
                for (int i = 0; i < weights.Length; i++)
                {
                    double probability = weights[i] / weightSum;
                    cumulativeProbability[i] = cumulativeProbability[Math.Max(0, i - 1)] + probability;
                }

                // Select the matching move based on the cumulative probability and a random number between 0 and 1
                double random = rng.NextDouble();
                for (int i = 0; i < bookMoves.Length; i++)
                {
                    if (random <= cumulativeProbability[i])
                    {
                        chosenMove = bookMoves[i].MoveLAN;
                        return true;
                    }
                }
            }

            chosenMove = "Null";
            return false;

            // Local method: calculates the weight of a move based on times played raised to weightPower
            int CalculateWeight(int timesPlayed) => (int)Math.Ceiling(Math.Pow(timesPlayed, weightPower));
        }
    }
}