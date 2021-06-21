using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Api.Controllers
{
  [Route("user")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
      _userService = userService;
    }

    /// <summary>
    /// Get user details
    /// </summary>
    /// <response  code="200">Successfully returns user</response>
    /// <response  code="404">Requested user is not found</response>
    /// <remarks>
    /// Sample request:
    /// User/123
    /// </remarks>
    [HttpGet("{id}")]
    [Produces("application/json")]
    [SwaggerOperation(Tags = new[] { "user" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<UserDetails> Get(string id)
    {
      return await _userService.GetAsync(id);
    }

    /// <summary>
    /// Get user basic details
    /// </summary>
    /// <response  code="200">Successfully returns user</response>
    /// <response  code="404">Requested user is not found</response>
    /// <remarks>
    /// Sample request:
    /// User/123
    /// </remarks>
    [HttpGet("GetUser")]
    [Produces("application/json")]
    [SwaggerOperation(Tags = new[] { "user" })]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<UserDetails> GetUser(string userName)
    {
      //return new UserDetails()
      //{
      //  FirstName = "Hiran",
      //  LastName = "Amarasinghe",
      //  UserName = "hiran@geveo.com",
      //  UserGroups = new List<UserGroup>()
      //  {
      //    new UserGroup() {Group = "123",Role = "123"}
      //  }
      //};
      return await _userService.GetAsync(userName);
    }

    /// <summary>
    /// Method to add a user
    /// </summary>
    /// <response  code="200">User Id</response>
    /// <response  code="400">Bad Request</response>
    /// <remarks>
    /// Sample request:
    /// POST /user
    /// {
    ///    "firstName": "John",
    ///    "lastName": "Doe",
    ///    "userName": "email@gmail.com",
    ///    "jobTitle": "MR",
    ///    "organisationId": 1,
    ///    "partyId": 1,
    /// }
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Tags = new[] { "user" })]
    // [ProducesResponseType(typeof(string), 200)]
    // [ProducesResponseType(401)]
    // [ProducesResponseType(typeof(string), 400)]
    public async Task<string> Post(UserDto model)
    {
      CookieOptions httpCookieOptions = new CookieOptions()
      {
        HttpOnly = true,
        SameSite = SameSiteMode.None,
        Secure = true
      };
      string userDetailsCookie = "ud";
      Response.Cookies.Delete(userDetailsCookie);
      //"as" stands for activation email sent
      Response.Cookies.Append(userDetailsCookie, "as", httpCookieOptions);
      return await _userService.CreateAsync(model);
    }

    [HttpGet("GetPermissions")]
    [SwaggerOperation(Tags = new[] { "user" })]
    public async Task<List<ServicePermissionDto>> GetPermissions(string userName, string serviceClientId)
    {
      return await _userService.GetPermissions(userName, serviceClientId);
    }

    [HttpPost("useractivationemail")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task SendUserActivationEmail(IFormCollection userDetails)
    {
      string userDetailsCookie = "ud";
      if (Request.Cookies.ContainsKey(userDetailsCookie))
      {
        Request.Cookies.TryGetValue(userDetailsCookie, out string details);
        if (details == "as")
        {
          userDetails.TryGetValue("email", out StringValues email);
          await _userService.SendUserActivationEmailAsync(email);
          CookieOptions httpCookieOptions = new CookieOptions()
          {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true
          };
          //"ras" stands for activation email re-sent
          Response.Cookies.Append(userDetailsCookie, "ras", httpCookieOptions);
        }
      }
    }
  }
}
