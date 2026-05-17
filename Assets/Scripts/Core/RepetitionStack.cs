using System;

namespace Ajedrez.Core
{
    public class RepetitionStack
    {
        private const int MAX_STORED_POSITIONS = 128;

        private readonly ulong[] hashes; // Array containing position hashes (0 -> bottom of the stack, 64 -> top of the stack)
        private readonly int[] startIndices; // Array containing indices where a valid segment starts (a valid segment is defined after an irreversible move occurs)
        private int counter; // Index of the last registered position in hashes

        public RepetitionStack()
        {
            hashes = new ulong[MAX_STORED_POSITIONS];
            startIndices = new int[MAX_STORED_POSITIONS + 1];
            counter = 0;
        }

        public RepetitionStack(int maxPositions)
        {
            hashes = new ulong[maxPositions];
            startIndices = new int[maxPositions + 1];
            counter = 0;
        }

        public void Push(ulong hash, bool isIrreversibleMove)
        {
            // Check bounds
            if (counter < hashes.Length)
            {
                hashes[counter] = hash;
                startIndices[counter + 1] = isIrreversibleMove ? counter : startIndices[counter];
            }
            counter++;
        }

        public void Pop()
        {
            counter = Math.Max(0, counter - 1);
        }

        public bool IsThreefoldRepetition(ulong hash)
        {
            int start = startIndices[counter];
            int repetitions = 0;

            for (int i = start; i < counter - 1; i++) // excludes current position (up to counter - 1)
            {
                if (hashes[i] == hash)
                {
                    repetitions++;
                    if (repetitions == 2) // if it appears 2 times (already seen twice before)
                        return true;
                }
            }

            /// TODO:
            /// If one match is found
            /// and the current ply is greater than root ply + 2
            /// (meaning the search has already progressed somewhat),
            /// also return a draw.

            return false;
        }
        
        public void Clear()
        {
            Array.Clear(hashes, 0, counter);
            Array.Clear(startIndices, 0, counter + 1);
            counter = 0;
        }
    }
}