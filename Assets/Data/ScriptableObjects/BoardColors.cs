using UnityEngine;

namespace Ajedrez.Data
{
    [CreateAssetMenu(fileName = "NewBoardColors", menuName = "Ajedrez/Board Colors")]
    public class BoardColors : ScriptableObject
    {
        public Color lightSquareColor;

        public Color darkSquareColor;

        public Color lastMoveSquareColor;

        public Color checkSquareColor;

        public Color moveSquareColor;
    }
}