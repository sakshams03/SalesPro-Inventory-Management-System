using Azure.Messaging.ServiceBus;
using BusinessLogic;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Entity;
using Newtonsoft.Json;
using Provider;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using System.Drawing;

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
            var principal = functionContext.GetHttpContext()?.User;
            var roles = principal?.Claims
                            .Where(c => c.Type == "roles")
                            .Select(c => c.Value)
                            .ToList() ?? new List<string>();
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.AddItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return await Helper.CreateHttpResponse<string>(HttpStatusCode.Forbidden, request, errors:validationFailures);
                }

            }
            #endregion
            try
            {
                using var reader = new StreamReader(request.Body);
                var requestContent = await reader.ReadToEndAsync();
                requestContent = requestContent.Trim();

                if (!Validations.IsValid<Product>(requestContent, validationFailures))
                {
                    var response = await Helper.CreateHttpResponse(HttpStatusCode.BadRequest, request, validationFailures);
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
                return await Helper.CreateHttpResponse(HttpStatusCode.Created, request, validationFailures);

            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                _telemetry.TrackException(ex, properties);
                isSucess = false;
                return await Helper.CreateHttpResponse<string>(HttpStatusCode.BadRequest, request,errors: validationFailures); //since this is a caught exception
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
        public async Task<HttpResponseData> Run2([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            var validationFailures = new List<string>();
            var properties = new Dictionary<string, string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var startTime = DateTime.UtcNow;
            #region Authorization
            var principal = functionContext.GetHttpContext()?.User;
            var roles = principal?.Claims
                            .Where(c => c.Type == "roles")
                            .Select(c => c.Value)
                            .ToList() ?? new List<string>();
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.DeleteItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return await Helper.CreateHttpResponse<string>(HttpStatusCode.Forbidden, request, errors: validationFailures);
                }

            }
            #endregion
            try
            {
                using var reader = new StreamReader(request.Body);
                var requestContent = await reader.ReadToEndAsync();
                requestContent = requestContent.Trim();
                var payload = JsonConvert.DeserializeObject<DeleteRequestDTO>(requestContent);
                _telemetry.TrackEvent($"Performing delete option for product with product code {payload.ProductCode}", properties);
                await _cosmosDbProvider.DeleteAsync(payload);
                _telemetry.TrackTrace("Deletion successful", properties);
                return request.CreateResponse(HttpStatusCode.OK);
            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                isSucess = false;
                _telemetry.TrackException(ex, properties);
                validationFailures.Add(ex.Message);
                return await Helper.CreateHttpResponse<string>(HttpStatusCode.InternalServerError, request, errors: validationFailures); //since we don't know what happened here
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

        [Function("ListItems")]
        public async Task<HttpResponseData> Run3([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            var validationFailures = new List<string>();
            var properties = new Dictionary<string, string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var startTime = DateTime.UtcNow;
            #region Authorization
            var principal = functionContext.GetHttpContext()?.User;
            var roles = principal?.Claims
                            .Where(c => c.Type == "roles")
                            .Select(c => c.Value)
                            .ToList() ?? new List<string>();
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.ListItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return await Helper.CreateHttpResponse<string>(HttpStatusCode.Forbidden, request, errors: validationFailures);
                }

            }
            #endregion
            try
            {
                var location = request.Query["Location"].ToString(); // will never be null
                var category = request?.Query["Category"]?.ToString();
                var subCategory = request?.Query["SubCategory"]?.ToString();
                var getRequest = new GetProductsDTO()
                {
                    Location = location,
                    Category = category,
                    Subcategory = subCategory
                };
                _telemetry.TrackEvent($"Performing search operation based on query parameters", properties);
                var list = await _cosmosDbProvider.GetProducts(getRequest);
                _telemetry.TrackTrace("Search operation successful", properties);
                return await Helper.CreateHttpResponse(HttpStatusCode.OK, request, data: list);
            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                isSucess = false;
                _telemetry.TrackException(ex, properties);
                validationFailures.Add(ex.Message);
                return await Helper.CreateHttpResponse<string>(HttpStatusCode.InternalServerError, request, errors:validationFailures); //since we don't know what happened here
            }
            finally
            {
                timer.Stop();
                if (!isSucess)
                    _telemetry.TrackRequest("ListItemsRequest", startTime, timer.Elapsed, HttpStatusCode.BadRequest.ToString(), false);
                else
                    _telemetry.TrackRequest("ListItemsRequest", startTime, timer.Elapsed, HttpStatusCode.Created.ToString(), true);
            }
            #endregion
        }

        [Function("ListItem")]
        public async Task<HttpResponseData> Run4([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            var validationFailures = new List<string>();
            var properties = new Dictionary<string, string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            var startTime = DateTime.UtcNow;
            #region Authorization
            var principal = functionContext.GetHttpContext()?.User;
            var roles = principal?.Claims
                            .Where(c => c.Type == "roles")
                            .Select(c => c.Value)
                            .ToList() ?? new List<string>();
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.ListItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return await Helper.CreateHttpResponse<string>(HttpStatusCode.Forbidden, request, errors:validationFailures);
                }

            }
            #endregion
            try
            {
                var location = request.Query["Location"].ToString(); // will never be null
                var productCode = request.Query["ProductCode"]; // will not be null in this case

                if(string.IsNullOrEmpty(productCode) || string.IsNullOrWhiteSpace(productCode))
                {
                    isSucess = false;
                    validationFailures.Add("ProductCode is required when searching for a particular item");
                    return await Helper.CreateHttpResponse<string>(HttpStatusCode.BadRequest, request, errors: validationFailures);

                }
                var getRequest = new GetProductDTO()
                {
                    Location = location,
                    ProductCode = productCode
                };
                _telemetry.TrackEvent($"Performing search operation based on query parameters", properties);
                var list = await _cosmosDbProvider.GetProduct(getRequest);
                _telemetry.TrackTrace("Search operation successful", properties);

                return  await Helper.CreateHttpResponse(HttpStatusCode.OK, request, data: list);
            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                isSucess = false;
                _telemetry.TrackException(ex, properties);
                validationFailures.Add(ex.Message);
                return await Helper.CreateHttpResponse<string>(HttpStatusCode.InternalServerError, request, errors: validationFailures); //since we don't know what happened here
            }
            finally
            {
                timer.Stop();
                if (!isSucess)
                    _telemetry.TrackRequest("ListItemRequest", startTime, timer.Elapsed, HttpStatusCode.BadRequest.ToString(), false);
                else
                    _telemetry.TrackRequest("ListItemRequest", startTime, timer.Elapsed, HttpStatusCode.Created.ToString(), true);
            }
            #endregion
        }

        [Function("UpdateItem")]
        public async Task<HttpResponseData> Run5([HttpTrigger(AuthorizationLevel.Anonymous, "put")] HttpRequestData request, FunctionContext functionContext)
        {
            guid = Guid.NewGuid();
            Stopwatch timer = new Stopwatch();
            var properties = new Dictionary<string, string>();
            timer.Start();
            var startTime = DateTime.UtcNow;
            var validationFailures = new List<string>();
            AddOrUpdate("CorrelationID", guid.ToString(), properties);

            #region Authorization
            var principal = functionContext.GetHttpContext()?.User;
            var roles = principal?.Claims
                            .Where(c => c.Type == "roles")
                            .Select(c => c.Value)
                            .ToList() ?? new List<string>();
            if (functionContext.Items.TryGetValue("UserRoles", out var rolesObj) && rolesObj is List<string> roles)
            {
                // Authorization (Authentication is automatically taken care by Azure Entra Id)
                if (!roles.Contains("Inventory.UpdateItem"))
                {
                    _telemetry.TrackEvent("User does not have authorization persmissions", properties);
                    isSucess = false;
                    validationFailures.Add("User does not have authorization persmissions");
                    return await Helper.CreateHttpResponse<string>(HttpStatusCode.Forbidden, request, errors:validationFailures);
                }

            }
            #endregion
            try
            {
                using var reader = new StreamReader(request.Body);
                var requestContent = await reader.ReadToEndAsync();
                requestContent = requestContent.Trim();

                if (!Validations.IsValid<UpdateRequestDTO>(requestContent, validationFailures))
                {
                    var response = await Helper.CreateHttpResponse<string>(HttpStatusCode.BadRequest, request, errors: validationFailures);
                    _telemetry.TrackTrace($"Validation errors: {string.Join(",", validationFailures)}");
                    return response;
                }
                var payload = JsonConvert.DeserializeObject<UpdateRequestDTO>(requestContent);
                await _cosmosDbProvider.UpdateAsync(payload);
                return await Helper.CreateHttpResponse<string>(statusCode:HttpStatusCode.OK, request: request);

            }
            #region ExceptionHandling
            catch (Exception ex)
            {
                _telemetry.TrackException(ex, properties);
                isSucess = false;
                return await Helper.CreateHttpResponse<string>(HttpStatusCode.BadRequest, request, errors:validationFailures); //since this is a caught exception
            }
            finally
            {
                timer.Stop();
                if (!isSucess)
                    _telemetry.TrackRequest("UpdateItemRequest", startTime, timer.Elapsed, HttpStatusCode.BadRequest.ToString(), false);
                else
                    _telemetry.TrackRequest("UpdateItemRequest", startTime, timer.Elapsed, HttpStatusCode.Created.ToString(), true);

            }
            #endregion
        }



        #region await Helper Functions
        private void AddOrUpdate(string key, string value, Dictionary<string , string> properties)
        {
            if (properties.ContainsKey(key)) return;
            else properties.Add(key, value);
        }
        #endregion
    }


}
