using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Entity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Provider;

namespace Inventory_Management_System
{
    public class AddItemWorker
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly ICosmosDbProvider _cosmoDbProvier;

        public AddItemWorker(TelemetryClient telemetryClient, ICosmosDbProvider cosmosDbProvider)
        {
            _telemetryClient = telemetryClient;
            _cosmoDbProvier = cosmosDbProvider;
        }

        [Function("AddItemWorker")]
        public async Task Run(
            [ServiceBusTrigger("salespro-queue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
            FunctionContext context)
        {
            var metrics = new Dictionary<string, double>();
            var properties = new Dictionary<string, string>();
            var timer = new Stopwatch();
            var isSuccessful = true;

            try
            {
                timer.Start();

                if (message.ApplicationProperties.TryGetValue("CorrelationId", out var correlationId))
                    properties.Add("CorrelationId", correlationId.ToString());

                var bodyString = Encoding.UTF8.GetString(message.Body.ToArray());


                _telemetryClient.TrackTrace($"Processed message body: {bodyString}", SeverityLevel.Information, properties);

                // Deserialize to Product
                var payload = Helper.DeserializeProduct(bodyString);

                if (payload == null)
                {
                    throw new ArgumentException("Failed to deserialize message body - payload is null");
                }

                properties.Add("ProductCode", payload.ProductCode);
                _telemetryClient.TrackEvent("Adding product into CosmosDb", properties);
                _telemetryClient.TrackTrace($"Adding product: {payload.ProductCode}", SeverityLevel.Information, properties);

                await _cosmoDbProvier.AddAsync(payload);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackTrace("Error processing message", SeverityLevel.Error, properties);
                _telemetryClient.TrackException(ex, properties);
                isSuccessful = false;
                throw;
            }
            finally
            {
                timer.Stop();
                metrics.Add("ElapsedSeconds", timer.Elapsed.TotalSeconds);
                properties.Add("Worker Result", isSuccessful.ToString());
                _telemetryClient.TrackEvent("Add Item Worker Completed", properties, metrics);
                _telemetryClient.TrackMetric("ProcessingTimeSeconds", timer.Elapsed.TotalSeconds, properties);
            }
        }
    }
}