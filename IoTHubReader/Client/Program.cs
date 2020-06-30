using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BlazorFileSaver;
using Blazor.FileReader;

namespace IoTHubReader.Client
{
	public class Program
	{
		internal static string SharedAccessSignature;

		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("app");

			builder.Services.AddBlazorFileSaver();
			builder.Services.AddFileReaderService(options => options.UseWasmSharedBuffer = true);

			builder.Services.AddHttpClient("IoTHubReader.Client", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

			builder.Services.AddHttpClient("IoTCentral", client => {
				client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
				client.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("SharedAccessSignature", SharedAccessSignature);
			});

			// Supply HttpClient instances that include access tokens when making requests to the server project
			builder.Services.AddTransient(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("IoTHubReader.Client"));

			await builder.Build().RunAsync();
		}
	}
}
