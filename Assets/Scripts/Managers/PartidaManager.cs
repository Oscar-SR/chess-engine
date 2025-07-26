using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Ajedrez.UI;
using Ajedrez.IA;
using Ajedrez.Core;
using Ajedrez.Systems;
using Ajedrez.Utilities;
using Ajedrez.Debugging;

namespace Ajedrez.Managers
{
    public class PartidaManager : MonoBehaviour
    {
        private const float TIEMPO_ESPERA_RESULTADO = 3;

        [SerializeField] private PartidaUI partidaUI;
        [SerializeField] private TableroUI tableroUI;
        [SerializeField] private PromocionUI promocionUI;

        private Partida partida;
        private Jugador jugadorActual;
        private Pieza.Color turno;
        private bool partidaActiva = false;
        private bool primerTurno = true;
        private List<Movimiento> movimientosLegalesDePieza;

        // Start is called before the first frame update
        void Start()
        {
            if (!PersistenciaSystem.Instancia.HayPartida())
            {
                SceneManager.LoadScene("ConfiguracionesPrevias");
            }

            partida = PersistenciaSystem.Instancia.partida;
            bool blancasAbajo = PersistenciaSystem.Instancia.blancasAbajo;

            /*
            // Crear instancia del tablero
            Tablero tablero = new Tablero("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1");

            TextAsset archivoTexto = Resources.Load<TextAsset>("Aperturas");
            string libroAperturas = archivoTexto.text;

            float duracion = 10f;
            float incremento = 0f;
            Reloj reloj = new Reloj(duracion, incremento);

            // Crear las instancias de los jugadores
            //JugadorHumano jugadorBlancas = new JugadorHumano("Jugador 1");
            JugadorIA jugadorBlancas = new JugadorIA(JugadorIA.TipoDificultad.Dificil, tablero, libroAperturas, reloj, Pieza.Color.Blancas);
            JugadorIA jugadorNegras = new JugadorIA(JugadorIA.TipoDificultad.Dificil, tablero, libroAperturas, reloj, Pieza.Color.Negras);

            // Crear la instancia de la partida
            partida = new Partida(tablero, jugadorBlancas, jugadorNegras, reloj);

            // Definir orientación del tablero
            bool blancasAbajo = true;
            */

            partidaUI.Init(partida.JugadorBlancas.Nombre, partida.JugadorNegras.Nombre, partida.Reloj.DuracionInicial, blancasAbajo);
            tableroUI.Init(partida.Tablero, blancasAbajo);
            promocionUI.Init(blancasAbajo);
            BitboardDebugger.Instancia.Init(partida.Tablero);
            ActualizarTurno();
        }

        // Update is called once per frame
        void Update()
        {
            if (!partidaActiva)
                return;

            if (turno == Pieza.Color.Blancas)
            {
                partida.Reloj.ConsumirTiempoBlancas(Time.deltaTime);
                partidaUI.FormatearTiempoJugadorBlancas(partida.Reloj.TiempoRestanteBlancas);

                if (partida.Reloj.TiempoAgotadoBlancas)
                {
                    partida.Situacion = SituacionPartida.Tipo.TiempoAgotadoBlancas;
                    FinalizarPartida();
                }
            }
            else
            {
                partida.Reloj.ConsumirTiempoNegras(Time.deltaTime);
                partidaUI.FormatearTiempoJugadorNegras(partida.Reloj.TiempoRestanteNegras);

                if (partida.Reloj.TiempoAgotadoNegras)
                {
                    partida.Situacion = SituacionPartida.Tipo.TiempoAgotadoNegras;
                    FinalizarPartida();
                }
            }
        }

        private void ActualizarEstadoPartida()
        {
            // Añadir el incremento de tiempo al jugador correspondiente
            if (partidaActiva)
            {
                if (turno == Pieza.Color.Negras)
                {
                    partida.Reloj.AplicarIncrementoBlancas();
                    partidaUI.FormatearTiempoJugadorBlancas(partida.Reloj.TiempoRestanteBlancas);
                }
                else
                {
                    partida.Reloj.AplicarIncrementoNegras();
                    partidaUI.FormatearTiempoJugadorNegras(partida.Reloj.TiempoRestanteNegras);
                }
            }

            if (AjedrezUtils.MismoColor(turno, Pieza.Color.Blancas))
            {
                // Poner el cronómetro de las negras más transparente
                partidaUI.TransparentarTiempoJugadorNegras();
            }
            else
            {
                // Poner el cronómetro de las blancas más transparente
                partidaUI.TransparentarTiempoJugadorBlancas();
            }

            if (primerTurno)
            {
                partidaActiva = true;
                primerTurno = false;
            }

            // Iniciar el turno del jugador correspondiente
            IniciarTurno();
        }

        private void FinalizarPartida()
        {
            partidaActiva = false;
            InputManager.Instancia.ColorInteractuable = Pieza.Color.Nada;
            partidaUI.FinalizarPartida(partida.Situacion);
        }

        private async void IniciarTurno()
        {
            jugadorActual = turno == Pieza.Color.Blancas ? partida.JugadorBlancas : partida.JugadorNegras;

            if (jugadorActual is JugadorIA jugadorIA)
            {
                Movimiento movimiento = await jugadorIA.HallarMejorMovimiento(movimientosLegales : partida.UltimosMovimientosLegales);
                StartCoroutine(HacerMovimiento(movimiento, esIA: true));
            }
            else if (jugadorActual is JugadorHumano jugadorHumano)
            {
                // Activar la interacción con las piezas del jugador correspondiente
                InputManager.Instancia.ColorInteractuable = turno;
            }
        }

