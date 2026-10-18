using static System.Math;
using Chess.Core;
using Chess.Utilities;

namespace Chess.AI
{
    public class Evaluation
    {
        // Material value
        private const int PAWN_VALUE = 100;
        private const int KNIGHT_VALUE = 300;
        private const int BISHOP_VALUE = 300;
        private const int ROOK_VALUE = 500;
        private const int QUEEN_VALUE = 900;

        // Material value in the endgame
        const int QUEEN_VALUE_ENDGAME = 45;
        const int ROOK_VALUE_ENDGAME = 20;
        const int BISHOP_VALUE_ENDGAME = 10;
        const int KNIGHT_VALUE_ENDGAME = 10;

        private const float ENDGAME_THRESHOLD = 2 * ROOK_VALUE_ENDGAME + 2 * BISHOP_VALUE_ENDGAME + 2 * KNIGHT_VALUE_ENDGAME + QUEEN_VALUE_ENDGAME;
        private const int MAX_MANHATTAN_DISTANCE = 14;

        private Board board;

        public Evaluation(Board board)
        {
            this.board = board;
        }

        public struct EvaluationData
        {
            private int materialValue;
            private int kingPositionValue;
            private int pieceMapsValue;

            public int MaterialValue
            {
                get => materialValue;
                set => materialValue = value;
            }

            public int KingPositionValue
            {
                get => kingPositionValue;
                set => kingPositionValue = value;
            }

            public int PieceMapsValue
            {
                get => pieceMapsValue;
                set => pieceMapsValue = value;
            }

            public void CalculateMaterialValue(Material material)
            {
                materialValue = 0;
                materialValue += material.NumPawns * PAWN_VALUE;
                materialValue += material.NumKnights * KNIGHT_VALUE;
                materialValue += material.NumBishops * BISHOP_VALUE;
                materialValue += material.NumRooks * ROOK_VALUE;
                materialValue += material.NumQueens * QUEEN_VALUE;
            }

            public int Total
            {
                get
                {
                    return materialValue + kingPositionValue + pieceMapsValue;
                }
            }
        }

        public readonly struct Material
        {
            private readonly int numPawns;
            private readonly int numKnights;
            private readonly int numBishops;
            private readonly int numRooks;
            private readonly int numQueens;

            public Material(int numPawns, int numKnights, int numBishops, int numRooks, int numQueens)
            {
                this.numPawns = numPawns;
                this.numKnights = numKnights;
                this.numBishops = numBishops;
                this.numRooks = numRooks;
                this.numQueens = numQueens;
            }

            public int NumPawns => numPawns;
            public int NumKnights => numKnights;
            public int NumBishops => numBishops;
            public int NumRooks => numRooks;
            public int NumQueens => numQueens;
        }

        public static int GetPieceValue(Piece.Type type)
        {
            return type switch
            {
                Piece.Type.Pawn => PAWN_VALUE,
                Piece.Type.Knight => KNIGHT_VALUE,
                Piece.Type.Bishop => BISHOP_VALUE,
                Piece.Type.Rook => ROOK_VALUE,
                Piece.Type.Queen => QUEEN_VALUE,
                Piece.Type.King => Search.INFINITY,
                _ => 0
            };
        }

        public static int GetPromotionValue(int promotionFlag)
        {
            return promotionFlag switch
            {
                Move.PROMOTE_TO_KNIGHT => KNIGHT_VALUE,
                Move.PROMOTE_TO_BISHOP => BISHOP_VALUE,
                Move.PROMOTE_TO_ROOK => ROOK_VALUE,
                Move.PROMOTE_TO_QUEEN => QUEEN_VALUE,
                _ => 0
            };
        }

        public int Evaluate()
        {
            // Count the material of each player
            (Material whiteMaterial, Material blackMaterial) = CountMaterial();

            // Calculate the material value of each player
            EvaluationData whiteEvaluationData = new EvaluationData();
            EvaluationData blackEvaluationData = new EvaluationData();
            whiteEvaluationData.CalculateMaterialValue(whiteMaterial);
            blackEvaluationData.CalculateMaterialValue(blackMaterial);

            // Calculate the endgame value based on the remaining material (0 is an early endgame and 1 is a late endgame)
            float whiteEndgameValue = CalculateEndgameValue(whiteMaterial);
            float blackEndgameValue = CalculateEndgameValue(blackMaterial);

            // Calculate the king position value
            whiteEvaluationData.KingPositionValue = ForceKingMoveEndgame(Piece.Color.White, whiteEvaluationData.MaterialValue, blackEvaluationData.MaterialValue, blackEndgameValue);
            blackEvaluationData.KingPositionValue = ForceKingMoveEndgame(Piece.Color.Black, blackEvaluationData.MaterialValue, whiteEvaluationData.MaterialValue, whiteEndgameValue);

            // Calculate the piece maps value
            whiteEvaluationData.PieceMapsValue = EvaluatePieceMaps(Piece.Color.White, blackEndgameValue);
            blackEvaluationData.PieceMapsValue = EvaluatePieceMaps(Piece.Color.Black, whiteEndgameValue);

            // Calculate the total evaluation applying the perspective
            int evaluation = (whiteEvaluationData.Total - blackEvaluationData.Total) * (ChessUtils.SameColor(board.Turn, Piece.Color.White) ? 1 : -1);

            return evaluation;
        }

