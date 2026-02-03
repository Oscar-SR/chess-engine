using System;
using System.Collections.Generic;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public readonly struct Move
    {
        // Flags
        public const int NO_FLAG = 0b0000;
        public const int EN_PASSANT_CAPTURE = 0b0001;
        public const int CASTLE = 0b0010;
        public const int PAWN_TWO_UP = 0b0011;
        public const int PROMOTE_TO_QUEEN = 0b0100;
        public const int PROMOTE_TO_KNIGHT = 0b0101;
        public const int PROMOTE_TO_ROOK = 0b0110;
        public const int PROMOTE_TO_BISHOP = 0b0111;

        private readonly ushort moveValue; // FFFFDDDDDDOOOOOO

        public Move(int from, int to, int flag)
        {
            this.moveValue = (ushort)(from | to << 6 | flag << 12);
        }

        public Move(ushort valor)
        {
            this.moveValue = valor;
        }

        public Move(string LAN, Tablero board)
        {
            if (string.IsNullOrEmpty(LAN) || (LAN.Length != 4 && LAN.Length != 5))
                throw new ArgumentException("Invalid LAN");

            int fromSquare = NotacionAlgebraicaACasilla(LAN.Substring(0, 2));
            int toSquare = NotacionAlgebraicaACasilla(LAN.Substring(2, 2));
            Piece.Type pieceType = board.ObtenerPieza(fromSquare).PieceType;
            int flag = NO_FLAG;

            if (pieceType == Piece.Type.Pawn)
            {
                if (LAN.Length == 5)
                {
                    // Promotion
                    flag = LAN[4] switch
                    {
                        'q' => PROMOTE_TO_QUEEN,
                        'n' => PROMOTE_TO_KNIGHT,
                        'r' => PROMOTE_TO_ROOK,
                        'b' => PROMOTE_TO_BISHOP,
                        _ => throw new ArgumentException("Invalid LAN promotion")
                    };
                }
                else if (Math.Abs(AjedrezUtils.ObtenerFila(fromSquare) - AjedrezUtils.ObtenerFila(toSquare)) == 2)
                {
                    flag = PAWN_TWO_UP;
                }
                else if ((AjedrezUtils.ObtenerColumna(fromSquare) != AjedrezUtils.ObtenerColumna(toSquare)) && (board.ObtenerPieza(toSquare).PieceType == Piece.Type.None))
                {
                    flag = EN_PASSANT_CAPTURE;
                }
            }
            else if ((pieceType == Piece.Type.King) && (Math.Abs(AjedrezUtils.ObtenerColumna(fromSquare) - AjedrezUtils.ObtenerColumna(toSquare)) > 1))
            {
                // Castling
                flag = CASTLE;
            }

            moveValue = (ushort)(fromSquare | (toSquare << 6) | (flag << 12));
        }

        public static Move Null => new Move(0);

        public int From
        {
            get
            {
                return this.moveValue & 0b0000000000111111;
            }
        }

        public int To
        {
            get
            {
                return (this.moveValue & 0b0000111111000000) >> 6;
            }
        }

        public int Flag
        {
            get
            {
                return this.moveValue >> 12;
            }
        }

        public bool IsPromotion()
        {
            return Flag == PROMOTE_TO_QUEEN || Flag == PROMOTE_TO_KNIGHT
                || Flag == PROMOTE_TO_ROOK || Flag == PROMOTE_TO_BISHOP;
        }

        public string ToLAN()
        {
            string fromLAN = CasillaANotacionAlgebraica(From);
            string toLAN = CasillaANotacionAlgebraica(To);
            string suffix = "";

            if (IsPromotion())
            {
                suffix = Flag switch
                {
                    PROMOTE_TO_QUEEN => "q",
                    PROMOTE_TO_KNIGHT => "n",
                    PROMOTE_TO_ROOK => "r",
                    PROMOTE_TO_BISHOP => "b",
                    _ => ""
                };
            }

            return fromLAN + toLAN + suffix;
        }

        public string ToSAN(Tablero board)
        {
            Piece piece = board.ObtenerPieza(From);
            string san = "";

            bool isCapture = board.ObtenerPieza(To).PieceType != Piece.Type.None || Flag == EN_PASSANT_CAPTURE;

            // Castling
            if (Flag == CASTLE)
            {
                return To > From ? "O-O" : "O-O-O";
            }

            // Piece letter (omitted for pawns)
            bool isPawn = piece.PieceType == Piece.Type.Pawn;
            if (!isPawn)
                san += piece.GetSymbol(uppercase: true);

            // Disambiguation if ambiguous (e.g. Nbd2 or R1a3)
            if (!isPawn && piece.PieceType != Piece.Type.King)
            {
                (List<Move> moves, _) = board.GenerarMovimientosLegales();
                (int fromRank, int fromFile) = AjedrezUtils.IndiceACoordenadas(From);
                bool anotherSameRank = false;
                bool anotherSameFile = false;

                foreach (Move move in moves)
                {
                    if (move.To == To && move.From != From)
                    {
                        Piece.Type otroTipoPieza = board.ObtenerPieza(move.From).PieceType;
                        if (otroTipoPieza == piece.PieceType)
                        {
                            (int otherFromRank, int otherFromFile) = AjedrezUtils.IndiceACoordenadas(move.From);

                            if (fromRank == otherFromRank)
                                anotherSameRank = true;
                            if (fromFile == otherFromFile)
                                anotherSameFile = true;
                        }
                    }
                }

                if (anotherSameRank)
                    san += AjedrezUtils.ColumnaANombre(fromFile);

                if (anotherSameFile)
                    san += AjedrezUtils.FilaANombre(fromRank);
            }

            // Capture
            if (isCapture)
            {
                if (isPawn)
                    san += AjedrezUtils.ColumnaANombre(AjedrezUtils.ObtenerColumna(From)); // File of the pawn
                san += "x";
            }

            // From square
            san += CasillaANotacionAlgebraica(To);

            // Promotion
            if (IsPromotion())
            {
                string promocion = Flag switch
                {
                    PROMOTE_TO_QUEEN => "Q",
                    PROMOTE_TO_KNIGHT => "N",
                    PROMOTE_TO_ROOK => "R",
                    PROMOTE_TO_BISHOP => "B",
                    _ => ""
                };
                san += "=" + promocion;
            }

            // Check or check mate
            board.HacerMovimiento(this, enBusqueda : true);
            (List<Move> respuestas, bool jaque) = board.GenerarMovimientosLegales();
            if (jaque)
            {
                san += respuestas.Count == 0 ? "#" : "+";
            }
            board.DeshacerMovimiento(this);

            return san;
        }

        private static int NotacionAlgebraicaACasilla(string casilla)
        {
            char columna = casilla[0];
            char fila = casilla[1];
            int x = AjedrezUtils.NombreAColumna(columna);
            int y = AjedrezUtils.NombreAFila(fila);
            return AjedrezUtils.CoordenadasAIndice(y,x);
        }

        private string CasillaANotacionAlgebraica(int indice)
        {
            (int fila, int columna) = AjedrezUtils.IndiceACoordenadas(indice);
            char columnaCaracter = AjedrezUtils.ColumnaANombre(columna);
            char filaCaracter = AjedrezUtils.FilaANombre(fila);
            return $"{columnaCaracter}{filaCaracter}";
        }

        public static bool operator ==(Move a, Move b)
        {
            return a.moveValue == b.moveValue;
        }

        public static bool operator !=(Move a, Move b)
        {
            return a.moveValue != b.moveValue;
        }

        public override bool Equals(object obj)
        {
            return obj is Move otro && this.moveValue == otro.moveValue;
        }

        public override int GetHashCode()
        {
            return moveValue.GetHashCode();
        }
    }
}
