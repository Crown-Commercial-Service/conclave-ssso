using CcsSso.Core.DbModel.Constants;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class OrganisationAuditInfo
  {
    public OrgAutoValidationStatus Status { get; set; }

    public int OrganisationId { get; set; }

    public string SchemeIdentifier { get; set; }

    public string Actioned { get; set; }

    public string ActionedBy { get; set; }
  }
}
