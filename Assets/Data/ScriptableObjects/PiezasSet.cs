using UnityEngine;

namespace Ajedrez.Data
{
    [CreateAssetMenu(fileName = "NuevoSetDePiezas", menuName = "Ajedrez/Set de Piezas")]
    public class PiezasSet : ScriptableObject
    {
        [Header("Piezas Blancas")]
        public Sprite reyBlanco;
        public Sprite reinaBlanca;
        public Sprite torreBlanca;
        public Sprite alfilBlanco;
        public Sprite caballoBlanco;
        public Sprite peonBlanco;

        [Header("Piezas Negras")]
        public Sprite reyNegro;
        public Sprite reinaNegra;
        public Sprite torreNegra;
        public Sprite alfilNegro;
        public Sprite caballoNegro;
        public Sprite peonNegro;
    }
}