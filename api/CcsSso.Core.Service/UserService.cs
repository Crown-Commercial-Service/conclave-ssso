using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Service
{
  public class UserService : IUserService
  {
    private readonly IDataContext _dataContext;
    public UserService(IDataContext dataContext)
    {
      _dataContext = dataContext;
    }

    public async Task<UserDetails> GetAsync(int id)
    {
      var user = await _dataContext.User.FirstOrDefaultAsync(u => u.Id == id);
      if (user != null)
      {
        UserDetails userProfileDetails = new UserDetails()
        {
          Id = user.Id,
          // FirstName = "",
          // LastName = ""
          UserName = user.UserName,
        };
        return userProfileDetails;
      }
      throw new ResourceNotFoundException();
    }

    public async Task<UserDetails> GetAsync(string userName)
    {
      var user = await _dataContext.User.Include(u => u.UserGroupMemberships).ThenInclude(c => c.OrganisationUserGroup).FirstOrDefaultAsync(u => u.UserName.ToLower() == userName.ToLower());
      if (user != null)
      {
        UserDetails userProfileDetails = new UserDetails()
        {
          Id = user.Id,
          // FirstName = "",
          // LastName = "",
          UserName = user.UserName,
          UserGroups = user.UserGroupMemberships?.Select(ug => new UserGroup()
          {
            Role = user.JobTitle, // This is a temporary implementation
            Group = ug.OrganisationUserGroup?.UserGroupName
          }).ToList()
        };
        return userProfileDetails;
      }
      throw new ResourceNotFoundException();
    }

    /// <summary>
    /// Creates a user
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> CreateAsync(UserDto model)
    {
      var identifyProvider = _dataContext.IdentityProvider.FirstOrDefault(x => x.IdpConnectionName == "Username-Password-Authentication");
      var orgGroup = _dataContext.OrganisationUserGroup.FirstOrDefault(x => x.UserGroupNameKey == "ORG_ADMINISTRATOR_GROUP" && x.OrganisationId == model.OrganisationId);
      //var userExists = await _dataContext.User.SingleOrDefaultAsync(t => t.UserName == model.UserName);
      //if (userExists != null)
      //{
      //  throw new System.Exception("UserName already exists");
      //}
      // var identityProviderId = (await _dataContext.IdentityProvider.SingleOrDefaultAsync(t => t.IdpName == "AWS Cognito")).Id;
      var partyType = await _dataContext.PartyType.SingleOrDefaultAsync(t => t.PartyTypeName == "USER");
      var party = new CcsSso.DbModel.Entity.Party
      {
        PartyTypeId = partyType.Id,
        CreatedPartyId = 0,
        LastUpdatedPartyId = 0,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
        //ContactPoints = new System.Collections.Generic.List<CcsSso.DbModel.Entity.ContactPoint> {
        //  new CcsSso.DbModel.Entity.ContactPoint {
        //    ContactDetail = new CcsSso.DbModel.Entity.ContactDetail
        //    {
        //      EffectiveFrom = System.DateTime.UtcNow
        //    }
        //  }
        //}
      };
      _dataContext.Party.Add(party);
      await _dataContext.SaveChangesAsync();
      // return party.Id.ToString();
      var person = new CcsSso.DbModel.Entity.Person
      {
        Title = 1,
        FirstName = model.FirstName,
        LastName = model.LastName,
        OrganisationId = model.OrganisationId,// (model.OrganisationId.HasValue ? model.OrganisationId.Value : 0),
        PartyId = party.Id,
        CreatedPartyId = party.Id,
        LastUpdatedPartyId = party.Id,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.Person.Add(person);
      await _dataContext.SaveChangesAsync();
      var user = new CcsSso.DbModel.Entity.User
      {
        JobTitle = model.JobTitle,
        UserTitle = 1,
        UserName = model.UserName,
        IdentityProviderId = identifyProvider.Id,
        PartyId = party.Id,
        // PersonId = person.Id,
        CreatedPartyId = party.Id,
        LastUpdatedPartyId = party.Id,
        CreatedOnUtc = System.DateTime.UtcNow,
        LastUpdatedOnUtc = System.DateTime.UtcNow,
        IsDeleted = false,
      };
      _dataContext.User.Add(user);
      var userGroupMembership = new CcsSso.DbModel.Entity.UserGroupMembership {
        User = user,
        OrganisationUserGroup = orgGroup
      };
      _dataContext.UserGroupMembership.Add(userGroupMembership);
      await _dataContext.SaveChangesAsync();
      return user.Id.ToString();
      //var server = new CcsSso.DbModel.Entity.IdentityProvider
      //{
      //  IdpName = "name goes here",
      //  IdpUri = "google",
      //  CreatedPartyId = 1,
      //  LastUpdatedPartyId = 1,
      //  CreatedOnUtc = System.DateTime.UtcNow,
      //  LastUpdatedOnUtc = System.DateTime.UtcNow,
      //  IsDeleted = false,
      //};
      //_dataContext.IdentityProvider.Add(server);
      //await _dataContext.SaveChangesAsync();
      //return server.Id.ToString();
    }

    public async Task<List<ServicePermissionDto>> GetPermissions(string token)
    {
      try
      {
        var entities = await _dataContext.ServiceRolePermission
          .Where(srp => srp.ServicePermission.CcsServiceId == 1)
          .Include(srp => srp.CcsAccessRole)
          .Select(srp => new ServicePermissionDto
          {
            PermissionName = srp.ServicePermission.ServicePermissionName,
            RoleName = srp.CcsAccessRole.CcsAccessRoleName,
            RoleKey = srp.CcsAccessRole.CcsAccessRoleNameKey
          })
          .Distinct()
         .ToListAsync();

        return entities;
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        throw;
      }
    }
  }
}
