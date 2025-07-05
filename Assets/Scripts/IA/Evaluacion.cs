using static System.Math;
using Ajedrez.Core;
using Ajedrez.Utilities;

namespace Ajedrez.IA
{
    public class Evaluacion
    {
        // Valor del material
        private const int VALOR_PEON = 100;
        private const int VALOR_CABALLO = 300;
        private const int VALOR_ALFIL = 300;
        private const int VALOR_TORRE = 500;
        private const int VALOR_REINA = 900;

        // Valor del material en el endgame
        const int VALOR_REINA_ENDGAME = 45;
        const int VALOR_TORRE_ENDGAME = 20;
        const int VALOR_ALFIL_ENDGAME = 10;
        const int VALOR_CABALLO_ENDGAME = 10;

        private const float VALOR_UMBRAL_ENDGAME = 2 * VALOR_TORRE_ENDGAME + 2 * VALOR_ALFIL_ENDGAME + 2 * VALOR_CABALLO_ENDGAME + VALOR_REINA_ENDGAME;
        private const int MAX_DISTANCIA_MANHATTAN = 14;

        private Tablero tablero;

        public Evaluacion(Tablero tablero)
        {
            this.tablero = tablero;
        }

        public struct DatosEvaluacion
        {
            private int valorMaterial;
            private int valorPosicionRey;
            private int valorMapasPiezas;

            public int ValorMaterial
            {
                get => valorMaterial;
                set => valorMaterial = value;
            }

            public int ValorPosicionRey
            {
                get => valorPosicionRey;
                set => valorPosicionRey = value;
            }

            public int ValorMapasPiezas
            {
                get => valorMapasPiezas;
                set => valorMapasPiezas = value;
            }

            public void CalcularValorMaterial(Material material)
            {
                valorMaterial = 0;
                valorMaterial += material.NumPeones * VALOR_PEON;
                valorMaterial += material.NumCaballos * VALOR_CABALLO;
                valorMaterial += material.NumAlfiles * VALOR_ALFIL;
                valorMaterial += material.NumTorres * VALOR_TORRE;
                valorMaterial += material.NumReinas * VALOR_REINA;
            }

            public int Total
            {
                get
                {
                    return valorMaterial + valorPosicionRey + valorMapasPiezas;
                }
            }
        }

        public readonly struct Material
        {
            private readonly int numPeones;
            private readonly int numCaballos;
            private readonly int numAlfiles;
            private readonly int numTorres;
            private readonly int numReinas;

            public Material(int numPeones, int numCaballos, int numAlfiles, int numTorres, int numReinas)
            {
                this.numPeones = numPeones;
                this.numCaballos = numCaballos;
                this.numAlfiles = numAlfiles;
                this.numTorres = numTorres;
                this.numReinas = numReinas;
            }

            public int NumPeones => numPeones;
            public int NumCaballos => numCaballos;
            public int NumAlfiles => numAlfiles;
            public int NumTorres => numTorres;
            public int NumReinas => numReinas;
        }

        public static int ObtenerValorPieza(Pieza.Tipo tipo)
        {
            return tipo switch
            {
                Pieza.Tipo.Peon => VALOR_PEON,
                Pieza.Tipo.Caballo => VALOR_CABALLO,
                Pieza.Tipo.Alfil => VALOR_ALFIL,
                Pieza.Tipo.Torre => VALOR_TORRE,
                Pieza.Tipo.Reina => VALOR_REINA,
                Pieza.Tipo.Rey => Busqueda.INFINITO,
                _ => 0
            };
        }

        public static int ObtenerValorPromocion(int flagPromocion)
        {
            return flagPromocion switch
            {
                Movimiento.PROMOVER_A_CABALLO => VALOR_CABALLO,
                Movimiento.PROMOVER_A_ALFIL => VALOR_ALFIL,
                Movimiento.PROMOVER_A_TORRE => VALOR_TORRE,
                Movimiento.PROMOVER_A_REINA => VALOR_REINA,
                _ => 0
            };
        }

