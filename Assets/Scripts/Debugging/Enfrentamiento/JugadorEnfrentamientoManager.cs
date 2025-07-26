using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using Ajedrez.Core;
using Ajedrez.IA;

namespace Ajedrez.Debugging.Enfrentamiento
{
    public class JugadorEnfrentamientoManager : ClienteTCP
    {
		[SerializeField] private JugadorEnfrentamientoUI jugadorEnfrentamientoUI;
		[SerializeField] private TextAsset libroAperturas;
        [SerializeField] private string nombreJugador;
        private bool conectado;
        private Tablero tablero;
        private JugadorIA jugador;

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
                jugadorEnfrentamientoUI.EscribirDebug("[ERROR] " + logString + "\n" + stackTrace);
            }
            else if (type == LogType.Warning)
            {
                jugadorEnfrentamientoUI.EscribirDebug("[ADVERTENCIA] " + logString);
            }
            else
            {
                jugadorEnfrentamientoUI.EscribirDebug(logString);
            }
        }

		// Start is called before the first frame update
		void Start()
		{
			tablero = new Tablero();
			CrearJugador();
			jugadorEnfrentamientoUI.MostrarNombre(nombreJugador);
        }

		protected override void AlConectarse()
		{
			base.AlConectarse();

			string mensaje = MensajeEnfrentamiento.CrearMensajeRegistrarJugador(nombreJugador).ToJsonString();
			EnviarMensajeAlServidor(mensaje);
			conectado = true;
			jugadorEnfrentamientoUI.MostrarConectado();
		}

        protected override void MensajeRecibido(TcpClient cliente, string mensaje)
		{
			base.MensajeRecibido(cliente, mensaje);

			// Parse message
			MensajeEnfrentamiento mensajeEnfrentamiento = MensajeEnfrentamiento.CrearDesdeJson(mensaje);
			switch (mensajeEnfrentamiento.tipoMensaje)
			{
				case MensajeEnfrentamiento.TipoMensaje.NuevaPartida:
					EmpezarNuevaPartida(mensajeEnfrentamiento.fenInicial, mensajeEnfrentamiento.colorPiezas, mensajeEnfrentamiento.tiempoMaximoPensamientoMs);
					break;
				case MensajeEnfrentamiento.TipoMensaje.HacerMovimiento:
					MovimientoRivalRecibido(mensajeEnfrentamiento.nombreMovimiento);
					break;
			}
		}

		private void EmpezarNuevaPartida(string fen, Pieza.Color colorPiezas, int maxTiempoPensar)
		{
			tablero = new Tablero(fen);

			if (jugador == null)
			{
				jugador = new JugadorIA(nombreJugador, colorPiezas, tablero, null, ConfiguracionIA.CrearPersonalizada(Busqueda.TipoBusqueda.PorTiempo, maxTiempoPensar, true, 8, libroAperturas));
			}
			else
			{
				jugador.ColorPiezas = colorPiezas;
				jugador.Tablero = tablero;
			}

			// Actualizar UI
			jugadorEnfrentamientoUI.MostrarTablero(fen);
			jugadorEnfrentamientoUI.MostrarColorPiezas(colorPiezas.ToString());
			jugadorEnfrentamientoUI.MostrarTiempoPensar(maxTiempoPensar.ToString());

			if (tablero.Turno == colorPiezas)
			{
				ElegirMovimiento();
			}
		}

		private void MovimientoRivalRecibido(string nombreMovimiento)
		{
			jugadorEnfrentamientoUI.MostrarMovimientoRecibido(nombreMovimiento);
			Movimiento movimiento = new Movimiento(nombreMovimiento, tablero);
			tablero.HacerMovimiento(movimiento);
			ElegirMovimiento();
		}

		private async void ElegirMovimiento()
		{
			Movimiento movimiento = await jugador.HallarMejorMovimiento();
			tablero.HacerMovimiento(movimiento);
			string movimientoLAN = movimiento.ToLAN();
			MensajeEnfrentamiento mensajeEnfrentamiento = MensajeEnfrentamiento.CrearMensajeMovimiento(movimientoLAN);

			mensajeEnfrentamiento.profundidadBusquedaIterativa = jugador.Busqueda.Diagnostico.MaxProfundidad;

			string mensaje = mensajeEnfrentamiento.ToJsonString();
			EnviarMensajeAlServidor(mensaje);
			jugadorEnfrentamientoUI.MostrarMovimientoEnviado(movimientoLAN);
		}
    }
}