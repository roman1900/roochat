using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EncryptAuth;

namespace roochat
{
	class Program
	{
        const string encrypted_auth = "U3zlTLihG/6keZB/e8OTyYHg9WnP/CyPyNhtfKRPjkzutarSFthqFcUglCphNfqotEwcvxrZ8B0dOSdtQKvuVLgG2JRfp8xyF9Smo3ECdjg=";
        public static string oauth;
		public static async Task<int> Main(string[] args)
		{

			if (args.Length < 2)
			{
				Console.Error.WriteLine("Usage: WebSocketSample <URL> <Password>");
				Console.Error.WriteLine("");
				Console.Error.WriteLine("To connect to an ASP.NET Connection Handler, use 'ws://example.com/path/to/hub' or 'wss://example.com/path/to/hub' (for HTTPS)");
				return 1;
			}
            
            oauth = EncryptAuth.StringCipher.Decrypt(encrypted_auth,args[1]);
            
			await RunWebSockets(args[0]);
			return 0;
		}

		private static async Task RunWebSockets(string url)
		{
			var ws = new ClientWebSocket();
			await ws.ConnectAsync(new Uri(url), CancellationToken.None);

			Console.WriteLine("Connected");

			var bytes = Encoding.UTF8.GetBytes(oauth);
			await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
			bytes = Encoding.UTF8.GetBytes("NICK romancodes");
			await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
            bytes = Encoding.UTF8.GetBytes("CAP REQ :twitch.tv/tags");
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
			var sending = Task.Run(async () =>
			{
				string line;
				while ((line = Console.ReadLine()) != null)
				{
					var bytes = Encoding.UTF8.GetBytes(line);
					await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
				}

				await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
			});

			var receiving = Receiving(ws);



			await Task.WhenAll(sending, receiving);
		}

		private static async Task Receiving(ClientWebSocket ws)
		{
			var buffer = new byte[2048];

			while (true)
			{
				var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

				if (result.MessageType == WebSocketMessageType.Text)
				{
					string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
					Console.WriteLine(response);
				}
				else if (result.MessageType == WebSocketMessageType.Binary)
				{
				}
				else if (result.MessageType == WebSocketMessageType.Close)
				{
					await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
					break;
				}

			}
		}
	}
}
