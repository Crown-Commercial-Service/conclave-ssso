using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Contracts.Cii;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Shared.Domain.Excecptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service
{
  public class UserService : IUserService
  {
    private readonly IAttributeMappingService _attributeMappingService;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly IWrapperUserContactService _wrapperUserContactService;
    private readonly ICiiService _ciiService;
    public UserService(IAttributeMappingService attributeMappingService, IWrapperUserService wrapperUserService,
      IWrapperOrganisationService wrapperOrganisationService, IWrapperUserContactService wrapperUserContactService, ICiiService ciiService)
    {
      _attributeMappingService = attributeMappingService;
      _wrapperUserService = wrapperUserService;
      _wrapperOrganisationService = wrapperOrganisationService;
      _wrapperUserContactService = wrapperUserContactService;
      _ciiService = ciiService;
    }

    public async Task<Dictionary<string, object>> GetUserAsync(string userName)
    {
      List<Dictionary<string, object>> resultDictionaries = new();
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary = await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.User);
      userName = userName?.Trim();
      var userResponse = await _wrapperUserService.GetUserAsync(userName);
      var userResultDictionary = _attributeMappingService.GetMappedDataDictionary(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
      resultDictionaries.Add(userResultDictionary);
      var userIdpDictionary = _attributeMappingService.GetMappedIdentityProviders(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
      resultDictionaries.Add(userIdpDictionary);
      var userRolesDictionary = _attributeMappingService.GetMappedUserRoles(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
      resultDictionaries.Add(userRolesDictionary);
      var userGroupsDictionary = _attributeMappingService.GetMappedUserGroups(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
      resultDictionaries.Add(userGroupsDictionary);

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.UserContact))
      {
        var userContact = await _wrapperUserContactService.GetUserContactPointsAsync(userName);
        var contacResultDictionary = _attributeMappingService.
          GetMappedContactDataDictionaryFromContactPoints(userContact.ContactPoints, conclaveEntityMappingDictionary[ConclaveEntityNames.UserContact]);
        resultDictionaries.Add(contacResultDictionary);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgProfile))
      {
        try
        {
          var organisation = await _wrapperOrganisationService.GetOrganisationAsync(userResponse.OrganisationId);
          var orgResultDictionary = _attributeMappingService.GetMappedDataDictionary(organisation, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgProfile]);
          resultDictionaries.Add(orgResultDictionary);
        }
        catch (ResourceNotFoundException)
        {
          // If organisation not found at least send the other data avaialble
        }
      }

      // Get salesforce info. This is a temporary implementation just to support DIGITS integration
      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgIdentifiers))
      {
        var identifierInfo = await _ciiService.GetOrgIdentifierInfoAsync(userResponse.OrganisationId);
        var identifierInfoDictionary = _attributeMappingService.GetMappedOrgIdentifierInfo(identifierInfo, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgIdentifiers]);
        resultDictionaries.Add(identifierInfoDictionary);
      }

      var result = _attributeMappingService.GetMergedResultDictionary(resultDictionaries);

      return result;
    }
  }
}