        /*
        private async void IniciarTurnoIA(JugadorIA jugadorIA)
        {
            Movimiento mejorMovimiento;

            if (partida.Tablero.NumMovimientosTotales <= LibroAperturas.MAX_MOVIMIENTOS_LIBRO && jugadorIA.BuscarMovimientoLibro(partida.Tablero.ToFEN(incluirPeonAlPaso : false), out string movimientoLAN))
            {
                mejorMovimiento = new Movimiento(movimientoLAN, partida.Tablero);
            }
            else
            {
                CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

                // Lanzar una tarea que cancela la búsqueda después del tiempo requerido de búsqueda
                Task delayTask = Task.Delay(jugadorIA.ObtenerTiempoBusqueda(partida.Tablero.NumMovimientosTotales), cancelTokenSource.Token)
                .ContinueWith(_ =>
                {
                    if (!cancelTokenSource.IsCancellationRequested)
                        jugadorIA.TerminarBusqueda();
                });

                // Ejecutar la búsqueda como una tarea en segundo plano y esperar a que termine
                mejorMovimiento = await Task.Run(() =>
                {
                    return jugadorIA.EmpezarBusqueda(partida.UltimosMovimientosLegales);
                });

                // Cancelar temporizador si la búsqueda termina antes
                cancelTokenSource.Cancel();

                // Escribir el resultado del diagnóstico en el debugger
                BusquedaDebugger.Instancia.DebugBusqueda(jugadorIA.ObtenerDiagnostico().ToString());
            }

            // Hacer el mejor movimiento
            StartCoroutine(HacerMovimiento(mejorMovimiento, esIA: true));
        }
        */

        private void ActualizarTurno()
        {
            turno = partida.Tablero.Turno;

            // Colorear casilla del rey si hay jaque
            tableroUI.ColorearCasillaJaque(partida.Jaque, partida.Tablero.ObtenerCasillaRey(turno));

            // Analizar situacion tablero
            if (partida.Situacion == SituacionPartida.Tipo.EnCurso)
            {
                // Comunicar el cambio de turno
                ActualizarEstadoPartida();
            }
            else
            {
                // Comunicar el fin de la partida
                FinalizarPartida();
            }

            // Escribir contenido del estado en el debugger
            EstadoDebugger.Instancia.LogEstadoTablero(partida.Tablero.EstadoActual.ToString());

            // Actualizar contenido del bitboard en el debugger
            BitboardDebugger.Instancia.ActualizarModoDebugBitboard();
        }

        private void MovimientoElegido(Movimiento movimiento)
        {
            StartCoroutine(HacerMovimiento(movimiento, jugadorActual is JugadorIA));
        }

        public IEnumerator HacerMovimiento(Movimiento movimiento, bool esIA = false)
        {
            if (!partidaActiva)
                yield break;

            if (esIA)
            {
                // Se hace la animación del movimiento
                yield return StartCoroutine(tableroUI.HacerMovimiento(movimiento));
            }
            else
            {
                tableroUI.ActualizarTablero(movimiento);
            }

            // Se actualiza el modelo
            partida.HacerMovimiento(movimiento);

            // Actualizamos el turno
            ActualizarTurno();
        }

        public Pieza ObtenerPieza(int casilla)
        {
            return partida.Tablero.ObtenerPieza(casilla);
        }

        public void MostrarMovimientosLegalesDePieza(int casilla)
        {
            movimientosLegalesDePieza = partida.UltimosMovimientosLegales.FindAll(m => m.Origen == casilla);
            tableroUI.MostrarMovimientosLegales(movimientosLegalesDePieza);
        }

        public void OcultarMovimientosLegales()
        {
            tableroUI.OcultarMovimientosLegales(movimientosLegalesDePieza);
            movimientosLegalesDePieza.Clear();
        }

        public Movimiento ObtenerMovimientoLegal(int destino)
        {
            foreach (Movimiento movimiento in movimientosLegalesDePieza)
            {
                if (movimiento.Destino == destino)
                {
                    return movimiento;
                }
            }

            return Movimiento.Nulo;
        }

        public void ArrastrarPieza(int casillaPieza, Vector2 cursor)
        {
            tableroUI.ArrastrarPieza(casillaPieza, cursor);
        }

        public bool ObtenerCasillaDeCoordenada(Vector2 worldPos, out int casilla)
        {
            return tableroUI.ObtenerCasillaDeCoordenada(worldPos, out casilla);
        }

        public void DibujarPiezaPorEncima(int casilla)
        {
            tableroUI.DibujarPiezaPorEncima(casilla);
        }

        public void ColorearCasillaUltimoMovimiento(int casilla)
        {
            tableroUI.ColorearCasillaUltimoMovimiento(casilla);
        }

        public void PosicionarPieza(int origen, int destino, int flagMovimiento)
        {
            tableroUI.PosicionarPieza(origen, destino, flagMovimiento);
        }

        public void DevolverPiezaOrigen(int casilla)
        {
            tableroUI.DevolverPiezaOrigen(casilla);
        }

        public void DevolverPiezaACapa(int casilla)
        {
            tableroUI.DevolverPiezaACapa(casilla);
        }

        public void DevolverColoresCasillasUltimoMovimiento()
        {
            tableroUI.DevolverColoresCasillasUltimoMovimiento();
        }

        public void ColorearCasillaOriginal(int casilla)
        {
            tableroUI.ColorearCasillaOriginal(casilla);
        }
    }
}