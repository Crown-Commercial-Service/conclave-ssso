using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.DbPersistence.Entities
{
  [Table("RelyingParty")]
  public class RelyingParty : BaseEntity
  {
    public string Name { get; set; }

    public string ClientId { get; set; }

    public string BackChannelLogoutUrl { get; set; }
  }
}
