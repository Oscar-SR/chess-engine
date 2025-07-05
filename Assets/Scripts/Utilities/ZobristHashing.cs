using System;
using Ajedrez.Core;

namespace Ajedrez.Utilities
{
    public class ZobristHashing
    {
        private static readonly ulong[,] HashesPiezasEnCasilla = new ulong[12, 64];
        private static readonly ulong[] HashesEnroquesDisponibles = new ulong[16];
        private static readonly ulong[] HashesColumnasAlPaso = new ulong[8];
        private static ulong HashTurno;

        // Constructor estático
        static ZobristHashing()
        {
            // Precáculos
            InicializarZobristHashes();
        }

        private static void InicializarZobristHashes()
        {
            const int semilla = 123456;
            Random rng = new Random(semilla);

            for (int piece = 0; piece < 12; piece++)
            {
                for (int square = 0; square < 64; square++)
                {
                    HashesPiezasEnCasilla[piece, square] = RandomUlong(rng);
                }
            }

            for (int i = 0; i < 16; i++)
                HashesEnroquesDisponibles[i] = RandomUlong(rng);

            for (int i = 0; i < 8; i++)
                HashesColumnasAlPaso[i] = RandomUlong(rng);

            HashTurno = RandomUlong(rng);
        }

        private static ulong RandomUlong(Random rng)
        {
            return ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();

            /*
            byte[] buffer = new byte[8];
			rng.NextBytes(buffer);
			return BitConverter.ToUInt64(buffer, 0);
            */
        }

        public static ulong CrearZobristHash(Tablero tablero)
        {
            ulong hash = 0;

            for (int square = 0; square < 64; square++)
            {
                Pieza pieza = tablero.ObtenerPieza(square);
                if (pieza.TipoPieza != Pieza.Tipo.Nada)
                {
                    hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(pieza), square];
                }
            }

            // Enroque
            int enroquesDisponibles = tablero.EstadoActual.EnroquesDisponibles; // valor 0-15 según derechos
            hash ^= HashesEnroquesDisponibles[enroquesDisponibles];

            // Peón al paso
            if (tablero.EstadoActual.HayPeonVulnerable)
            {
                int columnaPeonAlPaso = AjedrezUtils.ObtenerColumna(tablero.EstadoActual.CasillaPeonVulnerable);
                hash ^= HashesColumnasAlPaso[columnaPeonAlPaso];
            }

            // Turno
            if (AjedrezUtils.MismoColor(tablero.Turno, Pieza.Color.Negras))
            {
                hash ^= HashTurno;
            }

            return hash;
        }

        public static ulong ActualizarZobristHashCasilla(ulong hash, Pieza pieza, int casilla)
        {
            return hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(pieza), casilla];
        }

        public static ulong ActualizarZobristHashEnroquesDisponibles(ulong hash, int enroquesDisponibles)
        {
            return hash ^= HashesEnroquesDisponibles[enroquesDisponibles];
        }

        public static ulong ActualizarZobristHashColumnasAlPaso(ulong hash, int peonAlPaso)
        {
            return hash ^= HashesColumnasAlPaso[AjedrezUtils.ObtenerColumna(peonAlPaso)];
        }

        public static ulong ActualizarZobristHashTurno(ulong hash)
        {
            return hash ^= HashTurno;
        }

        /*
        public static ulong ActualizarZobristHashMovimiento(ulong hash, Movimiento movimiento, Pieza pieza, int? peonAlPasoAnterior)
        {
            // Elimina la pieza del origen
            hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(pieza), movimiento.Origen];

            // Coloca la pieza en su nueva casilla
            switch (movimiento.Flag)
            {
                case Movimiento.PROMOVER_A_REINA:
                    {
                        hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, pieza.ColorPieza), movimiento.Destino];
                        break;
                    }

                case Movimiento.PROMOVER_A_TORRE:
                    {
                        hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, pieza.ColorPieza), movimiento.Destino];
                        break;
                    }

                case Movimiento.PROMOVER_A_ALFIL:
                    {
                        hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, pieza.ColorPieza), movimiento.Destino];
                        break;
                    }

                case Movimiento.PROMOVER_A_CABALLO:
                    {
                        hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, pieza.ColorPieza), movimiento.Destino];
                        break;
                    }

                default:
                    {
                        hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(pieza), movimiento.Destino];
                        break;
                    }
            }

            // Eliminar peon al paso anterior, si lo hubo
            if (peonAlPasoAnterior.HasValue)
            {
                hash ^= HashesColumnasAlPaso[AjedrezUtils.ObtenerColumna(peonAlPasoAnterior.Value)];
            }

            // Cambia el turno
            hash ^= HashTurno;

            return hash;
        }

        public static ulong ActualizarZobristHashCaptura(ulong hash, int casillaCaptura, Pieza piezaCapturada)
        {
            // Eliminar pieza capturada
            hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(piezaCapturada), casillaCaptura];

            return hash;
        }

        public static ulong ActualizarZobristHashEnroque(ulong hash, Movimiento movimientoTorre, Pieza.Color turno, int enroquesDisponiblesAntiguos, int enroquesDisponiblesNuevos)
        {
            // Actualizar movimiento de la torre
            hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, turno), movimientoTorre.Origen];
            hash ^= HashesPiezasEnCasilla[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, turno), movimientoTorre.Destino];

            // Eliminar los derechos de enroque antiguos y poner los nuevos
            hash ^= HashesEnroquesDisponibles[enroquesDisponiblesAntiguos];
            hash ^= HashesEnroquesDisponibles[enroquesDisponiblesNuevos];

            return hash;
        }

        public static ulong ActualizarZobristHashPeonAlPaso(ulong hash, int peonAlPaso)
        {
            // Actualizar el peon al paso
            hash ^= HashesColumnasAlPaso[AjedrezUtils.ObtenerColumna(peonAlPaso)];

            return hash;
        }
        */
    }
}