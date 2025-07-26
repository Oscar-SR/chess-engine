using static System.Math;
using Ajedrez.Core;

namespace Ajedrez.Utilities
{
    public static class AjedrezUtils
    {
        // CONSTANTES GLOBALES
        public const int TAM_TABLERO = 8;
        public const int MAX_INDICE = TAM_TABLERO * TAM_TABLERO;
        public const int NUM_PIEZAS = 32;
        public const int NUM_PIEZAS_POR_COLOR = NUM_PIEZAS / 2;
        public const int NORTE = 0;
        public const int SUR = 1;
        public const int OESTE = 2;
        public const int ESTE = 3;
        public const int NOROESTE = 4;
        public const int SUDESTE = 5;
        public const int NORDESTE = 6;
        public const int SUROESTE = 7;
        public const int ENROQUE_LARGO_BLANCAS = 2;
        public const int ENROQUE_CORTO_BLANCAS = 6;
        public const int ENROQUE_LARGO_NEGRAS = 58;
        public const int ENROQUE_CORTO_NEGRAS = 62;

        // VARIABLES
        public static readonly int[] Direcciones = { 8, -8, -1, 1, 7, -7, 9, -9 };
        public static readonly int[] SaltosCaballo = { 17, 15, 10, 6, -17, -15, -10, -6 };
        public static readonly int[][] NumCasillasHastaBorde = new int[MAX_INDICE][];
		public static readonly int[,] DistanciaManhattan = new int[MAX_INDICE, MAX_INDICE];
        public static readonly int[] DistanciaManhattanAlCentro = new int[MAX_INDICE];

        // Constructor estático
        static AjedrezUtils()
        {
            // Precáculos
            PrecalcularNumCasillasHastaBorde();
            PrecalcularDistanciaManhattan();
            PrecalcularDistanciaManhattanAlCentro();
        }

        // FUNCIONES AUXILIARES

        /// <summary>
        /// Convierte un índice de casilla (0-63) a coordenadas de fila
        /// </summary>
        /// <param name="indice">Índice de la casilla (de 0 a 63)</param>
        /// <returns>Valor de la fila correspondiente a la casilla</returns>
        public static int ObtenerFila(int indice)
        {
            return indice / TAM_TABLERO;
        }

        /// <summary>
        /// Convierte un índice de casilla (0-63) a coordenadas de columna
        /// </summary>
        /// <param name="indice">Índice de la casilla (de 0 a 63)</param>
        /// <returns>Valor de la columna correspondiente a la casilla</returns>
        public static int ObtenerColumna(int indice)
        {
            return indice % TAM_TABLERO;
        }

        /// <summary>
        /// Convierte un índice de casilla (0-63) a coordenadas de fila y columna en el tablero
        /// </summary>
        /// <param name="casilla">Índice de la casilla (de 0 a 63)</param>
        /// <returns>Una tupla (fila, columna) correspondiente a la casilla</returns>
        public static (int fila, int columna) IndiceACoordenadas(int indice)
        {
            return (ObtenerFila(indice), ObtenerColumna(indice));
        }

        /// <summary>
        /// Convierte coordenadas de fila y columna en el tablero a un índice de casilla (0-63)
        /// </summary>
        /// <param name="fila">Fila de una casilla del tablero (de 0 a 7)</param>
        /// <param name="columna">Columna de una casilla del tablero (de 0 a 7)</param>
        /// <returns>Indice de casilla correspondiente a las coordenadas de fila y columna</returns>
        public static int CoordenadasAIndice(int fila, int columna)
        {
            return fila * TAM_TABLERO + columna;
        }

        public static char FilaANombre(int fila)
        {
            return (char)('1' + fila);
        }

        public static char ColumnaANombre(int columna)
        {
            return (char)('a' + columna);
        }

        public static int NombreAFila(char fila)
        {
            return fila - '1';
        }

        public static int NombreAColumna(char columna)
        {
            return char.ToLower(columna) - 'a';
        }

        /// <summary>
        /// Devuelve verdadero si las coordenadas están dentro del tablero
        /// </summary>
        /// <param name="fila">Fila de una casilla del tablero (de 0 a 7)</param>
        /// <param name="columna">Columna de una casilla del tablero (de 0 a 7)</param>
        /// <returns>Verdadero si las coordenadas están dentro del tablero, y falso si no lo están</returns>
        public static bool EnTablero(int fila, int columna)
        {
            return fila >= 0 && fila < TAM_TABLERO && columna >= 0 && columna < TAM_TABLERO;
        }

        /// <summary>
        /// Devuelve verdadero si la casilla es oscura en función de sus coordenadas de fila y columna
        /// </summary>
        /// <param name="fila">Fila de una casilla del tablero (de 0 a 7)</param>
        /// <param name="columna">Columna de una casilla del tablero (de 0 a 7)</param>
        /// <returns>Verdadero si la casilla correspondiente a las coordenadas es oscura, y falso si es clara</returns>
        public static bool EsCasillaOscura(int fila, int columna)
        {
            return (fila + columna) % 2 == 0;
        }

