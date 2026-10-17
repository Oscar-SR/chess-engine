using static System.Math;
using Ajedrez.Core;

namespace Ajedrez.Utilities
{
    public static class ChessUtils
    {
        // GLOBAL CONSTANTS
        public const int BOARD_SIZE = 8;
        public const int BOARD_SQUARE_COUNT = BOARD_SIZE * BOARD_SIZE;
        public const int PIECE_COUNT = 32;
        public const int PIECES_PER_COLOR = PIECE_COUNT / 2;
        public const int NORTH = 0;
        public const int SOUTH = 1;
        public const int WEST = 2;
        public const int EAST = 3;
        public const int NORTHWEST = 4;
        public const int SOUTHEAST = 5;
        public const int NORTHEAST = 6;
        public const int SOUTHWEST = 7;
        public const int WHITE_QUEENSIDE_CASTLE_SQUARE = 2;
        public const int WHITE_KINGSIDE_CASTLE_SQUARE = 6;
        public const int BLACK_QUEENSIDE_CASTLE_SQUARE = 58;
        public const int BLACK_KINGSIDE_CASTLE_SQUARE = 62;

        // FIELDS
        public static readonly int[] Directions = { 8, -8, -1, 1, 7, -7, 9, -9 };
        public static readonly int[] KnightJumps = { 17, 15, 10, 6, -17, -15, -10, -6 };
        public static readonly int[][] SquaresToBorder = new int[BOARD_SQUARE_COUNT][];
		public static readonly int[,] ManhattanDistance = new int[BOARD_SQUARE_COUNT, BOARD_SQUARE_COUNT];
        public static readonly int[] ManhattanDistanceToCenter = new int[BOARD_SQUARE_COUNT];

        // Static constructor
        static ChessUtils()
        {
            // Precalculations
            PrecomputeSquaresToBorder();
            PrecomputeManhattanDistance();
            PrecomputeManhattanDistanceToCenter();
        }

        public static void Initialize()
        {
            // Empty method for lazy initialization
            // Call this method to force initialization
        }

        // HELPER FUNCTIONS

        /// <summary>
        /// Converts a square index (0-63) to its row coordinate
        /// </summary>
        /// <param name="index">Square index (0 to 63)</param>
        /// <returns>Row for the corresponding square</returns>
        public static int GetRow(int index)
        {
            return index / BOARD_SIZE;
        }

        /// <summary>
        /// Converts a square index (0-63) to its column coordinate
        /// </summary>
        /// <param name="index">Square index (0 to 63)</param>
        /// <returns>Column for the corresponding square</returns>
        public static int GetColumn(int index)
        {
            return index % BOARD_SIZE;
        }

        /// <summary>
        /// Converts a square index (0-63) to board row and column coordinates
        /// </summary>
        /// <param name="index">Square index (0 to 63)</param>
        /// <returns>A tuple (row, column) for the square</returns>
        public static (int row, int column) IndexToCoordinates(int index)
        {
            return (GetRow(index), GetColumn(index));
        }

        /// <summary>
        /// Converts board row and column coordinates to a square index (0-63)
        /// </summary>
        /// <param name="row">Board row of a square (0 to 7)</param>
        /// <param name="column">Board column of a square (0 to 7)</param>
        /// <returns>Square index for the coordinates</returns>
        public static int CoordinatesToIndex(int row, int column)
        {
            return row * BOARD_SIZE + column;
        }

        public static char RowToChar(int row)
        {
            return (char)('1' + row);
        }

        public static char ColumnToChar(int column)
        {
            return (char)('a' + column);
        }

        public static int NameToRow(char row)
        {
            return row - '1';
        }

        public static int NameToColumn(char column)
        {
            return char.ToLower(column) - 'a';
        }

        /// <summary>
        /// Returns true if the coordinates are inside the board
        /// </summary>
        /// <param name="row">Board row of a square (0 to 7)</param>
        /// <param name="column">Board column of a square (0 to 7)</param>
        /// <returns>True if the coordinates are inside the board, false otherwise</returns>
        public static bool IsOnBoard(int row, int column)
        {
            return row >= 0 && row < BOARD_SIZE && column >= 0 && column < BOARD_SIZE;
        }

        /// <summary>
        /// Returns true if the square is dark based on its row and column coordinates
        /// </summary>
        /// <param name="row">Board row of a square (0 to 7)</param>
        /// <param name="column">Board column of a square (0 to 7)</param>
        /// <returns>True if the corresponding square is dark, false if it is light</returns>
        public static bool IsDarkSquare(int row, int column)
        {
            return (row + column) % 2 == 0;
        }

