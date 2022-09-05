using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CcsSso.Shared.Domain.Dto;
using CcsSso.Shared.Domain.Constants;

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
          csvData = ConstructCSVDataToContactOrg((List<ContactOrgResponseInfo>)inputModel);
        }
        else if (filetype.ToLower() == "contact-user")
        {
          csvData = ConstructCSVDataToContactUser(inputModel);
        }
        else if (filetype.ToLower() == "contact-site")
        {
          csvData = ConstructCSVDataToContactSite(inputModel);
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

    private List<string> ConstructCSVDataToContactSite(List<ContactSiteResponseInfo> contactSiteResponseInfo)
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

      if (contactSiteResponseInfo != null)
      {
        foreach (var item in contactSiteResponseInfo)
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
      }
      return csvData;
    }

    private List<string> ConstructCSVDataToContactUser(List<ContactUserResponseInfo> contactUserResponseInfo)
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

      if (contactUserResponseInfo != null)
      {
        foreach (var item in contactUserResponseInfo)
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
      }
      return csvData;
    }

    private List<string> ConstructCSVDataToContactOrg(List<ContactOrgResponseInfo> contactOrgResponseInfo)
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

      if (contactOrgResponseInfo != null)
      {
        foreach (var item in contactOrgResponseInfo)
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
      }
      return csvData;
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
                ,AuditLogHeaderMap.EventTimeUtc
                };

      csvUserDataForAuditLog.Add(string.Join(",", csvUserHeader.ToArray()));

      string AuditLogUsers = string.Empty;

      if (auditLogResponseInfo != null)
      {
        foreach (var item in auditLogResponseInfo)
        {
          string[] row = { item.Id.ToString(),
                            EscapeCharacter(item.Event),
                            EscapeCharacter(item.UserId),
                            EscapeCharacter(item.Application),
                            EscapeCharacter(item.ReferenceData),
                            EscapeCharacter(item.IpAddress),
                            EscapeCharacter(item.Device),
                            EscapeCharacter(item.EventTimeUtc.ToString())
                            };
          csvUserDataForAuditLog.Add(string.Join(",", row));
        }
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

      if (userProfileList != null)
      {
        foreach (var item in userProfileList)
        {
          userGroups = string.Empty;
          rolePermissionInfo = string.Empty;
          identityProviders = string.Empty;

          if (item.detail != null)
          {
            //userGroups = (item != null && item.detail.userGroups.Any()) ? JsonConvert.SerializeObject(item.detail.userGroups).Replace(',', '|').ToString() : "";
            if (item.detail.userGroups != null && item.detail.userGroups.Any())
            {
              var groupIdName = item.detail.userGroups.Where(x => !string.IsNullOrEmpty(x.AccessRoleName)).Select(x => new { completeGroup = $"{x.GroupId} - {EscapeCharacter(x.AccessRoleName)} " }).ToArray();
              userGroups = String.Join(" | ", groupIdName.Select(x => x.completeGroup));
            }

            //rolePermissionInfo = (item != null && item.detail.rolePermissionInfo.Any()) ? JsonConvert.SerializeObject(item.detail.rolePermissionInfo).Replace(',', '|').ToString() : "";
            if (item.detail.rolePermissionInfo != null && item.detail.rolePermissionInfo.Any())
            {
              var roleIdAndName = item.detail.rolePermissionInfo.Select(x => new { completeRole = $"{x.RoleId} - {EscapeCharacter(x.RoleName)} " }).ToArray();
              rolePermissionInfo = String.Join(" | ", roleIdAndName.Select(x => x.completeRole));
            }

            //identityProviders = (item != null && item.detail.identityProviders.Any()) ? JsonConvert.SerializeObject(item.detail.identityProviders).Replace(',', '|').ToString() : "";
            if (item.detail.identityProviders != null && item.detail.identityProviders.Any())
            {
              var providerIdName = item.detail.identityProviders.Select(x => new { completeProvider = $"{x.IdentityProviderId} - { EscapeCharacter(x.IdentityProvider)} " }).ToArray();
              identityProviders = String.Join(" | ", providerIdName.Select(x => x.completeProvider));
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

          };

      csvData.Add(string.Join(",", csvHeader.ToArray()));
      string addtionalIdentifiers = string.Empty;

      if (orgProfileList != null)
      {
        foreach (var item in orgProfileList)
        {
          int countset = 1;
          //string addtionalIdentifiers = (item.AdditionalIdentifiers != null && item.AdditionalIdentifiers.Any()) ? JsonConvert.SerializeObject(item.AdditionalIdentifiers) : "";

          string appendPipe = string.Empty;
          foreach (var addtionalIdentifierItem in item.AdditionalIdentifiers)
          {
            if (item.AdditionalIdentifiers.Count > 1 && item.AdditionalIdentifiers.Count > countset)
            {
              appendPipe = " |";
            }
            else { appendPipe = string.Empty; }

            addtionalIdentifiers = addtionalIdentifiers + OrganisationHeaderMap.AdditionalIdentifiers_Id + ":" + CheckForNullFromChildFields(addtionalIdentifierItem.Id) + " - "
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_LegalName + ":" + CheckForNullFromChildFields(addtionalIdentifierItem.LegalName.Replace(",", " ")) + " - "
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_URI + ":" + CheckForNullFromChildFields(addtionalIdentifierItem.Uri) + " - "
                                                        //+ OrganisationHeaderMap.AdditionalIdentifiers_URI + ":" + EscapeCharacter(string.IsNullOrEmpty(addtionalIdentifierItem.Uri) ? OrganisationHeaderMap.AdditionalIdentifiers_NA : addtionalIdentifierItem.Uri).ToString() + " - "
                                                        + OrganisationHeaderMap.AdditionalIdentifiers_Scheme + ":" + CheckForNullFromChildFields(addtionalIdentifierItem.Scheme) + appendPipe;
            countset = countset + 1;
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
          addtionalIdentifiers = string.Empty;
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

      //return data.Replace(","," ").ToString();
      return data;
    }

    private string CheckForNullFromChildFields(string data)
    {
      if (string.IsNullOrEmpty(data))
      {
        return "NA";
      }
      else
      {
        return EscapeCharacter(data);
      }
    }
  }
}