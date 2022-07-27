using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CcsSso.Shared.Domain.Dto;
using Newtonsoft.Json;

namespace CcsSso.Shared.Services
{
  public class CSVConverter : ICSVConverter
  {

    public byte[] ConvertToCSV(dynamic inputModel, string filetype)
    {
      try
      {

        // Check for the filetype 
        List<string> csvData = new List<string>();

        if (filetype.ToLower() == "organisation")
        {
          csvData = ConstructCSVData(inputModel);
        }
        else if (filetype.ToLower() == "user")
        {
          csvData = ConstructCSVData(inputModel);
        }
        else if (filetype.ToLower() == "audit")
        {
          csvData = ConstructCSVData(inputModel);
        }
        else if (filetype.ToLower() == "contact-org")
        {
          csvData = ConstructCSVData(inputModel);
        }
        else if (filetype.ToLower() == "contact-user")
        {
          csvData = ConstructCSVData(inputModel);
        }
        else if (filetype.ToLower() == "contact-site")
        {
          csvData = ConstructCSVData(inputModel);
        }
        else
        {
          return Array.Empty<Byte>();
        }

        byte[] data;
        using (MemoryStream ms = new MemoryStream())
        {
          data = csvData.SelectMany(s => Encoding.UTF8.GetBytes(s + Environment.NewLine)).ToArray();

          return data;
        }
      }
      catch (Exception)
      {
        Console.WriteLine($"ConvertToCSV> Exception file type= {filetype}");
        throw;
      }
    }

