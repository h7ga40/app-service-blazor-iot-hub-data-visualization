using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace IoTHubReader.Server
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddControllersWithViews();
			services.AddRazorPages();
			services.AddAzureClients(builder => {
				builder.ConfigureDefaults(Configuration.GetSection("Defaults"));
#if DEBUG
				builder.AddEventHubConsumerClient(Configuration.GetSection("IoTHub")).WithName("IoTHub");
#else
				var consumerGroup = Environment.GetEnvironmentVariable("EVENTHUB_CONSUMER_GROUP");
				var connectionString = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING");
				var eventhubName = Environment.GetEnvironmentVariable("EVENTHUB_NAME");
				builder.AddEventHubConsumerClient(consumerGroup, connectionString, eventhubName).WithName("IoTHub");
#endif
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				app.UseWebAssemblyDebugging();
			}
			else {
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseBlazorFrameworkFiles();
			app.UseStaticFiles();
			app.UseWebSockets();
			app.UseRouting();

			app.Use(async (context, next) => {
				if (context.Request.Path == "/ws") {
					if (context.WebSockets.IsWebSocketRequest) {
						var socket = await context.WebSockets.AcceptWebSocketAsync();
						var socketFinishedTcs = new TaskCompletionSource<object>();

						IoTHubReaderService.AddSocket(socket, socketFinishedTcs);

						await socketFinishedTcs.Task;
					}
					else {
						context.Response.StatusCode = 400;
					}
				}
				else {
					await next();
				}
			});

			app.UseEndpoints(endpoints => {
				endpoints.MapRazorPages();
				endpoints.MapControllers();
				endpoints.MapFallbackToFile("index.html");
			});
		}
	}
}
