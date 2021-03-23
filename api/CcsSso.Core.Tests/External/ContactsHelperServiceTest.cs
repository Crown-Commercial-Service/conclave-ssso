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
      public async Task ReturnCorrectNameTuple(ContactInfo contactInfo, string expectedFirstName, string expectedLastName)
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
                }
            };

      [Theory]
      [MemberData(nameof(InCorrectContactData))]
      public async Task ThrowsException_WhenValidating(ContactInfo contactInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          var ex = Assert.Throws<CcsSsoException>(() => contactHelperService.ValidateContacts(contactInfo));

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
      public async Task UpdateContactPointObjectSuccessfully(ContactInfo contactInfo, ContactPoint contactPoint, int expectedVirtualAddressCount)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);

          await contactHelperService.AssignVirtualContactsToContactPointAsync(contactInfo, contactPoint);

          Assert.Equal(expectedVirtualAddressCount, contactPoint.ContactDetail.VirtualAddresses.Count);

          var email = contactPoint.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressTypeId == 1).VirtualAddressValue;
          var phone = contactPoint.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressTypeId == 2).VirtualAddressValue;

          Assert.Equal(contactInfo.Email, email);
          Assert.Equal(contactInfo.PhoneNumber, phone);
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
                    new ContactResponseInfo { ContactId = 1, ContactReason = "BILLING" },
                    new ContactResponseInfo { ContactId = 1, ContactReason = "BILLING", Name = "Test User", Email = "testuser@mail.com",
                      PhoneNumber = "1234phone", Fax = "1234fax", WebUrl = "testuserweb.com" },
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

          Assert.Equal(expectedContactResponseInfo.ContactId, contactResponseInfo.ContactId);
          Assert.Equal(expectedContactResponseInfo.ContactReason, contactResponseInfo.ContactReason);
          Assert.Equal(expectedContactResponseInfo.Name, contactResponseInfo.Name);
          Assert.Equal(expectedContactResponseInfo.Email, contactResponseInfo.Email);
          Assert.Equal(expectedContactResponseInfo.PhoneNumber, contactResponseInfo.PhoneNumber);
          Assert.Equal(expectedContactResponseInfo.Fax, contactResponseInfo.Fax);
          Assert.Equal(expectedContactResponseInfo.WebUrl, contactResponseInfo.WebUrl);
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
