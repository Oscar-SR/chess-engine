using System;
using Ajedrez.Core;

namespace Ajedrez.IA
{
    public class GestorTiempo
    {
        public const float FACTOR_INCREMENTO = 0.6f;
        private Reloj reloj;
        private Pieza.Color colorPiezas;
        private readonly float incrementoEmpleado;

        public GestorTiempo(Reloj reloj, Pieza.Color colorPiezas)
        {
            this.reloj = reloj;
            this.colorPiezas = colorPiezas;
            incrementoEmpleado = reloj.IncrementoPorMovimiento * FACTOR_INCREMENTO;
        }

        public Pieza.Color ColorPiezas
        {
            get { return colorPiezas; }
            set { colorPiezas = value; }
        }

        public int CalcularTiempoBusqueda(uint movimientoNumero)
        {
            const float FRACCION_MAX_TIEMPO = 0.1f; // 10%
            const float MIN_SEGUNDOS_BUSQUEDA = 0.05f; // 50 ms

            int movimientosEsperados = EstimarMovimientosRestantes(movimientoNumero);
            float tiempoRestante = TiempoRestante;
            float tiempoOptimo = TiempoRestante / movimientosEsperados + incrementoEmpleado;
            float tiempoMaximo = TiempoRestante * FRACCION_MAX_TIEMPO;

            // Limita el tiempo a no usar más del 10% del tiempo restante
            tiempoOptimo = Math.Min(tiempoOptimo, tiempoMaximo);

            // Garantiza un mínimo
            tiempoOptimo = Math.Max(tiempoOptimo, MIN_SEGUNDOS_BUSQUEDA);

            // Convierte a milisegundos y redondea
            //UnityEngine.Debug.Log(tiempoRestante + " " + tiempoOptimo);
            return (int)Math.Round(tiempoOptimo * 1000);
        }

        private int EstimarMovimientosRestantes(uint movimientoNumero)
        {
            if (movimientoNumero < 20)
                return 40;
            else if (movimientoNumero < 40)
                return 20;
            else
                return 10;
        }

        private float TiempoRestante
        {
            get
            {
                if (colorPiezas == Pieza.Color.Blancas)
                {
                    return reloj.TiempoRestanteBlancas;
                }
                else
                {
                    return reloj.TiempoRestanteNegras;
                }
            }
        }
    }
}