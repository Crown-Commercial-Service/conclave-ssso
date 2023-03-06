using CcsSso.Core.BSIRolesRemovalOneTimeJob.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.BSIRolesRemovalOneTimeJob.Contracts
{
  public interface IRemoveRoleFromAllOrganisationService
  {
    Task PerformJobAsync(List<OrganisationDetail> organisation);
  }
}
