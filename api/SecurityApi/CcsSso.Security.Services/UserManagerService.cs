using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Domain.Exceptions;
using CcsSso.Security.Services.Helpers;
using System.Threading.Tasks;

namespace CcsSso.Security.Services
{
  public class UserManagerService : IUserManagerService
  {
    private readonly IIdentityProviderService _identityProviderService;
    public UserManagerService(IIdentityProviderService identityProviderService)
    {
      _identityProviderService = identityProviderService;
    }

    public async Task<UserRegisterResult> CreateUserAsync(UserInfo userInfo)
    {
      ValidateUser(userInfo);
      return await _identityProviderService.CreateUserAsync(userInfo);
    }

    public async Task UpdateUserAsync(UserInfo userInfo)
    {
      ValidateUser(userInfo);
      await _identityProviderService.UpdateUserAsync(userInfo);
    }

    private void ValidateUser(UserInfo userInfo)
    {
      if (string.IsNullOrWhiteSpace(userInfo.FirstName))
      {
        throw new CcsSsoException("ERROR_FIRSTNAME_REQUIRED");
      }

      if (string.IsNullOrWhiteSpace(userInfo.LastName))
      {
        throw new CcsSsoException("ERROR_LASTNAME_REQUIRED");
      }

      if (string.IsNullOrWhiteSpace(userInfo.Email))
      {
        throw new CcsSsoException("ERROR_EMAIL_REQUIRED");
      }

      if (!UtilitiesHelper.IsEmailValid(userInfo.Email))
      {
        throw new CcsSsoException("ERROR_EMAIL_FORMAT");
      }
    }
  }
}
