using Chess.Utilities;
using Chess.Core;

namespace Chess.AI
{
    public static class PieceMaps
    {
        public static readonly int[] Pawns = {
             0,   0,   0,   0,   0,   0,   0,   0,
            50,  50,  50,  50,  50,  50,  50,  50,
            10,  10,  20,  30,  30,  20,  10,  10,
             5,   5,  10,  25,  25,  10,   5,   5,
             0,   0,   0,  20,  20,   0,   0,   0,
             5,  -5, -10,   0,   0, -10,  -5,   5,
             5,  10,  10, -20, -20,  10,  10,   5,
             0,   0,   0,   0,   0,   0,   0,   0
        };

        public static readonly int[] PawnsEndgame = {
             0,   0,   0,   0,   0,   0,   0,   0,
            80,  80,  80,  80,  80,  80,  80,  80,
            50,  50,  50,  50,  50,  50,  50,  50,
            30,  30,  30,  30,  30,  30,  30,  30,
            20,  20,  20,  20,  20,  20,  20,  20,
            10,  10,  10,  10,  10,  10,  10,  10,
            10,  10,  10,  10,  10,  10,  10,  10,
             0,   0,   0,   0,   0,   0,   0,   0
        };

        public static readonly int[] Rooks = {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };

        public static readonly int[] Knights = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50,
        };

        public static readonly int[] Bishops = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20,
        };
        public static readonly int[] Queens = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        public static readonly int[] King =
        {
            -80, -70, -70, -70, -70, -70, -70, -80,
            -60, -60, -60, -60, -60, -60, -60, -60,
            -40, -50, -50, -60, -60, -50, -50, -40,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
             20,  20,  -5,  -5,  -5,  -5,  20,  20,
             20,  30,  10,   0,   0,  10,  30,  20
        };

        public static readonly int[] KingEndgame =
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
             -5,   0,   5,   5,   5,   5,   0,  -5,
            -10,  -5,  20,  30,  30,  20,  -5, -10,
            -15, -10,  35,  45,  45,  35, -10, -15,
            -20, -15,  30,  40,  40,  30, -15, -20,
            -25, -20,  20,  25,  25,  20, -20, -25,
            -30, -25,   0,   0,   0,   0, -25, -30,
            -50, -30, -30, -30, -30, -30, -30, -50
        };

        public static readonly int[] PawnsFlipped;
        public static readonly int[] PawnsEndgameFlipped;
        public static readonly int[] RooksFlipped;
        public static readonly int[] KnightsFlipped;
        public static readonly int[] BishopsFlipped;
        public static readonly int[] QueensFlipped;
        public static readonly int[] KingFlipped;
        public static readonly int[] KingEndgameFlipped;

        //public static readonly int[][] Maps;

        static PieceMaps()
        {
            /*
            Maps = new int[Piece.NUM_PIECE_TYPES][];

            // White pieces
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Pawn, Piece.Color.White)] = Pawns;
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Rook, Piece.Color.White)] = Rooks;
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Knight, Piece.Color.White)] = Knights;
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Bishop, Piece.Color.White)] = Bishops;
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Queen, Piece.Color.White)] = Queens;
            Maps[ChessUtils.GetPieceIndex(Piece.Type.King, Piece.Color.White)] = King;

            // Black pieces
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Pawn, Piece.Color.Black)] = FlipMap(Pawns);
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Rook, Piece.Color.Black)] = FlipMap(Rooks);
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Knight, Piece.Color.Black)] = FlipMap(Knights);
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Bishop, Piece.Color.Black)] = FlipMap(Bishops);
            Maps[ChessUtils.GetPieceIndex(Piece.Type.Queen, Piece.Color.Black)] = FlipMap(Queens);
            Maps[ChessUtils.GetPieceIndex(Piece.Type.King, Piece.Color.Black)] = FlipMap(King);
            */
        }

        private static int[] FlipMap(int[] map)
        {
            int[] flippedMap = new int[ChessUtils.BOARD_SQUARE_COUNT];

            for (int i = 0; i < ChessUtils.BOARD_SQUARE_COUNT; i++)
            {
                (int row, int column) = ChessUtils.IndexToCoordinates(i);
                int index = ChessUtils.CoordinatesToIndex(7 - row, column);
                flippedMap[index] = map[i];
            }
            return flippedMap;
        }

        public static int Read(int[] map, int square, Piece.Color color)
        {
            if (ChessUtils.SameColor(color, Piece.Color.White))
            {
                (int row, int column) = ChessUtils.IndexToCoordinates(square);
                row = 7 - row;
                square = ChessUtils.CoordinatesToIndex(row, column);
            }

            return map[square];
        }

        public static int CalculatePawnInterpolation(int square, float interpolationValue)
        {
            return (int)((1 - interpolationValue) * Pawns[square] + interpolationValue * PawnsEndgame[square]);
        }

        public static int CalculateKingInterpolation(int square, float interpolationValue)
        {
            return (int)((1 - interpolationValue) * King[square] + interpolationValue * KingEndgame[square]);
        }

        public static int CalculateInterpolation(int initialValue, int finalValue, float interpolationValue)
        {
            return (int)((1 - interpolationValue) * initialValue + finalValue * interpolationValue);
        }
    }
}
