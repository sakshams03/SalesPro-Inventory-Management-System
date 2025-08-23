using Entity;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
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
            var response = request.CreateResponse();

            response.StatusCode = statusCode;

            var payload = new
            {
                IsSuccess = (int)statusCode >= 200 && (int)statusCode < 300,
                Data = data,
                Errors = errors ?? new List<string>(),
                CorrelationId = Guid.NewGuid().ToString()
            };

            await response.WriteStringAsync(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8
            );

            return response;
        }

        public static bool IsAuthorized(ClaimsPrincipal principal, params string[] requiredRoles)
        {
            if (principal == null || !principal.Identity?.IsAuthenticated == true)
                return false;

            var userRoles = principal.Claims
                                     .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                                     .Select(c => c.Value)
                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                     .ToList();

            return requiredRoles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
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
