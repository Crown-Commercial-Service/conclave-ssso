using CcsSso.Domain.Dtos;
using CcsSso.Dtos.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Domain.Contracts
{
  public interface IContactService
  {
    Task<int> CreateAsync(ContactDetailDto contactDetailModel);

    Task DeleteAsync(int contactId);

    Task<List<ContactDetailDto>> GetAsync(ContactRequestFilter contactRequestFilter);

    Task<ContactDetailDto> GetAsync(int contactId);

    Task<int> UpdateAsync(int contactId, ContactDetailDto contactDetailDto);
  }
}
