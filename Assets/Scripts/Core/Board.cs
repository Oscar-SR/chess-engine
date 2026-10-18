using System;
using System.Collections.Generic;
using Chess.Utilities;

namespace Chess.Core
{
    public class Board
    {
        public const string INITIAL_POSITION_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public const int MAX_INACTIVE_PLYS = 50;

        private ulong[] bitboards;
        private Stack<(Piece, int)> deadPieces;
        private BoardState currentState;
        private Stack<BoardState> stateHistory;
        private RepetitionStack repetitionHistory; // Keeps a history of the last hashed positions, up to the last irreversible move
        private uint totalMoveCount;

        public Board()
        {
            LoadFEN(INITIAL_POSITION_FEN);
        }

        public Board(string fen)
        {
            LoadFEN(fen);
        }

        public string ToFEN(bool includeEnPassant = true)
        {
            string fen = "";

            // 1 Generate the board part (ranks 8 to 1)
            for (int rank = 7; rank >= 0; rank--)
            {
                int emptySquares = 0;

                for (int file = 0; file < ChessUtils.BOARD_SIZE; file++)
                {
                    int index = ChessUtils.CoordinatesToIndex(rank, file);
                    Piece piece = GetPiece(index);

                    if (piece.PieceType == Piece.Type.None)
                    {
                        emptySquares++;
                    }
                    else
                    {
                        if (emptySquares > 0)
                        {
                            fen += emptySquares.ToString();
                            emptySquares = 0;
                        }

                        fen += piece.GetSymbol();
                    }
                }

                if (emptySquares > 0)
                {
                    fen += emptySquares.ToString();
                }

                if (rank > 0)
                {
                    fen += "/";
                }
            }

            // 2 Turn
            fen += currentState.Turn == Piece.Color.White ? " w " : " b ";

            // 3 Castling rights
            string castling = "";
            if (currentState.IsCastlingAvailable(BoardState.WHITE_KINGSIDE_CASTLING)) castling += "K";
            if (currentState.IsCastlingAvailable(BoardState.WHITE_QUEENSIDE_CASTLING)) castling += "Q";
            if (currentState.IsCastlingAvailable(BoardState.BLACK_KINGSIDE_CASTLING)) castling += "k";
            if (currentState.IsCastlingAvailable(BoardState.BLACK_QUEENSIDE_CASTLING)) castling += "q";
            fen += string.IsNullOrEmpty(castling) ? "-" : castling;
            fen += " ";

            // 4 En passant
            if (includeEnPassant && currentState.HasVulnerablePawn)
            {
                (int rank, int file) = ChessUtils.IndexToCoordinates(currentState.VulnerablePawnSquare);
                char fileChar = ChessUtils.ColumnToChar(file);
                if (rank == 3)
                {
                    rank--;
                }
                else
                {
                    rank++;
                }
                char rankChar = ChessUtils.RowToChar(rank);
                fen += $"{fileChar}{rankChar}";
            }
            else
            {
                fen += "-";
            }

            // 5 Inactive ply count
            fen += $" {currentState.InactivePlyCount}";

            // 6 Total move count
            fen += $" {totalMoveCount}";

            return fen;
        }

        public void LoadFEN(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
                throw new ArgumentException("The FEN string is empty or null.", nameof(fen));

            string[] parts = fen.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts.Length > 6)
                throw new ArgumentException($"The FEN string must have between 4 and 6 space-separated parts. It has {parts.Length}.", nameof(fen));

            // Initialize data structures
            bitboards = new ulong[12];
            deadPieces = new Stack<(Piece, int)>();
            stateHistory = new Stack<BoardState>(capacity: 64);
            repetitionHistory = new RepetitionStack();

            string piecesFEN = parts[0];
            string turnFEN = parts[1];
            string castlingFEN = parts[2];
            string enPassantFEN = parts[3];
            string plysFEN = parts.Length > 4 ? parts[4] : "0";
            string movesFEN = parts.Length > 5 ? parts[5] : "1";

            // 1. Pieces on the board
            string[] ranks = piecesFEN.Split('/');
            if (ranks.Length != 8)
                throw new ArgumentException("The pieces section must contain 8 rows separated by '/'.", nameof(fen));

            for (int rank = 0; rank < 8; rank++)
            {
                int file = 0;
                foreach (char c in ranks[rank])
                {
                    if (char.IsDigit(c))
                    {
                        file += c - '0';
                    }
                    else if ("pnbrqkPNBRQK".IndexOf(c) >= 0)
                    {
                        Piece piece = new Piece(c);
                        int index = (7 - rank) * 8 + file;

                        bitboards[ChessUtils.GetPieceIndex(piece)] |= BitboardUtils.SetBit(index);
                        file++;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid character in piece placement: '{c}' in rank {rank + 1}.", nameof(fen));
                    }
                }

                if (file != 8)
                    throw new ArgumentException($"Row {rank + 1} of the board does not have exactly 8 columns.", nameof(fen));
            }

            // 2. Turn color
            if (turnFEN != "w" && turnFEN != "b")
                throw new ArgumentException($"Invalid turn value: '{turnFEN}'. It must be 'w' or 'b'.", nameof(fen));

            currentState.Turn = turnFEN == "w" ? Piece.Color.White : Piece.Color.Black;

            // 3. Available castling rights
            if (!System.Text.RegularExpressions.Regex.IsMatch(castlingFEN, "^(K?Q?k?q?|\\-)$"))
                throw new ArgumentException($"Invalid castling format: '{castlingFEN}'.", nameof(fen));

            if (castlingFEN.Contains("K")) currentState.InitializeCastlingRights(BoardState.WHITE_KINGSIDE_CASTLING);
            if (castlingFEN.Contains("Q")) currentState.InitializeCastlingRights(BoardState.WHITE_QUEENSIDE_CASTLING);
            if (castlingFEN.Contains("k")) currentState.InitializeCastlingRights(BoardState.BLACK_KINGSIDE_CASTLING);
            if (castlingFEN.Contains("q")) currentState.InitializeCastlingRights(BoardState.BLACK_QUEENSIDE_CASTLING);

            // 4. En passant
            if (enPassantFEN != "-")
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(enPassantFEN, "^[a-h][36]$"))
                    throw new ArgumentException($"Invalid en passant value: '{enPassantFEN}'.", nameof(fen));

