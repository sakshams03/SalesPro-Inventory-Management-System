using System.Runtime.Serialization;

namespace Models
{
    public class DeleteRequest
    {
        [DataMember(IsRequired = false)]
        public string Name { get; set; }

        [DataMember(IsRequired = false)]
        public string Category { get; set; }

        [DataMember(IsRequired = false)]
        public string Subcategory { get; set; }

        [DataMember(IsRequired = true)]
        public string ProductCode { get; set; }

        [DataMember(IsRequired = true)]
        public string Location { get; set; }
    }
}
