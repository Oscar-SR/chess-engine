using static System.Math;

namespace Ajedrez.Utilities
{
    public static class BitboardUtils
    {
        public static readonly ulong[] AtaquesCaballo = new ulong[AjedrezUtils.MAX_INDICE]; // bitboard que contiene las casillas desde las que puede atacar un caballo a cada una de las casillas del tablero
        public static readonly ulong[] AtaquesRey = new ulong[AjedrezUtils.MAX_INDICE]; // bitboard que contiene las casillas desde las que puede atacar un rey a cada una de las casillas del tablero
        public static readonly ulong[,] AtaquesPeon = new ulong[2, AjedrezUtils.MAX_INDICE]; // bitboard que contiene las casillas desde las que puede atacar un peon de cada color a cada una de las casillas del tablero
        private static ulong[,] CasillasEntre = new ulong[64, 64]; // matriz de bitboards que definen las casillas que hay entre otras dos en línea recta o diagonal (si no hay este tipo de casillas entre medias, devuelve 0UL)
        private static readonly int[] Index64 = // secuencia De Bruijin
        {
            0, 47,  1, 56, 48, 27,  2, 60,
            57, 49, 41, 37, 28, 16,  3, 61,
            54, 58, 35, 52, 50, 42, 21, 44,
            38, 32, 29, 23, 17, 11,  4, 62,
            46, 55, 26, 59, 40, 36, 15, 53,
            34, 51, 20, 43, 31, 22, 10, 45,
            25, 39, 14, 33, 19, 30,  9, 24,
            13, 18,  8, 12,  7,  6,  5, 63
        };

        // Constructor estático
        static BitboardUtils()
        {
            // Precáculos
            PrecalcularAtaquesCaballo();
            PrecalcularAtaquesRey();
            PrecalcularAtaquesPeones();
            PrecalcularCasillasEntre();
        }

        /// <summary>
        /// Activa el bit correspondiente a la casilla dada.
        /// </summary>
        /// <param name="casilla">Índice de la casilla (0-63).</param>
        /// <returns>Bitboard con solo esa casilla activada.</returns>
        public static ulong SetBit(int casilla)
        {
            return 1UL << casilla;
        }

        /// <summary>
        /// Comprueba si una casilla está activa en el bitboard dado.
        /// </summary>
        /// <param name="casilla">Índice de la casilla.</param>
        /// <param name="bitboard">Bitboard a evaluar.</param>
        /// <returns>Verdadero si el bit está activo, falso si no.</returns>
        public static bool EstaCasillaActiva(int casilla, ulong bitboard)
        {
            return (bitboard & SetBit(casilla)) != 0;
        }

        /// <summary>
        /// Indica si hay más de un bit activo en el bitboard.
        /// </summary>
        /// <param name="bitboard">Bitboard a evaluar.</param>
        /// <returns>Verdadero si hay más de un bit a 1, falso si hay 0 o 1.</returns>
        public static bool MasDeUnBitActivo(ulong bitboard)
        {
            return (bitboard & (bitboard - 1)) != 0;
        }

        // ATAQUES CABALLO
        private static void PrecalcularAtaquesCaballo()
        {
            for (int i = 0; i < AjedrezUtils.MAX_INDICE; i++)
            {
                ulong ataques = 0UL;
                (int f, int c) = AjedrezUtils.IndiceACoordenadas(i);

                int[,] offsets = {
                    { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 },
                    { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 }
                };

                for (int j = 0; j < offsets.GetLength(0); j++)
                {
                    int nf = f + offsets[j, 0];
                    int nc = c + offsets[j, 1];
                    if (nf >= 0 && nf < AjedrezUtils.TAM_TABLERO && nc >= 0 && nc < AjedrezUtils.TAM_TABLERO)
                        ataques |= SetBit(AjedrezUtils.CoordenadasAIndice(nf, nc));
                }

                AtaquesCaballo[i] = ataques;
            }
        }

