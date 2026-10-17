using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using Ajedrez.Core;
using Ajedrez.Systems;
using Ajedrez.Utilities;
using Ajedrez.Data;

namespace Ajedrez.UI
{
    public class BoardUI : MonoBehaviour
    {
        private const int NO_SQUARE = -1;
        private const float DARKEN_VALUE = 0.4f;
        private const float MOVE_ANIMATION_DURATION = 0.2f;

        [SerializeField] [FormerlySerializedAs("tableroTransform")] private Transform boardTransform;
        [SerializeField] [FormerlySerializedAs("casillaPrefab")] private GameObject squarePrefab;
        [SerializeField] [FormerlySerializedAs("coloresTablero")] private BoardColors boardColors;
        [SerializeField] [FormerlySerializedAs("piezasSet")] private PieceSet pieceSet;
        [SerializeField] [FormerlySerializedAs("textoCoordenadaPrefab")] private GameObject coordinateTextPrefab;

        private GameObject[] squares;
        private Dictionary<int, Transform> pieceBySquare;
        private Move lastMove = Move.Null;
        private int lastCheckedKingSquare = NO_SQUARE;
        private bool whiteOnBottom = true;

        public void Init(Board board, bool whiteOnBottom = true)
        {
            // Set the board orientation
            this.whiteOnBottom = whiteOnBottom;

            // Generate the board
            GenerateBoard();

            // Generate the pieces
            if (board != null)
                GeneratePieces(board);

            // Initialize the piece dictionary so pieces can be moved
            InitializeDictionary();
        }

        public void ChangePosition(Board board)
        {
            ClearPieces();

            if (board != null)
                GeneratePieces(board);

            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            pieceBySquare = new Dictionary<int, Transform>();
            GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");

            int square;
            foreach (GameObject piece in pieces)
            {
                if (GetSquareFromCoordinate(piece.transform.position, out square))
                {
                    pieceBySquare[square] = piece.transform;
                }
            }
        }

        private void GenerateBoard()
        {
            // Calculate the offset to center the board at the origin
            const float offset = (ChessUtils.BOARD_SIZE - 1) / 2f;

            squares = new GameObject[ChessUtils.BOARD_SQUARE_COUNT];

            for (int i = 0; i < ChessUtils.BOARD_SIZE; i++)
            {
                for (int j = 0; j < ChessUtils.BOARD_SIZE; j++)
                {
                    Vector2 position = new Vector2((j - offset) * boardTransform.localScale.x, (i - offset) * boardTransform.localScale.y) + (Vector2)boardTransform.localPosition;

                    // Calculate the square index
                    int index = ChessUtils.CoordinatesToIndex(i, j);
                    if (!whiteOnBottom)
                        index = ChessUtils.BOARD_SQUARE_COUNT - 1 - index;

                    // Instantiate the square at the correct position
                    GameObject square = Instantiate(squarePrefab, position, Quaternion.identity);
                    square.transform.localScale = boardTransform.localScale;
                    square.transform.SetParent(boardTransform);

                    // Assign a name to the square
                    square.name = $"Casilla {index}";
                    square.tag = "Square";

                    // Set the square color
                    square.GetComponent<SpriteRenderer>().color = ChessUtils.IsDarkSquare(i, j) ? boardColors.darkSquareColor : boardColors.lightSquareColor;
                    squares[index] = square;

                    // Letters (a-h) below the first row (i == 0)
                    if (i == 0)
                    {
                        GameObject letter = Instantiate(coordinateTextPrefab, position, Quaternion.identity);
                        letter.transform.localScale = boardTransform.localScale;
                        letter.transform.SetParent(boardTransform);

                        TextMeshPro tmp = letter.GetComponent<TextMeshPro>();
                        tmp.text = ((char)('a' + j)).ToString();
                        tmp.alignment = TextAlignmentOptions.BottomLeft;
                        tmp.margin = new Vector4(0.05f, 0, 0, 0);

                        // Change color if the column index is even
                        if (j % 2 == 0)
                        {
                            tmp.color = boardColors.lightSquareColor;
                        }
                        else
                        {
                            tmp.color = DarkenColor(boardColors.darkSquareColor, DARKEN_VALUE);
                        }
                    }

                    // Numbers (1-8) to the left of the last column (j == BOARD_SIZE - 1)
                    if (j == ChessUtils.BOARD_SIZE - 1)
                    {
                        GameObject number = Instantiate(coordinateTextPrefab, position, Quaternion.identity);
                        number.transform.localScale = boardTransform.localScale;
                        number.transform.SetParent(boardTransform);

                        TextMeshPro tmp = number.GetComponent<TextMeshPro>();
                        tmp.text = (i + 1).ToString();
                        tmp.alignment = TextAlignmentOptions.TopRight;
                        tmp.margin = new Vector4(0, 0, 0.05f, 0);

                        // Change color if the row index is even
                        if (i % 2 == 0)
                        {
                            tmp.color = DarkenColor(boardColors.darkSquareColor, DARKEN_VALUE);
                        }
                        else
                        {
                            tmp.color = boardColors.lightSquareColor;
                        }
                    }
                }
            }
        }

