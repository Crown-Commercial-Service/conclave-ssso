using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Excecptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service
{
  public class ContactService : IContactService
  {
    private readonly IAttributeMappingService _attributeMappingService;
    private readonly IWrapperContactService _wrapperContactService;
    private readonly IWrapperUserContactService _wrapperUserContactService;
    private readonly IWrapperOrganisationContactService _wrapperOrganisationContactService;
    private readonly IWrapperSiteContactService _wrapperSiteContactService;
    private readonly IWrapperUserService _wrapperUserService;
    private readonly IWrapperOrganisationService _wrapperOrganisationService;
    private readonly IWrapperSiteService _wrapperSiteService;
    public ContactService(IAttributeMappingService attributeMappingService, IWrapperContactService wrapperContactService,
      IWrapperUserContactService wrapperUserContactService, IWrapperOrganisationContactService wrapperOrganisationContactService,
      IWrapperSiteContactService wrapperSiteContactService, IWrapperUserService wrapperUserService,
      IWrapperOrganisationService wrapperOrganisationService, IWrapperSiteService wrapperSiteService)
    {
      _wrapperContactService = wrapperContactService;
      _attributeMappingService = attributeMappingService;
      _wrapperUserContactService = wrapperUserContactService;
      _wrapperOrganisationContactService = wrapperOrganisationContactService;
      _wrapperUserService = wrapperUserService;
      _wrapperOrganisationService = wrapperOrganisationService;
      _wrapperSiteContactService = wrapperSiteContactService;
      _wrapperSiteService = wrapperSiteService;
    }

    #region Contacts

    /// <summary>
    /// Create individaul contact
    /// </summary>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> CreateContactAsync(Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.Contact);

      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      var contactRequest = GetContactRequestObject(contactData, contactMapping);
      var result = await _wrapperContactService.CreateContactAsync(contactRequest);
      return result;
    }

    /// <summary>
    /// Update an individual contact
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> UpdateContactAsync(int contactId, Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.Contact);

      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      var contactRequest = GetContactRequestObject(contactData, contactMapping);
      await _wrapperContactService.UpdateContactAsync(contactId, contactRequest);
      return contactId;
    }

    /// <summary>
    /// Get the individual contact (contactType and contactValue pair with the id)
    /// </summary>
    /// <param name="contactId"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetContactAsync(int contactId)
    {

      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.Contact);

      var contact = await _wrapperContactService.GetContactAsync(contactId);
      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      var result = GetContactDataMappedDictionary(contact, contactMapping);
      return result;
    }
    #endregion

    #region UserContacts

    /// <summary>
    /// Create a user contact point for a given contact
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> CreateUserContactAsync(string userName, Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.UserContact);

      userName = userName?.Trim();
      var wrapperContactPointRequest = GetWrapperContactPointRequestObjectForCreate(contactData, conclaveEntityMappingDictionary);

      var createdContactPointId = await _wrapperUserContactService.CreateUserContactPointAsync(userName, wrapperContactPointRequest);

      var createdUserContactPoint = await _wrapperUserContactService.GetUserContactPointAsync(userName, createdContactPointId);

      return createdUserContactPoint.Contacts.Select(c => c.ContactId).First(); // There should be a created one
    }

    /// <summary>
    /// Update a user contact point for a given contact id
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="userName"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> UpdateUserContactAsync(int contactId, string userName,  Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.UserContact);

      userName = userName?.Trim();
      var userContactInfoList = await _wrapperUserContactService.GetUserContactPointsAsync(userName);

      var contactPoint = GetContactPointOfRelevantContact(contactId, userContactInfoList);
      
      var wrapperContactPointRequest =  GetWrapperContactPointRequestObjectForUpdate(contactId, contactData, conclaveEntityMappingDictionary, contactPoint);

      await _wrapperUserContactService.UpdateUserContactPointAsync(userName, wrapperContactPointRequest.ContactPointId, wrapperContactPointRequest);

      return contactId;
    }

    /// <summary>
    /// Get User contact
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetUserContactAsync(int contactId, string userName)
    {
      List<Dictionary<string, object>> resultDictionaries = new();
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.UserContact);

      userName = userName?.Trim();
      var userContactInfoList = await _wrapperUserContactService.GetUserContactPointsAsync(userName);

      var contactPoint = GetContactPointOfRelevantContact(contactId, userContactInfoList);

      if (contactPoint == null)
      {
        throw new ResourceNotFoundException();
      }

      var contact = contactPoint.Contacts.First(c => c.ContactId == contactId); // Get relevant contact by contactId
      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      resultDictionaries.Add(GetContactDataMappedDictionary(contact, contactMapping));

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.UserContact))
      {
        var convertedMapping = GetConvertedFlatMappingForSingelContactPoint(conclaveEntityMappingDictionary[ConclaveEntityNames.UserContact]);
        var contactPointData = _attributeMappingService.GetMappedDataDictionary(contactPoint, convertedMapping);
        resultDictionaries.Add(contactPointData);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.UserProfile))
      {
        var userResponse = await _wrapperUserService.GetUserAsync(userName);
        var userResultDictionary = _attributeMappingService.GetMappedDataDictionary(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
        resultDictionaries.Add(userResultDictionary);
        var userIdpDictionary = _attributeMappingService.GetMappedIdentityProviders(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
        resultDictionaries.Add(userIdpDictionary);
        var userRolesDictionary = _attributeMappingService.GetMappedUserRoles(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
        resultDictionaries.Add(userRolesDictionary);
        var userGroupsDictionary = _attributeMappingService.GetMappedUserGroups(userResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.UserProfile]);
        resultDictionaries.Add(userGroupsDictionary);
      }

      var result = _attributeMappingService.GetMergedResultDictionary(resultDictionaries);

      return result;
    }
    #endregion

    #region OrgContacts

    /// <summary>
    /// Create an organisation contact point for a given contact
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> CreateOrganisationContactAsync(string organisationId, Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.OrganisationContact);

      var wrapperContactPointRequest = GetWrapperContactPointRequestObjectForCreate(contactData, conclaveEntityMappingDictionary);

      var createdContactPointId = await _wrapperOrganisationContactService.CreateOrganisationContactPointAsync(organisationId, wrapperContactPointRequest);

      var createdOrgContactPoint = await _wrapperOrganisationContactService.GetOrganisationContactPointAsync(organisationId, createdContactPointId);

      return createdOrgContactPoint.Contacts.Select(c => c.ContactId).First(); // There should be a created one
    }

    /// <summary>
    /// Update an organisation contact point for a given contact id
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="organisationId"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> UpdateOrganisationContactAsync(int contactId, string organisationId, Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.OrganisationContact);

      var userContactInfoList = await _wrapperOrganisationContactService.GetOrganisationContactsAsync(organisationId);

      var contactPoint = GetContactPointOfRelevantContact(contactId, userContactInfoList);

      var wrapperContactPointRequest = GetWrapperContactPointRequestObjectForUpdate(contactId, contactData, conclaveEntityMappingDictionary, contactPoint);

      await _wrapperOrganisationContactService.UpdateOrganisationContactPointAsync(organisationId, wrapperContactPointRequest.ContactPointId, wrapperContactPointRequest);

      return contactId;
    }

    /// <summary>
    /// Get organisation contact
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="organisationId"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetOrganisationContactAsync(int contactId, string organisationId)
    {
      List<Dictionary<string, object>> resultDictionaries = new();
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.OrganisationContact);

      var orgContactInfoList = await _wrapperOrganisationContactService.GetOrganisationContactsAsync(organisationId);

      var contactPoint = GetContactPointOfRelevantContact(contactId, orgContactInfoList);

      if (contactPoint == null)
      {
        throw new ResourceNotFoundException();
      }

      var contact = contactPoint.Contacts.First(c => c.ContactId == contactId); // Get relevant contact by contactId
      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      resultDictionaries.Add(GetContactDataMappedDictionary(contact, contactMapping));

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgContact))
      {
        var convertedMapping = GetConvertedFlatMappingForSingelContactPoint(conclaveEntityMappingDictionary[ConclaveEntityNames.OrgContact]);
        var contactPointData = _attributeMappingService.GetMappedDataDictionary(contactPoint, convertedMapping);
        resultDictionaries.Add(contactPointData);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgProfile))
      {
        var orgResponse = await _wrapperOrganisationService.GetOrganisationAsync(organisationId);
        var orgResultDictionary = _attributeMappingService.GetMappedDataDictionary(orgResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgProfile]);
        resultDictionaries.Add(orgResultDictionary);
      }

      var result = _attributeMappingService.GetMergedResultDictionary(resultDictionaries);

      return result;
    }
    #endregion

    #region SiteContacts

    /// <summary>
    /// Create a site contact point for an individual contact
    /// </summary>
    /// <param name="organisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> CreateSiteContactAsync(string organisationId, int siteId, Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.SiteContact);

      var wrapperContactPointRequest = GetWrapperContactPointRequestObjectForCreate(contactData, conclaveEntityMappingDictionary);

      var createdContactPointId = await _wrapperSiteContactService.CreateSiteContactPointAsync(organisationId, siteId, wrapperContactPointRequest);

      var createdSiteContactPoint = await _wrapperSiteContactService.GetSiteContactPointAsync(organisationId, siteId, createdContactPointId);

      return createdSiteContactPoint.Contacts.Select(c => c.ContactId).First(); // There should be a created one
    }

    /// <summary>
    /// Updating the site contact point given an individual contact id
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="organisationId"></param>
    /// <param name="siteId"></param>
    /// <param name="contactData"></param>
    /// <returns></returns>
    public async Task<int> UpdateSiteContactAsync(int contactId, string organisationId, int siteId, Dictionary<string, object> contactData)
    {
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.SiteContact);

      var userContactInfoList = await _wrapperSiteContactService.GetSiteContactPointsAsync(organisationId, siteId);

      var contactPoint = GetContactPointOfRelevantContact(contactId, userContactInfoList);

      var wrapperContactPointRequest = GetWrapperContactPointRequestObjectForUpdate(contactId, contactData, conclaveEntityMappingDictionary, contactPoint);

      await _wrapperSiteContactService.UpdateSiteContactPointAsync(organisationId, siteId, wrapperContactPointRequest.ContactPointId, wrapperContactPointRequest);

      return contactId;
    }

    /// <summary>
    /// Get site contact
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="organisationId"></param>
    /// <param name="siteId"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, object>> GetSiteContactAsync(int contactId, string organisationId, int siteId)
    {
      List<Dictionary<string, object>> resultDictionaries = new();
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary =
        await _attributeMappingService.GetMappedAttributeDictionaryAsync(ConsumerEntityNames.SiteContact);

      var orgContactInfoList = await _wrapperSiteContactService.GetSiteContactPointsAsync(organisationId, siteId);

      var contactPoint = GetContactPointOfRelevantContact(contactId, orgContactInfoList);

      if (contactPoint == null)
      {
        throw new ResourceNotFoundException();
      }

      var contact = contactPoint.Contacts.First(c => c.ContactId == contactId); // Get relevant contact by contactId
      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      resultDictionaries.Add(GetContactDataMappedDictionary(contact, contactMapping));

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.SiteContact))
      {
        var convertedMapping = GetConvertedFlatMappingForSingelContactPoint(conclaveEntityMappingDictionary[ConclaveEntityNames.SiteContact]);
        var contactPointData = _attributeMappingService.GetMappedDataDictionary(contactPoint, convertedMapping);
        resultDictionaries.Add(contactPointData);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.Site))
      {
        var orgResponse = await _wrapperSiteService.GetOrganisationSiteAsync(organisationId, siteId);
        var orgResultDictionary = _attributeMappingService.GetMappedDataDictionary(orgResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.Site]);
        resultDictionaries.Add(orgResultDictionary);
      }

      if (conclaveEntityMappingDictionary.Any(g => g.Key == ConclaveEntityNames.OrgProfile))
      {
        var orgResponse = await _wrapperOrganisationService.GetOrganisationAsync(organisationId);
        var orgResultDictionary = _attributeMappingService.GetMappedDataDictionary(orgResponse, conclaveEntityMappingDictionary[ConclaveEntityNames.OrgProfile]);
        resultDictionaries.Add(orgResultDictionary);
      }

      var result = _attributeMappingService.GetMergedResultDictionary(resultDictionaries);
      return result;
    }
    #endregion

    #region Private

    #region Request Related

    /// <summary>
    /// Get tye contact request object from contact mappings
    /// </summary>
    /// <param name="contactData"></param>
    /// <param name="contactMappingDictionary"></param>
    /// <returns></returns>
    private WrapperContactRequest GetContactRequestObject(Dictionary<string, object> contactData, Dictionary<string, string> contactMappingDictionary)
    {
      var validContactTypes = new List<string> { "PHONE", "EMAIL", "FAX", "MOBILE", "WEB_ADDRESS" };
      var contactTypeConsumerFieldName = contactMappingDictionary["ContactType"];
      var contactValueConsumerFieldName = contactMappingDictionary["ContactValue"];
      if (contactTypeConsumerFieldName == null || contactValueConsumerFieldName == null)
      {
        throw new CcsSsoException("ERROR_CONTACT_MAPPING_NOT_FOUND");
      }
      else
      {
        if (!contactData.ContainsKey(contactTypeConsumerFieldName) || !contactData.ContainsKey(contactValueConsumerFieldName))
        {
          throw new CcsSsoException("ERROR_INVALID_ATTRIBUTES");
        }
        var contactTypeValue = contactData[contactTypeConsumerFieldName];
        if (contactTypeValue == null)
        {
          throw new CcsSsoException($"ERROR_INVALID_CONTACT_TYPE");
        }
        if (validContactTypes.Contains(contactTypeValue))
        {
          throw new CcsSsoException($"ERROR_INVALID_VALUE_FOR:{contactTypeConsumerFieldName}");
        }
        WrapperContactRequest wrapperContactRequest = new()
        {
          ContactType = contactTypeValue.ToString(),
          ContactValue = contactData[contactValueConsumerFieldName]?.ToString()
        };
        return wrapperContactRequest;
      }
    }

    /// <summary>
    /// Get the WrapperContactPointRequest for contactpoint creation
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="contactData"></param>
    /// <param name="conclaveEntityMappingDictionary"></param>
    /// <returns></returns>
    private WrapperContactPointRequest GetWrapperContactPointRequestObjectForCreate(Dictionary<string, object> contactData,
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary)
    {
      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      var contactRequest = GetContactRequestObject(contactData, contactMapping);

      List<WrapperContactRequest> contactRequestList = new();
      contactRequestList.Add(contactRequest);
      return new WrapperContactPointRequest
      {
        Contacts = contactRequestList
      };
    }

    /// <summary>
    /// Get the updating contact point request object for contactpoint update
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="contactData"></param>
    /// <param name="conclaveEntityMappingDictionary"></param>
    /// <param name="contactPoint"></param>
    /// <returns></returns>
    private WrapperContactPointRequest GetWrapperContactPointRequestObjectForUpdate(int contactId, Dictionary<string, object> contactData,
      Dictionary<string, Dictionary<string, string>> conclaveEntityMappingDictionary, WrapperContactPoint contactPoint)
    {
      if (contactPoint == null)
      {
        throw new ResourceNotFoundException();
      }

      var contact = contactPoint.Contacts.First(c => c.ContactId == contactId); // Get relevant contact by contactId
      if (contact == null) //If the exact contact not found
      {
        throw new ResourceNotFoundException();
      }
      contactPoint.Contacts.Remove(contact); // Remove the exact contact from the list before updating it

      var contactMapping = conclaveEntityMappingDictionary[ConclaveEntityNames.Contact];
      var contactRequest = GetContactRequestObject(contactData, contactMapping);

      var contactRequestList = contactPoint.Contacts.Select(c => new WrapperContactRequest { ContactType = c.ContactType, ContactValue = c.ContactValue }).ToList();
      contactRequestList.Add(contactRequest);
      return new WrapperContactPointRequest
      {
        ContactPointId = contactPoint.ContactPointId,
        ContactPointName = contactPoint.ContactPointName,
        ContactPointReason = contactPoint.ContactPointReason,
        Contacts = contactRequestList
      };
    }

    #endregion

    #region Response related

    /// <summary>
    /// Get the mapped individual contact object or its fields as a dictionary
    /// </summary>
    /// <param name="wrapperContactResponse"></param>
    /// <param name="contactMappingDictionary"></param>
    /// <returns></returns>
    private Dictionary<string, object> GetContactDataMappedDictionary(WrapperContactResponse wrapperContactResponse,
      Dictionary<string, string> contactMappingDictionary)
    {
      // If required as an object with all the contact only information: mapped using "Contact" (ConclaveAttributeNames.ContactObject)
      if (contactMappingDictionary.ContainsKey(ConclaveAttributeNames.ContactObject))
      {
        return new Dictionary<string, object> { { contactMappingDictionary[ConclaveAttributeNames.ContactObject], wrapperContactResponse } };
      }
      else // If required as individually mapped fields
      {
        return _attributeMappingService.GetMappedDataDictionary(wrapperContactResponse, contactMappingDictionary);
      }
    }

    /// <summary>
    /// Get the nested field mappings of contact mappings. (Convert to flat mappings by removing the ContactPoint)
    /// Ex:- ContactPoint.Reason => Reason 
    /// Ex:- ContactPoint.Name => Name 
    /// </summary>
    /// <param name="contactMappingDictionary"></param>
    /// <returns></returns>
    private Dictionary<string, string> GetConvertedFlatMappingForSingelContactPoint(Dictionary<string, string> contactMappingDictionary)
    {
      Dictionary<string, string> convertedDictionary = new Dictionary<string, string>();

      foreach (var keyValue in contactMappingDictionary)
      {
        if (keyValue.Key.Contains("."))
        {
          convertedDictionary.Add(keyValue.Key.Split(".")[1], keyValue.Value);
        }
      }

      return convertedDictionary;
    }

    #endregion

    /// <summary>
    /// Get the contact point which has the given individual contact id 
    /// </summary>
    /// <param name="contactId"></param>
    /// <param name="wrapperContactPointInfoList"></param>
    /// <returns></returns>
    private WrapperContactPoint GetContactPointOfRelevantContact(int contactId, WrapperContactPointInfoList wrapperContactPointInfoList)
    {
      var contactPoint = wrapperContactPointInfoList.ContactPoints.FirstOrDefault(cp => cp.Contacts.Any(c => c.ContactId == contactId));

      return contactPoint;
    }

    #endregion
  }
}
