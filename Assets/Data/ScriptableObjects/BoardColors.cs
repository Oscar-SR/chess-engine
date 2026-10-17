using UnityEngine;
using UnityEngine.Serialization;

namespace Ajedrez.Data
{
    [CreateAssetMenu(fileName = "NewBoardColors", menuName = "Ajedrez/Board Colors")]
    public class BoardColors : ScriptableObject
    {
        [FormerlySerializedAs("colorCasillaClara")]
        public Color lightSquareColor;

        [FormerlySerializedAs("colorCasillaOscura")]
        public Color darkSquareColor;

        [FormerlySerializedAs("colorCasillaUltimoMovimiento")]
        public Color lastMoveSquareColor;

        [FormerlySerializedAs("colorCasillaJaque")]
        public Color checkSquareColor;

        [FormerlySerializedAs("colorCasillaMovimientos")]
        public Color moveSquareColor;
    }
}