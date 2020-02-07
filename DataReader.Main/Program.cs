using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using DataReader.Main.Config;
using DataReader.Main.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataReaderMain
{
	class Program
	{
		private static bool _activeSubthreads = true;

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
			var readingThread = new Thread(new ParameterizedThreadStart(ReadSocketData));
			readingThread.Start(new ClientSocketConfig()
			{
				Ip = cfg.IpAddress,
				Port = cfg.PortNumber,
				DataArray = buffer
			});

			// Start data processing thread
			var processingThread = new Thread(new ParameterizedThreadStart(ProcessRobotData));
			processingThread.Start(new DataProcessingConfig()
			{
				DeclaredRpm = 30,
				MessageBuffer = buffer,
				UsedAxisNumber = cfg.UsedAxisNumbers
			});

			lock(Console.Out)
			{
				Console.SetCursorPosition(0, 2);
				Console.WriteLine("Press any key to finish");
			}

			Console.ReadKey();

			_activeSubthreads = false;

			readingThread.Join();
			processingThread.Join();
		}

		/// <summary>
		/// Thread safe data reading
		/// </summary>
		/// <param name="config">Configuration data</param>
		private static void ReadSocketData(object config)
		{
			while(_activeSubthreads)
			{
				try
				{
					var socketConfig = config as ClientSocketConfig;
					using (var client = new TcpClient(socketConfig.Ip, socketConfig.Port))
					{
						var stream = client.GetStream();

						while (client.Connected && _activeSubthreads)
						{
							lock (socketConfig.DataArray)
							{
								var receivedBytesNumber = stream.Read(socketConfig.DataArray, 0, 3600);
								if (receivedBytesNumber == 0)
									break;
							}
							var str = Encoding.ASCII.GetString(socketConfig.DataArray);

							lock (Console.Out)
							{
								Console.SetCursorPosition(0, 0);
								Console.WriteLine("Received " + Guid.NewGuid());
								Console.SetCursorPosition(0, 3);
							}
						}
					}
				}
				catch(Exception e)
				{

				}
			}
			
		}
		
		/// <summary>
		/// Thread safe data processing
		/// </summary>
		/// <param name="config">Configuration data</param>
		private static void ProcessRobotData(object config)
		{
			// Check config
			var processConfig = config as DataProcessingConfig;
			if (processConfig == null
				|| processConfig.UsedAxisNumber == null
				|| processConfig.MessageBuffer == null)
			{
				Console.WriteLine("Process config was not received");
				return;
			}

			var converter = new RobotDataConverter(processConfig.DeclaredRpm, processConfig.UsedAxisNumber);

			using(var streamWriter = new StreamWriter("Output.txt"))
			{
				// Header
				streamWriter.Write("Time");
				WriteHeaders("AxisVelocity");
				WriteHeaders("AxisCurrent");
				WriteHeaders("AxisTorque");
				WriteHeaders("AxisTemperature");
				streamWriter.WriteLine();

				while (_activeSubthreads)
				{
					Thread.Sleep(500);
					converter.ConvertRobotData(processConfig.MessageBuffer);

					streamWriter.Write(String.Format("{0:0.0000}", converter.ActualRobotData.Time.TotalSeconds));

					foreach (var item in converter.ActualRobotData.Velocities)
					{
						streamWriter.Write("\t" + String.Format("{0:0.0000}", item));
					}

					foreach (var item in converter.ActualRobotData.Currents)
					{
						streamWriter.Write("\t" + String.Format("{0:0.0000}", item));
					}

					foreach (var item in converter.ActualRobotData.Torques)
					{
						streamWriter.Write("\t" + String.Format("{0:0.0000}", item));
					}

					foreach (var item in converter.ActualRobotData.Temperatures)
					{
						streamWriter.Write("\t" + String.Format("{0:0.0000}", item));
					}

					if (converter.ActualRobotData.Direction != null)
						streamWriter.Write("\t" + converter.ActualRobotData.Direction);

					streamWriter.WriteLine();

					lock (Console.Out)
					{
						Console.SetCursorPosition(0, 1);
						Console.WriteLine("Written " + Guid.NewGuid());
						Console.SetCursorPosition(0, 3);
					}
				}
				
				void WriteHeaders(string name)
				{
					foreach (var axisNumber in processConfig.UsedAxisNumber)
					{
						streamWriter.Write("\t" + name + '_' + axisNumber);
					}
				}
			}
		}
	}
}
