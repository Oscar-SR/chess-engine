using static System.Math;

namespace Ajedrez.Utilities
{
    public static class BitboardUtils
    {
        public static readonly ulong[] KnightAttacks = new ulong[ChessUtils.BOARD_SQUARE_COUNT]; // bitboard containing the squares from which a knight can attack each square of the board
        public static readonly ulong[] KingAttacks = new ulong[ChessUtils.BOARD_SQUARE_COUNT]; // bitboard containing the squares from which a king can attack each square of the board
        public static readonly ulong[,] PawnAttacks = new ulong[2, ChessUtils.BOARD_SQUARE_COUNT]; // bitboard containing the squares from which a pawn of each color can attack each square of the board
        private static ulong[,] SquaresBetween = new ulong[64, 64]; // matrix of bitboards defining the squares between two others in a straight or diagonal line (if there are no such squares in between, returns 0UL)
        private static readonly int[] Index64 = // De Bruijn sequence
        {
            0, 47,  1, 56, 48, 27,  2, 60,
            57, 49, 41, 37, 28, 16,  3, 61,
            54, 58, 35, 52, 50, 42, 21, 44,
            38, 32, 29, 23, 17, 11,  4, 62,
            46, 55, 26, 59, 40, 36, 15, 53,
            34, 51, 20, 43, 31, 22, 10, 45,
            25, 39, 14, 33, 19, 30,  9, 24,
            13, 18,  8, 12,  7,  6,  5, 63
        };

        // Static constructor
        static BitboardUtils()
        {
            // Precalculations
            PrecalculateKnightAttacks();
            PrecalculateKingAttacks();
            PrecalculatePawnAttacks();
            PrecalculateSquaresBetween();
        }

        public static void Initialize()
        {
            // Empty method for lazy initialization
            // Call this method to force initialization
        }

        /// <summary>
        /// Sets the bit corresponding to the given square.
        /// </summary>
        /// <param name="square">Square index (0-63).</param>
        /// <returns>Bitboard with only that square set.</returns>
        public static ulong SetBit(int square)
        {
            return 1UL << square;
        }

        /// <summary>
        /// Checks whether a square is set in the given bitboard.
        /// </summary>
        /// <param name="square">Square index.</param>
        /// <param name="bitboard">Bitboard to evaluate.</param>
        /// <returns>True if the bit is set, false otherwise.</returns>
        public static bool IsSquareActive(int square, ulong bitboard)
        {
            return (bitboard & SetBit(square)) != 0;
        }

        /// <summary>
        /// Indicates whether more than one bit is set in the bitboard.
        /// </summary>
        /// <param name="bitboard">Bitboard to evaluate.</param>
        /// <returns>True if more than one bit is set, false otherwise (0 or 1).</returns>
        public static bool MoreThanOneBitActive(ulong bitboard)
        {
            return (bitboard & (bitboard - 1)) != 0;
        }

        // KNIGHT ATTACKS
        private static void PrecalculateKnightAttacks()
        {
            for (int i = 0; i < ChessUtils.BOARD_SQUARE_COUNT; i++)
            {
                ulong attacks = 0UL;
                (int rank, int file) = ChessUtils.IndexToCoordinates(i);

                int[,] offsets = {
                    { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 },
                    { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 }
                };

                for (int j = 0; j < offsets.GetLength(0); j++)
                {
                    int newRank = rank + offsets[j, 0];
                    int newFile = file + offsets[j, 1];
                    if (newRank >= 0 && newRank < ChessUtils.BOARD_SIZE && newFile >= 0 && newFile < ChessUtils.BOARD_SIZE)
                        attacks |= SetBit(ChessUtils.CoordinatesToIndex(newRank, newFile));
                }

                KnightAttacks[i] = attacks;
            }
        }

        // KING ATTACKS
        private static void PrecalculateKingAttacks()
        {
            for (int i = 0; i < ChessUtils.BOARD_SQUARE_COUNT; i++)
            {
                ulong attacks = 0UL;
                (int rank, int file) = ChessUtils.IndexToCoordinates(i);

                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int df = -1; df <= 1; df++)
                    {
                        if (dr == 0 && df == 0) continue;

                        int newRank = rank + dr;
                        int newFile = file + df;

                        if (newRank >= 0 && newRank < ChessUtils.BOARD_SIZE && newFile >= 0 && newFile < ChessUtils.BOARD_SIZE)
                            attacks |= SetBit(ChessUtils.CoordinatesToIndex(newRank, newFile));
                    }
                }