        /// <summary>
        /// Devuelve verdadero si la casilla es oscura en función de su índice
        /// </summary>
        /// <param name="indice">Índice de la casilla (de 0 a 63)</param>
        /// <returns>Verdadero si la casilla correspondiente a las coordenadas es oscura, y falso si es clara</returns>
        public static bool EsCasillaOscura(int indice)
        {
            (int fila, int columna) = IndiceACoordenadas(indice);
            return (fila + columna) % 2 == 0;
        }

        /// <summary>
        /// Devuelve el inverso de un color
        /// </summary>
        /// <param name="color">Color de uno de los bandos del ajedrez (blanco o negro)</param>
        /// <returns>Color opuesto al color de entrada</returns>
        public static Pieza.Color InversoColor(Pieza.Color color)
        {
            return color == Pieza.Color.Blancas ? Pieza.Color.Negras : Pieza.Color.Blancas;
        }

        /// <summary>
        /// Devuelve verdadero si son el mismo color o falso si no
        /// </summary>
        /// <param name="color1">Color de uno de los bandos del ajedrez (blanco o negro)</param>
        /// <param name="color2">Color de uno de los bandos del ajedrez (blanco o negro)</param>
        /// <returns>Verdadero si ambos colores son el mismo, y falso si no lo son</returns>
        public static bool MismoColor(Pieza.Color color1, Pieza.Color color2)
        {
            return color1 == color2;
        }

        /// <summary>
        /// Devuelve verdadero si son el salto de un caballo es válido, dadas sus casillas de origen y destino
        /// </summary>
        /// <param name="origen">Indice de la casilla de origen del movimiento</param>
        /// <param name="destino">Indice de la casilla de destino del movimiento</param>
        /// <returns>Verdadero si el salto es válido, y falso si no lo es</returns>
        public static bool SaltoValido(int origen, int destino)
        {
            (int origenFila, int origenColumna) = IndiceACoordenadas(origen);
            (int destinoFila, int destinoColumna) = IndiceACoordenadas(destino);

            // Validar que el salto no sea de forma incorrecta (por ejemplo, que no cruce de columna 1 a 6)
            int diffFila = Abs(destinoFila - origenFila);
            int diffColumna = Abs(destinoColumna - origenColumna);
            
            return ((diffFila == 2 && diffColumna == 1) || (diffFila == 1 && diffColumna == 2));
        }

        /// <summary>
        /// Calcula el número de casillas hasta el borde para cada casilla en el tablero
        /// </summary>
        private static void PrecalcularNumCasillasHastaBorde()
        {
            for (int fila = 0; fila < TAM_TABLERO; fila++)
            {
                for (int columna = 0; columna < TAM_TABLERO; columna++)
                {

                    int numNorte = TAM_TABLERO - 1 - fila;
                    int numSur = fila;
                    int numOeste = columna;
                    int numEste = TAM_TABLERO - 1 - columna;

                    // Inicialización del arreglo para cada casilla
                    NumCasillasHastaBorde[CoordenadasAIndice(fila, columna)] = new int[]
                    {
                        numNorte,                   // 🡹
                        numSur,                     // 🡻
                        numOeste,                   // 🡸
                        numEste,                    // 🡺
                        Min(numNorte, numOeste),    // 🡼
                        Min(numSur, numEste),       // 🡾
                        Min(numNorte, numEste),     // 🡽
                        Min(numSur, numOeste)       // 🡿
                    };
                }
            }
        }

        private static void PrecalcularDistanciaManhattan()
        {
            for (int i = 0; i < MAX_INDICE; i++)
            {
                (int fila1, int columna1) = IndiceACoordenadas(i);

                for (int j = 0; j < MAX_INDICE; j++)
                {
                    (int fila2, int columna2) = IndiceACoordenadas(j);

                    int distancia = Abs(fila1 - fila2) + Abs(columna1 - columna2);
                    DistanciaManhattan[i, j] = distancia;
                }
            }
        }

        private static void PrecalcularDistanciaManhattanAlCentro()
        {
            for (int indice = 0; indice < MAX_INDICE; indice++)
            {
                (int fila, int columna) = IndiceACoordenadas(indice);

                // Centro del tablero (3.5, 3.5) → entre (3,3) y (4,4)
                int distanciaAlCentroFila = Max(3 - fila, fila - 4);
                int distanciaAlCentroColumna = Max(3 - columna, columna - 4);
                DistanciaManhattanAlCentro[indice] = distanciaAlCentroFila + distanciaAlCentroColumna;
            }
        }

        /// <summary>
        /// Halla si el tipo de pieza es deslizante o no
        /// </summary>
        /// <returns>Verdadero si el tipo de pieza es deslizante, y falso si no lo es</returns>
        public static bool EsDeslizante(this Pieza.Tipo tipo)
        {
            return tipo == Pieza.Tipo.Reina || tipo == Pieza.Tipo.Torre || tipo == Pieza.Tipo.Alfil;
        }

        public static int ObtenerIndicePieza(Pieza.Tipo tipoPieza, Pieza.Color color)
        {
            return (int) tipoPieza + (int) color;
        }

        public static int ObtenerIndicePieza(Pieza pieza)
        {
            return ObtenerIndicePieza(pieza.TipoPieza, pieza.ColorPieza);
        }
    }
}