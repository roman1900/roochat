using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EncryptAuth;
using System.Text.Json;

namespace roochat
{
	class Program
	{
        public static Settings settings = new Settings();
        public static Endpoints endpoints = null;
        public const String ENDPOINTS_FILE_PATH = "endpoints.json";
		public static async Task<int> Main(string[] args)
		{

			if (args.Length < 1)
			{
				Console.Error.WriteLine("Usage: roochat <secret json settings file>");
				Console.Error.WriteLine("");
				Console.Error.WriteLine("");
				return 1;
			}
            if (!File.Exists(ENDPOINTS_FILE_PATH))
            {
                Console.Error.WriteLine($"unable to locate Endpoints json file : {ENDPOINTS_FILE_PATH}");
                return 1;
            }
            endpoints = JsonSerializer.Deserialize<Endpoints>(File.ReadAllText(ENDPOINTS_FILE_PATH));

            if (endpoints==null)
            {
                 Console.Error.WriteLine($"problem reading values in Endpoints json file : {ENDPOINTS_FILE_PATH}");
                return 1;
            }   

            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine($"The secret json settings file: {args[0]} does not exist.");
                return 1;
            }

            settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(args[0]));
           
            if (settings==null)
            {
                Console.Error.WriteLine($"problem reading values in settings json file : {args[0]}");
                return 1;
            }         
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            await RunWebSockets(endpoints.Twitch_IRC);
			return 0;
		}

		private static async Task RunWebSockets(string url)
		{
			var ws = new ClientWebSocket();
			await ws.ConnectAsync(new Uri(url), CancellationToken.None);

			Console.WriteLine("Connected");

			var bytes = Encoding.UTF8.GetBytes($"PASS {settings.Password}");
			await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
			bytes = Encoding.UTF8.GetBytes($"NICK {settings.NickName}");
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
                    if (response.StartsWith("PING"))
                    {
                        var bytes = Encoding.UTF8.GetBytes("PONG :tmi.twitch.tv");
					    await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
                    }
                    else 
                    {
                        
					    Console.WriteLine(response);
                    }
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
