using CcsSso.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts.External
{
  public interface IUserContactService
  {
    Task<int> CreateUserContactAsync(string userName, ContactInfo contactInfo);

    Task DeleteUserContactAsync(string userName, int contactId);

    Task<UserContactInfo> GetUserContactAsync(string userName, int contactId);

    Task<UserContactInfoList> GetUserContactsListAsync(string userName, string contactType = null);

    Task UpdateUserContactAsync(string userName, int contactId, ContactInfo contactInfo);
  }
}
