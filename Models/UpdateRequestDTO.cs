using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Entity
{
    public class UpdateRequestDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double? Price { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public int? Quantity { get; set; }
        public required string ProductCode { get; set; }
        public required string Location { get; set; }
    }
}
