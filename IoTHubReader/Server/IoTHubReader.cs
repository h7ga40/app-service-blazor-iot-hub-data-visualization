using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using IoTHubReader.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoTHubReader.Server
{
	public class IoTHubReaderService : BackgroundService
	{
		private readonly ILogger<IoTHubReaderService> _logger;
		private readonly EventHubConsumerClient _resultsClient;
		List<Tuple<Task, CancellationTokenSource>> receiveHandlers = new List<Tuple<Task, CancellationTokenSource>>();

		static List<Tuple<WebSocket, TaskCompletionSource<object>>> webSockets = new List<Tuple<WebSocket, TaskCompletionSource<object>>>();
		CancellationTokenSource disposalTokenSource = new CancellationTokenSource();
		CancellationTokenSource readEventsCanseler = new CancellationTokenSource();

		public IoTHubReaderService(
			IAzureClientFactory<EventHubConsumerClient> producerFactory,
			ILogger<IoTHubReaderService> logger,
			IConfiguration configuration)
		{
			_logger = logger;
			_resultsClient = producerFactory.CreateClient("IoTHub");
			readEventsCanseler.Token.Register(() => {
				_logger.LogInformation("ReadEventsFromPartitionAsync cansel");
			});
		}

		internal delegate void StartReadMessageCallback(ReadOnlyMemory<byte> body, DateTimeOffset enqueuedTime, string deviceId);

		internal async Task startReadMessage(StartReadMessageCallback startReadMessageCallback)
		{
			_logger.LogInformation("Successfully created the EventHub Client from IoT Hub connection string.");

			var partitionIds = await _resultsClient.GetPartitionIdsAsync();
			_logger.LogInformation("The partition ids are: " + String.Join(", ", partitionIds));

			foreach (var id in partitionIds) {
				var task = new Task(async () => {
					await foreach (var message in _resultsClient.ReadEventsFromPartitionAsync(id, EventPosition.FromEnqueuedTime(DateTime.Now), readEventsCanseler.Token)) {
						var deviceId = (string)message.Data.SystemProperties["iothub-connection-device-id"];
						startReadMessageCallback(message.Data.Body, message.Data.EnqueuedTime, deviceId);
					}
				});
				task.Start();
				receiveHandlers.Add(new Tuple<Task, CancellationTokenSource>(task, readEventsCanseler));
			}
		}

		internal static void AddSocket(WebSocket socket, TaskCompletionSource<object> socketFinishedTcs)
		{
			webSockets.Add(new Tuple<WebSocket, TaskCompletionSource<object>>(socket, socketFinishedTcs));
		}

		// Close connection to Event Hub.
		internal async Task stopReadMessage()
		{
			var disposeHandlers = new List<Task>();
			foreach (var receiveHandler in this.receiveHandlers) {
				var task = new Task(() => receiveHandler.Item2.Cancel());
				disposeHandlers.Add(task);
				task.Start();
			};
			Task.WaitAll(disposeHandlers.ToArray());

			await this._resultsClient.CloseAsync();
		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Starting processor host");
			return base.StartAsync(cancellationToken);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			disposalTokenSource.Cancel();
			foreach (var webSocket in webSockets) {
				webSocket.Item2.TrySetCanceled();
			}
			return base.StopAsync(cancellationToken);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await startReadMessage(async (message, date, deviceId) => {
				var payload = new WSMessage {
					IotData = Convert.ToBase64String(message.ToArray()),
					MessageDate = date.UtcDateTime,
					DeviceId = deviceId
				};

				var json = JsonSerializer.Serialize(payload);

				foreach (var webSocket in webSockets) {
					await webSocket.Item1.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(json)), WebSocketMessageType.Text, true, disposalTokenSource.Token);
				}
			});
			Task.WaitAll(receiveHandlers.Select((hnd) => hnd.Item1).ToArray());
		}
	}
}
