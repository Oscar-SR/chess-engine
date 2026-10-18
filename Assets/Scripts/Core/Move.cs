using System;
using System.Collections.Generic;
using Chess.Utilities;

namespace Chess.Core
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

        public Move(ushort value)
        {
            this.moveValue = value;
        }

        public Move(string LAN, Board board)
        {
            if (string.IsNullOrEmpty(LAN) || (LAN.Length != 4 && LAN.Length != 5))
                throw new ArgumentException("Invalid LAN");

            int fromSquare = AlgebraicNotationToSquare(LAN.Substring(0, 2));
            int toSquare = AlgebraicNotationToSquare(LAN.Substring(2, 2));
            Piece.Type pieceType = board.GetPiece(fromSquare).PieceType;
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
                else if (Math.Abs(ChessUtils.GetRow(fromSquare) - ChessUtils.GetRow(toSquare)) == 2)
                {
                    flag = PAWN_TWO_UP;
                }
                else if ((ChessUtils.GetColumn(fromSquare) != ChessUtils.GetColumn(toSquare)) && (board.GetPiece(toSquare).PieceType == Piece.Type.None))
                {
                    flag = EN_PASSANT_CAPTURE;
                }
            }
            else if ((pieceType == Piece.Type.King) && (Math.Abs(ChessUtils.GetColumn(fromSquare) - ChessUtils.GetColumn(toSquare)) > 1))
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
            string fromLAN = SquareToAlgebraicNotation(From);
            string toLAN = SquareToAlgebraicNotation(To);
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

        public string ToSAN(Board board)
        {
            Piece piece = board.GetPiece(From);
            string san = "";

            bool isCapture = board.GetPiece(To).PieceType != Piece.Type.None || Flag == EN_PASSANT_CAPTURE;

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
                (List<Move> moves, _) = board.GenerateLegalMoves();
                (int fromRank, int fromFile) = ChessUtils.IndexToCoordinates(From);
                bool anotherSameRank = false;
                bool anotherSameFile = false;

                foreach (Move move in moves)
                {
                    if (move.To == To && move.From != From)
                    {
                        Piece.Type otherPieceType = board.GetPiece(move.From).PieceType;
                        if (otherPieceType == piece.PieceType)
                        {
                            (int otherFromRank, int otherFromFile) = ChessUtils.IndexToCoordinates(move.From);

                            if (fromRank == otherFromRank)
                                anotherSameRank = true;
                            if (fromFile == otherFromFile)
                                anotherSameFile = true;
                        }
                    }
                }

                if (anotherSameRank)
                    san += ChessUtils.ColumnToChar(fromFile);

                if (anotherSameFile)
                    san += ChessUtils.RowToChar(fromRank);
            }

            // Capture
            if (isCapture)
            {
                if (isPawn)
                    san += ChessUtils.ColumnToChar(ChessUtils.GetColumn(From)); // File of the pawn
                san += "x";
            }

            // From square
            san += SquareToAlgebraicNotation(To);

            // Promotion
            if (IsPromotion())
            {
                string promotion = Flag switch
                {
                    PROMOTE_TO_QUEEN => "Q",
                    PROMOTE_TO_KNIGHT => "N",
                    PROMOTE_TO_ROOK => "R",
                    PROMOTE_TO_BISHOP => "B",
                    _ => ""
                };
                san += "=" + promotion;
            }

            // Check or check mate
            board.MakeMove(this, inSearch : true);
            (List<Move> responses, bool inCheck) = board.GenerateLegalMoves();
            if (inCheck)
            {
                san += responses.Count == 0 ? "#" : "+";
            }
            board.UndoMove(this);

            return san;
        }

        private static int AlgebraicNotationToSquare(string square)
        {
            char column = square[0];
            char row = square[1];
            int x = ChessUtils.NameToColumn(column);
            int y = ChessUtils.NameToRow(row);
            return ChessUtils.CoordinatesToIndex(y,x);
        }

        private string SquareToAlgebraicNotation(int index)
        {
            (int row, int column) = ChessUtils.IndexToCoordinates(index);
            char columnChar = ChessUtils.ColumnToChar(column);
            char rowChar = ChessUtils.RowToChar(row);
            return $"{columnChar}{rowChar}";
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
            return obj is Move other && this.moveValue == other.moveValue;
        }

        public override int GetHashCode()
        {
            return moveValue.GetHashCode();
        }
    }
}
