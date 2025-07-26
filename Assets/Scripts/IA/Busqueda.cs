using static System.Math;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Ajedrez.Core;
using Ajedrez.Debugging;

namespace Ajedrez.IA
{
    public class Busqueda
    {
        public enum TipoBusqueda : byte
        {
            PorProfundidad,
            PorTiempo,
            PorNumeroDeNodos
        }

        public const int INFINITO = 9999999;
        public const int EVALUACION_JAQUE_MATE = 100000;
        public const int TAM_TABLA_TRANSPOSICION_MB = 64;
        private const int MAX_PROFUNDIDAD = 256;

        private readonly TipoBusqueda tipoBusqueda;
        private readonly int limiteBusqueda;

        private Tablero tablero;
        private Evaluacion evaluacion;
        private OrdenadorMovimientos ordenadorMovimientos;
        private TablaTransposicion tablaTransposicion;
        private Movimiento mejorMovimientoEstaIteracion;
        private int mejorEvaluacionEstaIteracion;
        private bool busquedaCancelada;
        //private bool haBuscadoUnMovimiento;
        private DiagnosticoBusqueda diagnostico;

        [Serializable]
        public struct DiagnosticoBusqueda
        {
            private float tiempoBusqueda;
            private int maxProfundidad;
            private int mejorEvaluacion;
            private Movimiento mejorMovimiento;
            private float porcentajeOcupacionTablaTransposicion;

            public float TiempoBusqueda
            {
                get { return tiempoBusqueda; }
                set { tiempoBusqueda = value; }
            }

            public int MaxProfundidad
            {
                get { return maxProfundidad; }
                set { maxProfundidad = value; }
            }

            public int MejorEvaluacion
            {
                get { return mejorEvaluacion; }
                set { mejorEvaluacion = value; }
            }

            public Movimiento MejorMovimiento
            {
                get { return mejorMovimiento; }
                set { mejorMovimiento = value; }
            }
            
            public float PorcentajeOcupacionTablaTransposicion
            {
                get { return porcentajeOcupacionTablaTransposicion; }
                set { porcentajeOcupacionTablaTransposicion = value; }
            }

            public override string ToString()
            {
                const string ROJO_OSCURO = "#CD6666";
                const string NARANJA = "#DDB267";
                const string LILA = "#C48AEB";
                const string AZUL_PASTEL = "#8DD2EC";
                const string VERDE = "#699A33";

                return $"Profundidad buscada: <color={ROJO_OSCURO}>{maxProfundidad}</color>\n" +
                        $"Evaluación: <color={NARANJA}>{(EsJaqueMate(mejorEvaluacion) ? "#" + ObtenerNumPlysJaqueMate(mejorEvaluacion) : mejorEvaluacion)}</color>\n" +
                        $"Movimiento: <color={LILA}>{(mejorMovimiento == Movimiento.Nulo ? "N/A" : mejorMovimiento.ToLAN())}</color>\n" +
                        $"Tiempo: <color={AZUL_PASTEL}>{tiempoBusqueda} ms</color>\n" +
                        $"Tabla de transposición: <color={VERDE}>{porcentajeOcupacionTablaTransposicion.ToString("F2")} %</color>\n";
            }
        }

        public DiagnosticoBusqueda Diagnostico
        {
            get
            {
                return diagnostico;
            }
        }

        public Busqueda(Tablero tablero, TipoBusqueda tipoBusqueda, int limiteBusqueda = 0)
        {
            this.tablero = tablero;
            this.tipoBusqueda = tipoBusqueda;
            this.limiteBusqueda = limiteBusqueda;
            evaluacion = new Evaluacion(tablero);
            ordenadorMovimientos = new OrdenadorMovimientos(tablero);
            tablaTransposicion = new TablaTransposicion(TAM_TABLA_TRANSPOSICION_MB);
        }

