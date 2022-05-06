using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("AdapterConsumer")]
  public class AdapterConsumer : BaseEntity
  {
    public string Name { get; set; }

    public string ClientId { get; set; }

    public List<AdapterConsumerEntity> AdapterConsumerEntities { get; set; }

    public List<AdapterSubscription> AdapterSubscriptions { get; set; }

    public AdapterConsumerSubscriptionAuthMethod AdapterConsumerSubscriptionAuthMethod { get; set; }
  }
}