    private List<string> ConstructCSVData(List<AuditLogResponseInfo> auditLogResponseInfo)
    {
      List<string> csvUserDataForAuditLog = new List<string>();

      string[] csvUserHeader =  {
                AuditLogHeaderMap.ID
                ,AuditLogHeaderMap.Event
                ,AuditLogHeaderMap.UserId
                ,AuditLogHeaderMap.Application
                ,AuditLogHeaderMap.ReferenceData
                ,AuditLogHeaderMap.IpAddress
                ,AuditLogHeaderMap.Device
                };

      csvUserDataForAuditLog.Add(string.Join(",", csvUserHeader.ToArray()));

      string AuditLogUsers = string.Empty;

      foreach (var item in auditLogResponseInfo)
      {
        string[] row = { item.Id.ToString(),
                            EscapeCharacter(item.Event),
                            EscapeCharacter(item.UserId),
                            EscapeCharacter(item.Application),
                            EscapeCharacter(item.ReferenceData),
                            EscapeCharacter(item.IpAddress),
                            EscapeCharacter(item.Device)
                            };
        csvUserDataForAuditLog.Add(string.Join(",", row));
      }
      return csvUserDataForAuditLog;

    }
    private List<string> ConstructCSVData(List<UserProfileResponseInfo> userProfileList)
    {
      List<string> csvUserData = new List<string>();

      string[] csvUserHeader =  {
                UserHeaderMap.ID,
                UserHeaderMap.UserName,
                UserHeaderMap.OrganisationID,
                UserHeaderMap.FirstName,
                UserHeaderMap.LastName,
                UserHeaderMap.Title,
                UserHeaderMap.mfaEnabled,
                UserHeaderMap.AccountVerified,
                UserHeaderMap.SendUserRegistrationEmail,
                UserHeaderMap.IsAdminUser,
                UserHeaderMap.UserGroups,
                UserHeaderMap.RolePermissionInfo,
                UserHeaderMap.IdentityProviders
                };

      csvUserData.Add(string.Join(",", csvUserHeader.ToArray()));

      string userGroups = string.Empty;
      string rolePermissionInfo = string.Empty;
      string identityProviders = string.Empty;
      string userId = string.Empty;

      foreach (var item in userProfileList)
      {
        if (item.detail != null)
        {
          //userGroups = (item != null && item.detail.userGroups.Any()) ? JsonConvert.SerializeObject(item.detail.userGroups).Replace(',', '|').ToString() : "";
          if (item.detail.userGroups != null && item.detail.userGroups.Any())
          {
            string completeGroup = string.Empty;
            string appendPipe = string.Empty;
            if (item.detail.userGroups.Count > 1)
            {
              appendPipe = " |";
            }
            foreach (var roleitem in item.detail.userGroups)
            {              
              completeGroup = completeGroup + roleitem.GroupId + " - " + roleitem.AccessRoleName + appendPipe;
            }
            userGroups = completeGroup;
          }

          //rolePermissionInfo = (item != null && item.detail.rolePermissionInfo.Any()) ? JsonConvert.SerializeObject(item.detail.rolePermissionInfo).Replace(',', '|').ToString() : "";
          if (item.detail.rolePermissionInfo != null && item.detail.rolePermissionInfo.Any())
          {
            string completeRole = String.Empty;
            string appendPipe = string.Empty;
            if (item.detail.rolePermissionInfo.Count > 1)
            {
              appendPipe = " |";
            }
            foreach (var roleitem in item.detail.rolePermissionInfo)
            {
              completeRole = completeRole + roleitem.RoleId + " - " + roleitem.RoleName + appendPipe;
            }
            rolePermissionInfo = completeRole;
          }

          //identityProviders = (item != null && item.detail.identityProviders.Any()) ? JsonConvert.SerializeObject(item.detail.identityProviders).Replace(',', '|').ToString() : "";
          if (item.detail.identityProviders != null && item.detail.identityProviders.Any())
          {
            string completeIdentityProvider = String.Empty;
            string appendPipe = string.Empty;
            if (item.detail.identityProviders.Count > 1)
            {
              appendPipe = " |";
            }
            foreach (var roleitem in item.detail.identityProviders)
            {
              completeIdentityProvider = completeIdentityProvider + roleitem.IdentityProviderId + " - " + roleitem.IdentityProvider + appendPipe;
            }
            identityProviders = completeIdentityProvider;
          }
          userId = item.detail.Id.ToString();
        }

        string[] row = { userId,
                            EscapeCharacter(item.UserName),
                            EscapeCharacter(item.OrganisationId),
                            EscapeCharacter(item.FirstName),
                            EscapeCharacter(item.LastName),
                            EscapeCharacter(item.Title),
                            EscapeCharacter(item.mfaEnabled.ToString()),
                            EscapeCharacter(item.AccountVerified.ToString()),
                            EscapeCharacter(item.SendUserRegistrationEmail.ToString()),
                            EscapeCharacter(item.IsAdminUser.ToString()),
                            userGroups,
                            rolePermissionInfo,
                            identityProviders
                            };
        csvUserData.Add(string.Join(",", row));
      }
      return csvUserData;

    }

