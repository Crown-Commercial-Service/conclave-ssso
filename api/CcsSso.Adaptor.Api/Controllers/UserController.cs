using CcsSso.Adaptor.Domain.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.Controllers
{
  [Route("users")]
  [ApiController]
  public class UserController : ControllerBase
  {
    private readonly IUserService _userService;
    public UserController(IUserService userService)
    {
      _userService = userService;
    }

    [HttpGet]
    public async Task<Dictionary<string,object>> GetUser([FromQuery(Name = "user-name")]string userName)
    {
      return await _userService.GetUserAsync(userName);
    }
  }
}
