using System;
using System.Collections.Generic;
using System.Text;

namespace IoTHubReader.Shared
{
	public class WSMessage
	{
		public string IotData { get; set; }
		public DateTime MessageDate { get; set; }
		public string DeviceId { get; set; }
	}
}
