using System.ComponentModel.DataAnnotations;
namespace Entity
{
    public class DeleteRequestDTO
    {
        public string? Name { get; set; }

        [Required]
        public string ProductCode { get; set; }

        [Required]
        public string Location { get; set; }
    }
}
