using Chess.Core;

namespace Chess.AI
{
    public class TranspositionTable
    {
        public const int SEARCH_FAILED = -1;

        // Not all evaluations have the same degree of certainty.
        // For that reason, each entry is tagged with one of these three value types.
        public enum EvaluationType : byte
        {
            Exact = 0, // All moves from this position were evaluated without pruning
            LowerBound = 1, // A move was found to be too good and a beta cutoff occurred, so there might be an even better one. This means a move was so good that the opponent will never allow reaching this position, because they will avoid that line.
            UpperBound = 2 // Refers to when no move beats alpha, so all were discarded as worse
        }

        private Entry[] entries;
        private readonly ulong entryCount;

        public struct Entry
        {
            private ulong key;
            private int value;
            private byte depth;
            private EvaluationType evaluationType;
            private Move move;

            public Entry(ulong key, int value, byte depth, EvaluationType evaluationType, Move move)
            {
                this.key = key;
                this.value = value;
                this.depth = depth; // equivalent to remainingDepth in each case
                this.evaluationType = evaluationType;
                this.move = move;
            }

            public ulong Key => key;
            public int Value => value;
            public byte Depth => depth;
            public EvaluationType EvaluationType => evaluationType;
            public Move Move => move;
        }

        public TranspositionTable(int sizeMB)
        {
            int entrySizeBytes = System.Runtime.InteropServices.Marshal.SizeOf<Entry>();
            int tableSizeBytes = sizeMB * 1024 * 1024;
            entryCount = (ulong)(tableSizeBytes / entrySizeBytes);
            entries = new Entry[entryCount];
        }

        private ulong GetIndex(ulong hash)
        {
            return hash % entryCount;
        }

        public int SearchEvaluation(ulong hash, int remainingDepth, int depthFromRoot, int alpha, int beta)
        {
            Entry entry = entries[GetIndex(hash)];

            // If the entry is filled with information for that position
            if (entry.Key == hash)
            {
                // Only reuse evaluations with equal or greater depth than the current one
                if (entry.Depth >= remainingDepth)
                {
                    int evaluation = AdjustMateEvaluationForSearch(entry.Value, depthFromRoot);

                    // If it is an exact evaluation, it can be returned directly
                    if (entry.EvaluationType == EvaluationType.Exact)
                    {
                        return evaluation;
                    }

                    // If the best the opponent can do does not beat alpha, this branch is safe.
                    if (entry.EvaluationType == EvaluationType.UpperBound && evaluation <= alpha)
                    {
                        return evaluation;
                    }

                    // If the worst the opponent can do is greater than beta, then you can do a beta cutoff
                    if (entry.EvaluationType == EvaluationType.LowerBound && evaluation >= beta)
                    {
                        return evaluation;
                    }
                }
            }

            return SEARCH_FAILED;
        }

        public void SaveEvaluation(ulong hash, int remainingDepth, int depthFromRoot, int evaluation, EvaluationType evaluationType, Move move)
        {
            ulong index = GetIndex(hash);

            //if (remainingDepth >= entries[index].remainingDepth) {
            Entry entry = new Entry(hash, AdjustMateEvaluationForSave(evaluation, depthFromRoot), (byte)remainingDepth, evaluationType, move);
            entries[index] = entry;
            //}
        }

        public Move GetStoredMove(ulong hash)
        {
            return entries[GetIndex(hash)].Move;
        }

        public int GetStoredEvaluation(ulong hash)
        {
            return entries[GetIndex(hash)].Value;
        }

        // Converts a local mate value (from the current position) into an absolute mate value (relative to the root of the search tree)
        private int AdjustMateEvaluationForSave(int evaluation, int depthFromRoot)
        {
            if (Search.IsCheckmate(evaluation))
            {
                int sign = System.Math.Sign(evaluation);
                return (evaluation * sign + depthFromRoot) * sign;
            }

            return evaluation;
        }

        // Converts an absolute mate value (relative to the root of the search tree) into a local mate value (from the current position)
        private int AdjustMateEvaluationForSearch(int evaluation, int depthFromRoot)
        {
            if (Search.IsCheckmate(evaluation))
            {
                int sign = System.Math.Sign(evaluation);
                return (evaluation * sign - depthFromRoot) * sign;
            }

            return evaluation;
        }

        public void Clear()
        {
            for (ulong i = 0; i < entryCount; i++)
            {
                entries[i] = new Entry();
            }
        }

        public float OccupancyPercentage
        {
            get
            {
                ulong occupied = 0;

                for (ulong i = 0; i < entryCount; i++)
                {
                    if (entries[i].Key != 0) // Assuming key=0 means empty entry
                    {
                        occupied++;
                    }
                }

                return (occupied / (float)entryCount) * 100f;
            }
        }
    }
}