    private List<string> ConstructCSVData(List<OrganisationProfileResponseInfo> orgProfileList)
    {

      List<string> csvData = new List<string>();

      string[] csvHeader =  {
            OrganisationHeaderMap.Identifier_Id,
            OrganisationHeaderMap.Identifier_LegalName,
            OrganisationHeaderMap.Identifier_Uri,
            OrganisationHeaderMap.Identifier_Scheme,
            OrganisationHeaderMap.AdditionalIdentifiers,
            OrganisationHeaderMap.Address_StreetAddress,
            OrganisationHeaderMap.Address_Locality,
            OrganisationHeaderMap.Address_Region,
            OrganisationHeaderMap.Address_PostalCode,
            OrganisationHeaderMap.Address_CountryCode,
            OrganisationHeaderMap.Address_CountryName,
            OrganisationHeaderMap.Detail_Organisation_Id,
            OrganisationHeaderMap.Detail_CreationDate,
            OrganisationHeaderMap.Detail_BusinessType,
            OrganisationHeaderMap.Detail_SupplierBuyerType,
            OrganisationHeaderMap.Detail_IsSme,
            OrganisationHeaderMap.Detail_IsVcse,
            OrganisationHeaderMap.Detail_RightToBuy,
            OrganisationHeaderMap.Detail_IsActive

            ////"Identifier_Id"
            ////,"Identifier_LegalName"
            ////,"Identifier_Uri"
            ////,"Identifier_Scheme"
            ////,"AdditionalIdentifiers"
            ////,"Address_streetAddress"
            ////,"Address_locality"
            ////,"Address_region"
            ////,"Address_postalCode"
            ////,"Address_countryCode"
            ////,"Address_countryName"
            ////,"detail_organisationId"
            ////,"detail_creationDate"
            ////,"detail_businessType"
            ////,"detail_supplierBuyerType"
            ////,"detail_isSme"
            ////,"detail_isVcse"
            ////,"detail_rightToBuy"
            ////,"detail_isActive"
          };

      csvData.Add(string.Join(",", csvHeader.ToArray()));
      string addtionalIdentifiers = string.Empty;

      foreach (var item in orgProfileList)
      {
        //string addtionalIdentifiers = (item.AdditionalIdentifiers != null && item.AdditionalIdentifiers.Any()) ? JsonConvert.SerializeObject(item.AdditionalIdentifiers) : "";
        if (item.AdditionalIdentifiers != null && item.AdditionalIdentifiers.Any())
        {
          string appendPipe = string.Empty;
          if (item.AdditionalIdentifiers.Count > 1)
          {
            appendPipe = " |";
          }
          foreach (var addtionalIdentifierItem in item.AdditionalIdentifiers)
          {
            addtionalIdentifiers = addtionalIdentifiers + OrganisationHeaderMap.AdditionalIdentifiers_Id + ":" +addtionalIdentifierItem.Id + " - " 
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_LegalName + ":" + addtionalIdentifierItem.LegalName + " - " 
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_URI + ":" + addtionalIdentifierItem.Uri + " - "
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_Scheme + ":" + addtionalIdentifierItem.Scheme + appendPipe;
          }

        }

        string[] row = { item.Identifier.Id,
                                  EscapeCharacter(item.Identifier.LegalName),
                                  EscapeCharacter(item.Identifier.Uri),
                                  EscapeCharacter(item.Identifier.Scheme),
                                  addtionalIdentifiers,
                                  EscapeCharacter(item.Address.StreetAddress),
                                  EscapeCharacter(item.Address.Locality),
                                  EscapeCharacter(item.Address.Region),
                                  EscapeCharacter(item.Address.PostalCode),
                                  EscapeCharacter(item.Address.CountryCode),
                                  EscapeCharacter(item.Address.CountryName),
                                  EscapeCharacter(item.Detail.OrganisationId),
                                  EscapeCharacter(item.Detail.CreationDate),
                                  EscapeCharacter(item.Detail.BusinessType),
                                  EscapeCharacter(item.Detail.SupplierBuyerType!=null?item.Detail.SupplierBuyerType.ToString():""),
                                  EscapeCharacter(item.Detail.IsSme!=null ? item.Detail.IsSme.ToString():""),
                                  EscapeCharacter(item.Detail.IsVcse!=null? item.Detail.IsVcse.ToString():""),
                                  EscapeCharacter(item.Detail.RightToBuy!=null? item.Detail.RightToBuy.ToString():""),
                                  EscapeCharacter(item.Detail.IsActive!=null?item.Detail.IsActive.ToString():"")};

        csvData.Add(string.Join(",", row));
      }
      return csvData;
    }
    private List<string> ConstructCSVData(List<ContactOrgResponseInfo> contactList)
    {

      List<string> csvData = new List<string>();

      string[] csvHeader =  {
                ContactsHeaderMap.ContactType
                ,ContactsHeaderMap.ContactID
                ,ContactsHeaderMap.ContactPointID
                ,ContactsHeaderMap.OriginalContactPointID
                ,ContactsHeaderMap.AssignedContactType
                ,ContactsHeaderMap.Contact_ContactID
                ,ContactsHeaderMap.Contacts_ContactType
                ,ContactsHeaderMap.Contacts_ContactValue
                ,ContactsHeaderMap.ContactPoint_Reason
                ,ContactsHeaderMap.ContactPoint_Name
          };

      csvData.Add(string.Join(",", csvHeader.ToArray()));

      foreach (var item in contactList)
      {
        foreach (var contactsItem in item.contacts)
        {
          string[] row = {
                            EscapeCharacter(item.contactType ),
                            EscapeCharacter(item.detail.organisationId.ToString()),
                            item.contactPointId.ToString(),
                            EscapeCharacter(item.originalContactPointId.ToString()),
                            EscapeCharacter(item.assignedContactType.ToString()),
                            EscapeCharacter(contactsItem.contactId.ToString()),
                            EscapeCharacter(contactsItem.contactType),
                            EscapeCharacter(contactsItem.contactValue),
                            EscapeCharacter(item.contactPointReason),
                            EscapeCharacter(item.contactPointName)
                            };

          csvData.Add(string.Join(",", row));
        }
      }
      return csvData;
    }

