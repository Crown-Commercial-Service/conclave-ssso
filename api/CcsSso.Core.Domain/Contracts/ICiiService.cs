using CcsSso.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface ICiiService
  {
    Task<CiiDto> GetAsync(string scheme, string companyNumber);

    Task<CiiDto> GetOrgAsync(string id);

    Task<CiiDto[]> GetOrgsAsync(string id);

    Task<CiiSchemeDto[]> GetSchemesAsync();

    Task<CiiDto> GetIdentifiersAsync(string orgId, string scheme, string id);

    Task<string> PostAsync(CiiDto model);

    Task<string> PutAsync(CiiPutDto model);

    Task<string> DeleteAsync(string id);

    Task<string> DeleteAsyncWithBody(CiiDto model);

    Task<string> DeleteOrgAsync(string id);

    Task<string> DeleteSchemeAsync(string orgId, string scheme, string id);
  }
}
