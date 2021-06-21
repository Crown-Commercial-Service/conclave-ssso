using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("AdapterConsumerEntity")]
  public class AdapterConsumerEntity : BaseEntity
  {
    public string Name { get; set; }

    public AdapterConsumer AdapterConsumer { get; set; }

    public int AdapterConsumerId { get; set; }

    public List<AdapterConsumerEntityAttribute> AdapterConsumerEntityAttributes { get; set; }
  }
}