    private List<string> ConstructCSVData(List<ContactUserResponseInfo> contactList)
    {

      List<string> csvData = new List<string>();

      string[] csvHeader =  {
                ContactsHeaderMap.ContactType
                ,ContactsHeaderMap.ContactID
                ,ContactsHeaderMap.ContactPointID
                ,ContactsHeaderMap.OriginalContactPointID
                ,ContactsHeaderMap.AssignedContactType
                ,ContactsHeaderMap.Contact_ContactID
                ,ContactsHeaderMap.Contacts_ContactType
                ,ContactsHeaderMap.Contacts_ContactValue
                ,ContactsHeaderMap.ContactPoint_Reason
                ,ContactsHeaderMap.ContactPoint_Name
          };

      //csvData.Add(string.Join(",", csvHeader.ToArray()));

      foreach (var item in contactList)
      {
        foreach (var contactsItem in item.contacts)
        {
          string[] row = {
                            EscapeCharacter(item.contactType ),
                            EscapeCharacter(item.detail.userId),
                            item.contactPointId.ToString(),
                            EscapeCharacter(item.originalContactPointId.ToString()),
                            EscapeCharacter(item.assignedContactType.ToString()),
                            EscapeCharacter(contactsItem.contactId.ToString()),
                            EscapeCharacter(contactsItem.contactType),
                            EscapeCharacter(contactsItem.contactValue),
                            EscapeCharacter(item.contactPointReason),
                            EscapeCharacter(item.contactPointName)
                            };

          csvData.Add(string.Join(",", row));
        }
      }
      return csvData;
    }

    private List<string> ConstructCSVData(List<ContactSiteResponseInfo> contactList)
    {

      List<string> csvData = new List<string>();

      string[] csvHeader =  {
                ContactsHeaderMap.ContactType
                ,ContactsHeaderMap.ContactID
                ,ContactsHeaderMap.ContactPointID
                ,ContactsHeaderMap.OriginalContactPointID
                ,ContactsHeaderMap.AssignedContactType
                ,ContactsHeaderMap.Contact_ContactID
                ,ContactsHeaderMap.Contacts_ContactType
                ,ContactsHeaderMap.Contacts_ContactValue
                ,ContactsHeaderMap.ContactPoint_Reason
                ,ContactsHeaderMap.ContactPoint_Name
          };

      //csvData.Add(string.Join(",", csvHeader.ToArray()));

      foreach (var item in contactList)
      {
        foreach (var contactsItem in item.contacts)
        {
          string[] row = {
                            EscapeCharacter(item.contactType ),
                            EscapeCharacter(item.detail.siteId),
                            item.contactPointId.ToString(),
                            EscapeCharacter(item.originalContactPointId.ToString()),
                            EscapeCharacter(item.assignedContactType.ToString()),
                            EscapeCharacter(contactsItem.contactId.ToString()),
                            EscapeCharacter(contactsItem.contactType),
                            EscapeCharacter(contactsItem.contactValue),
                            EscapeCharacter(item.contactPointReason),
                            EscapeCharacter(item.contactPointName)
                            };

          csvData.Add(string.Join(",", row));
        }
      }
      return csvData;
    }


