using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IAdaptorNotificationService
  {
    Task NotifyContactChangeAsync(string operation, int contactId);

    Task NotifyContactPointChangesAsync(string contactEntity, string operation, string organisationId, List<int> contactIds);

    Task NotifyOrganisationChangeAsync(string operation, string organisationId);

    Task NotifyUserChangeAsync(string operation, string userId, string organisationId);
  }
}
