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

        public void OrdenarMovimientos(List<Move> movimientos)
        {
            int count = movimientos.Count;

            // Calcular puntuaciones
            for (int i = 0; i < count; i++)
            {
                Move movimiento = movimientos[i];
                int puntuacion = 0;
                Piece piezaOrigen = tablero.ObtenerPieza(movimiento.From);
                Piece piezaCapturada = tablero.ObtenerPieza(movimiento.To);

                // Priorizar la captura de piezas más valiosas que la que se mueve
                if (piezaOrigen.PieceType != Piece.Type.None)
                {
                    puntuacion = 10 * Evaluacion.ObtenerValorPieza(piezaCapturada.PieceType) - Evaluacion.ObtenerValorPieza(piezaOrigen.PieceType);
                }

                // Priorizar las promociones
                if (movimiento.IsPromotion())
                {
                    puntuacion += Evaluacion.ObtenerValorPromocion(movimiento.Flag);
                }

                // Penalizar el mover hacia una casilla atacada por un peón rival
                if (tablero.CasillaAtacadaPorPeonRival(movimiento.To))
                {
                    puntuacion -= Evaluacion.ObtenerValorPieza(piezaOrigen.PieceType);
                }

                puntuacionesMovimientos[i] = puntuacion;
            }

            Ordenar(movimientos, count);
        }

        private void Ordenar(List<Move> movimientos, int count)
        {
            puntuacionesMovimientos = puntuacionesMovimientos.Take(count).ToArray();
            // Ordenar el array de puntuaciones
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => puntuacionesMovimientos[b].CompareTo(puntuacionesMovimientos[a]));

            // Reordenar lista original en base al array de puntuaciones
            List<Move> movimientosOrdenados = new List<Move>(count);
            for (int i = 0; i < count; i++)
            {
                movimientosOrdenados.Add(movimientos[indices[i]]);
            }

            movimientos.Clear();
            movimientos.AddRange(movimientosOrdenados);
        }
    }
}