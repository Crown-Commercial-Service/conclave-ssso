using CcsSso.Adaptor.Domain.Dtos.Cii;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts
{
  public interface IAttributeMappingService
  {
    Task<Dictionary<string, Dictionary<string, string>>> GetMappedAttributeDictionaryAsync(string consumerEntityName);

    Dictionary<string, object> GetMappedDataDictionary(object dataObject, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedContactDataDictionaryFromContactPoints(List<WrapperContactPoint> contactPoints, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedOrganisationSites(List<WrapperOrganisationSite> sites, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedOrganisationUsers(List<WrapperUserListInfo> orgUsers, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedOrgIdentifierInfo(CiiIdentifierAllDto identifierInfo, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedIdentityProviders(WrapperUserResponse user, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedUserRoles(WrapperUserResponse user, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMappedUserGroups(WrapperUserResponse user, Dictionary<string, string> attributeMappings);

    Dictionary<string, object> GetMergedResultDictionary(List<Dictionary<string, object>> dictionaries);
  }
}
