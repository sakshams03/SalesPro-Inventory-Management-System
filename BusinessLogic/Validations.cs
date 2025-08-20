using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Models;

namespace BusinessLogic
{
    public class Validations
    {
        public static bool IsValid(string payload, List<string> errors)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrWhiteSpace(payload))
            {
                errors.Add("Empty or null payload");
                return false;
            }
            try
            {
                JsonConvert.DeserializeObject<Product>(payload);
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