        public int Evaluar()
        {
            // Se calcula el numero del material de cada jugador
            (Material materialBlancas, Material materialNegras) = ContarMaterial();

            // Se calcula el valor del material de cada jugador
            DatosEvaluacion datosEvaluacionBlancas = new DatosEvaluacion();
            DatosEvaluacion datosEvaluacionNegras = new DatosEvaluacion();
            datosEvaluacionBlancas.CalcularValorMaterial(materialBlancas);
            datosEvaluacionNegras.CalcularValorMaterial(materialNegras);

            // Se calcula el valor del endgame en función del material restante (0 es un endgame temprano y 1 es un endgame tardío)
            float valorEndgameBlancas = CalcularValorEndgame(materialBlancas);
            float valorEndgameNegras = CalcularValorEndgame(materialNegras);

            // Se calcula el valor de la posición del rey
            datosEvaluacionBlancas.ValorPosicionRey = ForzarMovimientoReyEndgame(Pieza.Color.Blancas, datosEvaluacionBlancas.ValorMaterial, datosEvaluacionNegras.ValorMaterial, valorEndgameNegras);
            datosEvaluacionNegras.ValorPosicionRey = ForzarMovimientoReyEndgame(Pieza.Color.Negras, datosEvaluacionNegras.ValorMaterial, datosEvaluacionBlancas.ValorMaterial, valorEndgameBlancas);

            // Calcular el valor de los mapas de piezas
            datosEvaluacionBlancas.ValorMapasPiezas = EvaluarMapasPiezas(Pieza.Color.Blancas, valorEndgameNegras);
            datosEvaluacionNegras.ValorMapasPiezas = EvaluarMapasPiezas(Pieza.Color.Negras, valorEndgameBlancas);

            // Calcular el total de la evaluación aplicando la perspectiva
            int evaluacion = (datosEvaluacionBlancas.Total - datosEvaluacionNegras.Total) * (AjedrezUtils.MismoColor(tablero.Turno, Pieza.Color.Blancas) ? 1 : -1);

            return evaluacion;
        }

