using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("AdapterConclaveAttributeMapping")]
  public class AdapterConclaveAttributeMapping : BaseEntity
  {
    public AdapterConsumerEntityAttribute AdapterConsumerEntityAttribute { get; set; }

    public int AdapterConsumerEntityAttributeId { get; set; }

    public ConclaveEntityAttribute ConclaveEntityAttribute { get; set; }

    public int ConclaveEntityAttributeId { get; set; }
  }
}