        private (Material whiteMaterial, Material blackMaterial) CountMaterial()
        {
            int numWhitePawns = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, Piece.Color.White)]);
            int numWhiteKnights = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Knight, Piece.Color.White)]);
            int numWhiteBishops = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, Piece.Color.White)]);
            int numWhiteRooks = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, Piece.Color.White)]);
            int numWhiteQueens = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, Piece.Color.White)]);

            int numBlackPawns = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, Piece.Color.Black)]);
            int numBlackKnights = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Knight, Piece.Color.Black)]);
            int numBlackBishops = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, Piece.Color.Black)]);
            int numBlackRooks = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, Piece.Color.Black)]);
            int numBlackQueens = BitboardUtils.CountActiveBits(board.Bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, Piece.Color.Black)]);

            Material whiteMaterial = new Material(numWhitePawns, numWhiteKnights, numWhiteBishops, numWhiteRooks, numWhiteQueens);
            Material blackMaterial = new Material(numBlackPawns, numBlackKnights, numBlackBishops, numBlackRooks, numBlackQueens);
            return (whiteMaterial, blackMaterial);
        }

        /// <summary>
        /// Evaluates the position of the allied king and the enemy king in the "endgame" to favor moves that
        /// optimize the placement of the allied king, encouraging control positions and checkmate.
        /// </summary>
        /// <param name="color">Color of the player whose king move is being evaluated.</param>
        /// <param name="allyMaterialValue">Material value of the allied player.</param>
        /// <param name="enemyMaterialValue">Material value of the enemy player.</param>
        /// <param name="enemyEndgameValue">"Endgame" parameter value of the enemy player.</param>
        /// <returns>Integer value representing the weighted evaluation of the king position in the "endgame".</returns>
        /// <remarks>
        /// Implementation based on the "mop-up" evaluation for endgames, where control positions and
        /// strategic proximity between the kings are rewarded.
        /// Source: <see href="https://www.chessprogramming.org/Mop-up_Evaluation">Chess Programming - Mop-up Evaluation</see>
        /// </remarks>
        private int ForceKingMoveEndgame(Piece.Color color, int allyMaterialValue, int enemyMaterialValue, float enemyEndgameValue)
        {
            if ((allyMaterialValue > (enemyMaterialValue + PAWN_VALUE * 2)) && enemyEndgameValue > 0)
            {
                int evaluation = 0;
                int allyKingSquare = board.GetKingSquare(color);
                int enemyKingSquare = board.GetKingSquare(ChessUtils.InverseColor(color));

                // Encourage the allied king to approach the opponent's king
                evaluation += (MAX_MANHATTAN_DISTANCE - ChessUtils.ManhattanDistance[allyKingSquare, enemyKingSquare]) * 4;
                // Encourage pushing the opponent's king towards the edge of the board
                evaluation += ChessUtils.ManhattanDistanceToCenter[enemyKingSquare] * 10;

                return (int)(evaluation * enemyEndgameValue);
            }

            return 0;
        }

        private float CalculateEndgameValue(Material material)
        {
            float totalEndgameMaterial = QUEEN_VALUE_ENDGAME * material.NumQueens + ROOK_VALUE_ENDGAME * material.NumRooks + BISHOP_VALUE_ENDGAME * material.NumBishops + KNIGHT_VALUE_ENDGAME * material.NumKnights;
            float endgameValue = 1 - Min(1, totalEndgameMaterial / (float)ENDGAME_THRESHOLD);
            return endgameValue;
        }

        private int EvaluatePieceMaps(Piece.Color color, float endgameValue)
        {
            int evaluation = 0;

            int kingEvaluation = EvaluatePieceMap(PieceMaps.King, Piece.Type.King, color);
            int kingEndgameEvaluation = EvaluatePieceMap(PieceMaps.KingEndgame, Piece.Type.King, color);
            evaluation += PieceMaps.CalculateInterpolation(kingEvaluation, kingEndgameEvaluation, endgameValue);

            int pawnsEvaluation = EvaluatePieceMap(PieceMaps.Pawns, Piece.Type.Pawn, color);
            int pawnsEndgameEvaluation = EvaluatePieceMap(PieceMaps.PawnsEndgame, Piece.Type.Pawn, color);
            evaluation += PieceMaps.CalculateInterpolation(pawnsEvaluation, pawnsEndgameEvaluation, endgameValue);

            //evaluation += EvaluatePieceMap(PieceMaps.King, Piece.Type.King, color);
            //evaluation += EvaluatePieceMap(PieceMaps.Pawns, Piece.Type.Pawn, color);
            evaluation += EvaluatePieceMap(PieceMaps.Queens, Piece.Type.Queen, color);
            evaluation += EvaluatePieceMap(PieceMaps.Rooks, Piece.Type.Rook, color);
            evaluation += EvaluatePieceMap(PieceMaps.Bishops, Piece.Type.Bishop, color);
            evaluation += EvaluatePieceMap(PieceMaps.Knights, Piece.Type.Knight, color);

            return evaluation;
        }

        private int EvaluatePieceMap(int[] map, Piece.Type pieceType, Piece.Color pieceColor)
        {
            int evaluation = 0;
            ulong pieces = board.Bitboards[ChessUtils.GetPieceIndex(pieceType, pieceColor)];

            while (pieces != 0)
            {
                int square = BitboardUtils.FirstActiveBit(pieces);
                pieces &= pieces - 1;
                evaluation += PieceMaps.Read(map, square, pieceColor);
            }

            return evaluation;
        }
    }
}