        public Movimiento EmpezarBusqueda(List<Movimiento> movimientosIniciales = null)
        {
            // Inicializar datos de diagnóstico
            diagnostico = new DiagnosticoBusqueda();
            Stopwatch cronometro = new Stopwatch();
            cronometro.Start();
            BusquedaDebugger.Instancia?.Log("Empezando búsqueda\n");

            // Generar los movimientos iniciales si no los hay
            movimientosIniciales ??= tablero.GenerarMovimientosLegales(acortarGeneracion: true).movimientosLegales;
            int mejorEvaluacion; Movimiento mejorMovimiento;

            switch (tipoBusqueda)
            {
                case TipoBusqueda.PorProfundidad:
                    {
                        (mejorEvaluacion, mejorMovimiento) = BusquedaFija(movimientosIniciales, limiteBusqueda);
                        break;
                    }
                case TipoBusqueda.PorTiempo:
                    {
                        busquedaCancelada = false;
                        (mejorEvaluacion, mejorMovimiento) = BusquedaIterativa(movimientosIniciales);
                        break;
                    }
                case TipoBusqueda.PorNumeroDeNodos:
                    {
                        (mejorEvaluacion, mejorMovimiento) = BusquedaFija(movimientosIniciales, limiteBusqueda);
                        break;
                    }
                default:
                    {
                        mejorEvaluacion = 0;
                        mejorMovimiento = Movimiento.Nulo;
                        break;
                    }
            }

            if (mejorMovimiento == Movimiento.Nulo)
            {
                mejorMovimiento = movimientosIniciales[0];
            }

            // Guardar datos de diagnóstico
            BusquedaDebugger.Instancia?.Log("Búsqueda terminada con: " + mejorMovimiento.ToLAN() + " (" + mejorEvaluacion + ")\n\n");
            cronometro.Stop();
            diagnostico.TiempoBusqueda = cronometro.ElapsedMilliseconds;
            diagnostico.MejorEvaluacion = mejorEvaluacion;
            diagnostico.MejorMovimiento = mejorMovimiento;
            diagnostico.PorcentajeOcupacionTablaTransposicion = tablaTransposicion.PorcentajeOcupacion;

            return mejorMovimiento;
        }

        public void TerminarBusqueda()
        {
            busquedaCancelada = true;
        }

        private (int mejorEvaluacion, Movimiento mejorMovimiento) BusquedaIterativa(List<Movimiento> movimientos)
        {
            int mejorEvaluacion = -INFINITO;
            Movimiento mejorMovimiento = Movimiento.Nulo;

            /// LIMPIAR LA TABLA DE TRANSPOSICIONES, ANTES DE UNA BÚSQUEDA, NO ES CORRECTO. SIN EMBARGO, HAY ALGÚN ERROR
            /// EN ALGUNA PARTE DEL CÓDIGO Y ESTO HACE QUE MEJORE EN EL FINAL DE PARTIDA
            //tablaTransposicion.Limpiar();

            for (int profundidadBusqueda = 1; profundidadBusqueda <= MAX_PROFUNDIDAD; profundidadBusqueda++)
            {
                BusquedaDebugger.Instancia?.Log("Empezando iteración: " + profundidadBusqueda + "\n");

                mejorEvaluacionEstaIteracion = -INFINITO;
                mejorMovimientoEstaIteracion = Movimiento.Nulo;
                //haBuscadoUnMovimiento = false;

                AlfabetaRaiz(movimientos, profundidadBusqueda);

                if (busquedaCancelada)
                {
                    /*
                    //if (haBuscadoUnMovimiento && mejorEvaluacionEstaIteracion > mejorEvaluacion)
                    if (haBuscadoUnMovimiento)
                    {
                        mejorEvaluacion = mejorEvaluacionEstaIteracion;
                        mejorMovimiento = mejorMovimientoEstaIteracion;
                        BusquedaDebugger.Instancia.Log("Usando resultado de una búsqueda parcial: " + mejorMovimiento.ToLAN() + " con evaluación " + mejorEvaluacion + "\n");
                    }
                    */

                    BusquedaDebugger.Instancia?.Log("Búsqueda cancelada\n");
                    break;
                }
                else
                {
                    diagnostico.MaxProfundidad = profundidadBusqueda;
                    mejorEvaluacion = mejorEvaluacionEstaIteracion;
                    mejorMovimiento = mejorMovimientoEstaIteracion;
                    BusquedaDebugger.Instancia?.Log("Resultado de la iteración: " + mejorMovimiento.ToLAN() + " (" + mejorEvaluacion + ")\n");
                }
            }

            return (mejorEvaluacion, mejorMovimiento);
        }

