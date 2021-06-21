using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Service.External;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
namespace CcsSso.Core.Tests.External
{
  public class ContactsHelperServiceTest
  {
    public class GetContactPersonNameTuple
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", null, "tuser@mail.com", "12312", null, null),
                    string.Empty, string.Empty
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "", "tuser@mail.com", "12312", null, null),
                    string.Empty, string.Empty
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", " ", "tuser@mail.com", "12312", null, null),
                    string.Empty, string.Empty
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "12312", null, null),
                    "Test", "User"
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test Middle User", "tuser@mail.com", "12312", null, null),
                    "Test", "User"
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "12312", null, null),
                    "Test", "User"
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task ReturnCorrectNameTuple(ContactRequestInfo contactInfo, string expectedFirstName, string expectedLastName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          var (firstName, lastName) = contactHelperService.GetContactPersonNameTuple(contactInfo);

          Assert.Equal(expectedFirstName, firstName);
          Assert.Equal(expectedLastName, lastName);

        });
      }
    }

    public class GetContactPointReasonId
    {
      [Theory]
      [InlineData("OTHER", 1)]
      [InlineData("SHIPPING", 2)]
      [InlineData("BILLING", 3)]
      [InlineData("", 5)]
      [InlineData(null, 5)]
      public async Task ReturnsCorrectContactReasonId_WhenCorrectReason(string reason, int expectedId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          var result = await contactHelperService.GetContactPointReasonIdAsync(reason);

          Assert.Equal(expectedId, result);

        });
      }

      [Theory]
      [InlineData("INVALID")]
      [InlineData("others")]
      public async Task ThrowsException_WhenIncorrectContactReason(string reason)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactHelperService.GetContactPointReasonIdAsync(reason));
          Assert.Equal(ErrorConstant.ErrorInvalidContactReason, ex.Message);
        });
      }
    }

    public class ValidateContacts
    {
      public static IEnumerable<object[]> InCorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test User", "wrongemail", "+551155256325", null, null),
                    ErrorConstant.ErrorInvalidEmail
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "551155256325", null, null),
                    ErrorConstant.ErrorInvalidPhoneNumber
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+0551155", null, null),
                    ErrorConstant.ErrorInvalidPhoneNumber
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "", "", "", "", ""),
                    ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", " ", " ", " ", " ", " "),
                    ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", null, null, null, null, null),
                    ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                    new ContactRequestInfo
                    {
                      ContactPointReason = "OTHER",
                      ContactPointName = "",
                      Contacts = new List<ContactRequestDetail> { new ContactRequestDetail { ContactType = VirtualContactTypeName.Email, ContactValue = ""} }
                    },
                    ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                    new ContactRequestInfo
                    {
                      ContactPointReason = "OTHER",
                      ContactPointName = " ",
                      Contacts = new List<ContactRequestDetail> { new ContactRequestDetail { ContactType = VirtualContactTypeName.Email, ContactValue = " "} }
                    },
                    ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                    new ContactRequestInfo
                    {
                      ContactPointReason = "OTHER",
                      ContactPointName = null,
                      Contacts = new List<ContactRequestDetail> { new ContactRequestDetail { ContactType = VirtualContactTypeName.Email, ContactValue = null} }
                    },
                    ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                    new ContactRequestInfo
                    {
                      ContactPointReason = "OTHER",
                      ContactPointName = "OTHER",
                      Contacts = new List<ContactRequestDetail> { new ContactRequestDetail { ContactType = "wrongType", ContactValue = "asdas"} }
                    },
                    ErrorConstant.ErrorInvalidContactType
                },
                new object[]
                {
                    new ContactRequestInfo
                    {
                      ContactPointReason = "OTHER",
                      ContactPointName = "OTHER",
                      Contacts = new List<ContactRequestDetail> { new ContactRequestDetail { ContactType = "", ContactValue = "asdas"} }
                    },
                    ErrorConstant.ErrorInvalidContactType
                },
                new object[]
                {
                    new ContactRequestInfo
                    {
                      ContactPointReason = "OTHER",
                      ContactPointName = "OTHER",
                      Contacts = new List<ContactRequestDetail> { new ContactRequestDetail { ContactType = null, ContactValue = "asdas"} }
                    },
                    ErrorConstant.ErrorInvalidContactType
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectContactData))]
      public async Task ThrowsException_WhenValidating(ContactRequestInfo contactInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactHelperService.ValidateContactsAsync(contactInfo));

          Assert.Equal(expectedError, ex.Message);
        });
      }
    }

    public class AssignVirtualContactsToContactPoint
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "12312", null, null),
                    new ContactPoint{
                      ContactPointReasonId = 1,
                      ContactDetail = new ContactDetail
                      {
                        EffectiveFrom = DateTime.UtcNow
                      }
                    },
                    2
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactPointObjectSuccessfully(ContactRequestInfo contactInfo, ContactPoint contactPoint, int expectedVirtualAddressCount)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          await contactHelperService.AssignVirtualContactsToContactPointAsync(contactInfo, contactPoint);

          Assert.Equal(expectedVirtualAddressCount, contactPoint.ContactDetail.VirtualAddresses.Count);

          var email = contactPoint.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressTypeId == 1).VirtualAddressValue;
          var phone = contactPoint.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressTypeId == 2).VirtualAddressValue;

          Assert.Equal(contactInfo.Contacts.First(c => c.ContactType ==  VirtualContactTypeName.Email).ContactValue, email);
          Assert.Equal(contactInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Phone).ContactValue, phone);
        });
      }
    }

    public class AssignVirtualContactsToContactResponse
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    new ContactPoint{
                      ContactPointReasonId = 1,
                      Party = new Party
                      {
                        Id = 1,
                        Person = new Person
                        {
                          Id = 1,
                          FirstName = "Test",
                          LastName = "User"
                        }
                      },
                      ContactDetail = new ContactDetail
                      {
                        Id =1,
                        EffectiveFrom = DateTime.UtcNow,
                        VirtualAddresses = new List<VirtualAddress>
                        {
                          new VirtualAddress { VirtualAddressTypeId = 1, VirtualAddressValue = "testuser@mail.com"},
                          new VirtualAddress { VirtualAddressTypeId = 2, VirtualAddressValue = "1234phone"},
                          new VirtualAddress { VirtualAddressTypeId = 3, VirtualAddressValue = "1234fax"},
                          new VirtualAddress { VirtualAddressTypeId = 4, VirtualAddressValue = "testuserweb.com"},
                        }
                      }
                    },
                    new List<VirtualAddressType>
                    {
                      new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email },
                      new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone },
                      new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax },
                      new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url }
                    },
                    new ContactResponseInfo { ContactPointId = 1, ContactPointReason = "BILLING", Contacts = new List<ContactResponseDetail>() },
                    new ContactResponseInfo { ContactPointId = 1, ContactPointReason = "BILLING", ContactPointName = "Test User",
                      Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "testuser@mail.com" },
                        new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "1234phone" },
                        new ContactResponseDetail { ContactId = 3, ContactType = VirtualContactTypeName.Fax, ContactValue = "1234fax" },
                        new ContactResponseDetail { ContactId = 4, ContactType = VirtualContactTypeName.Url, ContactValue = "testuserweb.com" },
                      }
                    },
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactResponseObjectSuccessfully(ContactPoint contactPoint, List<VirtualAddressType> virtualContactTypes,
        ContactResponseInfo contactResponseInfo, ContactResponseInfo expectedContactResponseInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          contactHelperService.AssignVirtualContactsToContactResponse(contactPoint, virtualContactTypes, contactResponseInfo);

          Assert.Equal(expectedContactResponseInfo.ContactPointId, contactResponseInfo.ContactPointId);
          Assert.Equal(expectedContactResponseInfo.ContactPointReason, contactResponseInfo.ContactPointReason);
          Assert.Equal(expectedContactResponseInfo.ContactPointName, contactResponseInfo.ContactPointName);
          Assert.Equal(expectedContactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Email).ContactValue,
              contactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Email).ContactValue);
          Assert.Equal(expectedContactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Phone).ContactValue,
              contactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Phone).ContactValue);
          Assert.Equal(expectedContactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Fax).ContactValue,
              contactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Fax).ContactValue);
          Assert.Equal(expectedContactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Url).ContactValue,
              contactResponseInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Url).ContactValue);
        });
      }
    }
    public static ContactsHelperService GetContactHelperService(IDataContext dataContext)
    {
      var service = new ContactsHelperService(dataContext);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax, Description = "fax" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url, Description = "url" });

      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });

      await dataContext.SaveChangesAsync();
    }
  }
}
