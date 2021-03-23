using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Service.External;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class OrganisationSiteContactServiceTest
  {
    public class CreateOrganisationSiteContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", "+551155256325", "url.com")
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateSiteContactSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          var createdContactId = await contactService.CreateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactInfo);

          var createdSiteContact = await dataContext.SiteContact.FirstOrDefaultAsync(sc => sc.Id == createdContactId);

          var createdContactData = await dataContext.ContactPoint.Where(c => c.Id == createdSiteContact.ContactId)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);
          var name = contactInfo.Name;
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Trim().Split(" ");
            Assert.Equal(nameArray[0], createdContactData.Party.Person.FirstName);
            if (nameArray.Length >= 2)
            {
              Assert.Equal(nameArray[nameArray.Length - 1], createdContactData.Party.Person.LastName);
            }
            else
            {
              Assert.Empty(createdContactData.Party.Person.LastName);
            }
          }
          else
          {
            Assert.Empty(createdContactData.Party.Person.FirstName);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSite =>
            new List<object[]>
            {
                new object[]
                {
                  1,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  2,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  3,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  4,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSite))]
      public async Task ThrowsResourceNotFoundException_WhenOrgSiteDoesnotExists(string ciiOrganisationId, int siteId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.CreateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactInfo));
        });
      }
    }

    public class DeleteOrganisationSiteContact
    {
      [Theory]
      [InlineData("1", 2, 1)]
      public async Task DeleteOrganisationSiteContactSuccessfully(string ciiOrganisationId, int siteId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteOrganisationSiteContactAsync(ciiOrganisationId, siteId, deletingContactId);

          var deletedContact = await dataContext.SiteContact.FirstOrDefaultAsync(c => c.Id == deletingContactId);
          Assert.True(deletedContact.IsDeleted);
        });
      }

      [Theory]
      [InlineData("1", 2, 2)]
      [InlineData("1", 2, 3)]
      [InlineData("1", 3, 3)]
      [InlineData("2", 3, 3)]
      [InlineData("4", 3, 3)]
      public async Task ThrowsException_WhenOrgSiteContactDoesntExists(string ciiOrganisationId, int siteId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.DeleteOrganisationSiteContactAsync(ciiOrganisationId, siteId, deletingContactId));
        });
      }
    }

    public class GetOrganisationSiteContact
    {
      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  1,
                  new OrganisationSiteContactInfo { ContactId = 1, OrganisationId = "1", SiteId = 2, ContactReason = "BILLING",
                    Name = "PesronFN1 LN1", Email = "email1@mail.com", PhoneNumber = "+94112345671", Fax = string.Empty, WebUrl = string.Empty}
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectSiteContact(string ciiOrganisationId, int siteId, int contactId, OrganisationSiteContactInfo expectedSiteContact)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId);

          Assert.NotNull(result);
          Assert.Equal(expectedSiteContact.ContactId, result.ContactId);
          Assert.Equal(expectedSiteContact.OrganisationId, result.OrganisationId);
          Assert.Equal(expectedSiteContact.SiteId, result.SiteId);
          Assert.Equal(expectedSiteContact.ContactReason, result.ContactReason);
          Assert.Equal(expectedSiteContact.Name, result.Name);
          Assert.Equal(expectedSiteContact.Email, result.Email);
          Assert.Equal(expectedSiteContact.PhoneNumber, result.PhoneNumber);
          Assert.Equal(expectedSiteContact.Fax, result.Fax);
          Assert.Equal(expectedSiteContact.WebUrl, result.WebUrl);

        });
      }

      [Theory]
      [InlineData("1", 2, 2)]
      [InlineData("1", 2, 3)]
      [InlineData("2", 2, 2)]
      [InlineData("4", 2, 2)]
      public async Task ThrowsException_WhenNotExsists(string ciiOrganisationId, int siteId, int contactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId));

        });
      }
    }

    public class GetOrganisationSiteContactsList
    {

      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  new OrganisationSiteContactInfoList
                  {
                    OrganisationId = "1",
                    SiteId = 2,
                    SiteContacts = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactId = 1, ContactReason = "BILLING", Name = "PesronFN1 LN1",
                              Email = "email1@mail.com", PhoneNumber = "+94112345671", Fax = string.Empty, WebUrl = string.Empty}
                    }
                  }
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectList(string ciiOrganisationId, int siteId, OrganisationSiteContactInfoList expectedResult)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationSiteContactsListAsync(ciiOrganisationId, siteId);

          Assert.NotNull(result);
          Assert.Equal(expectedResult.SiteContacts.Count, result.SiteContacts.Count);

          var expectedContactResponse = expectedResult.SiteContacts.First();
          var actualContactResponse = result.SiteContacts.First();

          Assert.Equal(expectedContactResponse.ContactId, actualContactResponse.ContactId);
          Assert.Equal(expectedContactResponse.ContactReason, actualContactResponse.ContactReason);
          Assert.Equal(expectedContactResponse.Name, actualContactResponse.Name);
          Assert.Equal(expectedContactResponse.Email, actualContactResponse.Email);
          Assert.Equal(expectedContactResponse.PhoneNumber, actualContactResponse.PhoneNumber);
          Assert.Equal(expectedContactResponse.Fax, actualContactResponse.Fax);
          Assert.Equal(expectedContactResponse.WebUrl, actualContactResponse.WebUrl);
        });
      }

      [Theory]
      [InlineData("1", 4)]
      [InlineData("2", 4)]
      public async Task ThrowsException_WhenOrgSiteNotExsists(string ciiOrganisationId, int siteId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationSiteContactsListAsync(ciiOrganisationId, siteId));

        });
      }
    }

    public class UpdateOrganisationSiteContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", "+551155256325", "url.com")
                },
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateSiteContactSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, int contactId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await contactService.UpdateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId, contactInfo);

          var updatedSiteContact = await dataContext.SiteContact.FirstOrDefaultAsync(c => c.Id == contactId);

          var updatedContactData = await dataContext.ContactPoint.Where(c => c.Id == updatedSiteContact.ContactId
            && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party)
            .ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(updatedContactData);

          var name = contactInfo.Name;
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Trim().Split(" ");
            Assert.Equal(nameArray[0], updatedContactData.Party.Person.FirstName);
            if (nameArray.Length >= 2)
            {
              Assert.Equal(nameArray[nameArray.Length - 1], updatedContactData.Party.Person.LastName);
            }
            else
            {
              Assert.Empty(updatedContactData.Party.Person.LastName);
            }
          }
          else
          {
            Assert.Empty(updatedContactData.Party.Person.FirstName);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSiteContact =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", "+551155256325", "url.com")
                },
                new object[]
                {
                  "1",
                  2,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "2",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "4",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSiteContact))]
      public async Task ThrowsException_WhenUserDoesnotExists(string ciiOrganisationId, int siteId, int contactId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId, contactInfo));
        });
      }
    }

    public static OrganisationSiteContactService ContactService(IDataContext dataContext)
    {
      IContactsHelperService contactsHelperService = new ContactsHelperService(dataContext);
      var service = new OrganisationSiteContactService(dataContext, contactsHelperService);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = PartyTypeName.InternalOrgnaisation });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
      dataContext.PartyType.Add(new PartyType { Id = 4, PartyTypeName = PartyTypeName.ExternalOrgnaisation });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax, Description = "fax" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url, Description = "url" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      //Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", LegalName = "Org1", OrganisationUri = "Org1Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });
      //Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 1, ContactDetailId = 1, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 1 });
      //Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 2, ContactDetailId = 2, StreetAddress = "streetsite1", Locality = "localitysite1", Region = "regionsite1", PostalCode = "postalcodesite1", CountryCode = "countrycodesite1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 1, PartyTypeId = 1, IsSite = true, SiteName = "Org1Site1", ContactPointReasonId = 2, ContactDetailId = 2 });
      dataContext.SiteContact.Add(new SiteContact { Id = 1, ContactPointId = 2, ContactId = 5 });
      dataContext.SiteContact.Add(new SiteContact { Id = 2, ContactPointId = 2, ContactId = 6, IsDeleted = true });

      //Org2
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true, IsDeleted = true });
      //Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 3, ContactDetailId = 3, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 3 });
      //Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 4, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 4, ContactDetailId = 4, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 2, PartyTypeId = 1, IsSite = true, SiteName = "Org2Site1", IsDeleted = true, ContactPointReasonId = 1, ContactDetailId = 4 });

      // Org1 contact person 5 used in site 1 contact
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 5, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 5, VirtualAddressTypeId = 1, VirtualAddressValue = "email1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 5, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 5, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 5 });

      // Org1 contact person 6 used in site 1 contact but deleted
      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "PesronFN2", LastName = "LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 6, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 6, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 6, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 6, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 6, IsDeleted = true });

      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, IdentityProviderId = 1, PartyId = 5, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 7, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 6 });

      //Org3
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 3, PartyId = 6, CiiOrganisationId = "3", LegalName = "Org3", OrganisationUri = "Org3Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });

      await dataContext.SaveChangesAsync();
    }
  }
}
