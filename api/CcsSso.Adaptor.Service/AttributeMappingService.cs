using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Dtos.Cii;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Shared.Domain.Excecptions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service
{
  public class AttributeMappingService : IAttributeMappingService
  {
    private readonly IDataContext _dataContext;
    private readonly AdaptorRequestContext _requestContext;
    public AttributeMappingService(IDataContext dataContext, AdaptorRequestContext requestContext)
    {
      _dataContext = dataContext;
      _requestContext = requestContext;
    }

    /// <summary>
    /// This returns the attribute mappings between the given adpter consumer entity and the conclave entity.
    /// The consumer identification happens using the consumer id in the request context.
    /// Key is the Conclave Entity name
    /// Value is another dictionary where key is conclave attribute name and value is adapter consumer attribute name.
    /// Throws CcsSsoException(NoConfigurationFound) if no configuration found
    /// </summary>
    /// <param name="consumerEntityName"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, Dictionary<string, string>>> GetMappedAttributeDictionaryAsync(string consumerEntityName)
    {
      // Get mapping available for the consumer id and the relevant entity name
      var attributeMappings = await _dataContext.AdapterConclaveAttributeMappings
       .Include(acam => acam.ConclaveEntityAttribute).ThenInclude(cea => cea.ConclaveEntity)
       .Include(acam => acam.AdapterConsumerEntityAttribute).ThenInclude(aea => aea.AdapterConsumerEntity)
       .Where(am => !am.IsDeleted && am.AdapterConsumerEntityAttribute.AdapterConsumerEntity.Name == consumerEntityName &&
       am.AdapterConsumerEntityAttribute.AdapterConsumerEntity.AdapterConsumerId == _requestContext.ConsumerId)
       .ToListAsync();

      // Group the mappings by conclave entity names
      var conclaveEntityMappingDictionary = attributeMappings.GroupBy(am => am.ConclaveEntityAttribute.ConclaveEntity.Name)
       .ToDictionary(g => g.Key,
       g => g.ToList().ToDictionary(m => m.ConclaveEntityAttribute.AttributeName, m => m.AdapterConsumerEntityAttribute.AttributeName));

      if (!conclaveEntityMappingDictionary.Any())
      {
        throw new CcsSsoException(ErrorConstant.NoConfigurationFound);
      }

      return conclaveEntityMappingDictionary;
    }

    /// <summary>
    /// Returns the only the mapped data fields in a dictionary format.
    /// Key is the adapter consumber entity attribute name and
    /// Value is the mapped atttribute's value from the wrapper response
    /// </summary>
    /// <param name="dataObject"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedDataDictionary(object dataObject, Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();
      foreach (var mapping in attributeMappings) // mapping.Key = Conclave attribute name, mapping.Value = Consumber attribute name
      {
        var propertyName = mapping.Key;

        if (string.IsNullOrWhiteSpace(propertyName))
        {
          throw new CcsSsoException("INVALID_CONCLAVE_PROPERTY");
        }

        var value = GetPropertyValue(dataObject, propertyName);
        if (value != null)
        {
          resultDictionary.Add(mapping.Value, value);
        }
      }
      return resultDictionary;
    }

    /// <summary>
    /// Get mapped contact data for org, user, site
    /// Supports three typs of mappings using "ContactPoints", "Contacts" or "PHONE/EMAIL/FAX/WEB_ADDRESS"
    /// </summary>
    /// <param name="contactPoints"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedContactDataDictionaryFromContactPoints(List<WrapperContactPoint> contactPoints,
      Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();

      // 1. Request all the details (ContactPoints with contacts): mapped using "ContactPoints"
      if (attributeMappings.ContainsKey("ContactPoints"))
      {
        resultDictionary.Add(attributeMappings["ContactPoints"], contactPoints);
      }

      // 2. Only Contact object List (without reasons): mapped using "Contacts"
      else if (attributeMappings.ContainsKey("Contacts"))
      {
        resultDictionary.Add(attributeMappings["Contacts"], contactPoints.Select(cp => cp.Contacts).ToList());
      }

      // 3. Specific contacts (PHONE/EMAIL/FAX/WEB_ADDRESS/MOBILE): mapped using "PHONE/EMAIL/FAX/WEB_ADDRESS/MOBILE"
      else
      {
        var contactTypes = new List<string> { ContactType.Email, ContactType.Phone, ContactType.Fax, ContactType.Url, ContactType.Mobile };
        foreach (var mapping in attributeMappings.Where(m => contactTypes.Contains(m.Key))) // mapping.Key = ContactType in conclave, mapping.Value = Consumber attribute name
        {
          var contactType = mapping.Key;

          // Since conclave has multiple contacts (ex:- multiple phonenumbers, fax numbers) the result is a list
          var contactValueList = new List<string>();

          contactValueList.AddRange(contactPoints.Select(cp => cp.Contacts.FirstOrDefault(c => c.ContactType == contactType)?.ContactValue)
            .Where(c => !string.IsNullOrWhiteSpace(c)).ToList());

          resultDictionary.Add(mapping.Value, contactValueList);
        }
      }

      return resultDictionary;
    }

    /// <summary>
    /// Get mapped site data
    /// </summary>
    /// <param name="sites"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedOrganisationSites(List<WrapperOrganisationSite> sites,
      Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();
      sites = sites.OrderBy(s => s.Details.SiteId).ToList();
      if (attributeMappings.ContainsKey("Sites")) // Take the whole object
      {
        var mappedSites = sites.Select(s => new
        {
          s.Details.SiteId,
          s.SiteName,
          s.Address
        }).ToList();
        resultDictionary.Add(attributeMappings["Sites"], mappedSites);
      }
      if (attributeMappings.ContainsKey("Sites.Details.SiteId")) // Take the site ids
      {
        var mappedSites = sites.Select(s => s.Details.SiteId).ToList();
        resultDictionary.Add(attributeMappings["Sites.Details.SiteId"], mappedSites);
      }
      if (attributeMappings.ContainsKey("Sites.SiteName")) // Take the site names
      {
        var mappedSites = sites.Select(s => s.SiteName).ToList();
        resultDictionary.Add(attributeMappings["Sites.SiteName"], mappedSites);
      }

      return resultDictionary;
    }

    /// <summary>
    /// Get mapped organisation users
    /// </summary>
    /// <param name="orgUsers"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedOrganisationUsers(List<WrapperUserListInfo> orgUsers,
      Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();

      if (attributeMappings.ContainsKey("Users")) // Take the whole user object
      {
        var mappedUsers = orgUsers.Select(s => new
        {
          s.Name,
          s.UserName
        }).ToList();
        resultDictionary.Add(attributeMappings["Users"], mappedUsers);
      }
      if (attributeMappings.ContainsKey("Name")) { // Take user full names
        var mappedUsers = orgUsers.Select(s => s.Name).ToList();
        resultDictionary.Add(attributeMappings["Name"], mappedUsers);
      }
      if (attributeMappings.ContainsKey("UserName")) // Take user emails
      {
        var mappedUsers = orgUsers.Select(s => s.UserName).ToList();
        resultDictionary.Add(attributeMappings["UserName"], mappedUsers);
      }
      return resultDictionary;
    }

    /// <summary>
    /// Get mapped salesforce information
    /// </summary>
    /// <param name="identyifierInfo"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedOrgIdentifierInfo(CiiIdentifierAllDto identyifierInfo, Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();

      if (attributeMappings.ContainsKey("Identifier"))
      {
        resultDictionary.Add(attributeMappings["Identifier"], identyifierInfo?.Identifier);
      }
      if (attributeMappings.ContainsKey("AdditionalIdentifiers"))
      {
        resultDictionary.Add(attributeMappings["AdditionalIdentifiers"], identyifierInfo?.AdditionalIdentifiers);
      }

      return resultDictionary;
    }

    /// <summary>
    /// Get mapped user roles
    /// </summary>
    /// <param name="user"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedIdentityProviders(WrapperUserResponse user, Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();

      var identityProviderInfo = user.Detail?.IdentityProviders;

      if (identityProviderInfo != null)
      {
        identityProviderInfo = identityProviderInfo.OrderBy(i => i.IdentityProviderId).ToList();
        if (attributeMappings.ContainsKey("Detail.IdentityProviders.IdentityProviderId")) // Get idp ids
        {
          var mappedUserRoles = identityProviderInfo.Select(i => i.IdentityProviderId).ToList();
          resultDictionary.Add(attributeMappings["Detail.IdentityProviders.IdentityProviderId"], mappedUserRoles);
        }
        if (attributeMappings.ContainsKey("Detail.IdentityProviders.IdentityProviderDisplayName")) // Get idp names
        {
          var mappedUserRoles = identityProviderInfo.Select(i => i.IdentityProviderDisplayName).ToList();
          resultDictionary.Add(attributeMappings["Detail.IdentityProviders.IdentityProviderDisplayName"], mappedUserRoles);
        }
      }

      return resultDictionary;
    }

    /// <summary>
    /// Get mapped user roles
    /// </summary>
    /// <param name="user"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedUserRoles(WrapperUserResponse user, Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();

      var rolePresmissionInfo = user.Detail?.RolePermissionInfo;

      if (rolePresmissionInfo != null)
      {
        rolePresmissionInfo = rolePresmissionInfo.OrderBy(r => r.RoleId).ToList();
        if (attributeMappings.ContainsKey("Detail.RolePermissionInfo.RoleId")) // Get roles ids
        {
          var mappedUserRoles = rolePresmissionInfo.Select(s => s.RoleId).ToList();
          resultDictionary.Add(attributeMappings["Detail.RolePermissionInfo.RoleId"], mappedUserRoles);
        }
        if (attributeMappings.ContainsKey("Detail.RolePermissionInfo.RoleName")) // Get role names
        {
          var mappedUserRoles = rolePresmissionInfo.Select(s => s.RoleName).ToList();
          resultDictionary.Add(attributeMappings["Detail.RolePermissionInfo.RoleName"], mappedUserRoles);
        }
        if (attributeMappings.ContainsKey("Detail.RolePermissionInfo.RoleKey")) // Get role keys
        {
          var mappedUserRoles = rolePresmissionInfo.Select(s => s.RoleKey).ToList();
          resultDictionary.Add(attributeMappings["Detail.RolePermissionInfo.RoleKey"], mappedUserRoles);
        } 
      }

      return resultDictionary;
    }

    /// <summary>
    /// Get mapped user groups
    /// </summary>
    /// <param name="user"></param>
    /// <param name="attributeMappings"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMappedUserGroups(WrapperUserResponse user, Dictionary<string, string> attributeMappings)
    {
      Dictionary<string, object> resultDictionary = new Dictionary<string, object>();

      var userGroupsInfo = user.Detail?.UserGroups;

      if (userGroupsInfo != null)
      {
        userGroupsInfo = userGroupsInfo.OrderBy(r => r.GroupId).ToList();
        if (attributeMappings.ContainsKey("Detail.UserGroups.GroupId")) // Get group ids
        {
          var mappedUserRoles = userGroupsInfo.Select(s => s.GroupId).Distinct().ToList();
          resultDictionary.Add(attributeMappings["Detail.UserGroups.GroupId"], mappedUserRoles);
        }
        if (attributeMappings.ContainsKey("Detail.UserGroups.Group")) // Get group names
        {
          var mappedUserRoles = userGroupsInfo.Select(s => s.Group).Distinct().ToList();
          resultDictionary.Add(attributeMappings["Detail.UserGroups.Group"], mappedUserRoles);
        }
        if (attributeMappings.ContainsKey("Detail.UserGroups.AccessRole")) // Get role keys of groups
        {
          var mappedUserRoles = userGroupsInfo.Select(s => s.AccessRole).Distinct().ToList();
          resultDictionary.Add(attributeMappings["Detail.UserGroups.AccessRole"], mappedUserRoles);
        }
        if (attributeMappings.ContainsKey("Detail.UserGroups.AccessRoleName")) // Get role names of groups
        {
          var mappedUserRoles = userGroupsInfo.Select(s => s.AccessRoleName).Distinct().ToList();
          resultDictionary.Add(attributeMappings["Detail.UserGroups.AccessRoleName"], mappedUserRoles);
        }
      }
      return resultDictionary;
    }

    /// <summary>
    /// Get a merged single dictionary from a list of dictionaries
    /// </summary>
    /// <param name="dictionaries"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetMergedResultDictionary(List<Dictionary<string, object>> dictionaries)
    {
      var result = dictionaries.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
      return result;
    }

    /// <summary>
    /// Get property values from a dynamic object
    /// Supports nested properties by calling recursively (nested using dot '.')
    /// </summary>
    /// <param name="sourceObject"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    private object GetPropertyValue(object sourceObject, string propertyName)
    {
      if (propertyName.Contains('.'))
      {
        var nestedProperties = propertyName.Split(new char[] { '.' }, 2);
        return GetPropertyValue(GetPropertyValue(sourceObject, nestedProperties[0]), nestedProperties[1]);
      }
      else
      {
        var propertyInfo = sourceObject?.GetType().GetProperty(propertyName);
        return propertyInfo?.GetValue(sourceObject);
      }
    }
  }
}
