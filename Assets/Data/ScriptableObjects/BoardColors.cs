using UnityEngine;

namespace Chess.Data
{
    [CreateAssetMenu(fileName = "NewBoardColors", menuName = "Chess/Board Colors")]
    public class BoardColors : ScriptableObject
    {
        public Color lightSquareColor;

        public Color darkSquareColor;

        public Color lastMoveSquareColor;

        public Color checkSquareColor;

        public Color moveSquareColor;
    }
}