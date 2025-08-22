using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Entity;

namespace BusinessLogic
{
    public class Validations
    {
        public static bool IsValid<T>(string payload, List<string> errors)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrWhiteSpace(payload))
            {
                errors.Add("Empty or null payload");
                return false;
            }
            try
            {
                JsonConvert.DeserializeObject<T>(payload);
            }
            catch (Newtonsoft.Json.JsonException ex) 
            {
                errors.Add($"Invalid JSON for type {typeof(T).Name}: {ex.Message}");
                return false;
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
                return false;
            }
            return true;
        }
    }
}
