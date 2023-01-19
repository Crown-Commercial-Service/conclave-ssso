using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.BSIRolesRemovalOneTimeJob.Model
{
  public class OrganisationDetail
  {
    public int Id { get; set; }
    public string CiiOrganisationId { get; set; }
    public string LegalName { get; set; }
    public bool? RightToBuy { get; set; }

    public DateTime? CreatedOnUtc { get; set; }

    public int? SupplierBuyerType { get; set; }

  }
}
