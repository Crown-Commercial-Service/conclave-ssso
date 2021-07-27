using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Service;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class ContactExternalServiceTest
  {
    public class Create
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, "user@email.com")
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, "+541231231")
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, "+541231231")
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Url, "web2.com")
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateContactSuccessfully_WhenCorrectData(ContactRequestDetail contactRequestDetail)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.CreateAsync(contactRequestDetail);
          Assert.NotEqual(0, result);

          var createdContactData = await dataContext.VirtualAddress.Where(c => c.Id == result)
            .Include(va => va.VirtualAddressType)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);

          Assert.Equal(contactRequestDetail.ContactType, createdContactData.VirtualAddressType.Name);
          Assert.Equal(contactRequestDetail.ContactValue, createdContactData.VirtualAddressValue);
        });
      }

      public static IEnumerable<object[]> InCorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactRequestDetail("WrongType", "wrongemailcom"),
                    ErrorConstant.ErrorInvalidContactType
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, " "),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, null),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, " "),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, null),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, " "),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, null),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, "wrongemail.com"),
                    ErrorConstant.ErrorInvalidEmail
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, "1231"),
                    ErrorConstant.ErrorInvalidPhoneNumber
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, "1231"),
                    ErrorConstant.ErrorInvalidFaxNumber
                },
                new object[]
                {
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Mobile, "1231"),
                    ErrorConstant.ErrorInvalidMobileNumber
                }
            };

      [Theory]
      [MemberData(nameof(InCorrectContactData))]
      public async Task ThrowsException_WhenInCorrectData(ContactRequestDetail contactRequestDetail, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.CreateAsync(contactRequestDetail));

          Assert.Equal(expectedError, ex.Message);
        });
      }
    }

    public class Delete
    {
      [Theory]
      [InlineData(1)]
      public async Task DeleteContactSuccessfully(int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteAsync(deletingContactId);

          var deletedContact = await dataContext.VirtualAddress.FirstOrDefaultAsync(c => c.Id == deletingContactId);

          Assert.Null(deletedContact);
        });
      }

      [Theory]
      [InlineData(10)]
      public async Task ThrowsNotFoundException_WhenNoContact(int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.DeleteAsync(deletingContactId));
        });
      }
    }

    public class Get
    {
      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  1,
                  DtoHelper.GetContactResponseDetail(1, VirtualContactTypeName.Email, "email1@mail.com")
                },
                new object[]
                {
                  2,
                  DtoHelper.GetContactResponseDetail(2, VirtualContactTypeName.Phone, "+94112345671")
                },
                new object[]
                {
                  4,
                  DtoHelper.GetContactResponseDetail(4, VirtualContactTypeName.Phone, "+94112345672")
                },
                new object[]
                {
                  5,
                  DtoHelper.GetContactResponseDetail(5, VirtualContactTypeName.Fax, "+94112345673")
                },
                new object[]
                {
                  6,
                  DtoHelper.GetContactResponseDetail(6, VirtualContactTypeName.Url, "url.com")
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsContact_WhenExsists(int id, ContactResponseDetail expectedContact)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetAsync(id);

          Assert.NotNull(result);
          Assert.Equal(expectedContact.ContactType, result.ContactType);
          Assert.Equal(expectedContact.ContactValue, result.ContactValue);
        });
      }

      [Theory]
      [InlineData(10)]
      public async Task ThrowsNotFoundException_WhenNoContact(int id)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetAsync(id));
        });
      }
    }

    public class Update
    {

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  1,
                  DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, "user@email.com")
                },
                new object[]
                {
                  1,
                  DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, "+551234121")
                },
                new object[]
                {
                  2,
                  DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, "+942342345")
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactSuccessfully_WhenCorrectData(int id, ContactRequestDetail contactRequestDetail)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.UpdateAsync(id, contactRequestDetail);

          var updatedContact = await dataContext.VirtualAddress.Where(c => c.Id == id)
           .Include(va => va.VirtualAddressType)
           .FirstOrDefaultAsync();

          Assert.NotNull(updatedContact);

          Assert.Equal(contactRequestDetail.ContactType, updatedContact.VirtualAddressType.Name);
          Assert.Equal(contactRequestDetail.ContactValue, updatedContact.VirtualAddressValue);

        });
      }

      public static IEnumerable<object[]> InCorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail("WrongType", "wrongemailcom"),
                    ErrorConstant.ErrorInvalidContactType
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, " "),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, null),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, " "),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, null),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, " "),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, null),
                    ErrorConstant.ErrorInvalidContactValue
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, "wrongemail.com"),
                    ErrorConstant.ErrorInvalidEmail
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Phone, "1231"),
                    ErrorConstant.ErrorInvalidPhoneNumber
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Fax, "1231"),
                    ErrorConstant.ErrorInvalidFaxNumber
                },
                new object[]
                {
                    1,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Mobile, "1231"),
                    ErrorConstant.ErrorInvalidMobileNumber
                }
            };

      [Theory]
      [MemberData(nameof(InCorrectContactData))]
      public async Task ThrowsException_WhenInCorrectData(int id, ContactRequestDetail contactRequestDetail, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.UpdateAsync(id, contactRequestDetail));

          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> NotExistingContactData =>
            new List<object[]>
            {
                new object[]
                {
                    10,
                    DtoHelper.GetContactRequestDetail(VirtualContactTypeName.Email, "user@email.com")
                },
            };

      [Theory]
      [MemberData(nameof(NotExistingContactData))]
      public async Task ThrowsNotFoundException_WhenNoContact(int id, ContactRequestDetail contactRequestDetail)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateAsync(id, contactRequestDetail));
        });
      }
    }

    public static ContactExternalService ContactService(IDataContext dataContext)
    {
      var mockAdaptorNotificationService = new Mock<IAdaptorNotificationService>();
      var mockWrapperCacheService = new Mock<IWrapperCacheService>();

      var service = new ContactExternalService(dataContext, mockAdaptorNotificationService.Object, mockWrapperCacheService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax, Description = "fax" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url, Description = "url" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 5, Name = VirtualContactTypeName.Mobile, Description = "mobile" });

      dataContext.ContactDetail.Add(new ContactDetail { Id = 1 });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2 });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3 });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 4 });

      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 1, VirtualAddressTypeId = 1, VirtualAddressValue = "email1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 1, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345671" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345672" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 5, ContactDetailId = 3, VirtualAddressTypeId = 3, VirtualAddressValue = "+94112345673" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 6, ContactDetailId = 4, VirtualAddressTypeId = 4, VirtualAddressValue = "url.com" });

      await dataContext.SaveChangesAsync();
    }
  }
}
