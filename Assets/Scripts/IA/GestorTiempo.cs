using System;

namespace Ajedrez.IA
{
    public class GestorTiempo
    {
        public const float FACTOR_INCREMENTO = 0.6f;
        private readonly float incremento; // En segundos
        private readonly float incrementoEmpleado;

        public GestorTiempo(float incremento)
        {
            this.incremento = incremento;
            incrementoEmpleado = incremento * FACTOR_INCREMENTO;
        }

        public int CalcularTiempoBusqueda(float tiempoRestante, uint movimientoNumero)
        {
            const float FRACCION_MAX_TIEMPO = 0.1f; // 10%
            const float MIN_SEGUNDOS_BUSQUEDA = 0.05f; // 50 ms

            int movimientosEsperados = EstimarMovimientosRestantes(movimientoNumero);
            float tiempoOptimo = tiempoRestante / movimientosEsperados + incrementoEmpleado;
            float tiempoMaximo = tiempoRestante * FRACCION_MAX_TIEMPO;

            // Limita el tiempo a no usar más del 10% del tiempo restante
            tiempoOptimo = Math.Min(tiempoOptimo, tiempoMaximo);

            // Garantiza un mínimo
            tiempoOptimo = Math.Max(tiempoOptimo, MIN_SEGUNDOS_BUSQUEDA);

            // Convierte a milisegundos y redondea
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
    }
}