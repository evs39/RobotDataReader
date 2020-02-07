using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataReader.Main.Converters
{
	public class RobotDataConverter
	{
		private static readonly int _motorVelocityDataAddr = 100;
		private static readonly int _currentDataAddr = 244;
		private static readonly int _torqueDataAddr = 196;
		private static readonly int _motorTemperatureDataAddr = 148;
		private static readonly int _directionAddr = 510;

		private static readonly int _kelvinConstant = 273;

		private DateTime _creatingTime;
		private float _declaredRpm;
		private List<int> _usedAxisNumbers;

		public RobotData ActualRobotData { get; set; }

		public class RobotData
		{
			public TimeSpan Time { get; private set; }
			public List<float> Velocities { get; private set; }
			public List<float> Currents { get; private set; }
			public List<float> Torques { get; set; }
			public List<int> Temperatures { get; set; }
			public string Direction { get; set; }

			public RobotData(DateTime initialTime)
			{
				Time = DateTime.Now - initialTime;
				Velocities = new List<float>();
				Currents = new List<float>();
				Torques = new List<float>();
				Temperatures = new List<int>();
				Direction = null;
			}
		}

		public RobotDataConverter(float declaredRpm, List<int> usedAxisNumbers)
		{
			_declaredRpm = declaredRpm;
			_creatingTime = DateTime.Now;

			if (usedAxisNumbers == null)
			{
				_usedAxisNumbers = new List<int>();

				for (int i = 1; i <= 12; i++)
				{
					_usedAxisNumbers.Add(i);
				}
			}
			else
				_usedAxisNumbers = usedAxisNumbers;
		}

		public bool ConvertRobotData(byte[] dataArray)
		{
			// Check message
			if (dataArray.Length < 570)
				return false;

			// Read data
			ActualRobotData = new RobotData(_creatingTime);

			lock(dataArray)
			{
				foreach(var axisNumber in _usedAxisNumbers)
				{
					ActualRobotData.Velocities.Add(BitConverter.ToSingle(dataArray, _motorVelocityDataAddr + (axisNumber - 1) * 4));
					ActualRobotData.Currents.Add(BitConverter.ToSingle(dataArray, _currentDataAddr + (axisNumber - 1) * 4));
					ActualRobotData.Torques.Add(BitConverter.ToSingle(dataArray, _torqueDataAddr + (axisNumber - 1) * 4));
					ActualRobotData.Temperatures.Add(BitConverter.ToInt32(dataArray, _motorTemperatureDataAddr + (axisNumber - 1) * 4) - _kelvinConstant);

					var directionIndicator = BitConverter.ToInt32(dataArray, _directionAddr);
					switch (directionIndicator)
					{
						case 1:
							{
								ActualRobotData.Direction = "right";
								break;
							}
						case 2:
							{
								ActualRobotData.Direction = "left";
								break;
							}
						default:
							{
								break;
							}
					}
				}
			}

			return true;
		}
	}
}
