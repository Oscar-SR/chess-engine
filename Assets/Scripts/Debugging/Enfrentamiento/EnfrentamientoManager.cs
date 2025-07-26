using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using UnityEngine;
using Ajedrez.Core;
using Ajedrez.UI;
using Ajedrez.Utilities;

namespace Ajedrez.Debugging.Enfrentamiento
{
    public class JugadorRemoto : Jugador
    {
        private TcpClient cliente;
        private int numVictorias;
        private int numDerrotas;
        private int numEmpates;
        private int numMovimientos;
        private int totalProfundidadBuscada;

        public JugadorRemoto(string nombre, Pieza.Color colorPiezas, TcpClient cliente)
            : base(nombre, colorPiezas)
        {
            this.cliente = cliente;
        }

        public TcpClient Cliente
        {
            get => cliente;
            set => cliente = value;
        }

        public int NumVictorias
        {
            get => numVictorias;
            set => numVictorias = value;
        }

        public int NumDerrotas
        {
            get => numDerrotas;
            set => numDerrotas = value;
        }

        public int NumEmpates
        {
            get => numEmpates;
            set => numEmpates = value;
        }

        public int NumMovimientos
        {
            get => numMovimientos;
            set => numMovimientos = value;
        }

        public int TotalProfundidadBuscada
        {
            get => totalProfundidadBuscada;
            set => totalProfundidadBuscada = value;
        }
    }

    public class EnfrentamientoManager : ServidorTCP
    {
        [SerializeField] private TableroUI tableroUI;
        [SerializeField] private EnfrentamientoUI enfrentamientoUI;
        [SerializeField] private int maxTiempoPensar = 1000;
        [SerializeField] private int maxMovimientosPartida = 100;
        [SerializeField] private TextAsset posicionesIniciales;
        [SerializeField] private bool ambosJueganCadaPosicion = true;
        [SerializeField] string rutaDeGuardado;

        private Partida partida;
        private JugadorRemoto jugador1;
        private JugadorRemoto jugador2;
        private int indicePartida;
        private int numPartidas;
        private string[] posicionesInicialesFEN;

        void OnEnable()
        {
            Application.logMessageReceived += ManejarLog;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= ManejarLog;
        }

        void ManejarLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                enfrentamientoUI.EscribirDebug("[ERROR] " + logString + "\n" + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                enfrentamientoUI.EscribirDebug("[ADVERTENCIA] " + logString);
            }
            else
            {
                enfrentamientoUI.EscribirDebug(logString);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            posicionesInicialesFEN = posicionesIniciales.text.Split('\n');
            indicePartida = -1;
            numPartidas = ambosJueganCadaPosicion ? posicionesInicialesFEN.Length * 2 : posicionesInicialesFEN.Length;
            partida = new Partida();
            tableroUI.Init(null);
            enfrentamientoUI.Init(maxTiempoPensar, maxMovimientosPartida);
            enfrentamientoUI.ActualizarEstadisticas(numPartidas, indicePartida, jugador1, jugador2);
            IniciarServidor();
        }

        public void EmpezarNuevaPartida()
        {
            indicePartida++;
			int indiceFEN = ambosJueganCadaPosicion ? indicePartida / 2 : indicePartida;

            if (indiceFEN < posicionesInicialesFEN.Length)
            {
                // Cargar posición del tablero
                partida.CargarTableroFEN(posicionesInicialesFEN[indiceFEN]);

                if (indicePartida > 0)
                {
                    // Intercambiar los colores
                    partida.IntercambiarColoresJugadores();
                }

                // Actualizar la UI
                tableroUI.CambiarPosicion(partida.Tablero);
                enfrentamientoUI.EstablecerNombreBlancas(partida.JugadorBlancas.Nombre);
                enfrentamientoUI.EstablecerNombreNegras(partida.JugadorNegras.Nombre);

                string mensajeBlancas = MensajeEnfrentamiento.CrearMensajeNuevaPartida(partida.FenInicioPartida, Pieza.Color.Blancas, maxTiempoPensar).ToJsonString();
                string mensajeNegras = MensajeEnfrentamiento.CrearMensajeNuevaPartida(partida.FenInicioPartida, Pieza.Color.Negras, maxTiempoPensar).ToJsonString();

                EnviarMensajeAlCliente(((JugadorRemoto)partida.JugadorBlancas).Cliente, mensajeBlancas);
                EnviarMensajeAlCliente(((JugadorRemoto)partida.JugadorNegras).Cliente, mensajeNegras);
            }

            enfrentamientoUI.ActualizarEstadisticas(numPartidas, indicePartida, jugador1, jugador2);
        }

        protected override void OnClienteConectado(TcpClient cliente)
		{
			base.OnClienteConectado(cliente);
            enfrentamientoUI.EscribirDebug("Cliente conectado\n");
		}

        protected override void MensajeRecibido(TcpClient cliente, string mensaje)
        {
            MensajeEnfrentamiento mensajeEnfrentamiento = MensajeEnfrentamiento.CrearDesdeJson(mensaje);
            switch (mensajeEnfrentamiento.tipoMensaje)
            {
                case MensajeEnfrentamiento.TipoMensaje.RegistrarJugador:
                    RegistrarJugador(cliente, mensajeEnfrentamiento.nombreJugador);
                    break;
                case MensajeEnfrentamiento.TipoMensaje.HacerMovimiento:
                    MovimientoRecibido(mensajeEnfrentamiento.nombreMovimiento, mensajeEnfrentamiento.profundidadBusquedaIterativa);
                    break;
            }
        }

