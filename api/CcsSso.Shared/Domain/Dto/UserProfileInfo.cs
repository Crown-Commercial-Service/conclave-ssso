using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Domain.Dto
{
     
    public class UserProfileResponseInfo
    {
        public UserDetails detail { get; set; }
        public string UserName { get; set; }
        public string OrganisationId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public bool mfaEnabled { get; set; }
        public string Password { get; set; }
        public bool AccountVerified { get; set; }
        public bool SendUserRegistrationEmail { get; set; }
        public bool IsAdminUser { get; set; }

    }

    public class UserDetails
    {
        public int Id { get; set; }
        public List<GroupAccessRole> userGroups { get; set; }
        public bool CanChangePassword { get; set; }
        
        public List<RolePermissionInfo> rolePermissionInfo { get; set; }
        public List<IdentityProviders> identityProviders { get; set; }
        
    }

    public class GroupAccessRole
    {
        public int GroupId { get; set; }

        public string AccessRole { get; set; }

        public string AccessRoleName { get; set; }

        public string Group { get; set; }

        public string ServiceClientName { get; set; }
    }

    public class RolePermissionInfo
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleKey { get; set; }
        public string ServiceClientId { get; set; }
        public string ServiceClientName { get; set; }
    }
        
    public class IdentityProviders
    {
        public int IdentityProviderId { get; set; }
        public string IdentityProvider { get; set; }
        public string IdentityProviderDisplayName { get; set; }

    }
}
