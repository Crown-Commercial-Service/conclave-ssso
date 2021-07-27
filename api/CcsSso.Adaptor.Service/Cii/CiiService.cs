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
    public async Task<SalesForceInfo> GetSalesforceInfoAsync(string ciiOrgId)
    {
      SalesForceInfo salesForceInfo = new();

      var ciiDtoList = await _ciiApiService.GetAsync<List<CiiIdentifierAllDto>>($"identities/schemes/organisations/sso/all?ccs_org_id={ciiOrgId}", "ERROR_RETRIEVING_SALECEFORCE_INFO_FROM_CII");

      var salesforceInfoIdentifier = ciiDtoList.FirstOrDefault()?.AdditionalIdentifiers.FirstOrDefault(ai => ai.Id.Contains('~'));

      if (salesforceInfoIdentifier != null)
      {
        var idUrnArray = salesforceInfoIdentifier.Id.Split('~');
        salesForceInfo.Id = idUrnArray[0];
        salesForceInfo.Urn = idUrnArray[1];
      }

      return salesForceInfo;
    }
  }
}
