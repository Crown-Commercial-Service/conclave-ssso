using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("ConclaveEntityAttribute")]
  public class ConclaveEntityAttribute : BaseEntity
  {
    public string AttributeName { get; set; }

    public ConclaveEntity ConclaveEntity { get; set; }

    public int ConclaveEntityId { get; set; }

    public List<AdapterConclaveAttributeMapping> AdapterConclaveAttributeMappings { get; set; }
  }
}
