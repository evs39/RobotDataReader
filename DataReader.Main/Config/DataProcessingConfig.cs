using System;
using System.Collections.Generic;
using System.Text;

namespace DataReader.Main.Config
{
	public class DataProcessingConfig
	{
		public float DeclaredRpm { get; set; }
		public byte[] MessageBuffer { get; set; }
		public List<int> UsedAxisNumber { get; set; }
	}
}
