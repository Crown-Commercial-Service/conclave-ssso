using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service
{
  public class OrganisationService : IOrganisationService
  {
    private readonly IAttributeMappingService _attributeMappingService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly IWrapperOrganisationContactService _wrapperOrganisationContactService;
    private readonly IWrapperSiteService _wrapperSiteService;
    public OrganisationService(IAttributeMappingService attributeMappingService,
      IWrapperOrganisationService wrapperOrganisationService, IWrapperOrganisationContactService wrapperOrganisationContactService,
      IWrapperSiteService wrapperSiteService)
    {
      _attributeMappingService = attributeMappingService;
      _wrapperOrganisationService = wrapperOrganisationService;
      _wrapperOrganisationContactService = wrapperOrganisationContactService;
      _wrapperSiteService = wrapperSiteService;
    }

    public async Task<Dictionary<string,object>> GetOrganisationAsync(string organisationId)
    {
      List<Dictionary<string, object>> resultDictionaries = new();
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary = await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.Organisation);

      var organisation = await _wrapperOrganisationService.GetOrganisationAsync(organisationId);
      var orgResultDictionary = _attributeMappingService.GetMappedDataDictionary(organisation, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgProfile]);
      resultDictionaries.Add(orgResultDictionary);

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgContact))
      {
        var orgContact = await _wrapperOrganisationContactService.GetOrganisationContactsAsync(organisationId);
        var contacResultDictionary = _attributeMappingService.
          GetMappedContactDataDictionaryFromContactPoints(orgContact.ContactPoints, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgContact]);
        resultDictionaries.Add(contacResultDictionary);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.Site))
      {
        var orgSites = await _wrapperSiteService.GetOrganisationSitesAsync(organisationId);
        var siteAttributeMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Site];
        var siteResultDictionary = _attributeMappingService.GetMappedOrganisationSites(orgSites.Sites, siteAttributeMapping);
        resultDictionaries.Add(siteResultDictionary);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgUser))
      {
        var orgUsers = await _wrapperOrganisationService.GetOrganisationUsersAsync(organisationId);
        var orgUserAttributeMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.OrgUser];
        var siteResultDictionary = _attributeMappingService.GetMappedOrganisationUsers(orgUsers, orgUserAttributeMapping);
        resultDictionaries.Add(siteResultDictionary);
      }

      var result = _attributeMappingService.GetMergedResultDictionary(resultDictionaries);

      return result;
    }
  }

  
}
