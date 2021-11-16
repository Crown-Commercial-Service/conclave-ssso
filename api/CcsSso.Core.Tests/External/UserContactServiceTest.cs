using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Service.External;
using CcsSso.Shared.Cache.Contracts;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class UserContactServiceTest
  {
    public class CreateUserContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "user3@mail.com",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "user3@mail.com",
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "user3@mail.com",
                  DtoHelper.GetContactInfo("OTHER", "Test", null, null, null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateContactSuccessfully_WhenCorrectData(string userName, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          var createdContactPointId = await contactService.CreateUserContactAsync(userName, contactInfo);

          var userContactData = await dataContext.ContactPoint.Where(c => c.Id == createdContactPointId)
            .FirstOrDefaultAsync();

          var createdContactData = await dataContext.ContactPoint.Where(c => c.ContactDetailId == userContactData.ContactDetailId
            && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);
          var name = contactInfo.ContactPointName;
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

      public static IEnumerable<object[]> ContactDataInvalidUser =>
            new List<object[]>
            {
                new object[]
                {
                  "user4@mail.com",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidUser))]
      public async Task ThrowsException_WhenUserDoesnotExists(string userName, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.CreateUserContactAsync(userName, contactInfo));
        });
      }
    }

    public class GetUserContact
    {
      [Theory]
      [InlineData("user1@mail.com", 1)]
      public async Task ReturnsCorrectContactsIncludingName(string userName, int contactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetUserContactAsync(userName, contactId);

          Assert.NotNull(result);

        });
      }

      [Theory]
      [InlineData("user1@mail.com", 10)]
      public async Task ThrowsException_WhenNotExsists(string userName, int contactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetUserContactAsync(userName, contactId));

        });
      }
    }

    public class GetUserContactsList
    {

      [Theory]
      [InlineData("user1@mail.com", 2)]
      [InlineData("user2@mail.com", 1)]
      public async Task ReturnsCorrectList(string userName, int expectedCount)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetUserContactsListAsync(userName);

          Assert.Equal(expectedCount, result.ContactPoints.Count);

        });
      }

      [Theory]
      [InlineData("user4@mail.com")]
      public async Task ThrowsException_WhenUserNotExsists(string userName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetUserContactsListAsync(userName));

        });
      }
    }

    public class DeleteUserContact
    {
      [Theory]
      [InlineData("user1@mail.com", 1)]
      public async Task DeleteOrganisationContactSuccessfully(string userName, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteUserContactAsync(userName, deletingContactId);

          var deletedContact = await dataContext.ContactPoint.FirstOrDefaultAsync(c => c.Id == deletingContactId);
          Assert.True(deletedContact.IsDeleted);
        });
      }

      [Theory]
      [InlineData("user2@mail.com", 7)]
      [InlineData("user3@mail.com", 2)]
      public async Task ThrowsException_WhenContactDoesntExists(string userName, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.DeleteUserContactAsync(userName, deletingContactId));
        });
      }
    }

    public class UpdateUserContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com",
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "user1@mail.com",
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactSuccessfully_WhenCorrectData(string userName, int contactId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await contactService.UpdateUserContactAsync(userName, contactId, contactInfo);

          var updatedUserContactData = await dataContext.ContactPoint.Where(c => c.Id == contactId)
            .FirstOrDefaultAsync();

          var updatedContactData = await dataContext.ContactPoint.Where(c => c.ContactDetailId == updatedUserContactData.ContactDetailId
            && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party)
            .ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(updatedContactData);

          var name = contactInfo.ContactPointName;
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

      public static IEnumerable<object[]> ContactDataInvalidUser =>
            new List<object[]>
            {
                new object[]
                {
                  "user4@mail.com",
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "user3@mail.com",
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidUser))]
      public async Task ThrowsException_WhenUserDoesnotExists(string userName, int contactId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateUserContactAsync(userName, contactId, contactInfo));
        });
      }
    }

    public static UserContactService ContactService(IDataContext dataContext)
    {
      Mock<ILocalCacheService> mockLocalCacheService = new();
      mockLocalCacheService.Setup(s => s.GetOrSetValueAsync<List<ContactPointReason>>("CONTACT_POINT_REASONS", It.IsAny<Func<Task<List<ContactPointReason>>>>(), It.IsAny<int>()))
        .ReturnsAsync(new List<ContactPointReason> {
          new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" },
          new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" },
          new ContactPointReason { Id = 3, Name = ContactReasonType.Site, Description = "Billing" },
          new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" },
          new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" }
        });
      IContactsHelperService contactsHelperService = new ContactsHelperService(dataContext, mockLocalCacheService.Object);
      IUserProfileHelperService userProfileHelperService = new UserProfileHelperService();
      Mock<ICcsSsoEmailService> mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
      var mockAdaptorNotificationService = new Mock<IAdaptorNotificationService>();
      var mockWrapperCacheService = new Mock<IWrapperCacheService>();
      var mockAuditLoginService = new Mock<IAuditLoginService>();
      var service = new UserContactService(dataContext, contactsHelperService, userProfileHelperService, mockCcsSsoEmailService.Object, mockAdaptorNotificationService.Object,
        mockWrapperCacheService.Object, mockAuditLoginService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = "INTERNAL_ORGANISATION" });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
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

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, OrganisationUri = "Org1Uri", RightToBuy = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });

      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, OrganisationUri = "Org2Uri", RightToBuy = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 2, IdentityProviderId = 1 });

      #region User1 contacts
      // User 1 has two contacts
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, PartyId = 3, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 3, PartyTypeId = 3, ContactPointReasonId = 1, ContactDetailId = 1 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 3, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 2 });

      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "PesronFN1", LastName = "PersonLN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 1, VirtualAddressTypeId = 1, VirtualAddressValue = "person1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 1, VirtualAddressTypeId = 2, VirtualAddressValue = "941123456p1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 1, ContactDetailId = 1 });

      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "PesronFN2", LastName = "PersonLN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "person2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "941123456p2" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 5, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 2 });
      #endregion

      #region User 2 has 1 contact (1 exisiting and 1 deleted)
      // User 2 has only 1 contact with 1 deleted contact.
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 4, PartyId = 6, OrganisationId = 1, FirstName = "UserFN2", LastName = "UserLN2" });
      dataContext.User.Add(new User { Id = 2, PartyId = 6, UserName = "user2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 5, PartyId = 6, PartyTypeId = 3, ContactPointReasonId = 1, ContactDetailId = 2 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 6, PartyId = 6, PartyTypeId = 3, ContactPointReasonId = 1, ContactDetailId = 3, IsDeleted = true });

      dataContext.Party.Add(new Party { Id = 7, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 5, PartyId = 7, OrganisationId = 1, FirstName = "PesronFN3", LastName = "PersonLN3" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow, IsDeleted = true });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 5, ContactDetailId = 3, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 6, ContactDetailId = 3, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 7, PartyId = 7, PartyTypeId = 2, ContactPointReasonId = 1, ContactDetailId = 3, IsDeleted = true });
      #endregion

      #region User 3 has no contacts
      // User 3 has no contcats
      dataContext.Party.Add(new Party { Id = 8, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 6, PartyId = 8, OrganisationId = 1, FirstName = "UserFN3", LastName = "UserLN3" });
      dataContext.User.Add(new User { Id = 3, PartyId = 8, UserName = "user3@mail.com" });
      #endregion

      await dataContext.SaveChangesAsync();
    }
  }
}
