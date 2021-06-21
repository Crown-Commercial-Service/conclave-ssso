using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("AdapterConsumerEntityAttribute")]
  public class AdapterConsumerEntityAttribute : BaseEntity
  {
    public string AttributeName { get; set; }

    public AdapterConsumerEntity AdapterConsumerEntity { get; set; }

    public int AdapterConsumerEntityId { get; set; }

    public List<AdapterConclaveAttributeMapping> AdapterConclaveAttributeMappings { get; set; }
  }
}
