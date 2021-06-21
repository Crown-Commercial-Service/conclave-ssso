using CcsSso.Domain.Dtos.External;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface ICiiService
  {
    Task<CiiDto> GetAsync(string scheme, string companyNumber, string token);

    Task<CiiDto> GetOrgAsync(string id, string token);

    Task<CiiDto[]> GetOrgsAsync(string id, string token);

    Task<CiiSchemeDto[]> GetSchemesAsync(string token);

    Task<CiiDto> GetIdentifiersAsync(string orgId, string scheme, string id, string token);

    Task<string> PostAsync(CiiDto model, string token);

    Task<string> PutAsync(CiiPutDto model, string token);

    Task<string> DeleteAsync(string id, string token);

    Task<string> DeleteAsyncWithBody(CiiDto model, string token);

    Task<string> DeleteOrgAsync(string id, string token);

    Task<string> DeleteSchemeAsync(string orgId, string scheme, string id, string token);
  }
}