        private void RegistrarJugador(TcpClient cliente, string nombreJugador)
        {
            enfrentamientoUI.EscribirDebug("Registrar " + nombreJugador + "\n");

            if (jugador1 == null)
            {
                jugador1 = new JugadorRemoto(nombreJugador, Pieza.Color.Blancas, cliente);
                partida.JugadorBlancas = jugador1;
			}
            else if (jugador2 == null)
            {
                jugador2 = new JugadorRemoto(nombreJugador, Pieza.Color.Negras, cliente);
                partida.JugadorNegras = jugador2;

                // Ambos jugadores han sido registrados
                enfrentamientoUI.ActivarBotonEmpezar();
            }
            else
            {
                enfrentamientoUI.EscribirDebug("Registro inesperado, ya hay dos jugadores. Nombre: " + nombreJugador + "\n");
            }

            enfrentamientoUI.EscribirNombresJugadores(jugador1, jugador2);
            enfrentamientoUI.ActualizarEstadisticas(numPartidas, indicePartida, jugador1, jugador2);
        }

        private void MovimientoRecibido(string movimientoLAN, int profundidadBusquedaIterativa)
        {
            enfrentamientoUI.EscribirDebug("Movimiento: " + movimientoLAN + "\n");

            JugadorRemoto jugadorActual = AjedrezUtils.MismoColor(partida.Tablero.Turno, Pieza.Color.Blancas) ? (JugadorRemoto)partida.JugadorBlancas : (JugadorRemoto)partida.JugadorNegras;
            jugadorActual.NumMovimientos++;
            jugadorActual.TotalProfundidadBuscada += Mathf.Min(profundidadBusquedaIterativa, 15);

            // Efectuar el movimiento
            Movimiento movimiento = new Movimiento(movimientoLAN, partida.Tablero);
            partida.HacerMovimiento(movimiento);
            StartCoroutine(tableroUI.HacerMovimiento(movimiento, animar: false));
            
			if (partida.MovimientosRealizados.Count / 2 >= maxMovimientosPartida)
            {
                partida.Situacion = SituacionPartida.Tipo.TablasPorArbitro;
            }

            if (partida.Situacion == SituacionPartida.Tipo.EnCurso)
            {
				JugadorRemoto siguienteJugador = AjedrezUtils.MismoColor(partida.Tablero.Turno, Pieza.Color.Blancas) ? (JugadorRemoto)partida.JugadorBlancas : (JugadorRemoto)partida.JugadorNegras;
				string json = MensajeEnfrentamiento.CrearMensajeMovimiento(movimientoLAN).ToJsonString();
				EnviarMensajeAlCliente(siguienteJugador.Cliente, json);
            }
            else
            {
                FinalizarPartida();
            }
        }

        private void FinalizarPartida()
		{
			GuardarPartidaPGN();
			ActualizarEstadisticas();
            enfrentamientoUI.ActualizarEstadisticas(numPartidas, indicePartida, jugador1, jugador2);
            EmpezarNuevaPartida();

			void GuardarPartidaPGN()
            {
                if (string.IsNullOrEmpty(rutaDeGuardado))
                {
                    rutaDeGuardado = Path.Combine(CarpetaGuardadoPartidas, $"Partidas_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                    Directory.CreateDirectory(rutaDeGuardado);
                }

                string subcarpetaResultado = partida.Situacion switch
                {
                    SituacionPartida.Tipo.JaqueMateBlancas => partida.JugadorBlancas == jugador1 ? "Derrota" : "Victoria",
                    SituacionPartida.Tipo.JaqueMateNegras => partida.JugadorBlancas == jugador1 ? "Victoria" : "Derrota",
                    _ => "Tablas"
                };
                string directorioGuardado = Path.Combine(rutaDeGuardado, subcarpetaResultado);
				Directory.CreateDirectory(directorioGuardado);

                string pgn = partida.ToPGN();
				string ruta = Path.Combine(directorioGuardado, $"Game {indicePartida}.txt");
				StreamWriter writer = new StreamWriter(ruta);
				writer.Write(pgn);
				writer.Close();
            }

            void ActualizarEstadisticas()
			{
                if (partida.Situacion == SituacionPartida.Tipo.JaqueMateBlancas)
                {
                    ((JugadorRemoto)partida.JugadorNegras).NumVictorias++;
                    ((JugadorRemoto)partida.JugadorBlancas).NumDerrotas++;
                }
                else if (partida.Situacion == SituacionPartida.Tipo.JaqueMateNegras)
                {
                    ((JugadorRemoto)partida.JugadorBlancas).NumVictorias++;
                    ((JugadorRemoto)partida.JugadorNegras).NumDerrotas++;
                }
                else
                {
                    jugador1.NumEmpates++;
                    jugador2.NumEmpates++;
                }
			}
		}

        public int MaxTiempoPensar
        {
            set
            {
                maxTiempoPensar = value;
            }
        }

        public int MaxMovimientosPartida
        {
            set
            {
                maxMovimientosPartida = value;
            }
        }

        public static string CarpetaGuardadoPartidas => Path.Combine(Application.persistentDataPath, "Partidas enfrentamiento");
    }
}