        // ATAQUES REY
        private static void PrecalcularAtaquesRey()
        {
            for (int i = 0; i < AjedrezUtils.MAX_INDICE; i++)
            {
                ulong ataques = 0UL;
                (int f, int c) = AjedrezUtils.IndiceACoordenadas(i);

                for (int df = -1; df <= 1; df++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (df == 0 && dc == 0) continue;

                        int nf = f + df;
                        int nc = c + dc;

                        if (nf >= 0 && nf < AjedrezUtils.TAM_TABLERO && nc >= 0 && nc < AjedrezUtils.TAM_TABLERO)
                            ataques |= SetBit(AjedrezUtils.CoordenadasAIndice(nf, nc));
                    }
                }

                AtaquesRey[i] = ataques;
            }
        }

        // ATAQUES PEONES
        private static void PrecalcularAtaquesPeones()
        {
            for (int i = 0; i < AjedrezUtils.MAX_INDICE; i++)
            {
                (int f, int c) = AjedrezUtils.IndiceACoordenadas(i);

                ulong blanco = 0UL, negro = 0UL;

                // Ataques peón blanco (hacia el norte = fila -1)
                if (f > 0)
                {
                    if (c > 0) blanco |= SetBit(AjedrezUtils.CoordenadasAIndice(f - 1, c - 1));
                    if (c < 7) blanco |= SetBit(AjedrezUtils.CoordenadasAIndice(f - 1, c + 1));
                }

                // Ataques peón negro (hacia el sur = fila +1)
                if (f < 7)
                {
                    if (c > 0) negro |= SetBit(AjedrezUtils.CoordenadasAIndice(f + 1, c - 1));
                    if (c < 7) negro |= SetBit(AjedrezUtils.CoordenadasAIndice(f + 1, c + 1));
                }

                AtaquesPeon[0, i] = blanco; // Blancas
                AtaquesPeon[1, i] = negro; // Negras
            }
        }

        private static void PrecalcularCasillasEntre()
        {
            for (int desde = 0; desde < 64; desde++)
            {
                for (int hasta = 0; hasta < 64; hasta++)
                {
                    CasillasEntre[desde, hasta] = CalcularCasillasEntre(desde, hasta);
                }
            }
        }

        private static ulong CalcularCasillasEntre(int desde, int hasta)
        {
            int filaDesde = desde / 8;
            int colDesde = desde % 8;
            int filaHasta = hasta / 8;
            int colHasta = hasta % 8;

            int deltaFila = Sign(filaHasta - filaDesde);
            int deltaCol = Sign(colHasta - colDesde);

            // Si son la misma casilla, no hay nada entre ellas
            if (deltaFila == 0 && deltaCol == 0)
                return 0UL;

            // Verifica si están alineadas (misma fila, columna o diagonal)
            if ((Abs(filaHasta - filaDesde) == Abs(colHasta - colDesde)) ||
                filaDesde == filaHasta || colDesde == colHasta)
            {
                ulong ray = 0UL;
                int f = filaDesde + deltaFila;
                int c = colDesde + deltaCol;

                while (f != filaHasta || c != colHasta)
                {
                    ray |= 1UL << (f * 8 + c);
                    f += deltaFila;
                    c += deltaCol;
                }

                // Excluye las casillas de origen y destino
                ray &= ~(1UL << desde);
                ray &= ~(1UL << hasta);

                return ray;
            }

            return 0UL;
        }

        /// <summary>
        /// Devuelve las casillas entre dos posiciones si están alineadas.
        /// </summary>
        /// <param name="desde">Índice de casilla inicial.</param>
        /// <param name="hasta">Índice de casilla final.</param>
        /// <returns>Bitboard con las casillas entre 'desde' y 'hasta'.</returns>
        public static ulong ObtenerCasillasEntre(int desde, int hasta)
        {
            return CasillasEntre[desde, hasta];
        }