        private (int mejorEvaluacion, Movimiento mejorMovimiento) BusquedaFija(List<Movimiento> movimientos, int profundidadBusqueda)
        {
            AlfabetaRaiz(movimientos, profundidadBusqueda);
            diagnostico.MaxProfundidad = profundidadBusqueda;
            return (mejorEvaluacionEstaIteracion, mejorMovimientoEstaIteracion);
        }

        //Hacer una búsqueda por número de nodos

        private void AlfabetaRaiz(List<Movimiento> movimientos, int profundidadBusqueda)
        {
            if (busquedaCancelada)
                return;

            int alfa = -INFINITO; // Almacena valores máximos de evaluación
            int beta = INFINITO; // Almacena valores mínimos de evaluación

            int evaluacion = tablaTransposicion.BuscarEvaluacion(tablero.EstadoActual.ZobristHash, profundidadBusqueda, 0, alfa, beta);
            if (evaluacion != TablaTransposicion.BUSQUEDA_FALLIDA)
            {
                mejorMovimientoEstaIteracion = tablaTransposicion.ObtenerMovimientoGuardado(tablero.EstadoActual.ZobristHash);
                mejorEvaluacionEstaIteracion = evaluacion;
            }

            TablaTransposicion.TipoEvaluacion tipoEvaluacion = TablaTransposicion.TipoEvaluacion.CotaSuperior;

            OrdenarMovimientos(movimientos);
            foreach (Movimiento movimiento in movimientos)
            {
                tablero.HacerMovimiento(movimiento, enBusqueda: true);
                evaluacion = -Alfabeta(profundidadBusqueda - 1, 1, -beta, -alfa);
                tablero.DeshacerMovimiento(movimiento);

                if (busquedaCancelada)
                    return;

                if (evaluacion > alfa)
                {
                    // Se encontró un nuevo mejor movimiento en esta posición
                    tipoEvaluacion = TablaTransposicion.TipoEvaluacion.Exacta;
                    mejorMovimientoEstaIteracion = movimiento;
                    mejorEvaluacionEstaIteracion = evaluacion;
                    //haBuscadoUnMovimiento = true;
                    alfa = evaluacion;
                }
            }

            // Guardar posición evaluada (con alfa, puesto que almacena evaluaciones máximas, es decir, la mejor evaluación)
            tablaTransposicion.GuardarEvaluacion(tablero.EstadoActual.ZobristHash, /*maxProfundidad*/ profundidadBusqueda, 0, alfa, tipoEvaluacion, mejorMovimientoEstaIteracion);
        }

