using System.Runtime.Serialization;

namespace Models
{
    public class Product
    {
        [DataMember(IsRequired = true)]
        public string Name { get; set; }

        [DataMember(IsRequired = false)]
        public string Description { get; set; }

        [DataMember(IsRequired = true)]
        public double Price { get; set; }

        [DataMember(IsRequired = false)]
        public string Category { get; set; }

        [DataMember(IsRequired = false)]
        public string Subcategory { get; set; }

        [DataMember(IsRequired = true)]
        public int Quantity { get; set; }

        [DataMember(IsRequired = true)]
        public string ProductCode { get; set; }

        [DataMember(IsRequired = true)]
        public string Location { get; set; }
    }
}
