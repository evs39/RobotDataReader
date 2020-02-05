using System;
using System.Collections.Generic;
using System.Text;

namespace DataReader.Main.Config
{
	public class ClientSocketConfig
	{
		public short Port { get; set; }
		public string Ip { get; set; }
		public byte[] DataArray { get; set; }
	}
}
