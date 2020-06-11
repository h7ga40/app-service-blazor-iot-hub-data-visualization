using System;
using System.Collections.Generic;
using System.Text;

namespace IoTHubReader.Shared
{
	public class Telemetry
	{
		public DateTime Date { get; set; }

		public double Temperature { get; set; }

		public double Humidity { get; set; }

		public string Summary { get; set; }

		public double TemperatureF => 32 + (int)(Temperature / 0.5556);
	}
}
