using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Ajedrez.Managers;
using Ajedrez.Core;
using Ajedrez.Utilities;
using Ajedrez.IA;
using Ajedrez.UI;

namespace Ajedrez.Debugging
{
    public enum BitboardDebugMode : byte
    {
        Off,
        WhiteKing,
        WhiteQueens,
        WhiteRooks,
        WhiteBishops,
        WhiteKnights,
        WhitePawns,
        BlackKing,
        BlackQueens,
        BlackRooks,
        BlackBishops,
        BlackKnights,
        BlackPawns,
        PinnedPieces,
        KingAttackers,
        Pinners
    }

    public class BitboardDebugger : MonoBehaviour
    {
        public static BitboardDebugger Instance { get; private set; }

        private const string LIGHT_RED = "#00FFFF80";
        private const string CYAN = "#FF000080";

        [SerializeField] private Transform boardTransform;
        [SerializeField] private Sprite debugMarkSprite;
        [SerializeField] private TextMeshProUGUI debugModeText;

        private bool isInitialized = false;
        private Color bitboardColor1;
        private Color bitboardColor0;
        private Vector2 boardPosition;
        private Board board;
        private BitboardDebugMode currentMode = BitboardDebugMode.Off;

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

        private void OnEnable()
        {
            // Beware of script execution order
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnDebugBitboardKey += ChangeDebugBitboardMode;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnDebugBitboardKey -= ChangeDebugBitboardMode;
            }
        }

        void Start()
        {
            // Initialize board position
            boardPosition = boardTransform.position;

            // Load colors from hex
            if (!ColorUtility.TryParseHtmlString(LIGHT_RED, out bitboardColor1))
                Debug.LogError("Error converting LIGHT_RED to Color.");

            if (!ColorUtility.TryParseHtmlString(CYAN, out bitboardColor0))
                Debug.LogError("Error converting CYAN to Color.");
        }

        public void Init(Board board)
        {
            this.board = board;
            isInitialized = true;
        }

        public void UpdateDebugBitboardMode()
        {
            if (currentMode != BitboardDebugMode.Off)
            {
                ShowDebugBitboardMode(currentMode);
            }
        }

        private void ShowDebugBitboardMode(BitboardDebugMode mode)
        {
            if (!isInitialized)
                return;

            switch (mode)
            {
                case BitboardDebugMode.Off:
                    {
                        ClearDebug();
                        debugModeText.text = "";
                        break;
                    }

                case BitboardDebugMode.WhiteKing:
                    {
                        ShowDebugBitboard(board.WhiteKingBitboard);
                        break;
                    }

                case BitboardDebugMode.WhiteQueens:
                    {
                        ShowDebugBitboard(board.WhiteQueensBitboard);
                        break;
                    }

                case BitboardDebugMode.WhiteRooks:
                    {
                        ShowDebugBitboard(board.WhiteRooksBitboard);
                        break;
                    }

                case BitboardDebugMode.WhiteBishops:
                    {
                        ShowDebugBitboard(board.WhiteBishopsBitboard);
                        break;
                    }

                case BitboardDebugMode.WhiteKnights:
                    {
                        ShowDebugBitboard(board.WhiteKnightsBitboard);
                        break;
                    }

                case BitboardDebugMode.WhitePawns:
                    {
                        ShowDebugBitboard(board.WhitePawnsBitboard);
                        break;
                    }

                case BitboardDebugMode.BlackKing:
                    {
                        ShowDebugBitboard(board.BlackKingBitboard);
                        break;
                    }

                case BitboardDebugMode.BlackQueens:
                    {
                        ShowDebugBitboard(board.BlackQueensBitboard);
                        break;
                    }

                case BitboardDebugMode.BlackRooks:
                    {
                        ShowDebugBitboard(board.BlackRooksBitboard);
                        break;
                    }

                case BitboardDebugMode.BlackBishops:
                    {
                        ShowDebugBitboard(board.BlackBishopsBitboard);
                        break;
                    }

                case BitboardDebugMode.BlackKnights:
                    {
                        ShowDebugBitboard(board.BlackKnightsBitboard);
                        break;
                    }

                case BitboardDebugMode.BlackPawns:
                    {
                        ShowDebugBitboard(board.BlackPawnsBitboard);
                        break;
                    }

                case BitboardDebugMode.PinnedPieces:
                    {
                        ShowDebugBitboard(board.GetPinnedPiecesDebug());
                        break;
                    }

                case BitboardDebugMode.KingAttackers:
                    {
                        ShowDebugBitboard(board.GetKingAttackersDebug());
                        break;
                    }

                case BitboardDebugMode.Pinners:
                    {
                        ShowDebugBitboard(board.GetPinnersDebug());
                        break;
                    }
            }
        }

        private void ChangeDebugBitboardMode(BitboardDebugMode newMode)
        {
            if (currentMode != newMode)
            {
                currentMode = newMode;
                ShowDebugBitboardMode(newMode);
            }
        }

        private void ClearDebug()
        {
            GameObject[] debugObjects = GameObject.FindGameObjectsWithTag("Debug");

            foreach (GameObject obj in debugObjects)
            {
                Destroy(obj);
            }
        }

        private string FormatBitboard(ulong bitboard)
        {
            var sb = new System.Text.StringBuilder(64 * 40); // more room for tags
            for (int i = 63; i >= 0; i--)
            {
                bool bit = (bitboard & BitboardUtils.SetBit(i)) != 0;
                string color = bit ? LIGHT_RED : CYAN;
                sb.Append($"<color={color}>{(bit ? '1' : '0')}</color>");

                // Optional: visually separate every 8 bits
                if (i % 8 == 0 && i != 0)
                    sb.Append(" ");
            }
            return sb.ToString();
        }

        private void ShowDebugBitboard(ulong bitboard)
        {
            ClearDebug();
            debugModeText.text = $"{currentMode}\n{FormatBitboard(bitboard)}";

            for (int square = 0; square < ChessUtils.BOARD_SQUARE_COUNT; square++)
            {
                (int i, int j) = ChessUtils.IndexToCoordinates(square);
                Vector2 position = new Vector2(-3.5f + j, -3.5f + i);

                // Check if the bit is set on that square
                if ((bitboard & BitboardUtils.SetBit(square)) != 0)
                {
                    InstantiateDebugSquare(boardPosition + position, 1, bitboardColor1);
                }
                else
                {
                    InstantiateDebugSquare(boardPosition + position, 0, bitboardColor0);
                }
            }
        }

        public void InstantiateDebugSquare(Vector2 position, int number, Color color)
        {
            GameObject debugMark = new GameObject("DebugBitboard");

            // Assign the "Debug" tag
            debugMark.tag = "Debug";

            // SpriteRenderer for the square
            SpriteRenderer sr = debugMark.AddComponent<SpriteRenderer>();
            sr.sprite = debugMarkSprite;
            sr.color = color;
            sr.sortingLayerName = "Debug";
            sr.sortingOrder = 0;
            debugMark.transform.position = position;

            // Create the text as a child
            GameObject text = new GameObject("Number");
            text.tag = "Debug";
            text.transform.SetParent(debugMark.transform);
            text.transform.localPosition = new Vector3(0, 0, -0.1f);

            // TextMesh component
            TextMesh textMesh = text.AddComponent<TextMesh>();
            textMesh.text = number.ToString();
            textMesh.fontSize = 100;
            textMesh.characterSize = 0.05f;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.color = Color.white;

            // Make it render on top of the square
            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Debug";
            renderer.sortingOrder = 1;
        }
    }
}