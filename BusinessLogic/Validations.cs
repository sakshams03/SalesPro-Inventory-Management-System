using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Entity;
using Provider;

namespace BusinessLogic
{
    public class Validations
    { 
        private readonly ICosmosDbProvider _cosmosDbProvider;
        public  Validations(ICosmosDbProvider cosmosDbProvider)
        {
            _cosmosDbProvider = cosmosDbProvider;
        }
        public void IsValid<T>(string payload, List<string> errors)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrWhiteSpace(payload))
            {
                errors.Add("Empty or null payload");
            }
            try
            {
                 JsonConvert.DeserializeObject<T>(payload);
            }
            catch (Newtonsoft.Json.JsonException ex) 
            {
                errors.Add($"Invalid JSON for type {typeof(T).Name}: {ex.Message}");
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
            }
        }

        public void ProductExists(string payload, List<string> errors)
        {
            var product = JsonConvert.DeserializeObject<Product>(payload);
            var productExist = _cosmosDbProvider.GetProduct(new GetProductDTO()
            {
                Location = product.Location,
                ProductCode = product.ProductCode
            });
            if (productExist is not null  )
            {
                errors.Add("Product already exists, try adding a unique product");
            }
        }

        public void IsEmptyOrNullString(string value, string fieldName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"{fieldName} cannot be null or empty");
            }
        }
    }
}
