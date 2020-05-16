using System;
using System.Collections.Generic;
using System.Text;

namespace IoTHubReader.Shared
{
	public class Telemetry
	{
		public DateTime Date { get; set; }

		public int TemperatureC { get; set; }

		public int Humidity { get; set; }

		public string Summary { get; set; }

		public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
	}
}
