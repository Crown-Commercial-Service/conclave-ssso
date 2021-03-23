using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IOrganisationService
  {
    Task<int> CreateAsync(OrganisationDto model);

    Task DeleteAsync(int id);

    Task<OrganisationDto> GetAsync(string id);
  }
}
