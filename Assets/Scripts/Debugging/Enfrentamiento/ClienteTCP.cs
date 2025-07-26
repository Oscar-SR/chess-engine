using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Ajedrez.Debugging.Enfrentamiento
{
	public class ClienteTCP : MonoBehaviour
	{
		TcpClient cliente;
		EscuchadorMensajes escuchador;

		protected virtual void Update()
		{
			if (cliente != null && escuchador == null)
			{
				string listenerName = "Escuchador del jugador";
				escuchador = EscuchadorMensajes.CrearInstancia(cliente, listenerName);
				escuchador.AlRecibirMensaje += MensajeRecibido;
				AlConectarse();
			}
		}

		protected virtual void AlConectarse()
		{

		}

		protected void EnviarMensajeAlServidor(string mensaje)
		{
			byte[] data = System.Text.Encoding.ASCII.GetBytes(mensaje);
			cliente.GetStream().Write(data, 0, data.Length);
		}

		protected virtual void MensajeRecibido(TcpClient cliente, string mensaje)
		{

		}

		public void CrearJugador()
		{
			new Task(IntentarConectarseAlServidor).Start();
		}

		async void IntentarConectarseAlServidor()
		{
			while (cliente == null)
			{
				try
				{
					cliente = new TcpClient(ServidorTCP.LOCALHOST, ServidorTCP.PUERTO);
				}
				catch
				{
					await Task.Delay(10);
				}
			}
		}

		void OnDestroy()
		{
			cliente?.GetStream().Close();
			cliente?.Close();
		}

	}
}