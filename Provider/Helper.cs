using Entity;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.Net;
namespace Provider
{
    public class Helper
    {
        public static Product DeserializeProduct(string jsonString)
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = DateParseHandling.None
            };

            try
            {
                return JsonConvert.DeserializeObject<Product>(jsonString, settings);
            }
            catch (JsonException)
            {
                // If that fails, clean the string and try again
                var cleanedJson = CleanJsonString(jsonString);
                return JsonConvert.DeserializeObject<Product>(cleanedJson, settings);
            }
        }
        public static async Task<HttpResponseData> CreateHttpResponse<T>(HttpStatusCode statusCode, HttpRequestData request, T data = default, List<string> errors = null)
        {
            var response = request.CreateResponse(statusCode);

            var apiResponse = new ApiResponse<T>
            {
                IsSuccess = (int)statusCode >= 200 && (int)statusCode < 300,
                Data = data,
                Errors = errors ?? new List<string>(),
                CorrelationId = Guid.NewGuid().ToString()
            };

            await response.WriteAsJsonAsync(apiResponse);
            return response;
        }



        private static string CleanJsonString(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return jsonString;

            var cleaned = jsonString.Trim();
            if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2);
            }

            cleaned = cleaned.Replace("\\r\\n", "\r\n");
            cleaned = cleaned.Replace("\\r", "\r");
            cleaned = cleaned.Replace("\\n", "\n");

            cleaned = cleaned.Replace("\\\"", "\"");
            cleaned = cleaned.Replace("\\\\", "\\");

            return cleaned;
        }
    }
}
