using CcsSso.Core.DbModel.Entity;
using CcsSso.DbModel.Entity;
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

    Task<List<OrganisationDto>> GetAllAsync();

    Task PutAsync(OrganisationDto model);

    Task<List<OrganisationUserDto>> GetUsersAsync();

    Task Rollback(OrganisationRollbackDto model);

    Task<List<OrganisationEligibleRole>> GetOrganisationEligibleRolesAsync(Organisation org, int supplierBuyerType);
  }
}
