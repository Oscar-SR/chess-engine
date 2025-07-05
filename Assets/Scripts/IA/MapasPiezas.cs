using Ajedrez.Utilities;
using Ajedrez.Core;

namespace Ajedrez.IA
{
    public static class MapasPiezas
    {
        public static readonly int[] Peones = {
             0,   0,   0,   0,   0,   0,   0,   0,
            50,  50,  50,  50,  50,  50,  50,  50,
            10,  10,  20,  30,  30,  20,  10,  10,
             5,   5,  10,  25,  25,  10,   5,   5,
             0,   0,   0,  20,  20,   0,   0,   0,
             5,  -5, -10,   0,   0, -10,  -5,   5,
             5,  10,  10, -20, -20,  10,  10,   5,
             0,   0,   0,   0,   0,   0,   0,   0
        };

        public static readonly int[] PeonesEndgame = {
             0,   0,   0,   0,   0,   0,   0,   0,
            80,  80,  80,  80,  80,  80,  80,  80,
            50,  50,  50,  50,  50,  50,  50,  50,
            30,  30,  30,  30,  30,  30,  30,  30,
            20,  20,  20,  20,  20,  20,  20,  20,
            10,  10,  10,  10,  10,  10,  10,  10,
            10,  10,  10,  10,  10,  10,  10,  10,
             0,   0,   0,   0,   0,   0,   0,   0
        };

        public static readonly int[] Torres = {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };

        public static readonly int[] Caballos = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50,
        };

        public static readonly int[] Alfiles = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20,
        };
        public static readonly int[] Reinas = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        public static readonly int[] Rey =
        {
            -80, -70, -70, -70, -70, -70, -70, -80,
            -60, -60, -60, -60, -60, -60, -60, -60,
            -40, -50, -50, -60, -60, -50, -50, -40,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
             20,  20,  -5,  -5,  -5,  -5,  20,  20,
             20,  30,  10,   0,   0,  10,  30,  20
        };

        public static readonly int[] ReyEndgame =
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
             -5,   0,   5,   5,   5,   5,   0,  -5,
            -10,  -5,  20,  30,  30,  20,  -5, -10,
            -15, -10,  35,  45,  45,  35, -10, -15,
            -20, -15,  30,  40,  40,  30, -15, -20,
            -25, -20,  20,  25,  25,  20, -20, -25,
            -30, -25,   0,   0,   0,   0, -25, -30,
            -50, -30, -30, -30, -30, -30, -30, -50
        };

        public static readonly int[] PeonesVolteado;
        public static readonly int[] PeonesEndgameVolteado;
        public static readonly int[] TorresVolteado;
        public static readonly int[] CaballosVolteado;
        public static readonly int[] AlfilesVolteado;
        public static readonly int[] ReinasVolteado;
        public static readonly int[] ReyVolteado;
        public static readonly int[] ReyEndgameVolteado;

        //public static readonly int[][] Mapas;

        static MapasPiezas()
        {
            /*
            Mapas = new int[Pieza.NUM_TIPOS_PIEZAS][];

            // Piezas blancas
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, Pieza.Color.Blancas)] = Peones;
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, Pieza.Color.Blancas)] = Torres;
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, Pieza.Color.Blancas)] = Caballos;
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, Pieza.Color.Blancas)] = Alfiles;
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, Pieza.Color.Blancas)] = Reinas;
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Rey, Pieza.Color.Blancas)] = Rey;

            // Piezas negras
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, Pieza.Color.Negras)] = VoltearMapa(Peones);
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, Pieza.Color.Negras)] = VoltearMapa(Torres);
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, Pieza.Color.Negras)] = VoltearMapa(Caballos);
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, Pieza.Color.Negras)] = VoltearMapa(Alfiles);
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, Pieza.Color.Negras)] = VoltearMapa(Reinas);
            Mapas[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Rey, Pieza.Color.Negras)] = VoltearMapa(Rey);
            */
        }

        private static int[] VoltearMapa(int[] mapa)
        {
            int[] mapaVolteado = new int[AjedrezUtils.MAX_INDICE];

            for (int i = 0; i < AjedrezUtils.MAX_INDICE; i++)
            {
                (int fila, int columna) = AjedrezUtils.IndiceACoordenadas(i);
                int indice = AjedrezUtils.CoordenadasAIndice(7 - fila, columna);
                mapaVolteado[indice] = mapa[i];
            }
            return mapaVolteado;
        }

        public static int Leer(int[] mapa, int casilla, Pieza.Color color)
        {
            if (AjedrezUtils.MismoColor(color, Pieza.Color.Blancas))
            {
                (int fila, int columna) = AjedrezUtils.IndiceACoordenadas(casilla);
                fila = 7 - fila;
                casilla = AjedrezUtils.CoordenadasAIndice(fila, columna);
            }

            return mapa[casilla];
        }

        public static int CalcularInterpolacionPeones(int casilla, float valorInterpolacion)
        {
            return (int)((1 - valorInterpolacion) * Peones[casilla] + valorInterpolacion * PeonesEndgame[casilla]);
        }

        public static int CalcularInterpolacionRey(int casilla, float valorInterpolacion)
        {
            return (int)((1 - valorInterpolacion) * Rey[casilla] + valorInterpolacion * ReyEndgame[casilla]);
        }

        public static int CalcularInterpolacion(int valorInicial, int valorFinal, float valorInterpolacion)
        {
            return (int)((1 - valorInterpolacion) * valorInicial + valorInterpolacion * valorFinal);
        }
    }
}