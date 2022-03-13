using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
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
                    DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", null,  "+0551155", null),
                    ErrorConstant.ErrorInvalidFaxNumber
                },
                new object[]
                {
                    DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", null, null, null, "+0551155"),
                    ErrorConstant.ErrorInvalidMobileNumber
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

          Assert.Equal(contactInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Email).ContactValue, email);
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

    public class CheckAssignableSiteContactPointsExistence
    {
      public static IEnumerable<object[]> InCorrectSiteData =>
            new List<object[]>
            {
                new object[]
                {
                  "1", 3, new List<int> { 1, 2 },
                },
                new object[]
                {
                  "2", 3, new List<int> { 1, 2 },
                },
                new object[]
                {
                  "3", 3, new List<int> { 1, 2 },
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectSiteData))]
      public async Task ThrowsResourceNotFoundExceptionWhenSiteNotFound(string organisationId, int siteId, List<int> contactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactHelperService.CheckAssignableSiteContactPointsExistenceAsync(organisationId, siteId, contactPointIds));
        });
      }

      public static IEnumerable<object[]> IncorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1", 2, new List<int> { 3 }, ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1", 2, new List<int> { 1, 2, 3 }, ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1", 2, new List<int> { 1, 2, 3, 4 }, ErrorConstant.ErrorInvalidAssigningContactIds
                },
            };

      [Theory]
      [MemberData(nameof(IncorrectContactData))]
      public async Task ThrowsExceptionWhenIncorrectContactData(string organisationId, int siteId, List<int> contactPointIds, string expectedErrorMsg)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactHelperService.CheckAssignableSiteContactPointsExistenceAsync(organisationId, siteId, contactPointIds));
          Assert.Equal(expectedErrorMsg, ex.Message);
        });
      }

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1", 2, new List<int> { 1, 2 }
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task DoesNotThrowExceptionWhenCorrectContactData(string organisationId, int siteId, List<int> contactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          var ex = await Record.ExceptionAsync(() => contactHelperService.CheckAssignableSiteContactPointsExistenceAsync(organisationId, siteId, contactPointIds));
          Assert.Null(ex);
        });
      }
    }

    public class CheckAssignableUserContactPointsExistence
    {
      public static IEnumerable<object[]> InCorrectSiteData =>
            new List<object[]>
            {
                new object[]
                {
                  "1", "invaliduser", new List<int> { 1, 2 },
                },
                new object[]
                {
                  "2", "invaliduser", new List<int> { 1, 2 },
                },
                new object[]
                {
                  "3", "invaliduser", new List<int> { 1, 2 },
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectSiteData))]
      public async Task ThrowsResourceNotFoundExceptionWhenUserNotFound(string organisationId, string userName, List<int> contactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactHelperService.CheckAssignableUserContactPointsExistenceAsync(organisationId, userName, contactPointIds));
        });
      }

      public static IEnumerable<object[]> IncorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1", "user1@mail.com", new List<int> { 12 }, ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1", "user1@mail.com", new List<int> { 9, 10, 12 }, ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1", "user1@mail.com", new List<int> { 9, 10, 4, 53 }, ErrorConstant.ErrorInvalidAssigningContactIds
                },
            };

      [Theory]
      [MemberData(nameof(IncorrectContactData))]
      public async Task ThrowsExceptionWhenIncorrectContactData(string organisationId, string userName, List<int> contactPointIds, string expectedErrorMsg)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactHelperService.CheckAssignableUserContactPointsExistenceAsync(organisationId, userName, contactPointIds));
          Assert.Equal(expectedErrorMsg, ex.Message);
        });
      }

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1", "user1@mail.com", new List<int> { 9, 10, 14 }
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task DoesNotThrowExceptionWhenCorrectContactData(string organisationId, string userName, List<int> contactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          var ex = await Record.ExceptionAsync(() => contactHelperService.CheckAssignableUserContactPointsExistenceAsync(organisationId, userName, contactPointIds));
          Assert.Null(ex);
        });
      }
    }

    public class ValidateContactAssignment
    {
      public static IEnumerable<object[]> IncorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                  new List<AssignedContactType> { AssignedContactType.User },
                  ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = null,
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                  new List<AssignedContactType> { AssignedContactType.User },
                  ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = 2,
                  },
                  new List<AssignedContactType> { AssignedContactType.User },
                  ErrorConstant.ErrorInvalidContactAssignmentType
                },
                new object[] // No valid site for site contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = 0,
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidSiteIdForContactAssignment
                },
                new object[] // No valid site for site contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidSiteIdForContactAssignment
                },
                new object[] // No valid site for site contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = null,
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidSiteIdForContactAssignment
                },

                 new object[] // No valid user for user contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "",
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidUserIdForContactAssignment
                },
                new object[] // No valid user for user contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = " "
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidUserIdForContactAssignment
                },
                new object[] // No valid user for user contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = null
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidUserIdForContactAssignment
                },
                new object[] // No valid user for user contact assignment
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsSiteId = 2,
                  },
                  new List<AssignedContactType> { AssignedContactType.User, AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidUserIdForContactAssignment
                },
                new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 13 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                  new List<AssignedContactType> { AssignedContactType.User },
                  ErrorConstant.ErrorInvalidAssigningContactIds
                },
                new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 4 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = 2
                  },
                  new List<AssignedContactType> { AssignedContactType.Site },
                  ErrorConstant.ErrorInvalidAssigningContactIds
                },
            };

      [Theory]
      [MemberData(nameof(IncorrectContactData))]
      public async Task ThrowsExceptionWhenIncorrectContactData(string organisationId, ContactAssignmentInfo contactAssignmentInfo,
        List<AssignedContactType> allowedContactTypes, string expectedErrorMsg)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactHelperService.ValidateContactAssignmentAsync(organisationId, contactAssignmentInfo, allowedContactTypes));
          Assert.Equal(expectedErrorMsg, ex.Message);
        });
      }

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                 new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9, 10 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                  new List<AssignedContactType> { AssignedContactType.User }
                },
                new object[]
                {
                  "1",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 1 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = 2
                  },
                  new List<AssignedContactType> { AssignedContactType.Site }
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task DoesNotThrowExceptionWhenCorrectContactData(string organisationId, ContactAssignmentInfo contactAssignmentInfo,
        List<AssignedContactType> allowedContactTypes)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          var ex = await Record.ExceptionAsync(() => contactHelperService.ValidateContactAssignmentAsync(organisationId, contactAssignmentInfo, allowedContactTypes));
          Assert.Null(ex);
        });
      }
    }

    public class DeleteAssignedContacts
    {
      [Theory]
      [InlineData(14)]
      public async Task DeleteAssignedContactsWhenExists(int contactPointId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataForContactAssignmentAsync(dataContext);
          var contactHelperService = GetContactHelperService(dataContext);
          await contactHelperService.DeleteAssignedContactsAsync(contactPointId);

          var assignedContactPoints = await dataContext.ContactPoint.Where(cp => cp.OriginalContactPointId == contactPointId).ToListAsync();
          var assignedSiteContacts = await dataContext.SiteContact.Where(cp => cp.OriginalContactId == contactPointId).ToListAsync();

          Assert.NotEmpty(assignedContactPoints);
          Assert.NotEmpty(assignedSiteContacts);

          assignedContactPoints.ForEach((contact) => Assert.True(contact.IsDeleted));
          assignedSiteContacts.ForEach((contact) => Assert.True(contact.IsDeleted));
        });
      }
    }

    public static ContactsHelperService GetContactHelperService(IDataContext dataContext)
    {
      Mock<ILocalCacheService> mockLocalCacheService = new();
      mockLocalCacheService.Setup(s => s.GetOrSetValueAsync<List<ContactPointReason>>("CONTACT_POINT_REASONS", It.IsAny<Func<Task<List<ContactPointReason>>>>(), It.IsAny<int>()))
        .ReturnsAsync(new List<ContactPointReason> {
          new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" },
          new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" },
          new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" },
          new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" },
          new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" }
        });
      var service = new ContactsHelperService(dataContext, mockLocalCacheService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax, Description = "fax" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url, Description = "url" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 5, Name = VirtualContactTypeName.Mobile, Description = "mobile" });

      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Site, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });

      await dataContext.SaveChangesAsync();
    }

    public static async Task SetupTestDataForContactAssignmentAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = PartyTypeName.InternalOrgnaisation });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
      dataContext.PartyType.Add(new PartyType { Id = 4, PartyTypeName = PartyTypeName.ExternalOrgnaisation });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax, Description = "fax" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url, Description = "url" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 5, Name = VirtualContactTypeName.Mobile, Description = "mobile" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      //Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", LegalName = "Org1", OrganisationUri = "Org1Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });
      //Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 1, ContactDetailId = 1, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 1 });
      //Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 2, ContactDetailId = 2, StreetAddress = "streetsite1", Locality = "localitysite1", Region = "regionsite1", PostalCode = "postalcodesite1", CountryCode = "countrycodesite1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 1, PartyTypeId = 1, IsSite = true, SiteName = "Org1Site1", ContactPointReasonId = 2, ContactDetailId = 2 });
      dataContext.SiteContact.Add(new SiteContact { Id = 1, ContactPointId = 2, ContactId = 5 });
      dataContext.SiteContact.Add(new SiteContact { Id = 2, ContactPointId = 2, ContactId = 6 });

      #region Org 2
      // Org2 No user contacts no site contacts
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true, IsDeleted = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 2, IdentityProviderId = 1 });
      // Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 3, ContactDetailId = 3, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 3 });
      // Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 4, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 4, ContactDetailId = 4, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 2, PartyTypeId = 1, IsSite = true, SiteName = "Org2Site1", IsDeleted = true, ContactPointReasonId = 1, ContactDetailId = 4 });
      #endregion

      // Org1 site 1 contact
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "Site1C1FN", LastName = "Site1C1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 5, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 5, VirtualAddressTypeId = 1, VirtualAddressValue = "site1c1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 5, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 5, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 5 });

      // Org1 site 1 contact2
      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "Site1C2FN", LastName = "Site1C2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 6, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 6, VirtualAddressTypeId = 1, VirtualAddressValue = "site1c2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 6, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 6, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 6 });

      // Org1 user 1
      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "User1FN", LastName = "User1LN" });
      dataContext.User.Add(new User { Id = 1, PartyId = 5, UserName = "user1@mail.com" });

      // Org1 user 1 contact1
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 4, PartyId = 6, OrganisationId = 1, FirstName = "User1C1FN", LastName = "User1C1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 7, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 5, ContactDetailId = 7, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 7, PartyId = 6, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 7 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 9, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 7 }); // User contact

      // Org1 user 1 contact2
      dataContext.Party.Add(new Party { Id = 7, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 5, PartyId = 7, OrganisationId = 1, FirstName = "User1C2FN", LastName = "User1C2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 8, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 6, ContactDetailId = 8, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 8, PartyId = 7, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 8 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 10, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 8 }); // User contact

      // Org1 user 1 contact3 deleted
      dataContext.Party.Add(new Party { Id = 8, PartyTypeId = 2, IsDeleted = true });
      dataContext.Person.Add(new Person { Id = 6, PartyId = 8, OrganisationId = 1, FirstName = "User1C3FN", LastName = "User1C3LN", IsDeleted = true });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 9, EffectiveFrom = DateTime.UtcNow, IsDeleted = true });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 7, ContactDetailId = 9, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c3@mail.com", IsDeleted = true });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 11, PartyId = 8, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 9, IsDeleted = true });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 12, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 9, IsDeleted = true }); // User contact

      // Org1 user 1 contact4 assigned to site1 and org1
      dataContext.Party.Add(new Party { Id = 9, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 7, PartyId = 9, OrganisationId = 1, FirstName = "User1C4S1FN", LastName = "User1C4S1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 10, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 8, ContactDetailId = 10, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c4s1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 13, PartyId = 9, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 10 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 14, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 10 }); // User contact
      dataContext.SiteContact.Add(new SiteContact { Id = 3, ContactPointId = 2, ContactId = 14, OriginalContactId = 14, AssignedContactType = AssignedContactType.User }); // Assigned to site 1
      dataContext.ContactPoint.Add(new ContactPoint { Id = 16, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 2, ContactDetailId = 10, OriginalContactPointId = 14, AssignedContactType = AssignedContactType.User }); // Assigned to org1

      // Org1 site 1 contact4 deleted (since site1 contact3 is assigned from user1 contact4)
      dataContext.Party.Add(new Party { Id = 10, PartyTypeId = 2, IsDeleted = true });
      dataContext.Person.Add(new Person { Id = 8, PartyId = 10, OrganisationId = 1, FirstName = "Site1C3FN", LastName = "Site1C3LN", IsDeleted = true });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 11, EffectiveFrom = DateTime.UtcNow, IsDeleted = true });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 9, ContactDetailId = 11, VirtualAddressTypeId = 1, VirtualAddressValue = "site1c3@mail.com", IsDeleted = true });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 15, PartyId = 10, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 11, IsDeleted = true });
      dataContext.SiteContact.Add(new SiteContact { Id = 4, ContactPointId = 2, ContactId = 15, IsDeleted = true }); // Site 1 Contact 4

      await dataContext.SaveChangesAsync();
    }
  }
}
