using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Ajedrez.Debugging.Enfrentamiento
{
	public abstract class ServidorTCP : MonoBehaviour
	{
		public const int PUERTO = 1300;
		public const string LOCALHOST = "127.0.0.1";

		protected List<TcpClient> clientesConectados;
		ConcurrentQueue<TcpClient> conexionesPendientes;
		TcpListener servidor;

		protected void IniciarServidor()
		{
			clientesConectados = new List<TcpClient>();
			conexionesPendientes = new ConcurrentQueue<TcpClient>();

			servidor = new TcpListener(System.Net.IPAddress.Parse(LOCALHOST), PUERTO);
			servidor.Start();

			Task tarea = new Task(RevisarConexiones);
			tarea.Start();
		}

		protected virtual void Update()
		{
			ProcesarConexiones();
		}

		protected void EnviarMensajeAlCliente(TcpClient cliente, string mensaje)
		{
			byte[] datos = System.Text.Encoding.ASCII.GetBytes(mensaje);
			cliente.GetStream().Write(datos, 0, datos.Length);
		}

		protected abstract void MensajeRecibido(TcpClient cliente, string mensaje);

		async void RevisarConexiones()
		{
			while (true)
			{
				TcpClient cliente = await servidor.AcceptTcpClientAsync();
				conexionesPendientes.Enqueue(cliente);
			}
		}

		protected virtual void OnClienteConectado(TcpClient cliente)
		{
			// Se puede sobrescribir para manejar eventos cuando un cliente se conecta
		}

		void ProcesarConexiones()
		{
			if (conexionesPendientes == null) { return; }

			while (conexionesPendientes.Count > 0)
			{
				if (conexionesPendientes.TryDequeue(out TcpClient clienteConectado))
				{
					string nombreEscuchador = "Servidor escuchando al cliente";
					EscuchadorMensajes escuchador = EscuchadorMensajes.CrearInstancia(clienteConectado, nombreEscuchador);
					escuchador.AlRecibirMensaje += MensajeRecibido;
					clientesConectados.Add(clienteConectado);
					OnClienteConectado(clienteConectado);
				}
				else
				{
					break;
				}
			}
		}

		protected virtual void OnDestroy()
		{
			servidor?.Stop();
		}
	}
}