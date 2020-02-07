﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DataReader.Main.Config
{
	public class ConfigFile
	{
		public string IpAddress { get; set; }
		public ushort PortNumber { get; set; }
		public List<int> UsedAxisNumbers { get; set; }
	}
}
