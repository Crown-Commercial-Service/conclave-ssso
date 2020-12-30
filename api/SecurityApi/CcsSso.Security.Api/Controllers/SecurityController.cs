using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Security.Api.Controllers
{
  [Route("[controller]")]
  [ApiController]
  public class SecurityController : ControllerBase
  {

    private readonly ISecurityService _securityService;
    private readonly IUserManagerService _userManagerService;

    public SecurityController(ISecurityService securityService, IUserManagerService userManagerService)
    {
      _securityService = securityService;
      _userManagerService = userManagerService;
    }

    [HttpPost]
    [Route("login")]
    public async Task<AuthResultDto> Login([FromBody] AuthRequestDto authRequestDto)
    {
      var authResponse = await _securityService.LoginAsync(authRequestDto.UserName, authRequestDto.UserPassword);
      return authResponse;
    }

    [HttpGet("authorize")]
    public AuthResultDto Authorize(string authToken)
    {
      return new AuthResultDto()
      {
        AccessToken = "123ABC1233wqwqwq",
        RefreshToken = "1230odpopopspospos"
      };
    }

    [HttpPost("token")]
    public async Task<string> Token(string refreshToken)
    {
      var idToken = await _securityService.GetRenewedTokenAsync(refreshToken);
      return idToken;
    }

    [HttpPost("register")]
    public async Task<UserRegisterResult> Register(UserInfo userInfo)
    {
      var userRegisterResult = await _userManagerService.CreateUserAsync(userInfo);
      return userRegisterResult;
    }

    [HttpGet("externalidp")]
    public async Task<List<IdentityProviderInfoDto>> GetIdentityProvidersList()
    {
      var idProviders = await _securityService.GetIdentityProvidersListAsync();
      return idProviders;
    }

    [HttpPost("updateUser")]
    public async Task UpdateUser(UserInfo userInfo)
    {
      await _userManagerService.UpdateUserAsync(userInfo);
    }

    [HttpPost("changepassword")]
    public async Task ChangePassword(ChangePasswordDto changePasswordDto)
    {
      await _securityService.ChangePasswordAsync(changePasswordDto);
    }

    [HttpPost("initiateresetpassword")]
    public async Task InitiateResetPassword(string userName)
    {
      await _securityService.InitiateResetPasswordAsync(userName);
    }

    [HttpPost("resetpassword")]
    public async Task ResetPassword(ResetPasswordDto resetPassword)
    {
      await _securityService.ResetPasswordAsync(resetPassword);
    }

    [HttpPost("logout")]
    public async Task LogOut(string userName)
    {
      await _securityService.LogoutAsync(userName);
    }
  }
}
