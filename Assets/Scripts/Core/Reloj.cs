using System;

namespace Ajedrez.Core
{
    public class Reloj
    {
        private readonly float duracionInicial;
        private readonly float incrementoPorMovimiento;
        private float tiempoRestanteBlancas;
        private float tiempoRestanteNegras;

        public Reloj(float duracionInicial, float incremento)
        {
            this.duracionInicial = tiempoRestanteBlancas = tiempoRestanteNegras = duracionInicial;
            incrementoPorMovimiento = incremento;
        }

        public float DuracionInicial
        {
            get => duracionInicial;
        }

        public float IncrementoPorMovimiento
        {
            get => incrementoPorMovimiento;
        }

        public float TiempoRestanteBlancas
        {
            get => tiempoRestanteBlancas;
            private set => tiempoRestanteBlancas = Math.Max(0, value);
        }

        public float TiempoRestanteNegras
        {
            get => tiempoRestanteNegras;
            private set => tiempoRestanteNegras = Math.Max(0, value);
        }

        public void ConsumirTiempoBlancas(float tiempoConsumido)
        {
            TiempoRestanteBlancas -= tiempoConsumido;
        }

        public void AplicarIncrementoBlancas()
        {
            TiempoRestanteBlancas += incrementoPorMovimiento;
        }

        public void ConsumirTiempoNegras(float tiempoConsumido)
        {
            TiempoRestanteNegras -= tiempoConsumido;
        }

        public void AplicarIncrementoNegras()
        {
            TiempoRestanteNegras += incrementoPorMovimiento;
        }

        public bool TiempoAgotadoBlancas => tiempoRestanteBlancas <= 0;
        public bool TiempoAgotadoNegras => tiempoRestanteNegras <= 0;
    }
}