    private string EscapeCharacter(string data)
    {
      char[] CHARACTERS_THAT_MUST_BE_QUOTED = { ',', '"', '\n' };
      const string QUOTE = "\"";

      if (data != null && data.IndexOfAny(CHARACTERS_THAT_MUST_BE_QUOTED) > -1)
        data = QUOTE + data + QUOTE;
      else if (data == null)
        data = "";

      return data;
    }
  }

  public static class UserHeaderMap
  {
    public const string ID = "id";
    public const string UserName = "username";
    public const string OrganisationID = "organisation_id";
    public const string FirstName = "firstname";
    public const string LastName = "lastname";
    public const string Title = "title";
    public const string mfaEnabled = "mfa_enabled";
    public const string AccountVerified = "accountverified";
    public const string SendUserRegistrationEmail = "send_user_registrationemail";
    public const string IsAdminUser = "is_adminuser";
    public const string UserGroups = "usergroups";
    public const string RolePermissionInfo = "rolepermissioninfo";
    public const string IdentityProviders = "identityproviders";

  }

  public static class OrganisationHeaderMap
  {
    public const string Identifier_Id = "identifier_id";
    public const string Identifier_LegalName = "identifier_legalname";
    public const string Identifier_Uri = "identifier_uri";
    public const string Identifier_Scheme = "identifier_scheme";
    public const string AdditionalIdentifiers = "additionalIdentifiers";
    public const string Address_StreetAddress = "address_streetaddress";
    public const string Address_Locality = "address_locality";
    public const string Address_Region = "address_region";
    public const string Address_PostalCode = "address_postalcode";
    public const string Address_CountryCode = "address_countrycode";
    public const string Address_CountryName = "address_countryname";  
    public const string Detail_Organisation_Id = "detail_organisation_id";
    public const string Detail_CreationDate = "detail_creationdate";
    public const string Detail_BusinessType = "detail_businesstype";
    public const string Detail_SupplierBuyerType = "detail_supplierbuyertype";
    public const string Detail_IsSme = "detail_is_sme";
    public const string Detail_IsVcse = "detail_is_vcse";
    public const string Detail_RightToBuy = "detail_rightTobuy";
    public const string Detail_IsActive = "detail_isactive";

    public const string AdditionalIdentifiers_Id = "Id";
    public const string AdditionalIdentifiers_LegalName = "LegalName";
    public const string AdditionalIdentifiers_URI = "Uri";
    public const string AdditionalIdentifiers_Scheme = "Scheme";

  }

  public static class ContactsHeaderMap
  {
    public const string ContactType = "contact_type";
    public const string ContactID = "contact_id";
    public const string ContactPointID = "contactpoint_id";
    public const string OriginalContactPointID = "original_contactpoint_id";
    public const string AssignedContactType = "assigned_contact_type";
    public const string Contact_ContactID = "contacts_contactid";
    public const string Contacts_ContactType = "contacts_contacttype";
    public const string Contacts_ContactValue = "contacts_contactvalue";
    public const string ContactPoint_Reason = "contactpoint_reason";
    public const string ContactPoint_Name = "contactpoint_name";         
  }

  public static class AuditLogHeaderMap
  {
    public const string ID = "ID";
    public const string Event = "Event";
    public const string UserId = "UserId";
    public const string Application = "Application";
    public const string ReferenceData = "ReferenceData";
    public const string IpAddress = "IpAddress";
    public const string Device = "Device";
  }
}