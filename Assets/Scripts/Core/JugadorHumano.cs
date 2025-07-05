using Ajedrez.Managers;
using Ajedrez.Utilities;
using Ajedrez.UI;

namespace Ajedrez.Core
{
    public class JugadorHumano : Jugador
    {
        public JugadorHumano(string nombre, Pieza.Color color, float tiempoRestante)
            : base(nombre, color, tiempoRestante)
        {
        }
    }
}