using CcsSso.Dtos.Domain.Models;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface ICiiService
  {
    Task<CiiDto> GetAsync(string scheme, string companyNumber, string token);

    Task<CiiDto[]> GetOrgsAsync(string id, string token);

    Task<CiiSchemeDto[]> GetSchemesAsync(string token);

    Task<CiiDto> GetIdentifiersAsync(string orgId, string scheme, string id, string token);

    Task<string> PostAsync(CiiDto model);

    Task PutAsync(CiiPutDto model, string token);

    Task DeleteOrgAsync(string id);

    Task DeleteSchemeAsync(string orgId, string scheme, string id, string token);
  }
}
