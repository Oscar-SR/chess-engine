using Ajedrez.Utilities;

namespace Ajedrez.Core
{
    public static class SituacionPartida
    {
        public enum Tipo : byte
        {
            EnCurso,
            JaqueMateBlancas,
            JaqueMateNegras,
            ReyAhogado,
            TriplePosicionRepetida,
            Regla50Movimientos,
            MaterialInsuficiente,
            TiempoAgotadoBlancas,
            TiempoAgotadoNegras,
            TablasPorArbitro
        }

        public static Tipo ObtenerSituacionPartida(Tablero tablero, int numMovimientosLegales, bool jaque)
        {
            Tipo situacionPartida = Tipo.EnCurso;

            if (tablero.EstadoActual.NumPlysInactivo >= Tablero.MAX_PLYS_INACTIVO)
            {
                // Regla de los 50 movimientos
                situacionPartida = Tipo.Regla50Movimientos;
            }
            else if (MaterialInsuficiente(tablero))
            {
                // Material insuficiente
                situacionPartida = Tipo.MaterialInsuficiente;
            }
            else if (tablero.HistorialPosicionesRepetidas.TripleRepeticion(tablero.EstadoActual.ZobristHash))
            {
                // El último movimiento realizado supuso una triple repetición
                situacionPartida = Tipo.TriplePosicionRepetida;
            }
            else if (numMovimientosLegales == 0)
            {
                if (jaque)
                {
                    // Hay jaque mate
                    situacionPartida = tablero.Turno == Pieza.Color.Blancas ? Tipo.JaqueMateBlancas : Tipo.JaqueMateNegras;

                }
                else
                {
                    // Hay rey ahogado
                    situacionPartida = Tipo.ReyAhogado;
                }
            }

            return situacionPartida;
        }

        public static bool MaterialInsuficiente(Tablero tablero)
        {
            ulong piezasSinReyes = tablero.Bitboards[1] | tablero.Bitboards[2] | tablero.Bitboards[3] | tablero.Bitboards[4] | tablero.Bitboards[5] | tablero.Bitboards[7] | tablero.Bitboards[8] | tablero.Bitboards[9] | tablero.Bitboards[10] | tablero.Bitboards[11];

            // Solo hay reyes
            if (piezasSinReyes == 0)
                return true;
            else if (BitboardUtils.EsPotenciaDe2(piezasSinReyes))
            {
                // Solo hay rey y caballo contra rey
                if (BitboardUtils.EsPotenciaDe2(tablero.Bitboards[4] | tablero.Bitboards[10]))
                    return true;

                // Solo hay rey y alfil contra rey
                if (BitboardUtils.EsPotenciaDe2(tablero.Bitboards[3] | tablero.Bitboards[9]))
                    return true;
            }
            else if ((tablero.Bitboards[1] | tablero.Bitboards[2] | tablero.Bitboards[4] | tablero.Bitboards[5] | tablero.Bitboards[7] | tablero.Bitboards[8] | tablero.Bitboards[10] | tablero.Bitboards[11]) == 0)
            {
                // Solo hay reyes y alfiles (si los alfiles controlan casillas del mismo color)
                if ((tablero.Bitboards[3] | tablero.Bitboards[9]) != 0 && BitboardUtils.AlfilesEnCasillasMismoColor(tablero.Bitboards[3] | tablero.Bitboards[9]))
                    return true;
            }

            return false;
        }

        public static string ObtenerDescripcion(this Tipo tipo)
        {
            return tipo switch
            {
                Tipo.EnCurso => "Partida en curso",
                Tipo.JaqueMateBlancas => "Jaque mate a las blancas",
                Tipo.JaqueMateNegras => "Jaque mate a las negras",
                Tipo.ReyAhogado => "Rey ahogado",
                Tipo.TriplePosicionRepetida => "Triple repetición de posición",
                Tipo.Regla50Movimientos => "Regla de los 50 movimientos",
                Tipo.MaterialInsuficiente => "Material insuficiente",
                Tipo.TiempoAgotadoBlancas => "Tiempo agotado para las blancas",
                Tipo.TiempoAgotadoNegras => "Tiempo agotado para las negras",
                Tipo.TablasPorArbitro => "Tablas por decisión del árbitro",
                _ => "Situación desconocida"
            };
        }

        public static Pieza.Color ObtenerGanador(Tipo resultado)
        {
            switch (resultado)
            {
                case Tipo.JaqueMateBlancas:
                case Tipo.TiempoAgotadoBlancas:
                    return Pieza.Color.Negras;

                case Tipo.JaqueMateNegras:
                case Tipo.TiempoAgotadoNegras:
                    return Pieza.Color.Blancas;

                default:
                    return Pieza.Color.Nada;
            }
        }
    }
}