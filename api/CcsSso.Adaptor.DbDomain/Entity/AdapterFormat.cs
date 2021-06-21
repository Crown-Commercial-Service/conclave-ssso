using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CcsSso.Adaptor.DbDomain.Entity
{
  [Table("AdapterFormat")]
  public class AdapterFormat : BaseEntity
  {
    public string FomatFileType { get; set; }

    public List<AdapterSubscription> AdapterSubscriptions { get; set; }
  }
}
