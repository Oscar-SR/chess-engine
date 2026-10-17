using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Ajedrez.Managers;
using Ajedrez.Core;
using Ajedrez.Utilities;
using Ajedrez.Debugging;

namespace Ajedrez.UI
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        public event Action<BitboardDebugMode> OnDebugBitboardKey;
        public event Action OnToggleBoardState;
        public event Action OnToggleSearchDebug;

        // Communicate with the board through events too, perhaps

        [SerializeField] private GameManager gameManager;
        [SerializeField] private PromotionUI promotionUI;
        [SerializeField] private BoardUI boardUI;

        private Mouse cursor;
        private Keyboard keyboard;
        private Camera cam;
        private LayerMask pieceLayer;
        private int square;

        private Piece.Color interactableColor = Piece.Color.None;
        private bool selectedPiece = false;
        private bool promotionInProgress = false;
        private Vector2 pieceCursorOffset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            cam = Camera.main;
            pieceLayer = LayerMask.GetMask("Piezas");
            cursor = Mouse.current;
            keyboard = Keyboard.current;

            if (cursor == null)
            {
                Debug.LogError("No cursor device detected");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (interactableColor != Piece.Color.None)
            {
                if (selectedPiece)
                {
                    if (!promotionInProgress)
                    {
                        // Follow the mouse with the piece
                        gameManager.DragPiece(square, CursorPosition());

                        if (cursor.leftButton.wasReleasedThisFrame)
                        {
                            // Check whether the piece can be placed
                            PlacePiece();
                        }
                    }
                }
                else
                {
                    if (!promotionInProgress && cursor.leftButton.wasPressedThisFrame)
                    {
                        // Check whether a piece has been selected
                        SelectPiece();
                    }
                }
            }

#if UNITY_EDITOR
            // BitboardDebugger
            if (keyboard.qKey.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(BitboardDebugMode.Off);
            if (keyboard.digit1Key.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(keyboard.tabKey.isPressed ? BitboardDebugMode.BlackKing : BitboardDebugMode.WhiteKing);
            if (keyboard.digit2Key.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(keyboard.tabKey.isPressed ? BitboardDebugMode.BlackQueens : BitboardDebugMode.WhiteQueens);
            if (keyboard.digit3Key.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(keyboard.tabKey.isPressed ? BitboardDebugMode.BlackRooks : BitboardDebugMode.WhiteRooks);
            if (keyboard.digit4Key.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(keyboard.tabKey.isPressed ? BitboardDebugMode.BlackBishops : BitboardDebugMode.WhiteBishops);
            if (keyboard.digit5Key.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(keyboard.tabKey.isPressed ? BitboardDebugMode.BlackKnights : BitboardDebugMode.WhiteKnights);
            if (keyboard.digit6Key.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(keyboard.tabKey.isPressed ? BitboardDebugMode.BlackPawns : BitboardDebugMode.WhitePawns);
            if (keyboard.cKey.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(BitboardDebugMode.PinnedPieces);
            if (keyboard.rKey.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(BitboardDebugMode.KingAttackers);
            if (keyboard.pKey.wasPressedThisFrame)
                OnDebugBitboardKey?.Invoke(BitboardDebugMode.Pinners);

            // StateDebugger
            if (keyboard.eKey.wasPressedThisFrame)
                OnToggleBoardState?.Invoke();

            // SearchDebugger
            if (keyboard.bKey.wasPressedThisFrame)
                OnToggleSearchDebug?.Invoke();
#endif
        }

        private Vector2 CursorPosition()
        {
            Vector2 cursorScreenPosition = cursor.position.ReadValue();
            return cam.ScreenToWorldPoint(cursorScreenPosition);
        }

        void SelectPiece()
        {
            Vector2 clickPosition = CursorPosition();

            if (gameManager.GetSquareFromCoordinate(clickPosition, out square))
            {
                Piece piece = gameManager.GetPiece(square);
                if (piece.PieceType != Piece.Type.None && ChessUtils.SameColor(piece.PieceColor, interactableColor))
                {
                    selectedPiece = true;
                    gameManager.DrawPieceAbove(square);
                    gameManager.HighlightLastMoveSquare(square);
                    gameManager.ShowLegalMovesForPiece(square);
                }
            }
        }

        private void PlacePiece()
        {
            Vector2 clickPosition = CursorPosition();
            int newSquare;

            if (gameManager.GetSquareFromCoordinate(clickPosition, out newSquare))
            {
                if (newSquare != square)
                {
                    Move move = gameManager.GetLegalMove(newSquare);

                    // Check whether the move can be made
                    if (move != Move.Null)
                    {
                        // The move can be made
                        gameManager.PositionPiece(square, newSquare, move.Flag);
                        gameManager.HideLegalMoves();
                        gameManager.ReturnPieceToLayer(newSquare);
                        gameManager.RestoreLastMoveSquareColors();
                        gameManager.HighlightLastMoveSquare(newSquare);
                        StartCoroutine(MakeMoveAfterPromotion(move));
                        return;
                    }
                }
            }

            DeselectPiece();
        }

        private void DeselectPiece()
        {
            gameManager.RestoreSquareColor(square);
            gameManager.HideLegalMoves();
            gameManager.ReturnPieceToLayer(square);
            gameManager.ReturnPieceToOrigin(square);
            selectedPiece = false;
        }

        private IEnumerator MakeMoveAfterPromotion(Move move)
        {
            // Disable piece interaction
            interactableColor = Piece.Color.None;
            selectedPiece = false;

            if (move.IsPromotion())
            {
                promotionInProgress = true;
                int promotionType = Move.PROMOTE_TO_QUEEN;

                promotionUI.Show(gameManager.GetPiece(square).PieceColor, type =>
                {
                    promotionType = type;
                    promotionInProgress = false;
                });

                yield return new WaitUntil(() => !promotionInProgress);

                move = new Move(move.From, move.To, promotionType);
            }

            StartCoroutine(gameManager.MakeMove(move));
        }

        public Piece.Color InteractableColor
        {
            set
            {
                interactableColor = value;

                if (value == Piece.Color.None && selectedPiece)
                {
                    // Release the piece if the game ends and a piece is selected
                    DeselectPiece();
                }
            }
        }
    }
}