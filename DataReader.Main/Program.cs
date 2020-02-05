using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DataReader.Main.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataReaderMain
{
	class Program
	{
		static void Main(string[] args)
		{
			ConfigFile cfg;
			byte[] buffer = new byte[3600];

			// Read config file
			try
			{
				using (var fileStream = File.OpenText(args.Length > 0 ? args[1] : "Config.json"))
				using (var jsonReader = new JsonTextReader(fileStream))
				{
					var jconfigFromFile = (JObject)JToken.ReadFrom(jsonReader);
					cfg = jconfigFromFile.ToObject<ConfigFile>();
				}
			}
			catch (FileNotFoundException e)
			{
				Console.WriteLine(e.Message);
				return;
			}

			// Start robot data reading thread
			var thread = new Thread(new ParameterizedThreadStart(ReadSocketData));
			thread.Start(new ClientSocketConfig()
			{
				Ip = cfg.IpAddress,
				Port = cfg.PortNumber,
				DataArray = buffer
			});

			Console.ReadKey();
		}

		/// <summary>
		/// Thread safe data reading
		/// </summary>
		/// <param name="config">Config object</param>
		private static void ReadSocketData(object config)
		{
			var socketConfig = config as ClientSocketConfig;
			using (var client = new TcpClient(socketConfig.Ip, socketConfig.Port))
			{
				var stream = client.GetStream();

				while (client.Connected)
				{
					lock (socketConfig.DataArray)
					{
						stream.Read(socketConfig.DataArray, 0, 3600);
					}
					var str = Encoding.ASCII.GetString(socketConfig.DataArray);
					Console.WriteLine(str);
				}
			}
		}
	}
}
