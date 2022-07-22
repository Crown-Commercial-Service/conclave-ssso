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
            else if (filetype.ToLower() == "contact")
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
                "ID"
                ,"userName"
                ,"organisationId"
                ,"firstName"
                ,"lastName"
                ,"title"
                ,"mfaEnabled"
                ,"password"
                ,"accountVerified"
                ,"sendUserRegistrationEmail"
                ,"isAdminUser"
                ,"userGroups"
                ,"rolePermissionInfo"
                ,"identityProviders"                   
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
                    userGroups = (item != null && item.detail.userGroups.Any()) ? JsonConvert.SerializeObject(item.detail.userGroups) : "";
                    rolePermissionInfo = (item != null && item.detail.rolePermissionInfo.Any()) ? JsonConvert.SerializeObject(item.detail.rolePermissionInfo) : "";
                    identityProviders = (item != null && item.detail.identityProviders.Any()) ? JsonConvert.SerializeObject(item.detail.identityProviders) : "";
                    userId = item.detail.Id.ToString();
                }

                string[] row = { userId,
                            EscapeCharacter(item.UserName),
                            EscapeCharacter(item.OrganisationId),
                            EscapeCharacter(item.FirstName),
                            EscapeCharacter(item.LastName),
                            EscapeCharacter(item.Title),
                            EscapeCharacter(item.mfaEnabled.ToString()),
                            EscapeCharacter(item.Password),
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
            "Identifier_Id"
            ,"Identifier_LegalName"
            ,"Identifier_Uri"
            ,"Identifier_Scheme"
            ,"AdditionalIdentifiers"
            ,"Address_streetAddress"
            ,"Address_locality"
            ,"Address_region"
            ,"Address_postalCode"
            ,"Address_countryCode"
            ,"Address_countryName"
            ,"detail_organisationId"
            ,"detail_creationDate"
            ,"detail_businessType"
            ,"detail_supplierBuyerType"
            ,"detail_isSme"
            ,"detail_isVcse"
            ,"detail_rightToBuy"
            ,"detail_isActive"
          };

          csvData.Add(string.Join(",", csvHeader.ToArray()));

          foreach (var item in orgProfileList)
          {
            string addtionalIdentifiers = (item.AdditionalIdentifiers != null && item.AdditionalIdentifiers.Any()) ? JsonConvert.SerializeObject(item.AdditionalIdentifiers) : "";

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
    private List<string> ConstructCSVData(List<ContactResponseInfo> contactList)
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
                            EscapeCharacter(item.contactDeducted ),
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
}