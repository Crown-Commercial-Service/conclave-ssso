using CcsSso.Core.DbModel.Constants;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class ContactAssignmentInfo
  {
    public AssignedContactType AssigningContactType { get; set; }

    public List<int> AssigningContactPointIds { get; set; }

    public string? AssigningContactsUserId { get; set; }

    public int? AssigningContactsSiteId { get; set; }
  }
}
