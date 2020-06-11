using IoTHubReader.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IoTHubReader.Server.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TelemetryController : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private readonly ILogger<TelemetryController> logger;

		public TelemetryController(ILogger<TelemetryController> logger)
		{
			this.logger = logger;
		}

		[HttpGet]
		public IEnumerable<Telemetry> Get()
		{
			var rng = new Random();
			var date = DateTime.Now.AddSeconds(-5);
			return Enumerable.Range(1, 5).Select(index => new Telemetry {
				Date = date.AddSeconds(index),
				Temperature = rng.Next(20, 35),
				Humidity = rng.Next(60, 80),
				Summary = Summaries[rng.Next(Summaries.Length)]
			})
			.ToArray();
		}
	}
}
