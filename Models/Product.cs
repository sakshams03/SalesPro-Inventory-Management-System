using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Entity
{
    public class Product
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public double Price { get; set; }

        public string Category { get; set; }

        public string Subcategory { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Key]
        public string ProductCode { get; set; }

        [Required]
        public string Location { get; set; }

        [ConcurrencyCheck]
        public string ETag{ get; set;}
    }
}