        private int Alfabeta(int profundidadRestante, int profundidadDesdeRaiz, int alfa, int beta)
        {
            if (busquedaCancelada)
                    return 0;
            
            int evaluacion = tablaTransposicion.BuscarEvaluacion(tablero.EstadoActual.ZobristHash, profundidadRestante, profundidadDesdeRaiz, alfa, beta);
            if (evaluacion != TablaTransposicion.BUSQUEDA_FALLIDA)
            {
                return evaluacion;
            }

            if (profundidadRestante == 0)
            {
                evaluacion = BuscarCapturas(alfa, beta);
                return evaluacion;
            }

            (List<Movimiento> movimientos, bool jaque) = tablero.GenerarMovimientosLegales(acortarGeneracion: true);
            if (movimientos.Count == 0)
            {
                if (jaque)
                {
                    // Derrota del jugador actual, se penaliza que la derrota esté a muchos movimientos de distancia
                    return -EVALUACION_JAQUE_MATE + profundidadDesdeRaiz;
                }

                // Tablas
                return 0;
            }

            TablaTransposicion.TipoEvaluacion tipoEvaluacion = TablaTransposicion.TipoEvaluacion.CotaSuperior;
            Movimiento mejorMovimientoEstaPosicion = Movimiento.Nulo;

            OrdenarMovimientos(movimientos);
            foreach (Movimiento movimiento in movimientos)
            {
                tablero.HacerMovimiento(movimiento, enBusqueda: true);
                evaluacion = -Alfabeta(profundidadRestante - 1, profundidadDesdeRaiz + 1, -beta, -alfa);
                tablero.DeshacerMovimiento(movimiento);

                if (busquedaCancelada)
                    return 0;

                if (evaluacion >= beta)
                {
                    // Podar
                    tablaTransposicion.GuardarEvaluacion(tablero.EstadoActual.ZobristHash, profundidadRestante, profundidadDesdeRaiz, evaluacion, TablaTransposicion.TipoEvaluacion.CotaInferior, movimiento);
                    return beta;
                }

                if (evaluacion > alfa)
                {
                    // Se encontró un nuevo mejor movimiento en esta posición
                    tipoEvaluacion = TablaTransposicion.TipoEvaluacion.Exacta;
                    mejorMovimientoEstaPosicion = movimiento;
                    alfa = evaluacion;
                }
            }

            tablaTransposicion.GuardarEvaluacion(tablero.EstadoActual.ZobristHash, profundidadRestante, profundidadDesdeRaiz, alfa, tipoEvaluacion, mejorMovimientoEstaPosicion);
            return alfa;
        }

        /// <summary>
        /// Realiza una búsqueda de capturas ("quiescence search" o búsqueda de quietud) para evitar el problema de la inestabilidad
        /// en la evaluación causada por capturas tácticas pendientes.
        /// </summary>
        /// <param name="tablero">Estado actual del tablero.</param>
        /// <param name="alfa">Valor alfa para poda alfa-beta.</param>
        /// <param name="beta">Valor beta para poda alfa-beta.</param>
        /// <returns>El valor de evaluación actualizado tras explorar capturas posibles.</returns>
        /// <remarks>
        /// La búsqueda de quietud extiende la búsqueda solo a movimientos de captura para estabilizar la evaluación.
        /// Fuente: <see href="https://www.chessprogramming.org/Quiescence_Search">Chess Programming - Quiescence Search</see>
        /// </remarks>
        private int BuscarCapturas(int alfa, int beta)
        {
            if (busquedaCancelada)
                    return 0;
            
            int valorEvaluacion = evaluacion.Evaluar();
            if (valorEvaluacion >= beta)
                return beta;
            alfa = Max(alfa, valorEvaluacion);

            (List<Movimiento> movimientosDeCaptura, _) = tablero.GenerarMovimientosLegales(acortarGeneracion: true, soloGenerarCapturas: true);
            OrdenarMovimientos(movimientosDeCaptura);

            foreach (Movimiento movimiento in movimientosDeCaptura)
            {
                tablero.HacerMovimiento(movimiento, enBusqueda : true);
                valorEvaluacion = -BuscarCapturas(-beta, -alfa);
                tablero.DeshacerMovimiento(movimiento);

                if (valorEvaluacion >= beta)
                    return beta;
                alfa = Max(alfa, valorEvaluacion);
            }

            return alfa;
        }

        public static bool EsJaqueMate(int evaluacion)
		{
			if (evaluacion == int.MinValue)
			{
				return false;
			}

			const int maxProfundidadJaqueMate = 1000;
			return Abs(evaluacion) > (EVALUACION_JAQUE_MATE - maxProfundidadJaqueMate);
		}

        public static int ObtenerNumPlysJaqueMate(int evaluacion)
		{
			return EVALUACION_JAQUE_MATE - Abs(evaluacion);

		}

        private void OrdenarMovimientos(List<Movimiento> movimientos)
        {
            int count = movimientos.Count;
            int[] puntuaciones = new int[count];

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

                puntuaciones[i] = puntuacion;
            }

            // Ordenar el array de puntuaciones
            int[] indices = new int[count];
            for (int i = 0; i < count; i++) indices[i] = i;
            Array.Sort(indices, (a, b) => puntuaciones[b].CompareTo(puntuaciones[a]));

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