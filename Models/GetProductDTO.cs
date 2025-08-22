using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Entity
{
    public class GetProductDTO
    {
        [Required]
        public string ProductCode { get; set; }
        
        [Required]
        public string Location { get; set; }
    }
}
