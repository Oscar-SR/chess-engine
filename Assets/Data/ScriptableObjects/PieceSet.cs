using UnityEngine;
using UnityEngine.Serialization;

namespace Ajedrez.Data
{
    [CreateAssetMenu(fileName = "NewPieceSet", menuName = "Chess/Piece Set")]
    public class PieceSet : ScriptableObject
    {
        [Header("White Pieces")]
        [FormerlySerializedAs("reyBlanco")]
        public Sprite whiteKing;

        [FormerlySerializedAs("reinaBlanca")]
        public Sprite whiteQueen;

        [FormerlySerializedAs("torreBlanca")]
        public Sprite whiteRook;

        [FormerlySerializedAs("alfilBlanco")]
        public Sprite whiteBishop;

        [FormerlySerializedAs("caballoBlanco")]
        public Sprite whiteKnight;

        [FormerlySerializedAs("peonBlanco")]
        public Sprite whitePawn;

        [Header("Black Pieces")]
        [FormerlySerializedAs("reyNegro")]
        public Sprite blackKing;

        [FormerlySerializedAs("reinaNegra")]
        public Sprite blackQueen;

        [FormerlySerializedAs("torreNegra")]
        public Sprite blackRook;

        [FormerlySerializedAs("alfilNegro")]
        public Sprite blackBishop;

        [FormerlySerializedAs("caballoNegro")]
        public Sprite blackKnight;

        [FormerlySerializedAs("peonNegro")]
        public Sprite blackPawn;
    }
}
