using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

// Escucha datos en un NetworkStream en un hilo separado.
// Lanza el evento alRecibirMensaje en el hilo principal cuando se ha leído un mensaje.
// Crea una instancia usando EscuchadorMensajes.CrearInstancia(TcpClient)
namespace Ajedrez.Debugging.Enfrentamiento
{
	public class EscuchadorMensajes : MonoBehaviour
	{
		const int RETARDO_MILISEGUNDOS = 10;

		public event System.Action<TcpClient, string> AlRecibirMensaje;

		TcpClient cliente;
		ConcurrentQueue<string> colaMensajes;

		void Awake()
		{
			colaMensajes = new ConcurrentQueue<string>();
		}

		// Procesa los mensajes en el hilo principal
		void Update()
		{
			if (colaMensajes.Count > 0)
			{
				string mensaje;
				if (colaMensajes.TryDequeue(out mensaje))
				{
					AlRecibirMensaje?.Invoke(cliente, mensaje);
				}
			}
		}

		async void EscucharMensajes()
		{
			NetworkStream flujo = cliente.GetStream();

			while (true)
			{
				if (flujo.DataAvailable)
				{
					byte[] datos = new byte[256];
					string mensajeRecibido = string.Empty;

					int cantidadBytes = flujo.Read(datos, 0, datos.Length);
					mensajeRecibido = System.Text.Encoding.ASCII.GetString(datos, 0, cantidadBytes);

					if (!string.IsNullOrEmpty(mensajeRecibido))
					{
						colaMensajes.Enqueue(mensajeRecibido);
					}
				}
				else
				{
					await Task.Delay(RETARDO_MILISEGUNDOS);
				}
			}
		}

		public static EscuchadorMensajes CrearInstancia(TcpClient cliente, string nombre = "Escuchador")
		{
			GameObject objeto = new GameObject(nombre);
			DontDestroyOnLoad(objeto);
			EscuchadorMensajes escuchador = objeto.AddComponent<EscuchadorMensajes>();
			escuchador.cliente = cliente;
			new Task(() => escuchador.EscucharMensajes()).Start();

			return escuchador;
		}
	}
}