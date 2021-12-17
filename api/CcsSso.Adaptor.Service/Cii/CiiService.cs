using CcsSso.Adaptor.Domain.Contracts.Cii;
using CcsSso.Adaptor.Domain.Dtos.Cii;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service.Cii
{
  public class CiiService : ICiiService
  {
    private readonly ICiiApiService _ciiApiService;
    public CiiService(ICiiApiService ciiApiService)
    {
      _ciiApiService = ciiApiService;
    }

    /// <summary>
    /// Get Salesforce information from cii
    /// This is kind of temporary solution to support DIGITS integration
    /// According to the Murugesh this will be thrown out when the correct requirement implemented (requires changs in CII and Wrapper)
    /// </summary>
    /// <param name="ciiOrgId"></param>
    /// <returns></returns>
    public async Task<CiiIdentifierAllDto> GetOrgIdentifierInfoAsync(string ciiOrgId)
    {

      var ciiDto = await _ciiApiService.GetAsync<CiiIdentifierAllDto>($"identities/organisations/{ciiOrgId}/all", "ERROR_RETRIEVING_IDENTIFIERS_FROM_CII");

      return ciiDto;
    }
  }
}
