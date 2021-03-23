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
  public class OrganisationContactServiceTest
  {
    public class CreateOrganisationContact
    {

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "2",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "2",
                  DtoHelper.GetContactInfo("OTHER", null, "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "2",
                  DtoHelper.GetContactInfo("OTHER", "Test Middle User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateContactSuccessfully_WhenCorrectData(string ciiOrganisationId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await contactService.CreateOrganisationContactAsync(ciiOrganisationId, contactInfo);

          var createdContactData = await dataContext.ContactPoint.Where(c => c.Party.Person.Organisation.CiiOrganisationId == ciiOrganisationId)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party)
            .ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);
          var name = contactInfo.Name;
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Split(" ");
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

      public static IEnumerable<object[]> ContactDataInvalidOrganisation =>
            new List<object[]>
            {
                new object[]
                {
                  "3",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrganisation))]
      public async Task ThrowsException_WhenOrganisationDoesntExists(string ciiOrganisationId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.CreateOrganisationContactAsync(ciiOrganisationId, contactInfo));
        });
      }
    }

    public class GetOrganisationContact
    {
      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  1,
                  new OrganisationContactInfo { ContactId = 1, OrganisationId = "1", ContactReason = ContactReasonType.Shipping,
                    Name = "PesronFN1 LN1", Email = "email1@mail.com", PhoneNumber = "+94112345671", Fax = string.Empty, WebUrl = string.Empty}
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectContactsIncludingName(string ciiOrganisationId, int contactId, OrganisationContactInfo expectedOrgContact)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationContactAsync(ciiOrganisationId, contactId);

          Assert.NotNull(result);
          Assert.Equal(expectedOrgContact.ContactId, result.ContactId);
          Assert.Equal(expectedOrgContact.OrganisationId, result.OrganisationId);
          Assert.Equal(expectedOrgContact.ContactReason, result.ContactReason);
          Assert.Equal(expectedOrgContact.Name, result.Name);
          Assert.Equal(expectedOrgContact.Email, result.Email);
          Assert.Equal(expectedOrgContact.PhoneNumber, result.PhoneNumber);
          Assert.Equal(expectedOrgContact.Fax, result.Fax);
          Assert.Equal(expectedOrgContact.WebUrl, result.WebUrl);

        });
      }

      [Theory]
      [InlineData("1", 10)]
      public async Task ThrowsException_WhenNotExsists(string ciiOrganisationId, int contactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationContactAsync(ciiOrganisationId, contactId));

        });
      }
    }

    public class GetOrganisationContactsList
    {

      [Theory]
      [InlineData("1", 1)]
      public async Task ReturnsCorrectList(string ciiOrganisationId, int expectedCount)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationContactsListAsync(ciiOrganisationId);

          Assert.Equal(expectedCount, result.ContactsList.Count);

        });
      }

      [Theory]
      [InlineData("2")]
      public async Task ReturnsEmpty_WhenNotExsists(string ciiOrganisationId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationContactsListAsync(ciiOrganisationId);

          Assert.Empty(result.ContactsList);

        });
      }

      [Theory]
      [InlineData("3")]
      public async Task ThrowsException_WhenOrganisationNotExsists(string ciiOrganisationId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationContactsListAsync(ciiOrganisationId));

        });
      }
    }

    public class DeleteOrganisationContact
    {
      [Theory]
      [InlineData("1", 1)]
      public async Task DeleteOrganisationContactSuccessfully(string ciiOrganisationId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteOrganisationContactAsync(ciiOrganisationId, deletingContactId);

          var deletedContact = await dataContext.ContactPoint.FirstOrDefaultAsync(c => c.Id == deletingContactId);

          Assert.True(deletedContact.IsDeleted);
        });
      }

      [Theory]
      [InlineData("1", 2)]
      [InlineData("3", 2)]
      public async Task ThrowsException_WhenContactDoesntExists(string ciiOrganisationId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.DeleteOrganisationContactAsync(ciiOrganisationId, deletingContactId));
        });
      }
    }

    public class UpdateOrganisationContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactSuccessfully_WhenCorrectData(string ciiOrganisationId, int contactId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.UpdateOrganisationContactAsync(ciiOrganisationId, contactId, contactInfo);

          var updatedContactData = await dataContext.ContactPoint.Where(c => c.Id == contactId)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(updatedContactData);
          var name = contactInfo.Name;
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Split(" ");
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

      public static IEnumerable<object[]> ContactDataInvalidOrganisation =>
            new List<object[]>
            {
                new object[]
                {
                  "3",
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrganisation))]
      public async Task ThrowsException_WhenOrganisationDoesntExists(string ciiOrganisationId, int contactId, ContactInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateOrganisationContactAsync(ciiOrganisationId, contactId, contactInfo));
        });
      }
    }

    public static OrganisationContactService ContactService(IDataContext dataContext)
    {
      IContactsHelperService contactsHelperService = new ContactsHelperService(dataContext);
      var service = new OrganisationContactService(dataContext, contactsHelperService);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = "INTERNAL_ORGANISATION" });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", OrganisationUri = "Org1Uri", RightToBuy = true });

      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true });

      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 1, VirtualAddressTypeId = 1, VirtualAddressValue = "email1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 1, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 1 });

      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "PesronFN2", LastName = "LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 2, IsDeleted = true });

      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, IdentityProviderId = 1, PartyId = 5, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 1 });

      await dataContext.SaveChangesAsync();
    }
  }
}
