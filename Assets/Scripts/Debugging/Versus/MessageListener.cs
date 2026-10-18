using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

// Listens for data on a NetworkStream on a separate thread.
// Raises the OnMessageReceived event on the main thread when a message has been read.
// Create an instance using MessageListener.CreateInstance(TcpClient)
namespace Chess.Debugging.Versus
{
	public class MessageListener : MonoBehaviour
	{
		const int DELAY_MILLISECONDS = 10;

		public event System.Action<TcpClient, string> OnMessageReceived;

		TcpClient client;
		ConcurrentQueue<string> messageQueue;

		void Awake()
		{
			messageQueue = new ConcurrentQueue<string>();
		}

		// Processes messages on the main thread
		void Update()
		{
			if (messageQueue.Count > 0)
			{
				string message;
				if (messageQueue.TryDequeue(out message))
				{
					OnMessageReceived?.Invoke(client, message);
				}
			}
		}

		async void ListenForMessages()
		{
			NetworkStream stream = client.GetStream();

			while (true)
			{
				if (stream.DataAvailable)
				{
					byte[] data = new byte[256];
					string receivedMessage = string.Empty;

					int byteCount = stream.Read(data, 0, data.Length);
					receivedMessage = System.Text.Encoding.ASCII.GetString(data, 0, byteCount);

					if (!string.IsNullOrEmpty(receivedMessage))
					{
						messageQueue.Enqueue(receivedMessage);
					}
				}
				else
				{
					await Task.Delay(DELAY_MILLISECONDS);
				}
			}
		}

		public static MessageListener CreateInstance(TcpClient client, string name = "Listener")
		{
			GameObject gameObject = new GameObject(name);
			DontDestroyOnLoad(gameObject);
			MessageListener listener = gameObject.AddComponent<MessageListener>();
			listener.client = client;
			new Task(() => listener.ListenForMessages()).Start();

			return listener;
		}
	}
}