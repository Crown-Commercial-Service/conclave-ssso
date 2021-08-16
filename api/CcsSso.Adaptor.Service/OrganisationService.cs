using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Contracts.Cii;
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
    private readonly ICiiService _ciiService;

    public OrganisationService(IAttributeMappingService attributeMappingService,
      IWrapperOrganisationService wrapperOrganisationService, IWrapperOrganisationContactService wrapperOrganisationContactService,
      IWrapperSiteService wrapperSiteService, ICiiService ciiService)
    {
      _attributeMappingService = attributeMappingService;
      _wrapperOrganisationService = wrapperOrganisationService;
      _wrapperOrganisationContactService = wrapperOrganisationContactService;
      _wrapperSiteService = wrapperSiteService;
      _ciiService = ciiService;
    }

    public async Task<Dictionary<string,object>> GetOrganisationAsync(string organisationId)
    {
      List<Dictionary<string, object>> resultDictionaries = new();
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary = await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.Organisation);

      var organisation = await _wrapperOrganisationService.GetOrganisationAsync(organisationId);
      var orgResultDictionary = _attributeMappingService.GetMappedDataDictionary(organisation, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgProfile]);
      resultDictionaries.Add(orgResultDictionary);

      // Get salesforce info. This is a temporary implementation just to support DIGITS integration
      // Added here if any other system required under org mapping
      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgIdentifiers))
      {
        var identifierInfo = await _ciiService.GetOrgIdentifierInfoAsync(organisationId);
        var identifierInfoDictionary = _attributeMappingService.GetMappedOrgIdentifierInfo(identifierInfo, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgIdentifiers]);
        resultDictionaries.Add(identifierInfoDictionary);
      }

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
