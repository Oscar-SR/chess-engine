using System;
using Chess.Core;

namespace Chess.Utilities
{
    public class ZobristHashing
    {
        private static readonly ulong[,] PieceSquareHashes = new ulong[12, 64];
        private static readonly ulong[] CastlingRightsHashes = new ulong[16];
        private static readonly ulong[] EnPassantFileHashes = new ulong[8];
        private static ulong TurnHash;

        // Static constructor
        static ZobristHashing()
        {
            // Precomputations
            InitializeZobristHashes();
        }

        public static void Initialize()
        {
            // Empty method for lazy initialization
            // Call this method to force initialization
        }

        private static void InitializeZobristHashes()
        {
            const int seed = 123456;
            Random rng = new Random(seed);

            for (int piece = 0; piece < 12; piece++)
            {
                for (int square = 0; square < 64; square++)
                {
                    PieceSquareHashes[piece, square] = RandomUlong(rng);
                }
            }

            for (int i = 0; i < 16; i++)
                CastlingRightsHashes[i] = RandomUlong(rng);

            for (int i = 0; i < 8; i++)
                EnPassantFileHashes[i] = RandomUlong(rng);

            TurnHash = RandomUlong(rng);
        }

        private static ulong RandomUlong(Random rng)
        {
            return ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();

            /*
            byte[] buffer = new byte[8];
            rng.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
            */
        }

        public static ulong CreateZobristHash(Board board)
        {
            ulong hash = 0;

            for (int square = 0; square < 64; square++)
            {
                Piece piece = board.GetPiece(square);
                if (piece.PieceType != Piece.Type.None)
                {
                    hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(piece), square];
                }
            }

            // Castling rights
            int castlingRights = board.CurrentState.CastlingRights; // value 0-15 according to rights
            hash ^= CastlingRightsHashes[castlingRights];

            // En passant pawn
            if (board.CurrentState.HasVulnerablePawn)
            {
                int enPassantFile = ChessUtils.GetColumn(board.CurrentState.VulnerablePawnSquare);
                hash ^= EnPassantFileHashes[enPassantFile];
            }

            // Side to move
            if (ChessUtils.SameColor(board.Turn, Piece.Color.Black))
            {
                hash ^= TurnHash;
            }

            return hash;
        }

        public static ulong UpdateZobristHashSquare(ulong hash, Piece piece, int square)
        {
            return hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(piece), square];
        }

        public static ulong UpdateZobristHashCastlingRights(ulong hash, int castlingRights)
        {
            return hash ^= CastlingRightsHashes[castlingRights];
        }

        public static ulong UpdateZobristHashEnPassantFile(ulong hash, int enPassantSquare)
        {
            return hash ^= EnPassantFileHashes[ChessUtils.GetColumn(enPassantSquare)];
        }

        public static ulong UpdateZobristHashTurn(ulong hash)
        {
            return hash ^= TurnHash;
        }

        /*
        public static ulong UpdateZobristHashMove(ulong hash, Move movement, Piece piece, int? previousEnPassant)
        {
            // Remove the piece from its origin square
            hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(piece), movement.From];

            // Place the piece on its new square
            switch (movement.Flag)
            {
                case Move.PROMOTE_TO_QUEEN:
                    {
                        hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(Piece.Type.Queen, piece.PieceColor), movement.To];
                        break;
                    }

                case Move.PROMOTE_TO_ROOK:
                    {
                        hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(Piece.Type.Rook, piece.PieceColor), movement.To];
                        break;
                    }

                case Move.PROMOTE_TO_BISHOP:
                    {
                        hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(Piece.Type.Bishop, piece.PieceColor), movement.To];
                        break;
                    }

                case Move.PROMOTE_TO_KNIGHT:
                    {
                        hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(Piece.Type.Knight, piece.PieceColor), movement.To];
                        break;
                    }

                default:
                    {
                        hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(piece), movement.To];
                        break;
                    }
            }

            // Remove the previous en passant pawn, if any
            if (previousEnPassant.HasValue)
            {
                hash ^= EnPassantFileHashes[ChessUtils.GetColumn(previousEnPassant.Value)];
            }

            // Change the side to move
            hash ^= TurnHash;

            return hash;
        }

        public static ulong UpdateZobristHashCapture(ulong hash, int captureSquare, Piece capturedPiece)
        {
            // Remove the captured piece
            hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(capturedPiece), captureSquare];

            return hash;
        }

        public static ulong UpdateZobristHashCastling(ulong hash, Move rookMove, Piece.Color turn, int oldCastlingRights, int newCastlingRights)
        {
            // Update the rook move
            hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(Piece.Type.Rook, turn), rookMove.From];
            hash ^= PieceSquareHashes[ChessUtils.GetPieceIndex(Piece.Type.Rook, turn), rookMove.To];

            // Remove the old castling rights and set the new ones
            hash ^= CastlingRightsHashes[oldCastlingRights];
            hash ^= CastlingRightsHashes[newCastlingRights];

            return hash;
        }

        public static ulong UpdateZobristHashEnPassant(ulong hash, int enPassantSquare)
        {
            // Update the en passant pawn
            hash ^= EnPassantFileHashes[ChessUtils.GetColumn(enPassantSquare)];

            return hash;
        }
        */
    }
}