        Color DarkenColor(Color color, float darkenAmount)
        {
            float r = Mathf.Max(color.r - darkenAmount, 0f);
            float g = Mathf.Max(color.g - darkenAmount, 0f);
            float b = Mathf.Max(color.b - darkenAmount, 0f);
            return new Color(r, g, b, color.a);
        }

        private void ClearBoard()
        {
            for (int i = boardTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = boardTransform.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        private void GeneratePieces(Board board)
        {
            ulong pieces;

            pieces = board.WhiteKingBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.King, Piece.Color.White, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.WhiteQueensBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Queen, Piece.Color.White, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.WhiteRooksBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Rook, Piece.Color.White, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.WhiteBishopsBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Bishop, Piece.Color.White, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.WhiteKnightsBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Knight, Piece.Color.White, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.WhitePawnsBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Pawn, Piece.Color.White, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.BlackKingBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.King, Piece.Color.Black, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.BlackQueensBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Queen, Piece.Color.Black, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.BlackRooksBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Rook, Piece.Color.Black, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.BlackBishopsBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Bishop, Piece.Color.Black, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.BlackKnightsBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Knight, Piece.Color.Black, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }

            pieces = board.BlackPawnsBitboard;
            while (pieces != 0)
            {
                GeneratePiece(Piece.Type.Pawn, Piece.Color.Black, BitboardUtils.FirstActiveBit(pieces));
                pieces &= pieces - 1;
            }
        }

        private void GeneratePiece(Piece.Type pieceType, Piece.Color color, int square)
        {
            string name;
            Sprite sprite;

            switch (pieceType)
            {
                case Piece.Type.King:
                    {
                        if (ChessUtils.SameColor(color, Piece.Color.White))
                        {
                            name = "White king";
                            sprite = pieceSet.whiteKing;
                        }
                        else
                        {
                            name = "Black king";
                            sprite = pieceSet.blackKing;
                        }
                        break;
                    }

                case Piece.Type.Queen:
                    {
                        if (ChessUtils.SameColor(color, Piece.Color.White))
                        {
                            name = "White queen";
                            sprite = pieceSet.whiteQueen;
                        }
                        else
                        {
                            name = "Black queen";
                            sprite = pieceSet.blackQueen;
                        }
                        break;
                    }

                case Piece.Type.Rook:
                    {
                        if (ChessUtils.SameColor(color, Piece.Color.White))
                        {
                            name = "White rook";
                            sprite = pieceSet.whiteRook;
                        }
                        else
                        {
                            name = "Black rook";
                            sprite = pieceSet.blackRook;
                        }
                        break;
                    }

                case Piece.Type.Bishop:
                    {
                        if (ChessUtils.SameColor(color, Piece.Color.White))
                        {
                            name = "White bishop";
                            sprite = pieceSet.whiteBishop;
                        }
                        else
                        {
                            name = "Black bishop";
                            sprite = pieceSet.blackBishop;
                        }
                        break;
                    }

                case Piece.Type.Knight:
                    {
                        if (ChessUtils.SameColor(color, Piece.Color.White))
                        {
                            name = "White knight";
                            sprite = pieceSet.whiteKnight;
                        }
                        else
                        {
                            name = "Black knight";
                            sprite = pieceSet.blackKnight;
                        }
                        break;
                    }

                case Piece.Type.Pawn:
                    {
                        if (ChessUtils.SameColor(color, Piece.Color.White))
                        {
                            name = "White pawn";
                            sprite = pieceSet.whitePawn;
                        }
                        else
                        {
                            name = "Black pawn";
                            sprite = pieceSet.blackPawn;
                        }
                        break;
                    }

                default:
                    {
                        name = "Piece";
                        sprite = pieceSet.whiteKing;
                        break;
                    }
            }

            // Create the piece game object
            GameObject pieceObject = new GameObject(name);
            pieceObject.tag = "Piece";

            // Create a sprite renderer for the piece
            SpriteRenderer spriteRenderer = pieceObject.AddComponent<SpriteRenderer>();

            // Assign the piece sprite to the game object
            spriteRenderer.sprite = sprite;

            // Adjust the piece size
            const float scaleFactor = 0.8f;
            pieceObject.transform.localScale = boardTransform.localScale * scaleFactor;

            // Assign the piece layer
            pieceObject.layer = LayerMask.NameToLayer("Pieces");
            spriteRenderer.sortingLayerName = "Pieces";
            spriteRenderer.sortingOrder = 0;

            // Set the square the piece is on as its parent
            pieceObject.transform.SetParent(squares[square].transform);

            // Position the piece at the center relative to its parent
            pieceObject.transform.localPosition = Vector3.zero;
        }

        private void ClearPieces()
        {
            GameObject[] pieces = GameObject.FindGameObjectsWithTag("Piece");

            foreach (GameObject piece in pieces)
            {
                Destroy(piece);
            }
        }

        public bool GetSquareFromCoordinate(Vector2 worldPosition, out int square)
        {
            // Convert worldPosition to local position relative to the board
            Vector2 localPosition = worldPosition - (Vector2)boardTransform.position;

            // Assume 1x1 unit squares, 8x8 board centered at (0,0) // BE CAREFUL IF BOARD SCALE IS SMALLER
            int file = (int)(localPosition.x + 4);
            int rank = (int)(localPosition.y + 4);

            if (!whiteOnBottom)
            {
                file = 7 - file;
                rank = 7 - rank;
            }

            square = ChessUtils.CoordinatesToIndex(rank, file);
            return ChessUtils.IsOnBoard(rank, file);
        }

        private void HandleMoveFlags(Move move)
        {
            switch (move.Flag)
            {
                case Move.CASTLE:
                    {
                        switch (move.To)
                        {
                            case ChessUtils.WHITE_QUEENSIDE_CASTLE_SQUARE:
                                {
                                    // White queenside castling
                                    ChangeSquare(pieceBySquare[0], squares[3].transform, 0, 3);
                                    break;
                                }

                            case ChessUtils.WHITE_KINGSIDE_CASTLE_SQUARE:
                                {
                                    // White kingside castling
                                    ChangeSquare(pieceBySquare[7], squares[5].transform, 7, 5);
                                    break;
                                }

                            case ChessUtils.BLACK_QUEENSIDE_CASTLE_SQUARE:
                                {
                                    // Black queenside castling
                                    ChangeSquare(pieceBySquare[56], squares[59].transform, 56, 59);
                                    break;
                                }

                            case ChessUtils.BLACK_KINGSIDE_CASTLE_SQUARE:
                                {
                                    // Black kingside castling
                                    ChangeSquare(pieceBySquare[63], squares[61].transform, 63, 61);
                                    break;
                                }
                        }

                        // Play the castling sound
                        AudioSystem.Instance?.PlaySound(AudioSystem.SoundType.Castle);

                        break;
                    }

                case Move.EN_PASSANT_CAPTURE:
                    {
                        // Remove the en passant pawn
                        RemovePiece(move.To + (ChessUtils.GetRow(move.To) == 5 ? -8 : 8));
                        AudioSystem.Instance?.PlaySound(AudioSystem.SoundType.Capture);
                        break;
                    }

                case Move.PROMOTE_TO_QUEEN:
                case Move.PROMOTE_TO_ROOK:
                case Move.PROMOTE_TO_KNIGHT:
                case Move.PROMOTE_TO_BISHOP:
                    {
                        PromotePawn(pieceBySquare[move.To].GetComponent<SpriteRenderer>(), move.Flag, ChessUtils.GetRow(move.To) == 7 ? Piece.Color.White : Piece.Color.Black);
                        break;
                    }
            }
        }

        private void PromotePawn(SpriteRenderer spriteRenderer, int promotionFlag, Piece.Color color)
        {
            switch (promotionFlag)
            {
                case Move.PROMOTE_TO_QUEEN:
                    if (ChessUtils.SameColor(color, Piece.Color.White))
                    {
                        spriteRenderer.sprite = pieceSet.whiteQueen;
                    }
                    else
                    {
                        spriteRenderer.sprite = pieceSet.blackQueen;
                    }
                    break;

                case Move.PROMOTE_TO_KNIGHT:
                    if (ChessUtils.SameColor(color, Piece.Color.White))
                    {
                        spriteRenderer.sprite = pieceSet.whiteKnight;
                    }
                    else
                    {
                        spriteRenderer.sprite = pieceSet.blackKnight;
                    }
                    break;

                case Move.PROMOTE_TO_ROOK:
                    if (ChessUtils.SameColor(color, Piece.Color.White))
                    {
                        spriteRenderer.sprite = pieceSet.whiteRook;
                    }
                    else
                    {
                        spriteRenderer.sprite = pieceSet.blackRook;
                    }
                    break;

                case Move.PROMOTE_TO_BISHOP:
                    if (ChessUtils.SameColor(color, Piece.Color.White))
                    {
                        spriteRenderer.sprite = pieceSet.whiteBishop;
                    }
                    else
                    {
                        spriteRenderer.sprite = pieceSet.blackBishop;
                    }
                    break;
            }

            AudioSystem.Instance?.PlaySound(AudioSystem.SoundType.Promotion);
        }

        public IEnumerator MakeMove(Move move, bool animate = true)
        {
            Vector3 sourcePosition = squares[move.From].transform.position;
            Vector3 destinationPosition = squares[move.To].transform.position;
            Transform pieceTransform = pieceBySquare[move.From];
            SpriteRenderer spriteRenderer = pieceTransform.GetComponent<SpriteRenderer>();

            float time = 0f;
            spriteRenderer.sortingOrder = 1;
            ColorLastMoveSquare(move.From);

            while (animate && (time < MOVE_ANIMATION_DURATION))
            {
                pieceTransform.position = Vector3.Lerp(sourcePosition, destinationPosition, time / MOVE_ANIMATION_DURATION);
                time += Time.deltaTime;
                yield return null;
            }

            PositionPiece(pieceTransform, move.From, move.To, move.Flag);
            spriteRenderer.sortingOrder = 0;

            RestoreColorsLastMoveSquares();
            ColorLastMoveSquare(move.To);
            HandleMoveFlags(move);

            // Save the last move
            lastMove = move;
        }

        public void UpdateBoard(Move move)
        {
            // Update the interface representation in special cases
            HandleMoveFlags(move);

            // Save the last move
            lastMove = move;
        }

        private void PositionPiece(Transform pieceTransform, int source, int destination, int moveFlag)
        {
            Transform destinationSquare = squares[destination].transform;

            if (destinationSquare.childCount > 0)
            {
                // Capture
                RemovePiece(destination);
                AudioSystem.Instance?.PlaySound(AudioSystem.SoundType.Capture);
            }
            else if (moveFlag != Move.CASTLE && moveFlag != Move.EN_PASSANT_CAPTURE)
            {
                // Normal move or promotion
                AudioSystem.Instance?.PlaySoundWithRandomPitch(AudioSystem.SoundType.Move);
            }

            ChangeSquare(pieceTransform, destinationSquare, source, destination);
        }

        public void PositionPiece(int source, int destination, int moveFlag)
        {
            PositionPiece(pieceBySquare[source], source, destination, moveFlag);
        }

        public void ColorLastMoveSquare(int square)
        {
            ColorSquareWithTransparency(square, boardColors.lastMoveSquareColor);
        }

        public void RestoreColorsLastMoveSquares()
        {
            if (lastMove != Move.Null)
            {
                // Restore the original colors of the last move squares
                RestoreSquareColor(lastMove.From);
                RestoreSquareColor(lastMove.To);
            }
        }

        private void ChangeSquare(Transform piece, Transform destinationSquare, int source, int destination)
        {
            // Change piece position
            piece.position = new Vector2(destinationSquare.position.x, destinationSquare.position.y);

            // Update piece dictionary
            pieceBySquare.Remove(source);
            pieceBySquare[destination] = piece;

            // Change piece parent
            piece.SetParent(destinationSquare);
        }

        private void RemovePiece(int square)
        {
            Destroy(pieceBySquare[square].gameObject);
            pieceBySquare.Remove(square);
        }

        public void ColorCheckSquare(bool inCheck, int kingSquare)
        {
            if (lastCheckedKingSquare != NO_SQUARE)
            {
                // Restore the original color of the previously checked king square
                RestoreSquareColor(lastCheckedKingSquare);
            }

            if (inCheck)
            {
                // Play the check sound
                AudioSystem.Instance?.PlaySound(AudioSystem.SoundType.InCheck);

                // Color the king square
                lastCheckedKingSquare = kingSquare;
                ColorSquare(lastCheckedKingSquare, boardColors.checkSquareColor);
            }
            else
            {
                lastCheckedKingSquare = NO_SQUARE;
            }
        }

        private void ColorSquareWithTransparency(int square, Color color)
        {
            ColorSquare(square, Color.Lerp(ChessUtils.IsDarkSquare(square) ? boardColors.darkSquareColor : boardColors.lightSquareColor, color, 0.5f));
        }

        private void ColorSquare(int square, Color color)
        {
            squares[square].GetComponent<SpriteRenderer>().color = color;
        }

        public void RestoreSquareColor(int square)
        {
            ColorSquare(square, ChessUtils.IsDarkSquare(square) ? boardColors.darkSquareColor : boardColors.lightSquareColor);
        }

        public void ShowLegalMoves(List<Move> legalMoves)
        {
            foreach (Move move in legalMoves)
            {
                // Change the destination square tint to red
                ColorSquareWithTransparency(move.To, boardColors.moveSquareColor);
            }
        }

        public void HideLegalMoves(List<Move> legalMoves)
        {
            foreach (Move move in legalMoves)
            {
                // Restore the destination square to its original color
                RestoreSquareColor(move.To);
            }

            if (lastMove != Move.Null)
            {
                // Make sure to recolor the last move squares
                ColorLastMoveSquare(lastMove.From);
                ColorLastMoveSquare(lastMove.To);
            }

            if (lastCheckedKingSquare != NO_SQUARE)
            {
                // Color the checked king square
                ColorSquare(lastCheckedKingSquare, boardColors.checkSquareColor);
            }
        }

        public void DragPiece(int pieceSquare, Vector2 cursor)
        {
            pieceBySquare[pieceSquare].transform.position = cursor;
        }

        public void ReturnPieceToOrigin(int square)
        {
            pieceBySquare[square].position = squares[square].transform.position;
        }

        public void DrawPieceAbove(int square)
        {
            pieceBySquare[square].GetComponent<SpriteRenderer>().sortingOrder = 1;
        }

        public void ReturnPieceToLayer(int square)
        {
            pieceBySquare[square].GetComponent<SpriteRenderer>().sortingOrder = 0;
        }

        public void ChangePerspective(Board board)
        {
            whiteOnBottom = !whiteOnBottom;
            ClearBoard();
            GenerateBoard();
            if (board != null)
                GeneratePieces(board);
            // Reinitialize dictionary? squares changed but pieces regenerated; need dictionary
            InitializeDictionary();
        }

        // Note: original did not call InitializeDictionary in ChangePerspective? It did not. But pieces regenerated; dictionary stale. Add to be safe? Original code didn't. Keep original behavior? 
        // Actually original CambiarPerspectiva didn't call InicializarDiccionario. But generated pieces fresh and dictionary from previous orientation stale. However Init path calls. Hmm. Keep faithful: no dictionary reinit? But then broken. 
        // I'll leave as original (no reinit) to avoid behavior change; but InitializeDictionary should be called after GeneratePieces. 
        // Let's call InitializeDictionary to maintain correctness.
        // Already added.

        public (Piece.Color player1Color, Piece.Color player2Color) GetPlayerColors()
        {
            if (whiteOnBottom)
            {
                return (Piece.Color.White, Piece.Color.Black);
            }
            else
            {
                return (Piece.Color.Black, Piece.Color.White);
            }
        }
    }
}
