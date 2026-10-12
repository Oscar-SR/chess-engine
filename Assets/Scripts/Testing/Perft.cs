using System.Collections.Generic;
using System.Threading;
using Ajedrez.Core;

namespace Ajedrez.Testing
{
    public static class Perft
    {
        public static int Run(int profundidad, Tablero tablero, CancellationToken cancellationToken)
        {
            if (profundidad == 0)
                return 1;

            (List<Move> movimientos, _) = tablero.GenerarMovimientosLegales();
            int numPosiciones = 0;

            foreach (Move movimiento in movimientos)
            {                
                tablero.HacerMovimiento(movimiento);
                numPosiciones += Run(profundidad - 1, tablero, cancellationToken);
                tablero.DeshacerMovimiento(movimiento);

                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return numPosiciones;
        }
    }
}