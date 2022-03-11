
using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  public class ContactSupportService : IContactSupportService
  {
    private IDataContext _dataContext;
    public ContactSupportService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task<bool> IsOrgSiteContactExistsAsync(List<int> userContacPointIds, int organisationId)
    {

      var organisation = await _dataContext.Organisation.Where(o => o.Id == organisationId)
        .FirstOrDefaultAsync();

      var orgContactsAvailable = await _dataContext.ContactPoint.AnyAsync(cp => !cp.IsDeleted && cp.PartyId == organisation.PartyId
        && (!userContacPointIds.Contains(cp.OriginalContactPointId) && cp.ContactPointReason.Name != ContactReasonType.Other && cp.ContactPointReason.Name != ContactReasonType.Site)); // Contact Points whihc are not physical addresses and sites

      if (orgContactsAvailable)
      {
        return true;
      }
      else
      {
        var siteContactsAvailable = await _dataContext.SiteContact.AnyAsync(sc => !sc.IsDeleted && sc.ContactPoint.PartyId == organisation.PartyId
        && !userContacPointIds.Contains(sc.OriginalContactId));

        return siteContactsAvailable;
      }
    }

    public async Task<bool> IsOtherUserContactExistsAsync(string userName, int organisationId)
    {

      var otherUserContactsAvailable = await _dataContext.User.AnyAsync(u => !u.IsDeleted && u.UserName != userName && u.Party.Person.OrganisationId == organisationId &&
        u.Party.ContactPoints.Any(cp => !cp.IsDeleted));

      return otherUserContactsAvailable;
    }
  }
}

