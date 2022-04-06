using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Domain.Constants;
using CcsSso.Shared.Domain.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Core.Service.External
{
  public class ContactExternalService : IContactExternalService
  {
    private IDataContext _dataContext;
    private IAdaptorNotificationService _adaptorNotificationService;
    private readonly IWrapperCacheService _wrapperCacheService;
    public ContactExternalService(IDataContext dataContext, IAdaptorNotificationService adaptorNotificationService,
      IWrapperCacheService wrapperCacheService)
    {
      _dataContext = dataContext;
      _adaptorNotificationService = adaptorNotificationService;
      _wrapperCacheService = wrapperCacheService;
    }

    public async Task<int> CreateAsync(ContactRequestDetail contactRequestDetail)
    {
      var virtualAddressTypes = await _dataContext.VirtualAddressType.ToListAsync();

      Validate(contactRequestDetail, virtualAddressTypes);

      var virtualAddress = new VirtualAddress
      {
        VirtualAddressTypeId = virtualAddressTypes.First(t => t.Name == contactRequestDetail.ContactType).Id,
        VirtualAddressValue = contactRequestDetail.ContactValue
      };

      var contactDetail = new ContactDetail
      {
        VirtualAddresses = new List<VirtualAddress> { virtualAddress },
        EffectiveFrom = DateTime.UtcNow
      };

      _dataContext.ContactDetail.Add(contactDetail);

      await _dataContext.SaveChangesAsync();

      await _adaptorNotificationService.NotifyContactChangeAsync(OperationType.Create, virtualAddress.Id);

      return virtualAddress.Id;
    }

    public async Task DeleteAsync(int id)
    {
      var deletingContact = await _dataContext.VirtualAddress.FirstOrDefaultAsync(va => va.Id == id && !va.IsDeleted);

      if (deletingContact == null)
      {
        throw new ResourceNotFoundException();
      }

      _dataContext.VirtualAddress.Remove(deletingContact);

      await _dataContext.SaveChangesAsync();

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.Contact}-{id}");

      //Notify adapter
      await _adaptorNotificationService.NotifyContactChangeAsync(OperationType.Delete, id);
    }

    public async Task<ContactResponseDetail> GetAsync(int id)
    {
      var contact = await _dataContext.VirtualAddress
        .Include(va => va.VirtualAddressType)
        .FirstOrDefaultAsync(va => va.Id == id && !va.IsDeleted);

      if (contact == null)
      {
        throw new ResourceNotFoundException();
      }

      ContactResponseDetail contactResponseDetail = new ContactResponseDetail
      {
        ContactId = id,
        ContactType = contact.VirtualAddressType.Name,
        ContactValue = contact.VirtualAddressValue
      };

      return contactResponseDetail;
    }

    public async Task UpdateAsync(int id, ContactRequestDetail contactRequestDetail)
    {
      var virtualAddressTypes = await _dataContext.VirtualAddressType.ToListAsync();

      Validate(contactRequestDetail, virtualAddressTypes);

      var contact = await _dataContext.VirtualAddress
        .Include(va => va.VirtualAddressType)
        .FirstOrDefaultAsync(va => va.Id == id && !va.IsDeleted);

      if (contact == null)
      {
        throw new ResourceNotFoundException();
      }

      contact.VirtualAddressTypeId = virtualAddressTypes.First(t => t.Name == contactRequestDetail.ContactType).Id;
      contact.VirtualAddressValue = contactRequestDetail.ContactValue;

      await _dataContext.SaveChangesAsync();

      //Invalidate redis
      await _wrapperCacheService.RemoveCacheAsync($"{CacheKeyConstant.Contact}-{id}");

      //Notify adapter
      await _adaptorNotificationService.NotifyContactChangeAsync(OperationType.Update, id);
    }

    private void Validate(ContactRequestDetail contactRequestDetail, List<VirtualAddressType> virtualAddressTypes)
    {
      if (string.IsNullOrWhiteSpace(contactRequestDetail.ContactType) || !virtualAddressTypes.Any(t => t.Name == contactRequestDetail.ContactType))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactType);
      }

      if (string.IsNullOrWhiteSpace(contactRequestDetail.ContactValue))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidContactValue);
      }

      if (contactRequestDetail.ContactType == VirtualContactTypeName.Email)
      {
        if (!UtilityHelper.IsEmailFormatValid(contactRequestDetail.ContactValue))
        {
          throw new CcsSsoException(ErrorConstant.ErrorInvalidEmail);

        }
        if (!UtilityHelper.IsEmailLengthValid(contactRequestDetail.ContactValue))
        {
          throw new CcsSsoException(ErrorConstant.ErrorEmailTooLong);
        }
      }

      if (contactRequestDetail.ContactType == VirtualContactTypeName.Phone && !UtilityHelper.IsPhoneNumberValid(contactRequestDetail.ContactValue))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidPhoneNumber);
      }

      if (contactRequestDetail.ContactType == VirtualContactTypeName.Fax && !UtilityHelper.IsPhoneNumberValid(contactRequestDetail.ContactValue))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidFaxNumber);
      }

      if (contactRequestDetail.ContactType == VirtualContactTypeName.Mobile && !UtilityHelper.IsPhoneNumberValid(contactRequestDetail.ContactValue))
      {
        throw new CcsSsoException(ErrorConstant.ErrorInvalidMobileNumber);
      }
    }
  }
}
