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
      //const string fileType = "organisation"; // Existing to new change (Accept Organization, Users)

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
                "ID"
                ,"Event"
                ,"UserId"
                ,"Application"
                ,"ReferenceData"
                ,"IpAddress"
                ,"Device"
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
            string completeGroup = String.Empty;
            foreach (var roleitem in item.detail.userGroups)
            {
              completeGroup = completeGroup + roleitem.GroupId + " - " + roleitem.AccessRoleName + " |";
            }
            userGroups = completeGroup;
          }

          //rolePermissionInfo = (item != null && item.detail.rolePermissionInfo.Any()) ? JsonConvert.SerializeObject(item.detail.rolePermissionInfo).Replace(',', '|').ToString() : "";
          if (item.detail.rolePermissionInfo != null && item.detail.rolePermissionInfo.Any())
          {
            string completeRole = String.Empty;
            foreach (var roleitem in item.detail.rolePermissionInfo)
            {
              completeRole = completeRole + roleitem.RoleId + " - " + roleitem.RoleName + " |";
            }
            rolePermissionInfo = completeRole;
          }

          //identityProviders = (item != null && item.detail.identityProviders.Any()) ? JsonConvert.SerializeObject(item.detail.identityProviders).Replace(',', '|').ToString() : "";
          if (item.detail.identityProviders != null && item.detail.identityProviders.Any())
          {
            string completeIdentityProvider = String.Empty;
            foreach (var roleitem in item.detail.identityProviders)
            {
              completeIdentityProvider = completeIdentityProvider + roleitem.IdentityProviderId + " - " + roleitem.IdentityProvider + " |";
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
          foreach (var addtionalIdentifierItem in item.AdditionalIdentifiers)
          {
            addtionalIdentifiers = addtionalIdentifiers + OrganisationHeaderMap.AdditionalIdentifiers_Id + ":" +addtionalIdentifierItem.Id + " - " 
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_LegalName + ":" + addtionalIdentifierItem.LegalName + " - " 
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_URI + ":" + addtionalIdentifierItem.Uri + " - "
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_Scheme + ":" + addtionalIdentifierItem.Scheme + " | ";
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
                "contact Type",
                "contact ID",
                "contactPointId"
                ,"originalContactPointId"
                ,"assignedContactType"
                ,"contacts_contactId"
                ,"contacts_contactType"
                ,"contacts_contactValue"
                ,"contactPointReason"
                ,"contactPointName"
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
                "contact Type",
                "contact ID",
                "contactPointId"
                ,"originalContactPointId"
                ,"assignedContactType"
                ,"contacts_contactId"
                ,"contacts_contactType"
                ,"contacts_contactValue"
                ,"contactPointReason"
                ,"contactPointName"
          };

      csvData.Add(string.Join(",", csvHeader.ToArray()));

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
                "contact Type",
                "contact ID",
                "contactPointId"
                ,"originalContactPointId"
                ,"assignedContactType"
                ,"contacts_contactId"
                ,"contacts_contactType"
                ,"contacts_contactValue"
                ,"contactPointReason"
                ,"contactPointName"
          };

      csvData.Add(string.Join(",", csvHeader.ToArray()));

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
    public static string ID = "id";
    public static string UserName = "username";
    public static string OrganisationID = "organisation_id";
    public static string FirstName = "firstname";
    public static string LastName = "lastname";
    public static string Title = "title";
    public static string mfaEnabled = "mfa_enabled";
    public static string AccountVerified = "accountverified";
    public static string SendUserRegistrationEmail = "send_user_registrationemail";
    public static string IsAdminUser = "is_adminuser";
    public static string UserGroups = "usergroups";
    public static string RolePermissionInfo = "rolepermissioninfo";
    public static string IdentityProviders = "identityproviders";

  }

  public static class OrganisationHeaderMap
  {
    public static string Identifier_Id = "identifier_id";
    public static string Identifier_LegalName = "identifier_legalname";
    public static string Identifier_Uri = "identifier_uri";
    public static string Identifier_Scheme = "identifier_scheme";
    public static string AdditionalIdentifiers = "additionalIdentifiers";
    public static string Address_StreetAddress = "address_streetaddress";
    public static string Address_Locality = "address_locality";
    public static string Address_Region = "address_region";
    public static string Address_PostalCode = "address_postalcode";
    public static string Address_CountryCode = "address_countrycode";
    public static string Address_CountryName = "address_countryname";  
    public static string Detail_Organisation_Id = "detail_organisation_id";
    public static string Detail_CreationDate = "detail_creationdate";
    public static string Detail_BusinessType = "detail_businesstype";
    public static string Detail_SupplierBuyerType = "detail_supplierbuyertype";
    public static string Detail_IsSme = "detail_is_sme";
    public static string Detail_IsVcse = "detail_is_vcse";
    public static string Detail_RightToBuy = "detail_rightTobuy";
    public static string Detail_IsActive = "detail_isactive";

    public static string AdditionalIdentifiers_Id = "Id";
    public static string AdditionalIdentifiers_LegalName = "LegalName";
    public static string AdditionalIdentifiers_URI = "Uri";
    public static string AdditionalIdentifiers_Scheme = "Scheme";

  }
}