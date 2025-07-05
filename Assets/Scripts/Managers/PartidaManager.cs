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
        private bool partidaActiva = false;
        private bool primerTurno = true;
        private List<Movimiento> movimientosLegales;
        private List<Movimiento> movimientosLegalesDePieza;
        private Jugador jugadorBlancas;
        private Jugador jugadorNegras;
        private Tablero.TipoSituacionTablero situacionTablero;

        // Start is called before the first frame update
        void Start()
        {
            if (!PersistenciaSystem.Instancia.HayPartida())
            {
                SceneManager.LoadScene("ConfiguracionesPrevias");
            }

            partida = PersistenciaSystem.Instancia.partida;
            
            /*
            // Crear instancia del tablero
            Tablero tablero = new Tablero("rnbqkbnr/ppp1pppp/8/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1");

            TextAsset archivoTexto = Resources.Load<TextAsset>("Aperturas");
            string libroAperturas = archivoTexto.text;

            float duracion = 300f;
            float incremento = 5f;

            // Crear las instancias de los jugadores
            JugadorHumano jugador2 = new JugadorHumano("Jugador 1", Pieza.Color.Blancas, duracion);
            //JugadorHumano jugador2 = new JugadorHumano("Jugador 2", Pieza.Color.Negras, duracion);
            //JugadorIA jugador1 = new JugadorIA(JugadorIA.TipoDificultad.Dificil, Pieza.Color.Blancas, duracion, tablero, libroAperturas, incremento);
            JugadorIA jugador1 = new JugadorIA(JugadorIA.TipoDificultad.Dificil, Pieza.Color.Negras, duracion, tablero, libroAperturas, incremento);

            // Crear la instancia de la partida
            partida = new Partida(tablero, jugador1, jugador2, duracion, incremento);
            */

            // Definir orientación del tablero
            bool blancasAbajo;
            if (AjedrezUtils.MismoColor(partida.Jugador1.ColorPiezas, Pieza.Color.Blancas))
            {
                blancasAbajo = true;
                jugadorBlancas = partida.Jugador1;
                jugadorNegras = partida.Jugador2;
            }
            else
            {
                blancasAbajo = false;
                jugadorBlancas = partida.Jugador2;
                jugadorNegras = partida.Jugador1;
            }

            partidaUI.Init(partida.Jugador1.Nombre, partida.Jugador2.Nombre, partida.Duracion, blancasAbajo);
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

            if (AjedrezUtils.MismoColor(jugadorActual.ColorPiezas, Pieza.Color.Blancas))
            {
                partida.Jugador1.TiempoRestante -= Time.deltaTime;
                partidaUI.FormatearTiempoJugadorBlancas(partida.Jugador1.TiempoRestante);

                if (partida.Jugador1.TiempoRestante <= 0)
                {
                    situacionTablero = Tablero.TipoSituacionTablero.SinTiempo;
                    FinalizarPartida(Pieza.Color.Negras);
                }
            }
            else
            {
                partida.Jugador2.TiempoRestante -= Time.deltaTime;
                partidaUI.FormatearTiempoJugadorNegras(partida.Jugador2.TiempoRestante);

                if (partida.Jugador2.TiempoRestante <= 0)
                {
                    situacionTablero = Tablero.TipoSituacionTablero.SinTiempo;
                    FinalizarPartida(Pieza.Color.Blancas);
                }
            }
        }

        private void ActualizarEstadoPartida(Pieza.Color turno)
        {
            // Añadir el incremento de tiempo al jugador correspondiente
            if (jugadorActual != null)
            {
                jugadorActual.TiempoRestante += partida.Incremento;
                if (AjedrezUtils.MismoColor(jugadorActual.ColorPiezas, Pieza.Color.Blancas))
                {
                    partidaUI.FormatearTiempoJugadorBlancas(jugadorActual.TiempoRestante);
                }
                else
                {
                    partidaUI.FormatearTiempoJugadorNegras(jugadorActual.TiempoRestante);
                }
            }

            if (AjedrezUtils.MismoColor(turno, Pieza.Color.Blancas))
            {
                // El jugador actual es el de las blancas
                jugadorActual = jugadorBlancas;

                // Poner el cronómetro de las negras más transparente
                partidaUI.TransparentarTiempoJugadorNegras();
            }
            else
            {
                // El jugador actual es el de las negras
                jugadorActual = jugadorNegras;

                // Poner el cronómetro de las blancas más transparente
                partidaUI.TransparentarTiempoJugadorBlancas();
            }

            if (situacionTablero == Tablero.TipoSituacionTablero.Normal)
            {
                partidaUI.ActivarLineaDivisoria();
            }
            else
            {
                partidaUI.DesactivarLineaDivisoria();
            }

            // Mostrar la situación del tablero
            partidaUI.MostrarSituacionTablero(situacionTablero);

            if (primerTurno)
            {
                partidaActiva = true;
                primerTurno = false;
            }

            // Iniciar el turno del jugador correspondiente
            IniciarTurno();
        }

        private void FinalizarPartida(Pieza.Color ganador)
        {
            partidaActiva = false;
            partidaUI.FinalizarPartida(situacionTablero, ganador);
        }

        private void IniciarTurno()
        {
            if (jugadorActual is JugadorIA jugadorIA)
            {
                IniciarTurnoIA(jugadorIA);
            }
            else if (jugadorActual is JugadorHumano jugadorHumano)
            {
                IniciarTurnoHumano(jugadorHumano);
            }
        }

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
                    return jugadorIA.EmpezarBusqueda(movimientosLegales);
                });

                // Cancelar temporizador si la búsqueda termina antes
                cancelTokenSource.Cancel();

                // Escribir el resultado del diagnóstico en el debugger
                BusquedaDebugger.Instancia.DebugBusqueda(jugadorIA.ObtenerDiagnostico().ToString());
            }

            // Hacer el mejor movimiento
            if (partidaActiva)
                StartCoroutine(HacerMovimiento(mejorMovimiento, animar: true));
        }

        private void IniciarTurnoHumano(JugadorHumano jugadorHumano)
        {
            // Activar la interacción con las piezas del jugador correspondiente
            InputManager.Instancia.ColorInteractuable = jugadorHumano.ColorPiezas;
        }

        private void ActualizarTurno()
        {
            bool jaque;

            // Generar nuevos movimientos legales
            (movimientosLegales, jaque) = partida.Tablero.GenerarMovimientosLegales();

            // Colorear casilla del rey si hay jaque
            tableroUI.ColorearCasillaJaque(jaque, partida.Tablero.ObtenerCasillaRey(partida.Tablero.Turno));

            // Analizar situacion tablero
            situacionTablero = partida.Tablero.ObtenerSituacionTablero(movimientosLegales.Count, jaque);
            switch (situacionTablero)
            {
                case Tablero.TipoSituacionTablero.JaqueMate:
                case Tablero.TipoSituacionTablero.ReyAhogado:
                case Tablero.TipoSituacionTablero.TriplePosicionRepetida:
                case Tablero.TipoSituacionTablero.Regla50Movimientos:
                case Tablero.TipoSituacionTablero.MaterialInsuficiente:
                    {
                        // Comunicar el fin de la partida
                        FinalizarPartida(AjedrezUtils.InversoColor(partida.Tablero.Turno));
                        break;
                    }

                case Tablero.TipoSituacionTablero.Jaque:
                case Tablero.TipoSituacionTablero.Normal:
                    {
                        // Comunicar el cambio de turno
                        ActualizarEstadoPartida(partida.Tablero.Turno);
                        break;
                    }
            }

            // Escribir contenido del estado en el debugger
            EstadoDebugger.Instancia.LogEstadoTablero(partida.Tablero.EstadoActual.ToString());

            // Actualizar contenido del bitboard en el debugger
            BitboardDebugger.Instancia.ActualizarModoDebugBitboard();
        }

        public IEnumerator HacerMovimiento(Movimiento movimiento, bool animar = false)
        {
            if (animar)
            {
                // Se hace la animación del movimiento
                yield return StartCoroutine(tableroUI.HacerMovimientoConAnimacion(movimiento));
            }
            else
            {
                tableroUI.ActualizarTablero(movimiento);
            }

            // Se actualiza el modelo
            partida.Tablero.HacerMovimiento(movimiento);

            // Actualizamos el turno
            ActualizarTurno();
        }

        public Pieza ObtenerPieza(int casilla)
        {
            return partida.Tablero.ObtenerPieza(casilla);
        }

        public void MostrarMovimientosLegalesDePieza(int casilla)
        {
            movimientosLegalesDePieza = movimientosLegales.FindAll(m => m.Origen == casilla);
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