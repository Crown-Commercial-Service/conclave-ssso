using CcsSso.Dtos.Domain.Models;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface ICiiService
  {
    Task AddSchemeAsync(string ciiOrganisationId, string scheme, string identifier, string token);

    Task DeleteOrgAsync(string ciiOrganisationId);

    Task DeleteSchemeAsync(string ciiOrganisationId, string scheme, string identifier, string token);    

    Task<CiiDto> GetOrganisationIdentifierDetailsAsync(string ciiOrganisationId, string scheme, string identifier, string token);
          
    Task<CiiSchemeDto[]> GetSchemesAsync();

    Task<string> PostAsync(CiiDto model);
  }
}
