using UnityEngine;
using TMPro;
using Ajedrez.Utilities;
using Ajedrez.Core;
using Ajedrez.IA;

namespace Ajedrez.Managers
{
    [ExecuteInEditMode]
    public class PieceMapsManager : MonoBehaviour
    {
        [SerializeField] private Transform pieceMaps;
        [SerializeField] private Color minColor;
        [SerializeField] private Color maxColor;
        [SerializeField] private Piece.Type pieceType = Piece.Type.Pawn;
        [SerializeField, Range(0f, 1f)] private float endgameValue;
        [SerializeField] private string output;

        private int[] map;
        private bool needsUpdate = false;

        private void OnValidate()
        {
            map = GetMap();
            output = GetOutput();

            // Mark to regenerate the map in Update
            needsUpdate = true;
        }

        private void Update()
        {
            if (needsUpdate)
            {
                ClearPreviousSquares(); // Clear previous ones
                GenerateMap();
                needsUpdate = false;
            }
        }

        private int[] GetMap()
        {
            int[] map;

            switch (pieceType)
            {
                case Piece.Type.Pawn:
                    {
                        map = CalculateTransitionMatrix(PieceMaps.Pawns, PieceMaps.PawnsEndgame, endgameValue);
                        break;
                    }

                case Piece.Type.Knight:
                    {
                        map = PieceMaps.Knights;
                        break;
                    }

                case Piece.Type.Bishop:
                    {
                        map = PieceMaps.Bishops;
                        break;
                    }

                case Piece.Type.Rook:
                    {
                        map = PieceMaps.Rooks;
                        break;
                    }

                case Piece.Type.Queen:
                    {
                        map = PieceMaps.Queens;
                        break;
                    }
                case Piece.Type.King:
                    {
                        map = CalculateTransitionMatrix(PieceMaps.King, PieceMaps.KingEndgame, endgameValue);
                        break;
                    }

                default:
                    map = null;
                    break;
            }

            return map;
        }

        private string GetOutput()
        {
            if (pieceType == Piece.Type.None)
                return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < ChessUtils.BOARD_SIZE; i++)
            {
                for (int j = 0; j < ChessUtils.BOARD_SIZE; j++)
                {
                    sb.Append(map[i * ChessUtils.BOARD_SIZE + j]);
                    if (j < ChessUtils.BOARD_SIZE - 1) sb.Append(", ");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void ClearPreviousSquares()
        {
            for (int i = pieceMaps.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(pieceMaps.GetChild(i).gameObject);
            }
        }

        private void GenerateMap()
        {
            if (map == null)
                return;

            int min = map[0];
            int max = map[0];

            // Find the minimum and maximum value
            for (int i = 1; i < ChessUtils.BOARD_SQUARE_COUNT; i++)
            {
                int value = map[i];
                if (value < min) min = value;
                else if (value > max) max = value;
            }

            for (int i = 0; i < ChessUtils.BOARD_SIZE; i++)
            {
                for (int j = 0; j < ChessUtils.BOARD_SIZE; j++)
                {
                    Vector2 position = new Vector2(j - 3.5f, 3.5f - i);
                    int value = map[ChessUtils.CoordinatesToIndex(i, j)];

                    // Normalize the value to [0, 1]
                    float normalizedValue = (float)(value - min) / (max - min);

                    // Interpolate between colorMin and colorMax
                    Color color = Color.Lerp(minColor, maxColor, normalizedValue);

                    InstantiateSquare(position, color, value);
                }
            }
        }

        private void InstantiateSquare(Vector2 position, Color color, int value)
        {
            GameObject square = new GameObject("Square");
            square.transform.parent = pieceMaps;
            square.transform.position = new Vector3(position.x, position.y, 0f);
            square.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            MeshFilter mf = square.AddComponent<MeshFilter>();
            MeshRenderer mr = square.AddComponent<MeshRenderer>();

            // Use Unity's built-in quad
            mf.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            // Dynamic material
            Shader shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader);
            material.color = color;
            mr.material = material;

            // --- Add TextMeshPro ---
            GameObject textGO = new GameObject("Value");
            textGO.transform.SetParent(square.transform);
            textGO.transform.localPosition = Vector3.zero;
            textGO.transform.localScale = Vector3.one;

            TextMeshPro tmp = textGO.AddComponent<TextMeshPro>();
            tmp.text = value.ToString();
            tmp.fontSize = 5;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static int[] CalculateTransitionMatrix(int[] initialMap, int[] finalMap, float endgameValue)
        {
            int[] result = new int[64];

            for (int i = 0; i < 64; i++)
            {
                result[i] = PieceMaps.CalculateInterpolation(initialMap[i], finalMap[i], endgameValue);
            }

            return result;
        }
    }
}
