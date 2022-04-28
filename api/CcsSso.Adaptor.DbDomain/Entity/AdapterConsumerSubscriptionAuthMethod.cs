using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
    [Table("AdapterConsumerSubscriptionAuthMethod")]
    public class AdapterConsumerSubscriptionAuthMethod : BaseEntity
    {

        public string APIKey { get; set; }

        public int AdapterConsumerId { get; set; }

        public AdapterConsumer AdapterConsumer { get; set; }
    }
}
