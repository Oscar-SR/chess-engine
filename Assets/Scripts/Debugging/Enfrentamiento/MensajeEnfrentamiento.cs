using UnityEngine;
using Ajedrez.Core;

namespace Ajedrez.Debugging.Enfrentamiento
{
	[System.Serializable]
	public class MensajeEnfrentamiento
	{
		// Tipos de mensaje que se pueden enviar
		public enum TipoMensaje { Ninguno, RegistrarJugador, HacerMovimiento, NuevaPartida }
		public TipoMensaje tipoMensaje;

		// Datos para el mensaje de RegistrarJugador:
		public string nombreJugador;

		// Datos para el mensaje de RealizarMovimiento:
		public string nombreMovimiento;
		public int profundidadBusquedaIterativa;

		// Datos para el mensaje de NuevaPartida:
		public string fenInicial;
		public Pieza.Color colorPiezas;
		public int tiempoMaximoPensamientoMs;

		// Método para crear un mensaje de registro de jugador
		public static MensajeEnfrentamiento CrearMensajeRegistrarJugador(string nombreJugador)
		{
			MensajeEnfrentamiento mensaje = new MensajeEnfrentamiento()
			{
				tipoMensaje = TipoMensaje.RegistrarJugador,
				nombreJugador = nombreJugador
			};

			return mensaje;
		}

		// Método para crear un mensaje de nueva partida
		public static MensajeEnfrentamiento CrearMensajeNuevaPartida(string fen, Pieza.Color color, int tiempoMaximoPensamientoMs)
		{
			MensajeEnfrentamiento mensaje = new MensajeEnfrentamiento()
			{
				tipoMensaje = TipoMensaje.NuevaPartida,
				fenInicial = fen,
				colorPiezas = color,
				tiempoMaximoPensamientoMs = tiempoMaximoPensamientoMs
			};

			return mensaje;
		}

		// Método para crear un mensaje de movimiento
		public static MensajeEnfrentamiento CrearMensajeMovimiento(string movimiento)
		{
			MensajeEnfrentamiento mensaje = new MensajeEnfrentamiento()
			{
				tipoMensaje = TipoMensaje.HacerMovimiento,
				nombreMovimiento = movimiento
			};

			return mensaje;
		}

		// Convertir este mensaje a formato JSON
		public string ToJsonString()
		{
			return JsonUtility.ToJson(this);
		}

		// Crear mensaje a partir de una cadena JSON
		public static MensajeEnfrentamiento CrearDesdeJson(string json)
		{
			return JsonUtility.FromJson<MensajeEnfrentamiento>(json);
		}
	}
}