        /// <summary>
        /// Calcula las casillas a las que puede atacar un alfil desde una posición dada, considerando bloqueos.
        /// </summary>
        /// <param name="casillasOcupadas">Bitboard con todas las piezas en el tablero.</param>
        /// <param name="casilla">Índice de la casilla del alfil.</param>
        /// <returns>Bitboard con casillas atacadas por el alfil.</returns>
        public static ulong AtaquesAlfil(ulong casillasOcupadas, int casilla)
        {
            (int rank, int file) = AjedrezUtils.IndiceACoordenadas(casilla);
            ulong ataques = 0UL;

            // Direcciones diagonales: NE, NO, SE, SO
            int[] dRank = { 1, 1, -1, -1 };
            int[] dFile = { 1, -1, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                int r = rank + dRank[dir];
                int f = file + dFile[dir];

                while (AjedrezUtils.EnTablero(r, f))
                {
                    int sq = r * 8 + f;
                    ataques |= SetBit(sq);
                    if ((casillasOcupadas & SetBit(sq)) != 0) break; // si hay una pieza, detenemos
                    r += dRank[dir];
                    f += dFile[dir];
                }
            }

            return ataques;
        }

        /// <summary>
        /// Calcula las casillas a las que puede atacar una torre desde una posición dada.
        /// </summary>
        /// <param name="casillasOcupadas">Bitboard con todas las piezas en el tablero.</param>
        /// <param name="casilla">Índice de la casilla de la torre.</param>
        /// <returns>Bitboard con casillas atacadas por la torre.</returns>
        public static ulong AtaquesTorre(ulong casillasOcupadas, int casilla)
        {
            (int rank, int file) = AjedrezUtils.IndiceACoordenadas(casilla);
            ulong ataques = 0UL;

            // Direcciones ortogonales: N, S, E, O
            int[] dRank = { 1, -1, 0, 0 };
            int[] dFile = { 0, 0, 1, -1 };

            for (int dir = 0; dir < 4; dir++)
            {
                int r = rank + dRank[dir];
                int f = file + dFile[dir];

                while (AjedrezUtils.EnTablero(r, f))
                {
                    int sq = r * 8 + f;
                    ataques |= SetBit(sq);
                    if ((casillasOcupadas & SetBit(sq)) != 0) break;
                    r += dRank[dir];
                    f += dFile[dir];
                }
            }

            return ataques;
        }

        /// <summary>
        /// Calcula los ataques de rayos X (piezas detrás de una obstrucción) para una torre.
        /// </summary>
        /// <param name="casillasOcupadas">Bitboard de todas las piezas.</param>
        /// <param name="bloqueadores">Bitboard de piezas que pueden bloquear.</param>
        /// <param name="casillaTorre">Índice de la torre.</param>
        /// <returns>Bitboard con casillas atacadas por rayos X.</returns>
        public static ulong AtaquesRayosXTorre(ulong casillasOcupadas, ulong bloqueadores, int casillaTorre)
        {
            ulong ataques = AtaquesTorre(casillasOcupadas, casillaTorre);
            bloqueadores &= ataques; // Nos quedamos solo con bloqueadores en la línea de ataque
            return ataques ^ AtaquesTorre(casillasOcupadas ^ bloqueadores, casillaTorre);
        }


        /// <summary>
        /// Calcula los ataques de rayos X para un alfil.
        /// </summary>
        /// <param name="casillasOcupadas">Bitboard de todas las piezas.</param>
        /// <param name="bloqueadores">Bitboard de piezas bloqueadoras.</param>
        /// <param name="casillaAlfil">Índice de la casilla del alfil.</param>
        /// <returns>Bitboard con casillas atacadas por rayos X del alfil.</returns>
        public static ulong AtaquesRayosXAlfil(ulong casillasOcupadas, ulong bloqueadores, int casillaAlfil)
        {
            ulong ataques = AtaquesAlfil(casillasOcupadas, casillaAlfil);
            bloqueadores &= ataques; // Solo bloqueadores en la diagonal
            return ataques ^ AtaquesAlfil(casillasOcupadas ^ bloqueadores, casillaAlfil);
        }

