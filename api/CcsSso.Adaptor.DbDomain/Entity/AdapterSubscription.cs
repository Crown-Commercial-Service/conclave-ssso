using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("AdapterSubscription")]
  public class AdapterSubscription : BaseEntity
  {
    public string SubscriptionType { get; set; }

    public string SubscriptionUrl { get; set; }

    public AdapterConsumer AdapterConsumer { get; set; }

    public int AdapterConsumerId { get; set; }

    public ConclaveEntity ConclaveEntity { get; set; }

    public int ConclaveEntityId { get; set; }

    public AdapterFormat AdapterFormat { get; set; }

    public int AdapterFormatId { get; set; }
  }
}
