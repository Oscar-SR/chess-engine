using System;
using System.Collections.Generic;
using System.Linq;
using Ajedrez.Core;

namespace Ajedrez.IA
{
    public class OrdenadorMovimientos
    {
        public const int MAX_NUM_MOVIMIENTOS = 218;

        private Tablero tablero;
        private int[] puntuacionesMovimientos;

        public OrdenadorMovimientos(Tablero tablero)
        {
            this.tablero = tablero;
            puntuacionesMovimientos = new int[MAX_NUM_MOVIMIENTOS];
        }

        public void OrdenarMovimientos(List<Movimiento> movimientos)
        {
            int count = movimientos.Count;

            // Calcular puntuaciones
            for (int i = 0; i < count; i++)
            {
                Movimiento movimiento = movimientos[i];
                int puntuacion = 0;
                Pieza piezaOrigen = tablero.ObtenerPieza(movimiento.Origen);
                Pieza piezaCapturada = tablero.ObtenerPieza(movimiento.Destino);

                // Priorizar la captura de piezas más valiosas que la que se mueve
                if (piezaOrigen.TipoPieza != Pieza.Tipo.Nada)
                {
                    puntuacion = 10 * Evaluacion.ObtenerValorPieza(piezaCapturada.TipoPieza) - Evaluacion.ObtenerValorPieza(piezaOrigen.TipoPieza);
                }

                // Priorizar las promociones
                if (movimiento.EsPromocion())
                {
                    puntuacion += Evaluacion.ObtenerValorPromocion(movimiento.Flag);
                }

                // Penalizar el mover hacia una casilla atacada por un peón rival
                if (tablero.CasillaAtacadaPorPeonRival(movimiento.Destino))
                {
                    puntuacion -= Evaluacion.ObtenerValorPieza(piezaOrigen.TipoPieza);
                }

                puntuacionesMovimientos[i] = puntuacion;
            }

            Ordenar(movimientos, count);
        }

        private void Ordenar(List<Movimiento> movimientos, int count)
        {
            puntuacionesMovimientos = puntuacionesMovimientos.Take(count).ToArray();
            // Ordenar el array de puntuaciones
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => puntuacionesMovimientos[b].CompareTo(puntuacionesMovimientos[a]));

            // Reordenar lista original en base al array de puntuaciones
            List<Movimiento> movimientosOrdenados = new List<Movimiento>(count);
            for (int i = 0; i < count; i++)
            {
                movimientosOrdenados.Add(movimientos[indices[i]]);
            }

            movimientos.Clear();
            movimientos.AddRange(movimientosOrdenados);
        }
    }
}