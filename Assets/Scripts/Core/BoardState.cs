using System;
using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public struct BoardState
    {
        public const int WHITE_KINGSIDE_CASTLING = 0b0001;
        public const int WHITE_QUEENSIDE_CASTLING = 0b0010;
        public const int BLACK_KINGSIDE_CASTLING = 0b0100;
        public const int BLACK_QUEENSIDE_CASTLING = 0b1000;

        private ushort state; // XXXCEEEEFPPPPPPT
        private byte inactivePlyCount;
        private ulong zobristHash;

        // Bit 0 - Turn (0 white, 1 black)
        public Piece.Color Turn
        {
            get
            {
                return (this.state & 0b0000000000000001) == 0 ? Piece.Color.White : Piece.Color.Black;
            }
            set
            {
                if (AjedrezUtils.MismoColor(value, Piece.Color.Black))
                    this.state |= 0b0000000000000001;
                else
                    this.state &= 0b1111111111111110;
            }
        }

        // Bits 1–6 - Vulnerable pawn square (0–63)
        public int VulnerablePawnSquare
        {
            get
            {
                return (this.state & 0b0000000001111110) >> 1;
            }
            set
            {
                this.state = (ushort)((value << 1) | (this.state & 0b1111111110000001) | 0b10000000);
            }
        }

        // Bit 7 - Is there a vulnerable pawn?
        public bool HasVulnerablePawn
        {
            get
            {
                return (this.state & 0b0000000010000000) != 0;
            }
            set
            {
                if (value)
                    state |= 0b0000000010000000;
                else
                    state &= 0b1111111101111111;
            }
        }

        // Bits 8–11 - Available castling rights (0000 = none, 1111 = all)
        public int CastlingRights
        {
            get
            {
                return (this.state & 0b0000111100000000) >> 8;
            }
        }

        // Bit 12 - Was there a capture?
        public bool HasCapture
        {
            get
            {
                return (this.state & 0b0001000000000000) != 0;
            }
            set
            {
                if (value)
                    state |= 0b0001000000000000;
                else
                    state &= 0b1110111111111111;
            }
        }

        // Property to get and set the number of inactive plies
        public byte InactivePlyCount
        {
            get
            {
                return inactivePlyCount;
            }
            set
            {
                // Ensure the value stays within the valid range (0–50)
                inactivePlyCount = (byte)Math.Min((byte)value, (byte)Tablero.MAX_PLYS_INACTIVO);
            }
        }

        // Property to get and set the Zobrist hash
        public ulong ZobristHash
        {
            get
            {
                return zobristHash;
            }
            set
            {
                zobristHash = value;
            }
        }

        public void InitializeCastlingRights(int castlingRights)
        {
            this.state = (ushort)(this.state | castlingRights << 8);
        }

        public void CancelCastling(int castlingType)
        {
            this.state = (ushort)(this.state & ~(castlingType << 8));
        }

        public bool IsCastlingAvailable(int castlingType)
        {
            return (this.state & (castlingType << 8)) != 0;
        }

        public override string ToString()
        {
            const string RED = "#FF0000";
            const string GREEN = "#00FF00";

            string castling = "";
            if (IsCastlingAvailable(WHITE_KINGSIDE_CASTLING)) castling += "WhiteKingside ";
            if (IsCastlingAvailable(WHITE_QUEENSIDE_CASTLING)) castling += "WhiteQueenside ";
            if (IsCastlingAvailable(BLACK_KINGSIDE_CASTLING)) castling += "BlackKingside ";
            if (IsCastlingAvailable(BLACK_QUEENSIDE_CASTLING)) castling += "BlackQueenside ";
            if (string.IsNullOrEmpty(castling)) castling = "None";

            return $"State:\n" +
                $"Turn: <color={GREEN}>{Turn}</color>\n" +
                $"Capture? {(HasCapture ? $"<color={GREEN}>Yes</color>" : $"<color={RED}>No</color>")}\n" +
                $"En passant pawn? {(HasVulnerablePawn ? $"<color={GREEN}>Yes</color>" : $"<color={RED}>No</color>")}\n" +
                $"Vulnerable pawn square: {(HasVulnerablePawn ? $"<color={GREEN}>" + VulnerablePawnSquare.ToString() + "</color>" : $"<color={RED}>N/A</color>")}\n" +
                $"Inactive ply count: <color={GREEN}>{inactivePlyCount}</color>\n" +
                $"Available castling rights: <color={GREEN}>{castling}</color>\n" +
                $"Zobrist hash: <color={GREEN}>{zobristHash}</color>";
        }
    }
}