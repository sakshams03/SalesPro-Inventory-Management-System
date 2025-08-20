using Azure.Messaging.ServiceBus;
using BusinessLogic;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Models;
using Newtonsoft.Json;
using Provider;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Inventory_Management_System
{
    public class SalesPro
    {
        private readonly TelemetryClient _telemetry;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly IConfiguration _configuration;
        private static Guid guid;
        bool isSucess = true;
        private readonly ICosmosDbProvider _cosmosDbProvider;
        public SalesPro(TelemetryClient telemetryClient, ServiceBusClient serviceBusClient, IConfiguration configuration, ICosmosDbProvider cosmosDbProvider)
        {
            _telemetry = telemetryClient;
            _serviceBusClient = serviceBusClient;
            _configuration = configuration;
            _cosmosDbProvider = cosmosDbProvider;
        }

        [Function("AddItem")]
        public async Task<HttpResponseData> Run1([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            Stopwatch timer = new Stopwatch();
            var  properties = new Dictionary<string, string>();
            timer.Start();
            var startTime = DateTime.UtcNow;
            var validationFailures = new List<string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);

            #region Authorization
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.AddItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return CreateHttpResponse(HttpStatusCode.Forbidden, request, validationFailures);
                }

            }
            #endregion
            try
            {
                using var reader = new StreamReader(request.Body);
                var requestContent = await reader.ReadToEndAsync();
                requestContent = requestContent.Trim();

                if (!Validations.IsValid(requestContent, validationFailures))
                {
                    var response = CreateHttpResponse(HttpStatusCode.BadRequest, request, validationFailures);
                    _telemetry.TrackTrace($"Validation errors: {string.Join(",", validationFailures)}");
                    return response;
                }

                var message = JsonConvert.SerializeObject(requestContent);
                await using var sender = _serviceBusClient.CreateSender(_configuration.GetValue<string>("queueName"));

                var serviceBusMessage = new ServiceBusMessage(message);
                _telemetry.TrackEvent("Sending data to service bus", properties);
                serviceBusMessage.ApplicationProperties.Add("CorrelationID", guid.ToString());// this will help tracking the requests accross multiple components
                await sender.SendMessageAsync(serviceBusMessage);
                _telemetry.TrackTrace("Message sent to servicebus", properties);
                return CreateHttpResponse(HttpStatusCode.Created, request, validationFailures);

            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                _telemetry.TrackException(ex, properties);
                isSucess = false;
                return CreateHttpResponse(HttpStatusCode.BadRequest, request, validationFailures); //since this is a caught exception
            }
            finally
            {
                timer.Stop();
                if (!isSucess)
                    _telemetry.TrackRequest("AddItemRequest", startTime, timer.Elapsed, HttpStatusCode.BadRequest.ToString(), false);
                else
                    _telemetry.TrackRequest("AddItemRequest", startTime, timer.Elapsed, HttpStatusCode.Created.ToString(), true);

            }
            #endregion
        }

        [Function("DeleteItem")]
        public async Task<HttpResponseData> Run2([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            var validationFailures = new List<string>();
            var properties = new Dictionary<string, string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var startTime = DateTime.UtcNow;
            #region Authorization
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.AddItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return CreateHttpResponse(HttpStatusCode.Forbidden, request, validationFailures);
                }

            }
            #endregion
            try
            {
                using var reader = new StreamReader(request.Body);
                var requestContent = await reader.ReadToEndAsync();
                requestContent = requestContent.Trim();
                var payload = JsonConvert.DeserializeObject<DeleteRequest>(requestContent);
                var containerId = _configuration.GetValue<string>("ContainerID");
                _telemetry.TrackEvent($"Performing delete option for product with product code {payload.ProductCode}", properties);
                await _cosmosDbProvider.DeleteAsync<Product>(payload.ProductCode, containerId, payload.Location);
                _telemetry.TrackTrace("Deletion successful", properties);
                return request.CreateResponse(HttpStatusCode.OK);
            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                isSucess = false;
                _telemetry.TrackException(ex, properties);
                validationFailures.Add(ex.Message);
                return CreateHttpResponse(HttpStatusCode.ExpectationFailed, request, validationFailures); //since we don't know what happened here
            }
            finally
            {
                timer.Stop();
                if (!isSucess)
                    _telemetry.TrackRequest("DeleteItemRequest", startTime, timer.Elapsed, HttpStatusCode.BadRequest.ToString(), false);
                else
                    _telemetry.TrackRequest("DeleteItemRequest", startTime, timer.Elapsed, HttpStatusCode.Created.ToString(), true);
            }
            #endregion
        }

        [Function("ListItem")]
        public async Task<HttpResponseData> Run3([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            var validationFailures = new List<string>();
            var properties = new Dictionary<string, string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var startTime = DateTime.UtcNow;
            #region Authorization
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.AddItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return CreateHttpResponse(HttpStatusCode.Forbidden, request, validationFailures);
                }

            }
            #endregion
            try
            {
                var location = request.Query["location"].ToString(); // will never be null
                var category = request?.Query["category"]?.ToString();
                var subCategory = request?.Query["subCategory"]?.ToString();

                var queryDefinition = new QueryDefintionProvider(location, category, subCategory).CreateQueryDefintion();
                
                var containerId = _configuration.GetValue<string>("ContainerID");
                _telemetry.TrackEvent($"Performing search operation based on query parameters", properties);
                var list = await _cosmosDbProvider.ListAsync<Product>(queryDefinition, containerId, location);
                _telemetry.TrackTrace("Search operation successful", properties);
                return CreateHttpResponse(HttpStatusCode.OK, request, list);
            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                isSucess = false;
                _telemetry.TrackException(ex, properties);
                validationFailures.Add(ex.Message);
                return CreateHttpResponse(HttpStatusCode.ExpectationFailed, request, validationFailures); //since we don't know what happened here
            }
            finally
            {
                timer.Stop();
                if (!isSucess)
                    _telemetry.TrackRequest("ListItemItemRequest", startTime, timer.Elapsed, HttpStatusCode.BadRequest.ToString(), false);
                else
                    _telemetry.TrackRequest("ListItemItemRequest", startTime, timer.Elapsed, HttpStatusCode.Created.ToString(), true);
            }
            #endregion
        }

        #region Helper Functions
        private static HttpResponseData CreateHttpResponse<T>(HttpStatusCode httpStatusCode, HttpRequestData request, List<T> items)
        {
            var response = request.CreateResponse(httpStatusCode);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            if (items != null && items.Count > 0)
            {
                foreach (var item in items)
                {
                    response.WriteStringAsync(item?.ToString() ?? string.Empty, Encoding.UTF8);
                    response.WriteStringAsync("\n");
                }
            }

            response.WriteStringAsync($"Please use CorrelationID {guid.ToString()} for log tracing");
            return response;
        }

        private void AddOrUpdate(string key, string value, Dictionary<string , string> properties)
        {
            if (properties.ContainsKey(key)) return;
            else properties.Add(key, value);
        }
        #endregion
    }


}
