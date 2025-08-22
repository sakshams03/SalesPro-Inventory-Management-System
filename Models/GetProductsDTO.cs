using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Entity
{
    public class GetProductsDTO
    {
        public string Category { get; set; }
        public string Subcategory { get; set; }
        [Required]
        public string Location { get; set; }
    }
}
