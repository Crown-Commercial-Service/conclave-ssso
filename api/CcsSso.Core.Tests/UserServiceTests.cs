using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using CcsSso.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Tests
{
  public class UserServiceTests
  {
    public class Get
    {
      [Fact]
      public async Task ReturnsNoFoundException_WhenProvideInValidId()
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = GetUserService(dataContext);
          await Assert.ThrowsAnyAsync<ResourceNotFoundException>(async () => await userService.GetAsync(100));
        });
      }


      [Fact]
      public async Task ReturnsUser_WhenProvidesValidId()
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = GetUserService(dataContext);
          var userDetails = await userService.GetAsync("ravi@gmail.com");
          Assert.NotNull(userDetails);
          Assert.Single(userDetails.UserGroups);
          var userGroup = userDetails.UserGroups.FirstOrDefault();
          Assert.Equal("ADMIN", userGroup.Group);
          Assert.Equal("SE", userGroup.Role);
          Assert.Equal("ravi", userDetails.FirstName);
          Assert.Equal("prasad", userDetails.LastName);
          Assert.NotNull(userDetails.UserGroups);
        });
      }

      [Fact]
      public async Task ReturnsNoFoundException_WhenProvideInUserName()
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = GetUserService(dataContext);
          await Assert.ThrowsAnyAsync<ResourceNotFoundException>(async () => await userService.GetAsync("ravi123@gmail.com"));
        });
      }


      [Fact]
      public async Task ReturnsUser_WhenProvidesValidUserName()
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = GetUserService(dataContext);
          var userDetails = await userService.GetAsync(1);
          Assert.NotNull(userDetails);
          Assert.Equal("ravi", userDetails.FirstName);
          Assert.Equal("prasad", userDetails.LastName);
        });
      }
    }

    static IUserService GetUserService(IDataContext dataContext)
    {
      return new UserService(dataContext);
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = "INTERNAL_ORGANISATION" });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = "NON_USER" });

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });

      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 2, OrganisationUri = "Org1Uri", RightToBuy = true });

      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { UserGroupName = "ADMIN", Id = 1, OrganisationId = 1 });

      dataContext.User.Add(new User
      {
        Id = 1,
        // IdpId = 1,
        PartyId = 1,
        JobTitle = "SE",
        UserName = "ravi@gmail.com",
        // FirstName = "ravi",
        // SurName = "prasad"
      });

      dataContext.IdentityProvider.Add(new IdentityProvider() { Id = 1, IdpName = "AWS" });
      dataContext.UserSettingType.Add(new UserSettingType() { Id = 1, UserSettingName = "us1" });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 1, UserId = 1, OrganisationUserGroupId = 1, MembershipStartDate = new DateTime() });
      await dataContext.SaveChangesAsync();
    }

    static List<User> GetUsers()
    {
      return new List<User>()
      {
        EntityDataProvider.GetUser(1,"f1","l1"),
        EntityDataProvider.GetUser(2,"f2","l2")
      };
    }
  }
}