        /// <summary>
        /// Devuelve el índice del primer bit activo (empezando desde el menos significativo).
        /// </summary>
        /// <param name="bitboard">Bitboard de entrada.</param>
        /// <returns>Índice del primer bit activo (0-63).</returns>
        /// <remarks>
        /// Fuente: <see href="https://www.chessprogramming.org/BitScan">Chess Programming Wiki</see><br/>
        /// Autor: Kim Walisch (2012)
        /// </remarks>
        public static int PrimerBitActivo(ulong bb)
        {
            const ulong DeBruijn64 = 0x03f79d71b4cb0a89UL;
            if (bb == 0)
                return -1;
            return Index64[((bb ^ (bb - 1)) * DeBruijn64) >> 58];
        }

        /// <summary>
        /// Genera ataques deslizantes sin filtrar hacia el rey enemigo (útil para rayos X).
        /// </summary>
        /// <param name="ocupadas">Bitboard de todas las piezas en el tablero.</param>
        /// <param name="torres">Bitboard de torres del atacante.</param>
        /// <param name="alfiles">Bitboard de alfiles del atacante.</param>
        /// <param name="damas">Bitboard de damas del atacante.</param>
        /// <param name="reyEnemigo">Bitboard con solo el rey enemigo activo.</param>
        /// <returns>Bitboard con casillas atacadas por piezas deslizantes.</returns>
        public static ulong AtaquesDeslizantesSinFiltrar(ulong ocupadas, ulong torres, ulong alfiles, ulong damas, ulong reyEnemigo)
        {
            ulong ataques = 0;

            // Quitamos el rey enemigo del bitboard de ocupadas
            ulong ocupadasSinRey = ocupadas & ~reyEnemigo;

            // Torres y damas (movimientos ortogonales)
            for (int i = 0; i < 64; i++)
            {
                if (((torres | damas) & (1UL << i)) != 0)
                {
                    ataques |= AtaquesTorre(ocupadasSinRey, i);
                }
            }

            // Alfiles y damas (movimientos diagonales)
            for (int i = 0; i < 64; i++)
            {
                if (((alfiles | damas) & (1UL << i)) != 0)
                {
                    ataques |= AtaquesAlfil(ocupadasSinRey, i);
                }
            }

            return ataques;
        }

        /// <summary>
        /// Indica si un número es potencia de 2.
        /// </summary>
        /// <param name="x">Número a evaluar.</param>
        /// <returns>Verdadero si es potencia de 2, falso si no.</returns>
        public static bool EsPotenciaDe2(ulong x)
        {
            return x != 0 && (x & (x - 1)) == 0;
        }

        /// <summary>
        /// Comprueba si todos los alfiles están en casillas del mismo color.
        /// </summary>
        /// <param name="alfiles">Bitboard de alfiles.</param>
        /// <returns>Verdadero si están en casillas del mismo color, falso en caso contrario.</returns>
        public static bool AlfilesEnCasillasMismoColor(ulong alfiles)
        {
            const ulong casillasClaras = 0x55AA55AA55AA55AA;
            const ulong casillasOscuras = 0xAA55AA55AA55AA55;

            return ((alfiles & casillasClaras) == 0) || ((alfiles & casillasOscuras) == 0);
        }

        /// <summary>
        /// Cuenta la cantidad de bits activos (peso de Hamming).
        /// </summary>
        /// <param name="x">Bitboard a evaluar.</param>
        /// <returns>Número de bits activos.</returns>
        /// <remarks>
        /// Fuente: <see href="https://discussions.unity.com/t/portable-pop-count-hamming-weight/807752">Unity Discussions - Portable Pop Count / Hamming Weight</see>
        /// </remarks>
        public static int ContarBitsActivos(ulong x)
        {
            const ulong h01 = 0x0101010101010101;
            const ulong m1 = 0x5555555555555555; // 0101...
            const ulong m2 = 0x3333333333333333; // 00110011..
            const ulong m4 = 0x0f0f0f0f0f0f0f0f;
            x -= (x >> 1) & m1;             // put count of each 2 bits into those 2 bits
            x = (x & m2) + ((x >> 2) & m2); // put count of each 4 bits into those 4 bits
            x = (x + (x >> 4)) & m4;        // put count of each 8 bits into those 8 bits
            return (int)((x * h01) >> 56);
        }
    }
}