                KingAttacks[i] = attacks;
            }
        }

        // PAWN ATTACKS
        private static void PrecalculatePawnAttacks()
        {
            for (int i = 0; i < ChessUtils.BOARD_SQUARE_COUNT; i++)
            {
                (int rank, int file) = ChessUtils.IndexToCoordinates(i);

                ulong white = 0UL, black = 0UL;

                // White pawn attacks (north = rank - 1)
                if (rank > 0)
                {
                    if (file > 0) white |= SetBit(ChessUtils.CoordinatesToIndex(rank - 1, file - 1));
                    if (file < 7) white |= SetBit(ChessUtils.CoordinatesToIndex(rank - 1, file + 1));
                }

                // Black pawn attacks (south = rank + 1)
                if (rank < 7)
                {
                    if (file > 0) black |= SetBit(ChessUtils.CoordinatesToIndex(rank + 1, file - 1));
                    if (file < 7) black |= SetBit(ChessUtils.CoordinatesToIndex(rank + 1, file + 1));
                }

                PawnAttacks[0, i] = white; // White
                PawnAttacks[1, i] = black; // Black
            }
        }

        private static void PrecalculateSquaresBetween()
        {
            for (int from = 0; from < 64; from++)
            {
                for (int to = 0; to < 64; to++)
                {
                    SquaresBetween[from, to] = CalculateSquaresBetween(from, to);
                }
            }
        }

        private static ulong CalculateSquaresBetween(int from, int to)
        {
            int fromRank = from / 8;
            int fromFile = from % 8;
            int toRank = to / 8;
            int toFile = to % 8;

            int rankDelta = Sign(toRank - fromRank);
            int fileDelta = Sign(toFile - fromFile);

            // If it is the same square, there is nothing between them
            if (rankDelta == 0 && fileDelta == 0)
                return 0UL;

            // Check whether they are aligned (same rank, file or diagonal)
            if ((Abs(toRank - fromRank) == Abs(toFile - fromFile)) ||
                fromRank == toRank || fromFile == toFile)
            {
                ulong ray = 0UL;
                int r = fromRank + rankDelta;
                int f = fromFile + fileDelta;

                while (r != toRank || f != toFile)
                {
                    ray |= 1UL << (r * 8 + f);
                    r += rankDelta;
                    f += fileDelta;
                }

                // Exclude the origin and destination squares
                ray &= ~(1UL << from);
                ray &= ~(1UL << to);

                return ray;
            }

            return 0UL;
        }

        /// <summary>
        /// Returns the squares between two positions if they are aligned.
        /// </summary>
        /// <param name="from">Starting square index.</param>
        /// <param name="to">Ending square index.</param>
        /// <returns>Bitboard with the squares between 'from' and 'to'.</returns>
        public static ulong GetSquaresBetween(int from, int to)
        {
            return SquaresBetween[from, to];
        }

        /// <summary>
        /// Calculates the squares a bishop can attack from a given position, considering blockers.
        /// </summary>
        /// <param name="occupiedSquares">Bitboard with all pieces on the board.</param>
        /// <param name="square">Index of the bishop's square.</param>
        /// <returns>Bitboard with squares attacked by the bishop.</returns>
        public static ulong BishopAttacks(ulong occupiedSquares, int square)
        {
            (int rank, int file) = ChessUtils.IndexToCoordinates(square);
            ulong attacks = 0UL;

            // Diagonal directions: NE, NW, SE, SW
            int[] rankDelta = { 1, 1, -1, -1 };
            int[] fileDelta = { 1, -1, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                int r = rank + rankDelta[dir];
                int f = file + fileDelta[dir];

                while (ChessUtils.IsOnBoard(r, f))
                {
                    int sq = r * 8 + f;
                    attacks |= SetBit(sq);
                    if ((occupiedSquares & SetBit(sq)) != 0) break; // if there is a piece, stop
                    r += rankDelta[dir];
                    f += fileDelta[dir];
                }
            }

            return attacks;
        }

        /// <summary>
        /// Calculates the squares a rook can attack from a given position.
        /// </summary>
        /// <param name="occupiedSquares">Bitboard with all pieces on the board.</param>
        /// <param name="square">Index of the rook's square.</param>
        /// <returns>Bitboard with squares attacked by the rook.</returns>
        public static ulong RookAttacks(ulong occupiedSquares, int square)
        {
            (int rank, int file) = ChessUtils.IndexToCoordinates(square);
            ulong attacks = 0UL;

            // Orthogonal directions: N, S, E, W
            int[] rankDelta = { 1, -1, 0, 0 };
            int[] fileDelta = { 0, 0, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                int r = rank + rankDelta[dir];
                int f = file + fileDelta[dir];

                while (ChessUtils.IsOnBoard(r, f))
                {
                    int sq = r * 8 + f;
                    attacks |= SetBit(sq);
                    if ((occupiedSquares & SetBit(sq)) != 0) break;
                    r += rankDelta[dir];
                    f += fileDelta[dir];
                }
            }

            return attacks;
        }

        /// <summary>
        /// Calculates X-ray attacks (pieces behind an obstruction) for a rook.
        /// </summary>
        /// <param name="occupiedSquares">Bitboard of all pieces.</param>
        /// <param name="blockers">Bitboard of pieces that may block.</param>
        /// <param name="rookSquare">Index of the rook.</param>
        /// <returns>Bitboard with squares attacked by X-rays.</returns>
        public static ulong XRayRookAttacks(ulong occupiedSquares, ulong blockers, int rookSquare)
        {
            ulong attacks = RookAttacks(occupiedSquares, rookSquare);
            blockers &= attacks; // Keep only blockers in the attack line
            return attacks ^ RookAttacks(occupiedSquares ^ blockers, rookSquare);
        }


        /// <summary>
        /// Calculates X-ray attacks for a bishop.
        /// </summary>
        /// <param name="occupiedSquares">Bitboard of all pieces.</param>
        /// <param name="blockers">Bitboard of blocking pieces.</param>
        /// <param name="bishopSquare">Index of the bishop's square.</param>
        /// <returns>Bitboard with squares attacked by the bishop's X-rays.</returns>
        public static ulong XRayBishopAttacks(ulong occupiedSquares, ulong blockers, int bishopSquare)
        {
            ulong attacks = BishopAttacks(occupiedSquares, bishopSquare);
            blockers &= attacks; // Only blockers on the diagonal
            return attacks ^ BishopAttacks(occupiedSquares ^ blockers, bishopSquare);
        }

        /// <summary>
        /// Returns the index of the first set bit (starting from the least significant).
        /// </summary>
        /// <param name="bitboard">Input bitboard.</param>
        /// <returns>Index of the first set bit (0-63).</returns>
        /// <remarks>
        /// Source: <see href="https://www.chessprogramming.org/BitScan">Chess Programming Wiki</see><br/>
        /// Author: Kim Walisch (2012)
        /// </remarks>
        public static int FirstActiveBit(ulong bitboard)
        {
            const ulong DeBruijn64 = 0x03f79d71b4cb0a89UL;
            if (bitboard == 0)
                return -1;
            return Index64[((bitboard ^ (bitboard - 1)) * DeBruijn64) >> 58];
        }

        /// <summary>
        /// Generates sliding attacks without filtering towards the enemy king (useful for X-rays).
        /// </summary>
        /// <param name="occupied">Bitboard with all pieces on the board.</param>
        /// <param name="rooks">Bitboard of the attacker's rooks.</param>
        /// <param name="bishops">Bitboard of the attacker's bishops.</param>
        /// <param name="queens">Bitboard of the attacker's queens.</param>
        /// <param name="enemyKing">Bitboard with only the enemy king set.</param>
        /// <returns>Bitboard with squares attacked by sliding pieces.</returns>
        public static ulong UnfilteredSlidingAttacks(ulong occupied, ulong rooks, ulong bishops, ulong queens, ulong enemyKing)
        {
            ulong attacks = 0;

            // Remove the enemy king from the occupied bitboard
            ulong occupiedWithoutKing = occupied & ~enemyKing;

            // Rooks and queens (orthogonal moves)
            for (int i = 0; i < 64; i++)
            {
                if (((rooks | queens) & (1UL << i)) != 0)
                {
                    attacks |= RookAttacks(occupiedWithoutKing, i);
                }
            }

            // Bishops and queens (diagonal moves)
            for (int i = 0; i < 64; i++)
            {
                if (((bishops | queens) & (1UL << i)) != 0)
                {
                    attacks |= BishopAttacks(occupiedWithoutKing, i);
                }
            }

            return attacks;
        }

        /// <summary>
        /// Indicates whether a number is a power of two.
        /// </summary>
        /// <param name="value">Number to evaluate.</param>
        /// <returns>True if it is a power of two, false otherwise.</returns>
        public static bool IsPowerOfTwo(ulong value)
        {
            return value != 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Checks whether all bishops are on squares of the same color.
        /// </summary>
        /// <param name="bishops">Bitboard of bishops.</param>
        /// <returns>True if they are on squares of the same color, false otherwise.</returns>
        public static bool BishopsOnSameColorSquares(ulong bishops)
        {
            const ulong lightSquares = 0x55AA55AA55AA55AA;
            const ulong darkSquares = 0xAA55AA55AA55AA55;

            return ((bishops & lightSquares) == 0) || ((bishops & darkSquares) == 0);
        }

        /// <summary>
        /// Counts the number of set bits (Hamming weight).
        /// </summary>
        /// <param name="value">Bitboard to evaluate.</param>
        /// <returns>Number of set bits.</returns>
        /// <remarks>
        /// Source: <see href="https://discussions.unity.com/t/portable-pop-count-hamming-weight/807752">Unity Discussions - Portable Pop Count / Hamming Weight</see>
        /// </remarks>
        public static int CountActiveBits(ulong value)
        {
            const ulong h01 = 0x0101010101010101;
            const ulong m1 = 0x5555555555555555; // 0101...
            const ulong m2 = 0x3333333333333333; // 00110011..
            const ulong m4 = 0x0f0f0f0f0f0f0f0f;
            value -= (value >> 1) & m1;             // put count of each 2 bits into those 2 bits
            value = (value & m2) + ((value >> 2) & m2); // put count of each 4 bits into those 4 bits
            value = (value + (value >> 4)) & m4;        // put count of each 8 bits into those 8 bits
            return (int)((value * h01) >> 56);
        }
    }
}