using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public static class GameStatus
    {
        public enum Type : byte
        {
            InProgress,
            WhiteCheckmate,
            BlackCheckmate,
            Stalemate,
            ThreefoldRepetition,
            FiftyMoveRule,
            InsufficientMaterial,
            WhiteTimeout,
            BlackTimeout,
            DrawByArbiter
        }

        public static Type GetGameStatus(Tablero board, int legalMoveCount, bool inCheck)
        {
            Type status = Type.InProgress;

            if (board.EstadoActual.InactivePlyCount >= Tablero.MAX_PLYS_INACTIVO)
            {
                // Fifty-move rule
                status = Type.FiftyMoveRule;
            }
            else if (IsInsufficientMaterial(board))
            {
                // Insufficient material
                status = Type.InsufficientMaterial;
            }
            else if (board.HistorialPosicionesRepetidas.IsThreefoldRepetition(board.EstadoActual.ZobristHash))
            {
                // The last move caused a threefold repetition
                status = Type.ThreefoldRepetition;
            }
            else if (legalMoveCount == 0)
            {
                if (inCheck)
                {
                    // Checkmate
                    status = board.Turno == Piece.Color.White ? Type.WhiteCheckmate : Type.BlackCheckmate;

                }
                else
                {
                    // Stalemate
                    status = Type.Stalemate;
                }
            }

            return status;
        }

        public static bool IsInsufficientMaterial(Tablero board)
        {
            ulong piecesWithoutKings = board.Bitboards[1] | board.Bitboards[2] | board.Bitboards[3] | board.Bitboards[4] | board.Bitboards[5] | board.Bitboards[7] | board.Bitboards[8] | board.Bitboards[9] | board.Bitboards[10] | board.Bitboards[11];

            // Only kings remain
            if (piecesWithoutKings == 0)
                return true;
            else if (BitboardUtils.EsPotenciaDe2(piecesWithoutKings))
            {
                // King and knight versus king
                if (BitboardUtils.EsPotenciaDe2(board.Bitboards[4] | board.Bitboards[10]))
                    return true;

                // King and bishop versus king
                if (BitboardUtils.EsPotenciaDe2(board.Bitboards[3] | board.Bitboards[9]))
                    return true;
            }
            else if ((board.Bitboards[1] | board.Bitboards[2] | board.Bitboards[4] | board.Bitboards[5] | board.Bitboards[7] | board.Bitboards[8] | board.Bitboards[10] | board.Bitboards[11]) == 0)
            {
                // Only kings and bishops remain (if bishops control squares of the same color)
                if ((board.Bitboards[3] | board.Bitboards[9]) != 0 && BitboardUtils.AlfilesEnCasillasMismoColor(board.Bitboards[3] | board.Bitboards[9]))
                    return true;
            }

            return false;
        }

        public static string GetDescription(this Type status)
        {
            return status switch
            {
                Type.InProgress => "Game in progress",
                Type.WhiteCheckmate => "White is checkmated",
                Type.BlackCheckmate => "Black is checkmated",
                Type.Stalemate => "Stalemate",
                Type.ThreefoldRepetition => "Threefold repetition",
                Type.FiftyMoveRule => "Fifty-move rule",
                Type.InsufficientMaterial => "Insufficient material",
                Type.WhiteTimeout => "White ran out of time",
                Type.BlackTimeout => "Black ran out of time",
                Type.DrawByArbiter => "Draw by arbiter",
                _ => "Unknown status"
            };
        }

        public static Piece.Color GetWinner(Type status)
        {
            switch (status)
            {
                case Type.WhiteCheckmate:
                case Type.WhiteTimeout:
                    return Piece.Color.Black;

                case Type.BlackCheckmate:
                case Type.BlackTimeout:
                    return Piece.Color.White;

                default:
                    return Piece.Color.None;
            }
        }
    }
}