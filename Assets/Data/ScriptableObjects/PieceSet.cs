using UnityEngine;

namespace Ajedrez.Data
{
    [CreateAssetMenu(fileName = "NewPieceSet", menuName = "Chess/Piece Set")]
    public class PieceSet : ScriptableObject
    {
        [Header("White Pieces")]
        public Sprite whiteKing;

        public Sprite whiteQueen;

        public Sprite whiteRook;

        public Sprite whiteBishop;

        public Sprite whiteKnight;

        public Sprite whitePawn;

        [Header("Black Pieces")]
        public Sprite blackKing;

        public Sprite blackQueen;

        public Sprite blackRook;

        public Sprite blackBishop;

        public Sprite blackKnight;

        public Sprite blackPawn;
    }
}
