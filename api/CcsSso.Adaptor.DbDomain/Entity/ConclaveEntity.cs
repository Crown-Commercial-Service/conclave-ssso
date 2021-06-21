using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("ConclaveEntity")]
  public class ConclaveEntity : BaseEntity
  {
    public string Name { get; set; }

    public List<ConclaveEntityAttribute> ConclaveEntityAttributes { get; set; }

    public List<AdapterSubscription> AdapterSubscriptions { get; set; }
  }
}
