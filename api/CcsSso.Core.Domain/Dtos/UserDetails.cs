using CcsSso.Dtos.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Domain.Dtos
{

  public class UserDto
  {
    public int Id { get; set; }

    public string UserName { get; set; }

    public int Title { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string JobTitle { get; set; }

    public int PartyId { get; set; }

    public int OrganisationId { get; set; }
  }

  public class UserDetails
  {
    public int Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string UserName { get; set; }

    public List<UserGroup> UserGroups { get; set; }
  }

  public class UserGroup
  {
    public string Group { get; set; }

    public string Role { get; set; }
  }

  public class ServicePermissionDto
  {
    public string PermissionName { get; set; }
    public string RoleName { get; set; }
    public string RoleKey { get; set; }

    public override bool Equals(object obj)
    {
      if ((obj == null) || !this.GetType().Equals(obj.GetType()))
      {
        return false;
      }
      ServicePermissionDto servicePermissionDto = (ServicePermissionDto)obj;
      return base.Equals(PermissionName == servicePermissionDto.PermissionName);
    }

    public override int GetHashCode()
    {
      return PermissionName.GetHashCode();
    }
  }

  public class SecurityApiUserInfo
  {
    public string Id { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public bool SendUserRegistrationEmail { get; set; }

    public string Email { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public bool MfaEnabled { get; set; }
  }

  public class UserRolePermissionInfo
  {
    public string RoleName { get; set; }
    public string RoleKey { get; set; }

    public List<string> PermissionList { get; set; }
  }
}