        private (Material materialBlancas, Material materialNegras) ContarMaterial()
        {
            int numPeonesBlancos = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, Pieza.Color.Blancas)]);
            int numCaballosBlancos = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, Pieza.Color.Blancas)]);
            int numAlfilesBlancos = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, Pieza.Color.Blancas)]);
            int numTorresBlancas = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, Pieza.Color.Blancas)]);
            int numReinasBlancas = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, Pieza.Color.Blancas)]);

            int numPeonesNegros = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Peon, Pieza.Color.Negras)]);
            int numCaballosNegros = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Caballo, Pieza.Color.Negras)]);
            int numAlfilesNegros = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Alfil, Pieza.Color.Negras)]);
            int numTorresNegras = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Torre, Pieza.Color.Negras)]);
            int numReinasNegras = BitboardUtils.ContarBitsActivos(tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(Pieza.Tipo.Reina, Pieza.Color.Negras)]);

            Material materialBlancas = new Material(numPeonesBlancos, numCaballosBlancos, numAlfilesBlancos, numTorresBlancas, numReinasBlancas);
            Material materialNegras = new Material(numPeonesNegros, numCaballosNegros, numAlfilesNegros, numTorresNegras, numReinasNegras);
            return (materialBlancas, materialNegras);
        }

        /// <summary>
        /// Evalúa la posición del rey aliado y el rey rival en el "endgame" para favorecer movimientos que
        /// optimicen la colocación del rey aliado, incentivando posiciones de control y jaque mate.
        /// </summary>
        /// <param name="color">Color del jugador del que se va a evaluar el movimiento del rey.</param>
        /// <param name="valorMaterialAliado">Valor del material del jugador aliado.</param>
        /// <param name="valorMaterialEnemigo">Valor del material del jugador enemigo.</param>
        /// <param name="valorEndgameEnemigo">Valor del parámetro de "endgame" del jugador enemigo.</param>
        /// <returns>Valor entero que representa la evaluación ponderada de la posición del rey en el "endgame".</returns>
        /// <remarks>
        /// Implementación basada en la evaluación del "mop-up" para finales, donde se premian posiciones de 
        /// control y cercanía estratégica entre los reyes.
        /// Fuente: <see href="https://www.chessprogramming.org/Mop-up_Evaluation">Chess Programming - Mop-up Evaluation</see>
        /// </remarks>
        private int ForzarMovimientoReyEndgame(Pieza.Color color, int valorMaterialAliado, int valorMaterialEnemigo, float valorEndgameEnemigo)
        {
            if ((valorMaterialAliado > (valorMaterialEnemigo + VALOR_PEON * 2)) && valorEndgameEnemigo > 0)
            {
                int evaluacion = 0;
                int casillaReyAliado = tablero.ObtenerCasillaRey(color);
                int casillaReyRival = tablero.ObtenerCasillaRey(AjedrezUtils.InversoColor(color));

                // Fomentar el acercamiento del rey aliado al rey del oponente
                evaluacion += (MAX_DISTANCIA_MANHATTAN - AjedrezUtils.DistanciaManhattan[casillaReyAliado, casillaReyRival]) * 4;
                // Fomentar el empuje del rey oponente hacia el borde del tablero
                evaluacion += AjedrezUtils.DistanciaManhattanAlCentro[casillaReyRival] * 10;

                return (int)(evaluacion * valorEndgameEnemigo);
            }

            return 0;
        }

        private float CalcularValorEndgame(Material material)
        {
            float materialTotalEndgame = VALOR_REINA_ENDGAME * material.NumReinas + VALOR_TORRE_ENDGAME * material.NumTorres + VALOR_ALFIL_ENDGAME * material.NumAlfiles + VALOR_CABALLO_ENDGAME * material.NumCaballos;
            float valorEndgame = 1 - Min(1, materialTotalEndgame / (float)VALOR_UMBRAL_ENDGAME);
            return valorEndgame;
        }

        private int EvaluarMapasPiezas(Pieza.Color color, float valorEndgame)
        {
            int evaluacion = 0;

            int evaluacionRey = EvaluarMapaPieza(MapasPiezas.Rey, Pieza.Tipo.Rey, color);
            int evaluacionReyEndgame = EvaluarMapaPieza(MapasPiezas.ReyEndgame, Pieza.Tipo.Rey, color);
            evaluacion += MapasPiezas.CalcularInterpolacion(evaluacionRey, evaluacionReyEndgame, valorEndgame);

            int evaluacionPeones = EvaluarMapaPieza(MapasPiezas.Peones, Pieza.Tipo.Peon, color);
            int evaluacionPeonesEndgame = EvaluarMapaPieza(MapasPiezas.PeonesEndgame, Pieza.Tipo.Peon, color);
            evaluacion += MapasPiezas.CalcularInterpolacion(evaluacionPeones, evaluacionPeonesEndgame, valorEndgame);

            //evaluacion += EvaluarMapaPieza(MapasPiezas.Rey, Pieza.Tipo.Rey, color);
            //evaluacion += EvaluarMapaPieza(MapasPiezas.Peones, Pieza.Tipo.Peon, color);
            evaluacion += EvaluarMapaPieza(MapasPiezas.Reinas, Pieza.Tipo.Reina, color);
            evaluacion += EvaluarMapaPieza(MapasPiezas.Torres, Pieza.Tipo.Torre, color);
            evaluacion += EvaluarMapaPieza(MapasPiezas.Alfiles, Pieza.Tipo.Alfil, color);
            evaluacion += EvaluarMapaPieza(MapasPiezas.Caballos, Pieza.Tipo.Caballo, color);

            return evaluacion;
        }

        private int EvaluarMapaPieza(int[] mapa, Pieza.Tipo tipoPieza, Pieza.Color colorPieza)
        {
            int evaluacion = 0;
            ulong piezas = tablero.Bitboards[AjedrezUtils.ObtenerIndicePieza(tipoPieza, colorPieza)];

            while (piezas != 0)
            {
                int casilla = BitboardUtils.PrimerBitActivo(piezas);
                piezas &= piezas - 1;
                evaluacion += MapasPiezas.Leer(mapa, casilla, colorPieza);
            }

            return evaluacion;
        }
    }
}