                int fileIndex = ChessUtils.NameToColumn(enPassantFEN[0]);
                int rank = ChessUtils.NameToRow(enPassantFEN[1]);
                //currentState.VulnerablePawnSquare = ChessUtils.CoordinatesToIndex(rank, fileIndex);
                if (rank == 2)
                    currentState.VulnerablePawnSquare = (24 + fileIndex);
                else
                    currentState.VulnerablePawnSquare = (32 + fileIndex);
            }
            else
            {
                currentState.HasVulnerablePawn = false;
            }

            // 5. Inactive ply count
            if (!byte.TryParse(plysFEN, out byte plys))
                throw new ArgumentException($"Invalid number of half-moves: '{plysFEN}'.", nameof(fen));

            currentState.InactivePlyCount = plys;

            // 6. Total move count
            if (!uint.TryParse(movesFEN, out uint moves))
                throw new ArgumentException($"Invalid number of moves: '{movesFEN}'.", nameof(fen));

            totalMoveCount = moves;

            // 7. Zobrist hash and repetitions
            currentState.ZobristHash = ZobristHashing.CreateZobristHash(this);
            repetitionHistory.Push(currentState.ZobristHash, true);
        }

        public BoardState CurrentState
        {
            get
            {
                return currentState;
            }
        }

        public RepetitionStack RepetitionHistory
        {
            get
            {
                return repetitionHistory;
            }
        }

        public Piece.Color Turn
        {
            get
            {
                return currentState.Turn;
            }
        }

        private int VulnerablePawn
        {
            get
            {
                return currentState.VulnerablePawnSquare;
            }
        }

        public ulong[] Bitboards
        {
            get
            {
                return bitboards;
            }
        }

        public ulong WhiteKingBitboard
        {
            get
            {
                return bitboards[0];
            }
        }

        public ulong WhiteQueensBitboard
        {
            get
            {
                return bitboards[1];
            }
        }

        public ulong WhiteRooksBitboard
        {
            get
            {
                return bitboards[2];
            }
        }

        public ulong WhiteBishopsBitboard
        {
            get
            {
                return bitboards[3];
            }
        }

        public ulong WhiteKnightsBitboard
        {
            get
            {
                return bitboards[4];
            }
        }

        public ulong WhitePawnsBitboard
        {
            get
            {
                return bitboards[5];
            }
        }

        public ulong BlackKingBitboard
        {
            get
            {
                return bitboards[6];
            }
        }

        public ulong BlackQueensBitboard
        {
            get
            {
                return bitboards[7];
            }
        }

        public ulong BlackRooksBitboard
        {
            get
            {
                return bitboards[8];
            }
        }

        public ulong BlackBishopsBitboard
        {
            get
            {
                return bitboards[9];
            }
        }

        public ulong BlackKnightsBitboard
        {
            get
            {
                return bitboards[10];
            }
        }

        public ulong BlackPawnsBitboard
        {
            get
            {
                return bitboards[11];
            }
        }

        public uint TotalMoveCount
        {
            get
            {
                return totalMoveCount;
            }
        }

        private void ChangeTurn()
        {
            currentState.Turn = ChessUtils.InverseColor(currentState.Turn);
            currentState.ZobristHash = ZobristHashing.UpdateZobristHashTurn(currentState.ZobristHash);

            // Clear the vulnerable pawn, if there is one
            if (currentState.HasVulnerablePawn)
            {
                if (ChessUtils.SameColor(GetPiece(VulnerablePawn).PieceColor, Turn))
                {
                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashEnPassantFile(currentState.ZobristHash, VulnerablePawn); // Be careful about moving this line further down, VulnerablePawn will no longer return the square
                    currentState.HasVulnerablePawn = false;
                }
            }
        }

        public void MakeMove(Move move, bool inSearch = false)
        {
            // Push the previous state onto the stack
            stateHistory.Push(currentState);

            // Clear the capture flag
            currentState.HasCapture = false;

            // Get the index of the piece that is going to move
            Piece movingPiece = GetPiece(move.From);
            Piece capturedPiece = GetPiece(move.To);

            // Update the Zobrist hash for the move
            currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, movingPiece, move.From);
            if (!move.IsPromotion())
                currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, movingPiece, move.To);

            // Change the square of the piece
            UpdateMoveBitboards(movingPiece, move.From, move.To);

            if (capturedPiece.PieceType != Piece.Type.None)
            {
                if (currentState.HasVulnerablePawn && (move.To == VulnerablePawn))
                {
                    // The captured piece was the vulnerable pawn, clear it
                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashEnPassantFile(currentState.ZobristHash, VulnerablePawn); // Be careful about moving this line further down, VulnerablePawn will no longer return the square
                    currentState.HasVulnerablePawn = false;
                }
                else if (capturedPiece.PieceType == Piece.Type.Rook)
                {
                    // Cancel the castling rights of each rook
                    switch (move.To)
                    {
                        case 0:
                            {
                                // Update the available rights of the state
                                currentState.CancelCastling(BoardState.WHITE_QUEENSIDE_CASTLING);
                                break;
                            }

                        case 7:
                            {
                                // Update the available rights of the state
                                currentState.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING);
                                break;
                            }

                        case 56:
                            {
                                // Update the available rights of the state
                                currentState.CancelCastling(BoardState.BLACK_QUEENSIDE_CASTLING);
                                break;
                            }

                        case 63:
                            {
                                // Update the available rights of the state
                                currentState.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING);
                                break;
                            }
                    }

                    // Update the Zobrist hash for the available castling rights
                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, stateHistory.Peek().CastlingRights); // Remove the old available castling rights
                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, currentState.CastlingRights); // Add the new available castling rights
                }

                // There is a capture
                RemovePiece(capturedPiece, move.To);
            }

            // Handle the flags
            switch (move.Flag)
            {
                case Move.EN_PASSANT_CAPTURE:
                    {
                        RemovePiece(new Piece(Piece.Type.Pawn, ChessUtils.InverseColor(Turn)), VulnerablePawn);
                        currentState.HasVulnerablePawn = false; ;
                        break;
                    }

                case Move.CASTLE:
                    {
                        Piece rook = new Piece(Piece.Type.Rook, Turn);

                        switch (move.To)
                        {
                            case ChessUtils.WHITE_QUEENSIDE_CASTLE_SQUARE:
                                {
                                    // White queenside castling
                                    UpdateMoveBitboards(rook, 0, 3);
                                    currentState.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING | BoardState.WHITE_QUEENSIDE_CASTLING);

                                    // Update the Zobrist hash for the move
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 0);
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 3);
                                    break;
                                }

                            case ChessUtils.WHITE_KINGSIDE_CASTLE_SQUARE:
                                {
                                    // White kingside castling
                                    UpdateMoveBitboards(rook, 7, 5);
                                    currentState.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING | BoardState.WHITE_QUEENSIDE_CASTLING);

                                    // Update the Zobrist hash for the move
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 7);
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 5);
                                    break;
                                }

                            case ChessUtils.BLACK_QUEENSIDE_CASTLE_SQUARE:
                                {
                                    // Black queenside castling
                                    UpdateMoveBitboards(rook, 56, 59);
                                    currentState.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING | BoardState.BLACK_QUEENSIDE_CASTLING);

                                    // Update the Zobrist hash for the move
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 56);
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 59);
                                    break;
                                }

                            case ChessUtils.BLACK_KINGSIDE_CASTLE_SQUARE:
                                {
                                    // Black kingside castling
                                    UpdateMoveBitboards(rook, 63, 61);
                                    currentState.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING | BoardState.BLACK_QUEENSIDE_CASTLING);

                                    // Update the Zobrist hash for the move
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 63);
                                    currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, rook, 61);
                                    break;
                                }
                        }

                        // Update the Zobrist hash for the available castling rights
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, stateHistory.Peek().CastlingRights); // Remove the old available castling rights
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, currentState.CastlingRights); // Add the new available castling rights

                        break;
                    }

                case Move.PAWN_TWO_UP:
                    {
                        currentState.VulnerablePawnSquare = move.To;
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashEnPassantFile(currentState.ZobristHash, currentState.VulnerablePawnSquare);
                        break;
                    }

                case Move.PROMOTE_TO_QUEEN:
                    {
                        UpdatePromotionBitboards(Piece.Type.Queen, Turn, move.To);
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, new Piece(Piece.Type.Queen, Turn), move.To);
                        break;
                    }

                case Move.PROMOTE_TO_KNIGHT:
                    {
                        UpdatePromotionBitboards(Piece.Type.Knight, Turn, move.To);
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, new Piece(Piece.Type.Knight, Turn), move.To);
                        break;
                    }

                case Move.PROMOTE_TO_ROOK:
                    {
                        UpdatePromotionBitboards(Piece.Type.Rook, Turn, move.To);
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, new Piece(Piece.Type.Rook, Turn), move.To);
                        break;
                    }

                case Move.PROMOTE_TO_BISHOP:
                    {
                        UpdatePromotionBitboards(Piece.Type.Bishop, Turn, move.To);
                        currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, new Piece(Piece.Type.Bishop, Turn), move.To);
                        break;
                    }
            }

            if (movingPiece.PieceType == Piece.Type.King /*&& move.Flag != Move.CASTLE*/)
            {
                if (ChessUtils.SameColor(movingPiece.PieceColor, Piece.Color.White))
                {
                    currentState.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING | BoardState.WHITE_QUEENSIDE_CASTLING);
                }
                else
                {
                    currentState.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING | BoardState.BLACK_QUEENSIDE_CASTLING);
                }

                // Update the Zobrist hash for the available castling rights
                currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, stateHistory.Peek().CastlingRights); // Remove the old available castling rights
                currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, currentState.CastlingRights); // Add the new available castling rights
            }
            else if (movingPiece.PieceType == Piece.Type.Rook)
            {
                if (ChessUtils.SameColor(movingPiece.PieceColor, Piece.Color.White))
                {
                    if (move.From == 0)
                        currentState.CancelCastling(BoardState.WHITE_QUEENSIDE_CASTLING);
                    else if (move.From == 7)
                        currentState.CancelCastling(BoardState.WHITE_KINGSIDE_CASTLING);
                }
                else
                {
                    if (move.From == 56)
                        currentState.CancelCastling(BoardState.BLACK_QUEENSIDE_CASTLING);
                    else if (move.From == 63)
                        currentState.CancelCastling(BoardState.BLACK_KINGSIDE_CASTLING);
                }

                // Update the Zobrist hash for the available castling rights
                currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, stateHistory.Peek().CastlingRights); // Remove the old available castling rights
                currentState.ZobristHash = ZobristHashing.UpdateZobristHashCastlingRights(currentState.ZobristHash, currentState.CastlingRights); // Add the new available castling rights
            }

            // Increment the total move count
            if (Turn == Piece.Color.Black)
            {
                totalMoveCount++;
            }

            // Switch turns
            ChangeTurn(); // Be careful about moving this further down, it affects the Zobrist hash

            if (!currentState.HasCapture && movingPiece.PieceType != Piece.Type.Pawn)
            {
                // Non irreversible move
                // Increment the fifty-move rule counter
                currentState.InactivePlyCount++;
                // Add the position to the repetitions stack
                repetitionHistory.Push(currentState.ZobristHash, false);
            }
            else
            {
                // Irreversible move
                currentState.InactivePlyCount = 0;

                if (inSearch)
                {
                    // If we are in a search, the stack content is not cleared, the last valid segment is marked instead
                    repetitionHistory.Push(currentState.ZobristHash, true);
                }
                else
                {
                    // If we are not in a search, the stack content is cleared
                    repetitionHistory.Clear();
                }
            }
        }

        public void UndoMove(Move move)
        {
            // Get the index of the piece that wants to undo its move
            Piece piece = GetPiece(move.To);

            // Change the square of the piece
            UpdateMoveBitboards(piece, move.To, move.From);

            // Handle the flags
            switch (move.Flag)
            {
                case Move.CASTLE:
                    {
                        switch (move.To)
                        {
                            case ChessUtils.WHITE_QUEENSIDE_CASTLE_SQUARE:
                                {
                                    // White queenside castling
                                    UpdateMoveBitboards(Piece.Type.Rook, piece.PieceColor, 3, 0);
                                    break;
                                }

                            case ChessUtils.WHITE_KINGSIDE_CASTLE_SQUARE:
                                {
                                    // White kingside castling
                                    UpdateMoveBitboards(Piece.Type.Rook, piece.PieceColor, 5, 7);
                                    break;
                                }

                            case ChessUtils.BLACK_QUEENSIDE_CASTLE_SQUARE:
                                {
                                    // Black queenside castling
                                    UpdateMoveBitboards(Piece.Type.Rook, piece.PieceColor, 59, 56);
                                    break;
                                }

                            case ChessUtils.BLACK_KINGSIDE_CASTLE_SQUARE:
                                {
                                    // Black kingside castling
                                    UpdateMoveBitboards(Piece.Type.Rook, piece.PieceColor, 61, 63);
                                    break;
                                }
                        }

                        break;
                    }

                case Move.PROMOTE_TO_QUEEN:
                    {
                        RevertPromotionBitboards(Piece.Type.Queen, piece.PieceColor, move.From);
                        break;
                    }

                case Move.PROMOTE_TO_KNIGHT:
                    {
                        RevertPromotionBitboards(Piece.Type.Knight, piece.PieceColor, move.From);
                        break;
                    }

                case Move.PROMOTE_TO_ROOK:
                    {
                        RevertPromotionBitboards(Piece.Type.Rook, piece.PieceColor, move.From);
                        break;
                    }

                case Move.PROMOTE_TO_BISHOP:
                    {
                        RevertPromotionBitboards(Piece.Type.Bishop, piece.PieceColor, move.From);
                        break;
                    }
            }

            if (currentState.HasCapture)
            {
                (Piece capturedPiece, int square) = deadPieces.Pop();
                bitboards[ChessUtils.GetPieceIndex(capturedPiece.PieceType, capturedPiece.PieceColor)] |= BitboardUtils.SetBit(square);
            }

            // Decrement the total move count
            if (Turn == Piece.Color.White)
            {
                totalMoveCount--;
            }

            // Restore the last state
            currentState = stateHistory.Pop();

            // Remove the last entry of the repetition history, if there is any
            repetitionHistory.Pop();
        }

        public (List<Move> legalMoves, bool inCheck) GenerateLegalMoves
        (
            bool shortenGeneration = false,  // Stop generating moves as soon as a draw situation is reached
            bool capturesOnly = false // Generate only capture moves
        )
        {
            List<Move> legalMoves = new List<Move>();
            int kingSquare = GetKingSquare(Turn);

            if (kingSquare == -1)
            {
                // There is no king
                return (legalMoves, false);
            }

            if (shortenGeneration)
            {
                if (currentState.InactivePlyCount >= MAX_INACTIVE_PLYS)
                {
                    // The fifty-move rule is fulfilled
                    return (legalMoves, false);
                }

                if (GameStatus.IsInsufficientMaterial(this))
                {
                    // The material is insufficient
                    return (legalMoves, false);
                }

                if (repetitionHistory.IsThreefoldRepetition(currentState.ZobristHash))
                {
                    // The last move made resulted in a threefold repetition
                    return (legalMoves, false);
                }
            }

            ulong occupiedSquares = GetOccupied();
            ulong kingAttackers = GetAttackers(occupiedSquares, kingSquare, ChessUtils.InverseColor(Turn));
            (ulong pinnedPieces, ulong pinners) = GetPinnedPieces(occupiedSquares, kingSquare, Turn);
            bool inCheck = kingAttackers == 0 ? false : true;
            ulong pieces = ChessUtils.SameColor(Turn, Piece.Color.White) ? GetWhiteOccupied() : GetBlackOccupied();

            while (pieces != 0)
            {
                int square = BitboardUtils.FirstActiveBit(pieces);
                pieces &= pieces - 1; // Clears the lowest active bit

                switch (GetPiece(square).PieceType)
                {
                    case Piece.Type.King:
                        {
                            GenerateKingMoves(legalMoves, square, occupiedSquares, GetValidKingMoveBitboard(inCheck, occupiedSquares), capturesOnly);
                            break;
                        }

                    case Piece.Type.Queen:
                        {
                            GenerateQueenMoves(legalMoves, square, occupiedSquares, GetValidMoveBitboard(square, inCheck, kingAttackers, pinnedPieces, pinners, kingSquare), capturesOnly);
                            break;
                        }

                    case Piece.Type.Rook:
                        {
                            GenerateRookMoves(legalMoves, square, occupiedSquares, GetValidMoveBitboard(square, inCheck, kingAttackers, pinnedPieces, pinners, kingSquare), capturesOnly);
                            break;
                        }

                    case Piece.Type.Bishop:
                        {
                            GenerateBishopMoves(legalMoves, square, occupiedSquares, GetValidMoveBitboard(square, inCheck, kingAttackers, pinnedPieces, pinners, kingSquare), capturesOnly);
                            break;
                        }

                    case Piece.Type.Knight:
                        {
                            GenerateKnightMoves(legalMoves, square, GetValidMoveBitboard(square, inCheck, kingAttackers, pinnedPieces, pinners, kingSquare), capturesOnly);
                            break;
                        }

                    case Piece.Type.Pawn:
                        {
                            GeneratePawnMoves(legalMoves, square, kingSquare, occupiedSquares, GetValidMoveBitboard(square, inCheck, kingAttackers, pinnedPieces, pinners, kingSquare), capturesOnly);
                            break;
                        }
                }
            }

            return (legalMoves, inCheck);
        }

        private void GenerateKingMoves(List<Move> legalMoves, int square, ulong occupiedSquares, ulong validMoves, bool capturesOnly)
        {
            for (int direction = 0; direction < 8; direction++)
            {
                if (ChessUtils.SquaresToBorder[square][direction] > 0)
                {
                    int destinationSquare = square + ChessUtils.Directions[direction];
                    Piece destinationPiece = GetPiece(destinationSquare);

                    // If there is a piece of the same color, it is not a valid move
                    if (destinationPiece.PieceType != Piece.Type.None && ChessUtils.SameColor(destinationPiece.PieceColor, Turn))
                    {
                        continue;
                    }

                    // If only captures are being generated and there is no piece on the destination, keep looking
                    if (capturesOnly && destinationPiece.PieceType == Piece.Type.None)
                    {
                        continue;
                    }

                    // If it is not an attacked square and the move is valid
                    if (!IsSquareAttacked(occupiedSquares, destinationSquare, ChessUtils.InverseColor(Turn)) && BitboardUtils.IsSquareActive(destinationSquare, validMoves))
                    {
                        legalMoves.Add(new Move(square, destinationSquare, Move.NO_FLAG));
                    }
                }
            }

            if (!capturesOnly)
            {
                if (Turn == Piece.Color.White)
                {
                    // White queenside castling
                    if (currentState.IsCastlingAvailable(BoardState.WHITE_QUEENSIDE_CASTLING) && !IsSquareAttacked(occupiedSquares, square, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 1) && IsSquareEmpty(occupiedSquares, 2) && !IsSquareAttacked(occupiedSquares, 2, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 3) && !IsSquareAttacked(occupiedSquares, 3, ChessUtils.InverseColor(Turn)))
                    {
                        legalMoves.Add(new Move(square, 2, Move.CASTLE));
                    }

                    // White kingside castling
                    if (currentState.IsCastlingAvailable(BoardState.WHITE_KINGSIDE_CASTLING) && !IsSquareAttacked(occupiedSquares, square, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 5) && !IsSquareAttacked(occupiedSquares, 5, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 6) && !IsSquareAttacked(occupiedSquares, 6, ChessUtils.InverseColor(Turn)))
                    {
                        legalMoves.Add(new Move(square, 6, Move.CASTLE));
                    }
                }
                else
                {
                    // Black queenside castling
                    if (currentState.IsCastlingAvailable(BoardState.BLACK_QUEENSIDE_CASTLING) && !IsSquareAttacked(occupiedSquares, square, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 57) && IsSquareEmpty(occupiedSquares, 58) && !IsSquareAttacked(occupiedSquares, 58, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 59) && !IsSquareAttacked(occupiedSquares, 59, ChessUtils.InverseColor(Turn)))
                    {
                        legalMoves.Add(new Move(square, 58, Move.CASTLE));
                    }

                    // Black kingside castling
                    if (currentState.IsCastlingAvailable(BoardState.BLACK_KINGSIDE_CASTLING) && !IsSquareAttacked(occupiedSquares, square, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 61) && !IsSquareAttacked(occupiedSquares, 61, ChessUtils.InverseColor(Turn)) && IsSquareEmpty(occupiedSquares, 62) && !IsSquareAttacked(occupiedSquares, 62, ChessUtils.InverseColor(Turn)))
                    {
                        legalMoves.Add(new Move(square, 62, Move.CASTLE));
                    }
                }
            }
        }

        private void GenerateQueenMoves(List<Move> legalMoves, int square, ulong occupied, ulong validMoves, bool capturesOnly)
        {
            ulong attacks = BitboardUtils.RookAttacks(occupied, square) | BitboardUtils.BishopAttacks(occupied, square);

            // Intersection between the attacks and the given valid moves (for example, due to a pin or check)
            ulong moves = attacks & validMoves;

            // Iterate over the active bits (possible moves)
            while (moves != 0)
            {
                int destination = BitboardUtils.FirstActiveBit(moves);
                moves &= moves - 1; // Clears the lowest active bit

                Piece destinationPiece = GetPiece(destination);

                // If there is no piece of the same color and the captures-only flag is set, only add it if there is a piece of the opposite color
                if (destinationPiece.PieceType == Piece.Type.None || !ChessUtils.SameColor(destinationPiece.PieceColor, Turn))
                {
                    if (!capturesOnly || (destinationPiece.PieceType != Piece.Type.None))
                    {
                        legalMoves.Add(new Move(square, destination, Move.NO_FLAG));
                    }
                }
            }
        }

        private void GenerateRookMoves(List<Move> legalMoves, int square, ulong occupied, ulong validMoves, bool capturesOnly)
        {
            ulong attacks = BitboardUtils.RookAttacks(occupied, square);

            // Intersection between the attacks and the given valid moves (for example, due to a pin or check)
            ulong moves = attacks & validMoves;

            // Iterate over the active bits (possible moves)
            while (moves != 0)
            {
                int destination = BitboardUtils.FirstActiveBit(moves);
                moves &= moves - 1; // Clears the lowest active bit

                Piece destinationPiece = GetPiece(destination);

                // If there is no piece of the same color and the captures-only flag is set, only add it if there is a piece of the opposite color
                if (destinationPiece.PieceType == Piece.Type.None || !ChessUtils.SameColor(destinationPiece.PieceColor, Turn))
                {
                    if (!capturesOnly || (destinationPiece.PieceType != Piece.Type.None))
                    {
                        legalMoves.Add(new Move(square, destination, Move.NO_FLAG));
                    }
                }
            }
        }

        private void GenerateBishopMoves(List<Move> legalMoves, int square, ulong occupied, ulong validMoves, bool capturesOnly)
        {
            ulong attacks = BitboardUtils.BishopAttacks(occupied, square);

            // Intersection between the attacks and the given valid moves (for example, due to a pin or check)
            ulong moves = attacks & validMoves;

            // Iterate over the active bits (possible moves)
            while (moves != 0)
            {
                int destination = BitboardUtils.FirstActiveBit(moves);
                moves &= moves - 1; // Clears the lowest active bit

                Piece destinationPiece = GetPiece(destination);

                // If there is no piece of the same color and the captures-only flag is set, only add it if there is a piece of the opposite color
                if (destinationPiece.PieceType == Piece.Type.None || !ChessUtils.SameColor(destinationPiece.PieceColor, Turn))
                {
                    if (!capturesOnly || (destinationPiece.PieceType != Piece.Type.None))
                    {
                        legalMoves.Add(new Move(square, destination, Move.NO_FLAG));
                    }
                }
            }
        }

        private void GenerateKnightMoves(List<Move> legalMoves, int square, ulong validMoves, bool capturesOnly)
        {
            foreach (int jump in ChessUtils.KnightJumps)
            {
                int destinationSquare = square + jump;

                // Validate that it does not go off the board and that the jump is valid
                if (destinationSquare < 0 || destinationSquare >= 64 || !ChessUtils.IsValidKnightJump(square, destinationSquare))
                {
                    continue;
                }

                // Check whether there is a piece on the destination
                Piece destinationPiece = GetPiece(destinationSquare);

                // If there is a piece of the same color, it cannot move there
                if (destinationPiece.PieceType != Piece.Type.None && ChessUtils.SameColor(destinationPiece.PieceColor, Turn))
                {
                    continue;
                }

                // If only captures are being generated and there is no piece on the destination, keep looking
                if (capturesOnly && destinationPiece.PieceType == Piece.Type.None)
                {
                    continue;
                }

                // It is a valid knight move
                if (BitboardUtils.IsSquareActive(destinationSquare, validMoves))
                {
                    legalMoves.Add(new Move(square, destinationSquare, Move.NO_FLAG));
                }
            }
        }

        private void GeneratePawnMoves(List<Move> legalMoves, int square, int kingSquare, ulong occupiedSquares, ulong validMoves, bool capturesOnly)
        {
            int advance = ChessUtils.SameColor(Turn, Piece.Color.White) ? 8 : -8;
            int advanceSquare = square + advance;
            Piece destinationPiece = GetPiece(advanceSquare);

            // It can promote
            if ((ChessUtils.SameColor(Turn, Piece.Color.White) && (ChessUtils.GetRow(square) == 6)) || (ChessUtils.SameColor(Turn, Piece.Color.Black) && (ChessUtils.GetRow(square) == 1)))
            {
                // It is not blocked by a piece
                if (destinationPiece.PieceType == Piece.Type.None && BitboardUtils.IsSquareActive(advanceSquare, validMoves))
                {
                    if (!capturesOnly)
                    {
                        legalMoves.Add(new Move(square, advanceSquare, Move.PROMOTE_TO_QUEEN));
                        legalMoves.Add(new Move(square, advanceSquare, Move.PROMOTE_TO_KNIGHT));
                        legalMoves.Add(new Move(square, advanceSquare, Move.PROMOTE_TO_ROOK));
                        legalMoves.Add(new Move(square, advanceSquare, Move.PROMOTE_TO_BISHOP));
                    }
                }

                // It can move to the northwest
                if ((ChessUtils.SquaresToBorder[square][ChessUtils.NORTHWEST] > 0) && BitboardUtils.IsSquareActive(advanceSquare - 1, validMoves))
                {
                    destinationPiece = GetPiece(advanceSquare - 1);

                    // There is a capture to the west
                    if (destinationPiece.PieceType != Piece.Type.None && !ChessUtils.SameColor(Turn, destinationPiece.PieceColor))
                    {
                        legalMoves.Add(new Move(square, advanceSquare - 1, Move.PROMOTE_TO_QUEEN));
                        legalMoves.Add(new Move(square, advanceSquare - 1, Move.PROMOTE_TO_KNIGHT));
                        legalMoves.Add(new Move(square, advanceSquare - 1, Move.PROMOTE_TO_ROOK));
                        legalMoves.Add(new Move(square, advanceSquare - 1, Move.PROMOTE_TO_BISHOP));
                    }
                }

                // It can move to the northeast
                if ((ChessUtils.SquaresToBorder[square][ChessUtils.NORTHEAST] > 0) && BitboardUtils.IsSquareActive(advanceSquare + 1, validMoves))
                {
                    destinationPiece = GetPiece(advanceSquare + 1);

                    // There is a capture to the east
                    if (destinationPiece.PieceType != Piece.Type.None && !ChessUtils.SameColor(Turn, destinationPiece.PieceColor))
                    {
                        legalMoves.Add(new Move(square, advanceSquare + 1, Move.PROMOTE_TO_QUEEN));
                        legalMoves.Add(new Move(square, advanceSquare + 1, Move.PROMOTE_TO_KNIGHT));
                        legalMoves.Add(new Move(square, advanceSquare + 1, Move.PROMOTE_TO_ROOK));
                        legalMoves.Add(new Move(square, advanceSquare + 1, Move.PROMOTE_TO_BISHOP));
                    }
                }
            }
            else
            {
                // Check whether the move is a normal push, whether it is not a capture and whether the captures-only flag is set
                if (!capturesOnly && destinationPiece.PieceType == Piece.Type.None && BitboardUtils.IsSquareActive(advanceSquare, validMoves))
                {
                    legalMoves.Add(new Move(square, advanceSquare, Move.NO_FLAG));
                }

                // It can move to the northwest
                if ((ChessUtils.SquaresToBorder[square][ChessUtils.NORTHWEST] > 0) && BitboardUtils.IsSquareActive(advanceSquare - 1, validMoves))
                {
                    destinationPiece = GetPiece(advanceSquare - 1);

                    // There is a capture to the west
                    if (destinationPiece.PieceType != Piece.Type.None && !ChessUtils.SameColor(Turn, destinationPiece.PieceColor))
                    {
                        legalMoves.Add(new Move(square, advanceSquare - 1, Move.NO_FLAG));
                    }
                }

                // It can move to the northeast
                if ((ChessUtils.SquaresToBorder[square][ChessUtils.NORTHEAST] > 0) && BitboardUtils.IsSquareActive(advanceSquare + 1, validMoves))
                {
                    destinationPiece = GetPiece(advanceSquare + 1);

                    // There is a capture to the east
                    if (destinationPiece.PieceType != Piece.Type.None && !ChessUtils.SameColor(Turn, destinationPiece.PieceColor))
                    {
                        legalMoves.Add(new Move(square, advanceSquare + 1, Move.NO_FLAG));
                    }
                }

                // There is a vulnerable pawn
                if (currentState.HasVulnerablePawn)
                {
                    int westDestinationSquare = advanceSquare - 1;
                    int eastDestinationSquare = advanceSquare + 1;

                    // There is an en passant capture to the west
                    if ((ChessUtils.SquaresToBorder[square][ChessUtils.NORTHWEST] > 0)
                        && (VulnerablePawn == square - 1)
                        && (BitboardUtils.IsSquareActive(westDestinationSquare, validMoves) || BitboardUtils.IsSquareActive(VulnerablePawn, validMoves))
                        && !DoesEnPassantExposeKing(square, westDestinationSquare, occupiedSquares, kingSquare, Turn))
                    {
                        legalMoves.Add(new Move(square, westDestinationSquare, Move.EN_PASSANT_CAPTURE));
                    }
                    else if ((ChessUtils.SquaresToBorder[square][ChessUtils.NORTHEAST] > 0)
                        && (VulnerablePawn == square + 1)
                        && (BitboardUtils.IsSquareActive(eastDestinationSquare, validMoves) || BitboardUtils.IsSquareActive(VulnerablePawn, validMoves))
                        && !DoesEnPassantExposeKing(square, eastDestinationSquare, occupiedSquares, kingSquare, Turn))
                    {
                        legalMoves.Add(new Move(square, eastDestinationSquare, Move.EN_PASSANT_CAPTURE));
                    }
                }

                // If it is the first move and the captures-only flag is not set
                if (!capturesOnly && ((ChessUtils.SameColor(Turn, Piece.Color.White) && ChessUtils.GetRow(square) == 1) || (ChessUtils.SameColor(Turn, Piece.Color.Black) && ChessUtils.GetRow(square) == 6)))
                {
                    // It can move two squares
                    destinationPiece = GetPiece(advanceSquare);
                    advanceSquare += advance;
                    Piece destinationPiece2 = GetPiece(advanceSquare);

                    // It is not blocked by a piece
                    if (destinationPiece.PieceType == Piece.Type.None && destinationPiece2.PieceType == Piece.Type.None && BitboardUtils.IsSquareActive(advanceSquare, validMoves))
                    {
                        legalMoves.Add(new Move(square, advanceSquare, Move.PAWN_TWO_UP));
                    }
                }
            }
        }

        private ulong GetValidKingMoveBitboard(bool inCheck, ulong occupiedSquares)
        {
            ulong validMoves = ulong.MaxValue;

            if (inCheck)
            {
                validMoves &= ~BitboardUtils.UnfilteredSlidingAttacks(occupiedSquares, bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, ChessUtils.InverseColor(Turn))], bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, ChessUtils.InverseColor(Turn))], bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, ChessUtils.InverseColor(Turn))], bitboards[ChessUtils.GetPieceIndex(Piece.Type.King, Turn)]);
            }

            return validMoves;
        }

        private ulong GetValidMoveBitboard(int pieceSquare, bool inCheck, ulong kingAttackers, ulong pinnedPieces, ulong pinners, int kingSquare)
        {
            ulong validMoves = ulong.MaxValue;

            if (inCheck)
            {
                // In check
                if (BitboardUtils.MoreThanOneBitActive(kingAttackers) || BitboardUtils.IsSquareActive(pieceSquare, pinnedPieces))
                {
                    // If there is more than one attacker, or the piece is pinned, it cannot move
                    validMoves = 0UL;
                }
                else
                {
                    //There is no more than one attacker and the piece is not pinned
                    int attackerSquare = BitboardUtils.FirstActiveBit(kingAttackers);

                    if (GetPiece(attackerSquare).PieceType.IsSliding())
                    {
                        // The piece can only move to a square between the attacker and the king, or to the attacker square
                        validMoves = kingAttackers | BitboardUtils.GetSquaresBetween(attackerSquare, kingSquare);
                    }
                    else
                    {
                        // The attacker is not a sliding piece, it can only be captured
                        validMoves = kingAttackers;
                    }
                }
            }
            else if (BitboardUtils.IsSquareActive(pieceSquare, pinnedPieces))
            {
                // The piece is pinned
                while (pinners != 0)
                {
                    // Find the pinner that pins this piece
                    int sq = BitboardUtils.FirstActiveBit(pinners);
                    ulong squaresBetween = BitboardUtils.GetSquaresBetween(sq, kingSquare);
                    if (BitboardUtils.IsSquareActive(pieceSquare, squaresBetween))
                    {
                        // Pinner found
                        validMoves = squaresBetween | (1UL << sq);
                        break;
                    }
                    pinners &= pinners - 1;
                }
            }

            return validMoves;
        }

        private bool DoesEnPassantExposeKing(int pawnSquare, int destinationSquare, ulong occupiedSquares, int kingSquare, Piece.Color kingColor)
        {
            // Simulate the occupied squares after the en passant capture
            ulong occupiedAfter = (occupiedSquares ^ BitboardUtils.SetBit(pawnSquare) ^ BitboardUtils.SetBit(VulnerablePawn)) | BitboardUtils.SetBit(destinationSquare);
            Piece.Color enemyColor = ChessUtils.InverseColor(kingColor);

            // 1. Check whether an enemy pawn attacks the king (excluding the captured vulnerable pawn)
            ulong enemyPawns = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, enemyColor)] & ~BitboardUtils.SetBit(VulnerablePawn);
            if ((BitboardUtils.PawnAttacks[ChessUtils.SameColor(enemyColor, Piece.Color.White) ? 0 : 1, kingSquare] & enemyPawns) != 0)
                return true;

            // 2. Check whether an enemy knight attacks the king
            ulong knights = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Knight, enemyColor)];
            if ((BitboardUtils.KnightAttacks[kingSquare] & knights) != 0)
                return true;

            // 3. Check whether an enemy queen or bishop attacks the king
            ulong queensBishops = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, enemyColor)]
                                | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, enemyColor)];
            if ((BitboardUtils.BishopAttacks(occupiedAfter, kingSquare) & queensBishops) != 0)
                return true;

            // 4. Check whether an enemy queen or rook attacks the king
            ulong queensRooks = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, enemyColor)]
                               | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, enemyColor)];
            if ((BitboardUtils.RookAttacks(occupiedAfter, kingSquare) & queensRooks) != 0)
                return true;

            return false;
        }

        private void RemovePiece(Piece piece, int square)
        {
            // Add the piece to the captured pieces list
            deadPieces.Push((piece, square));

            // Remove the piece from the bitboard
            RemovePieceFromBitboard(piece.PieceType, piece.PieceColor, square);

            // Mark that there is a capture
            currentState.HasCapture = true;

            // Update the Zobrist hash for the capture
            currentState.ZobristHash = ZobristHashing.UpdateZobristHashSquare(currentState.ZobristHash, piece, square);
        }

        private bool IsSquareEmpty(ulong occupied, int square)
        {
            return (occupied & BitboardUtils.SetBit(square)) == 0;
        }

        private bool IsSquareAttacked(ulong occupiedSquares, int square, Piece.Color color)
        {
            // 1. Check whether a pawn attacks the square
            ulong pawns = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, color)]; // Pawns of the opposite color
            if ((BitboardUtils.PawnAttacks[ChessUtils.SameColor(color, Piece.Color.White) ? 0 : 1, square] & pawns) != 0)
                return true;

            // 2. Check whether a knight attacks the square
            ulong knights = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Knight, color)]; // Knights of the opposite color
            if ((BitboardUtils.KnightAttacks[square] & knights) != 0)
                return true;

            // 3. Check whether a king attacks the square
            ulong king = bitboards[ChessUtils.GetPieceIndex(Piece.Type.King, color)]; // King of the opposite color
            if ((BitboardUtils.KingAttacks[square] & king) != 0)
                return true;

            // 4. Check whether a queen or bishop attacks the square
            ulong queensBishops = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, color)] // Queens of the opposite color
                                | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, color)]; // Bishops of the opposite color

            if ((BitboardUtils.BishopAttacks(occupiedSquares, square) & queensBishops) != 0)
                return true;

            // 5. Check whether a queen or rook attacks the square
            ulong queensRooks = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, color)] // Queens of the opposite color
                                | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, color)]; // Rooks of the opposite color

            if ((BitboardUtils.RookAttacks(occupiedSquares, square) & queensRooks) != 0)
                return true;

            return false;
        }

        private ulong GetAttackers(ulong occupiedSquares, int square, Piece.Color color)
        {
            ulong attackers = 0;

            // 1. Check whether a pawn attacks the square
            ulong pawns = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, color)]; // Pawns of the opposite color
            attackers |= (BitboardUtils.PawnAttacks[ChessUtils.SameColor(color, Piece.Color.White) ? 0 : 1, square] & pawns);

            // 2. Check whether a knight attacks the square
            ulong knights = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Knight, color)]; // Knights of the opposite color
            attackers |= (BitboardUtils.KnightAttacks[square] & knights);

            // 3. Check whether a king attacks the square
            ulong king = bitboards[ChessUtils.GetPieceIndex(Piece.Type.King, color)]; // King of the opposite color
            attackers |= (BitboardUtils.KingAttacks[square] & king);

            // 4. Check whether a queen or bishop attacks the square
            ulong queensBishops = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, color)] // Queens of the opposite color
                                | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, color)]; // Bishops of the opposite color

            attackers |= (BitboardUtils.BishopAttacks(occupiedSquares, square) & queensBishops);

            // 5. Check whether a queen or rook attacks the square
            ulong queensRooks = bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, color)] // Queens of the opposite color
                                | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, color)]; // Rooks of the opposite color

            attackers |= (BitboardUtils.RookAttacks(occupiedSquares, square) & queensRooks);

            return attackers;
        }

        private void UpdateMoveBitboards(Piece.Type pieceType, Piece.Color color, int from, int to)
        {
            bitboards[ChessUtils.GetPieceIndex(pieceType, color)] &= ~(1UL << from); // clear origin square
            bitboards[ChessUtils.GetPieceIndex(pieceType, color)] |= (1UL << to); // set destination square
        }

        private void UpdateMoveBitboards(Piece piece, int from, int to)
        {
            bitboards[ChessUtils.GetPieceIndex(piece)] &= ~(1UL << from); // clear origin square
            bitboards[ChessUtils.GetPieceIndex(piece)] |= (1UL << to); // set destination square
        }

        private void RemovePieceFromBitboard(Piece.Type pieceType, Piece.Color color, int square)
        {
            bitboards[ChessUtils.GetPieceIndex(pieceType, color)] &= ~(1UL << square);
        }

        private void RemovePieceFromBitboard(Piece piece, int square)
        {
            bitboards[ChessUtils.GetPieceIndex(piece)] &= ~(1UL << square);
        }

        private void UpdatePromotionBitboards(Piece.Type pieceType, Piece.Color color, int square)
        {
            bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, color)] &= ~(1UL << square);
            bitboards[ChessUtils.GetPieceIndex(pieceType, color)] |= (1UL << square);
        }

        private void UpdatePromotionBitboards(Piece piece, int square)
        {
            bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, piece.PieceColor)] &= ~(1UL << square);
            bitboards[ChessUtils.GetPieceIndex(piece.PieceType, piece.PieceColor)] |= (1UL << square);
        }

        private void RevertPromotionBitboards(Piece.Type pieceType, Piece.Color color, int square)
        {
            bitboards[ChessUtils.GetPieceIndex(pieceType, color)] &= ~(1UL << square);
            bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, color)] |= (1UL << square);
        }

        private void RevertPromotionBitboards(Piece piece, int square)
        {
            bitboards[ChessUtils.GetPieceIndex(piece.PieceType, piece.PieceColor)] &= ~(1UL << square);
            bitboards[ChessUtils.GetPieceIndex(Piece.Type.Pawn, piece.PieceColor)] |= (1UL << square);
        }

        private ulong GetWhiteOccupied()
        {
            return bitboards[0] | bitboards[1] | bitboards[2] | bitboards[3] | bitboards[4] | bitboards[5];
        }

        private ulong GetBlackOccupied()
        {
            return bitboards[6] | bitboards[7] | bitboards[8] | bitboards[9] | bitboards[10] | bitboards[11];
        }

        private ulong GetOccupied()
        {
            return GetWhiteOccupied() | GetBlackOccupied();
        }

        public int GetKingSquare(Piece.Color color)
        {
            ulong king = bitboards[ChessUtils.GetPieceIndex(Piece.Type.King, color)];
            if (king == 0)
            {
                return -1;
            }

            return BitboardUtils.FirstActiveBit(king);
        }

        private (ulong pinned, ulong pinners) GetPinnedPieces(ulong occupiedSquares, int kingSquare, Piece.Color kingColor)
        {
            ulong friendlyPieces = ChessUtils.SameColor(kingColor, Piece.Color.White) ? GetWhiteOccupied() : GetBlackOccupied();
            ulong pinned = 0;

            ulong pinners = BitboardUtils.XRayRookAttacks(occupiedSquares, friendlyPieces, kingSquare) & (bitboards[ChessUtils.GetPieceIndex(Piece.Type.Rook, ChessUtils.InverseColor(kingColor))] | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, ChessUtils.InverseColor(kingColor))]);
            ulong returnPinners = pinners;
            while (pinners != 0)
            {
                int sq = BitboardUtils.FirstActiveBit(pinners);
                pinned |= BitboardUtils.GetSquaresBetween(sq, kingSquare) & friendlyPieces;
                pinners &= pinners - 1;
            }

            pinners = BitboardUtils.XRayBishopAttacks(occupiedSquares, friendlyPieces, kingSquare) & (bitboards[ChessUtils.GetPieceIndex(Piece.Type.Bishop, ChessUtils.InverseColor(kingColor))] | bitboards[ChessUtils.GetPieceIndex(Piece.Type.Queen, ChessUtils.InverseColor(kingColor))]);
            returnPinners |= pinners;
            while (pinners != 0)
            {
                int sq = BitboardUtils.FirstActiveBit(pinners);
                pinned |= BitboardUtils.GetSquaresBetween(sq, kingSquare) & friendlyPieces;
                pinners &= pinners - 1;
            }

            return (pinned, returnPinners);
        }

        public Piece GetPiece(int square)
        {
            for (int i = 0; i < bitboards.Length; i++)
            {
                if ((bitboards[i] & BitboardUtils.SetBit(square)) != 0)
                {
                    Piece.Color color = (i < 6) ? Piece.Color.White : Piece.Color.Black;
                    int typeIndex = i % 6;

                    Piece.Type type = typeIndex switch
                    {
                        0 => Piece.Type.King,
                        1 => Piece.Type.Queen,
                        2 => Piece.Type.Rook,
                        3 => Piece.Type.Bishop,
                        4 => Piece.Type.Knight,
                        5 => Piece.Type.Pawn,
                        _ => Piece.Type.None
                    };

                    return new Piece(type, color);
                }
            }

            // If there is no piece on the square, we return an empty piece (None, White)
            return new Piece(Piece.Type.None, Piece.Color.White);
        }

        public bool IsSquareAttackedByEnemyPawn(int square)
        {
            if (ChessUtils.SameColor(Turn, Piece.Color.White))
            {
                ulong enemyPawns = bitboards[11];
                // Possible attacks on 'square' from enemy pawns
                ulong attacks = BitboardUtils.PawnAttacks[1, square];
                return (attacks & enemyPawns) != 0;
            }
            else
            {
                ulong enemyPawns = bitboards[5];
                // Possible attacks on 'square' from enemy pawns
                ulong attacks = BitboardUtils.PawnAttacks[0, square];
                return (attacks & enemyPawns) != 0;
            }
        }

        public ulong GetPinnedPiecesDebug()
        {
            (ulong pinnedPieces, _) = GetPinnedPieces(GetOccupied(), GetKingSquare(Turn), Turn);
            return pinnedPieces;
        }

        public ulong GetKingAttackersDebug()
        {
            return GetAttackers(GetOccupied(), GetKingSquare(Turn), ChessUtils.InverseColor(Turn));
        }

        public ulong GetPinnersDebug()
        {
            (_, ulong pinners) = GetPinnedPieces(GetOccupied(), GetKingSquare(Turn), Turn);
            return pinners;
        }
    }
}