        /// <summary>
        /// Returns true if the square is dark based on its index
        /// </summary>
        /// <param name="index">Square index (0 to 63)</param>
        /// <returns>True if the corresponding square is dark, false if it is light</returns>
        public static bool IsDarkSquare(int index)
        {
            (int row, int column) = IndexToCoordinates(index);
            return (row + column) % 2 == 0;
        }

        /// <summary>
        /// Returns the opposite color
        /// </summary>
        /// <param name="color">Color of one of the chess sides (white or black)</param>
        /// <returns>Color opposite to the input color</returns>
        public static Piece.Color InverseColor(Piece.Color color)
        {
            return color == Piece.Color.White ? Piece.Color.Black : Piece.Color.White;
        }

        /// <summary>
        /// Returns whether both colors are the same
        /// </summary>
        /// <param name="color1">Color of one of the chess sides (white or black)</param>
        /// <param name="color2">Color of one of the chess sides (white or black)</param>
        /// <returns>True if both colors are the same, false otherwise</returns>
        public static bool SameColor(Piece.Color color1, Piece.Color color2)
        {
            return color1 == color2;
        }

        /// <summary>
        /// Returns whether a knight jump between origin and destination squares is valid
        /// </summary>
        /// <param name="origin">Origin square index of the move</param>
        /// <param name="destination">Destination square index of the move</param>
        /// <returns>True if the jump is valid, false otherwise</returns>
        public static bool IsValidKnightJump(int origin, int destination)
        {
            (int originRow, int originColumn) = IndexToCoordinates(origin);
            (int destinationRow, int destinationColumn) = IndexToCoordinates(destination);

            // Validate that the jump is not malformed (e.g., crossing from column 1 to 6)
            int rowDiff = Abs(destinationRow - originRow);
            int columnDiff = Abs(destinationColumn - originColumn);

            return ((rowDiff == 2 && columnDiff == 1) || (rowDiff == 1 && columnDiff == 2));
        }

        /// <summary>
        /// Calculates the number of squares to the border for every square on the board
        /// </summary>
        private static void PrecomputeSquaresToBorder()
        {
            for (int row = 0; row < BOARD_SIZE; row++)
            {
                for (int column = 0; column < BOARD_SIZE; column++)
                {
                    int numNorth = BOARD_SIZE - 1 - row;
                    int numSouth = row;
                    int numWest = column;
                    int numEast = BOARD_SIZE - 1 - column;

                    // Initialize the array for each square
                    SquaresToBorder[CoordinatesToIndex(row, column)] = new int[]
                    {
                        numNorth,                   // up
                        numSouth,                    // down
                        numWest,                     // left
                        numEast,                     // right
                        Min(numNorth, numWest),     // up-left
                        Min(numSouth, numEast),     // down-right
                        Min(numNorth, numEast),     // up-right
                        Min(numSouth, numWest)      // down-left
                    };
                }
            }
        }

        private static void PrecomputeManhattanDistance()
        {
            for (int i = 0; i < BOARD_SQUARE_COUNT; i++)
            {
                (int row1, int column1) = IndexToCoordinates(i);

                for (int j = 0; j < BOARD_SQUARE_COUNT; j++)
                {
                    (int row2, int column2) = IndexToCoordinates(j);

                    int distance = Abs(row1 - row2) + Abs(column1 - column2);
                    ManhattanDistance[i, j] = distance;
                }
            }
        }

        private static void PrecomputeManhattanDistanceToCenter()
        {
            for (int index = 0; index < BOARD_SQUARE_COUNT; index++)
            {
                (int row, int column) = IndexToCoordinates(index);

                // Board center (3.5, 3.5) → between (3,3) and (4,4)
                int distanceToCenterRow = Max(3 - row, row - 4);
                int distanceToCenterColumn = Max(3 - column, column - 4);
                ManhattanDistanceToCenter[index] = distanceToCenterRow + distanceToCenterColumn;
            }
        }

        /// <summary>
        /// Determines whether the piece type is sliding
        /// </summary>
        /// <returns>True if the piece type is sliding, false otherwise</returns>
        public static bool IsSliding(this Piece.Type type)
        {
            return type == Piece.Type.Queen || type == Piece.Type.Rook || type == Piece.Type.Bishop;
        }

        public static int GetPieceIndex(Piece.Type pieceType, Piece.Color color)
        {
            return (int) pieceType + (int) color;
        }

        public static int GetPieceIndex(Piece piece)
        {
            return GetPieceIndex(piece.PieceType, piece.PieceColor);
        }
    }
}