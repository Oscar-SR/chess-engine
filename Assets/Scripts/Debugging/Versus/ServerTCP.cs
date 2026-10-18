using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Chess.Debugging.Versus
{
	public abstract class ServerTCP : MonoBehaviour
	{
		public const int PORT = 1300;
		public const string LOCALHOST = "127.0.0.1";

		protected List<TcpClient> connectedClients;
		ConcurrentQueue<TcpClient> pendingConnections;
		TcpListener server;

		protected void StartServer()
		{
			connectedClients = new List<TcpClient>();
			pendingConnections = new ConcurrentQueue<TcpClient>();

			server = new TcpListener(System.Net.IPAddress.Parse(LOCALHOST), PORT);
			server.Start();

			Task task = new Task(ReviewConnections);
			task.Start();
		}

		protected virtual void Update()
		{
			ProcessConnections();
		}

		protected void SendMessageToClient(TcpClient client, string message)
		{
			byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
			client.GetStream().Write(data, 0, data.Length);
		}

		protected abstract void MessageReceived(TcpClient client, string message);

		async void ReviewConnections()
		{
			while (true)
			{
				TcpClient client = await server.AcceptTcpClientAsync();
				pendingConnections.Enqueue(client);
			}
		}

		protected virtual void OnClientConnected(TcpClient client)
		{
			// Can be overridden to handle events when a client connects
		}

		void ProcessConnections()
		{
			if (pendingConnections == null) { return; }

			while (pendingConnections.Count > 0)
			{
				if (pendingConnections.TryDequeue(out TcpClient connectedClient))
				{
					string listenerName = "Server listening to client";
					MessageListener listener = MessageListener.CreateInstance(connectedClient, listenerName);
					listener.OnMessageReceived += MessageReceived;
					connectedClients.Add(connectedClient);
					OnClientConnected(connectedClient);
				}
				else
				{
					break;
				}
			}
		}

		protected virtual void OnDestroy()
		{
			server?.Stop();
		}
	}
}