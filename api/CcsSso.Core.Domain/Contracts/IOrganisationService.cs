using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IOrganisationService
  {
    Task<string> RegisterAsync(OrganisationRegistrationDto organisationRegistrationDto);

    Task DeleteAsync(string ciiOrgId);

    Task<OrganisationDto> GetAsync(string id);

    Task<List<OrganisationDto>> GetAllAsync(string orgName);

    Task<List<OrganisationUserDto>> GetUsersAsync(string name);
  }
}
