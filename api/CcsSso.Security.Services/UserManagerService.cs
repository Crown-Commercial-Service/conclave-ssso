using CcsSso.Security.Domain.Constants;
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
      if (string.IsNullOrWhiteSpace(userInfo.Id))
      {
        throw new CcsSsoException(Constants.ErrorCodes.UserIdRequired);
      }
      await _identityProviderService.UpdateUserAsync(userInfo);
    }

    private void ValidateUser(UserInfo userInfo)
    {
      if (string.IsNullOrWhiteSpace(userInfo.FirstName))
      {
        throw new CcsSsoException(Constants.ErrorCodes.FirstNameRequired);
      }

      if (string.IsNullOrWhiteSpace(userInfo.LastName))
      {
        throw new CcsSsoException(Constants.ErrorCodes.LastNameRequired);
      }

      if (string.IsNullOrWhiteSpace(userInfo.Email))
      {
        throw new CcsSsoException(Constants.ErrorCodes.EmailRequired);
      }

      if (!UtilitiesHelper.IsEmailValid(userInfo.Email))
      {
        throw new CcsSsoException(Constants.ErrorCodes.EmailFormatError);
      }
    }

    public async Task<UserClaims> GetUserAsync(string accessToken)
    {
      return await _identityProviderService.GetUserAsync(accessToken);
    }

    public async Task DeleteUserAsync(string email)
    {
      await _identityProviderService.DeleteAsync(email);
    }
  }
}
