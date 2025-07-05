using UnityEngine;

namespace Ajedrez.Data
{
    [CreateAssetMenu(fileName = "NuevosColoresDelTablero", menuName = "Ajedrez/Colores del Tablero")]
    public class ColoresTablero : ScriptableObject
    {
        public Color colorCasillaClara;
        public Color colorCasillaOscura;
        public Color colorCasillaUltimoMovimiento;
        public Color colorCasillaJaque;
        public Color colorCasillaMovimientos;
    }
}