using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class UserProfileServiceTest
  {

    public class CreateUser
    {
      public static IEnumerable<object[]> CorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "newuser@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP ", " UserLN1UP", "newuser@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo(" UserFN1UP ", " UserLN1UP ", "newuser@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                }
            };

      [Theory]
      [MemberData(nameof(CorrectUserData))]
      public async Task CreateUser_WhenCorrectInfo(UserProfileRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var resultEmail = await userService.CreateUserAsync(userRequestInfo);

          var createdUser = await dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
          .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == resultEmail);

          Assert.NotNull(createdUser);
          Assert.Equal(userRequestInfo.FirstName.Trim(), createdUser.Party.Person.FirstName);
          Assert.Equal(userRequestInfo.LastName.Trim(), createdUser.Party.Person.LastName);
          Assert.Equal(userRequestInfo.UserName, createdUser.UserName);
          Assert.Equal(userRequestInfo.OrganisationId, createdUser.Party.Person.Organisation.CiiOrganisationId);
          Assert.Equal(userRequestInfo.IdentityProviderId, createdUser.IdentityProviderId);
          userRequestInfo.GroupIds.Sort();
          var userGroupIds = createdUser.UserGroupMemberships.Select(gm => gm.OrganisationUserGroupId).ToList();
          userGroupIds.Sort();
          Assert.Equal(userRequestInfo.GroupIds, userGroupIds);
        });
      }

      public static IEnumerable<object[]> InCorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("", "UserLN1UP", "usernew@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "", "usernew@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo(null, "UserLN1UP", "usernew@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", null, "usernew@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo(" ", "UserLN1UP", "usernew@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", " ", "usernew@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", 0, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", 1, new List<int> {0, 1, 2 }),
                  ErrorConstant.ErrorInvalidUserGroup
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", 1, null),
                  ErrorConstant.ErrorInvalidUserGroup
                }
            };

      [Theory]
      [MemberData(nameof(InCorrectUserData))]
      public async Task ThrowsException_WhenIncorrectData(UserProfileRequestInfo userRequestInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => userService.CreateUserAsync(userRequestInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> NonExistingOrgForUserData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "usernew@mail.com", "WrongOrgId", 1, new List<int> {1, 2 })
                }
            };

      [Theory]
      [MemberData(nameof(NonExistingOrgForUserData))]
      public async Task ThrowsException_WhenOrganisationNotExsists(UserProfileRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => userService.CreateUserAsync(userRequestInfo));

        });
      }

      public static IEnumerable<object[]> AlreadyExistingUserData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                }
            };

      [Theory]
      [MemberData(nameof(AlreadyExistingUserData))]
      public async Task ThrowsException_WhenUserAlreadyExsists(UserProfileRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceAlreadyExistsException>(() => userService.CreateUserAsync(userRequestInfo));

        });
      }
    }

    public class GetUser
    {
      public static IEnumerable<object[]> ExpectedUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileResponseInfo("UserFN1", "UserLN1", "user1@mail.com", "CiiOrg1",
                    new List<GroupAccessRole>
                    {
                      DtoHelper.GetGroupAccessRole("Admin Group", "Administrator"),
                      DtoHelper.GetGroupAccessRole("Engineer Group", "Engineer"),
                    })
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetUserProfileResponseInfo("UserFN2", "UserLN2", "user2@mail.com", "CiiOrg1",
                    new List<GroupAccessRole>
                    {
                      DtoHelper.GetGroupAccessRole("DevOpsEngineer Group", "DevOps"),
                      DtoHelper.GetGroupAccessRole("DevOpsEngineer Group", "Engineer"),
                    })
                },
            };

      [Theory]
      [MemberData(nameof(ExpectedUserData))]
      public async Task ReturnsCorrectUserInfo_WhenUserExsists(string userName, UserProfileResponseInfo expectedUserInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var result = await userService.GetUserAsync(userName);

          Assert.NotNull(result);
          Assert.Equal(expectedUserInfo.FirstName, result.FirstName);
          Assert.Equal(expectedUserInfo.LastName, result.LastName);
          Assert.Equal(expectedUserInfo.UserName, result.UserName);
          Assert.Equal(expectedUserInfo.OrganisationId, result.OrganisationId);
          Assert.Equal(expectedUserInfo.UserGroups.Count, result.UserGroups.Count);

          foreach (var expectedGroupRole in expectedUserInfo.UserGroups)
          {
            Assert.Contains(result.UserGroups, gar => gar.Group == expectedGroupRole.Group && gar.AccessRole == expectedGroupRole.AccessRole);
          }

        });
      }

      [Theory]
      [InlineData("usernotfound@mail.com")]
      [InlineData("user4@mail.com")]
      public async Task ThrowsException_WhenUserNotExsists(string userName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => userService.GetUserAsync(userName));

        });
      }
    }

    public class GetUsers
    {
      public static IEnumerable<object[]> ExpectedUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  null,
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN2 UserLN2", "user2@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN3 UserLN3", "user3@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  " ",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN2 UserLN2", "user2@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN3 UserLN3", "user3@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN2 UserLN2", "user2@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN3 UserLN3", "user3@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize = 2 },
                  "",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 2, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN2 UserLN2", "user2@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =2, PageSize = 2 },
                  "",
                  DtoHelper.GetUserListResponse("CiiOrg1", 2, 2, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN3 UserLN3", "user3@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "user1@mail.com",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 1, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =2 },
                  "user1@mail.com",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 1, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =2 },
                  "user1",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 1, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "user",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN2 UserLN2", "user2@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN3 UserLN3", "user3@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "mail.com",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 3, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN2 UserLN2", "user2@mail.com"),
                    DtoHelper.GetUserListInfo("UserFN3 UserLN3", "user3@mail.com")
                  })
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedUserData))]
      public async Task ReturnsCorrectUserListInfo_WhenExsists(string organisationId, ResultSetCriteria resultSetCriteria,
        string userName, UserListResponse expectedUserInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var result = await userService.GetUsersAsync(organisationId, resultSetCriteria, userName);

          Assert.NotNull(result);
          Assert.Equal(expectedUserInfo.OrganisationId, result.OrganisationId);
          Assert.Equal(resultSetCriteria.CurrentPage, result.CurrentPage);
          Assert.Equal(expectedUserInfo.RowCount, result.RowCount); 
          Assert.Equal(expectedUserInfo.PageCount, result.PageCount);
          Assert.Equal(expectedUserInfo.UserList.Count, result.UserList.Count);

          foreach (var expectedUser in expectedUserInfo.UserList)
          {
            var resultUser = result.UserList.FirstOrDefault(u => u.UserName == expectedUser.UserName);
            Assert.NotNull(resultUser);
            Assert.Equal(expectedUser.Name, resultUser.Name);
            Assert.Equal(expectedUser.UserName, resultUser.UserName);
          }
        });
      }
    }

    public class UpdateUser
    {
      public static IEnumerable<object[]> CorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(" UserFN1UP", "UserLN1UP ", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(" UserFN2UP ", "  UserLN2UP ", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
            };

      [Theory]
      [MemberData(nameof(CorrectUserData))]
      public async Task UpdateUser_WhenCorrectInfo(string userName, UserProfileRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await userService.UpdateUserAsync(userName, true, userRequestInfo);

          var updatedUser = await dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

          Assert.NotNull(updatedUser);
          Assert.Equal(userRequestInfo.FirstName.Trim(), updatedUser.Party.Person.FirstName);
          Assert.Equal(userRequestInfo.LastName.Trim(), updatedUser.Party.Person.LastName);
        });
      }

      public static IEnumerable<object[]> InCorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("", "UserLN1UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(null, "UserLN1UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", null, "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(" ", "UserLN1UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", " ", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectUserData))]
      public async Task ThrowsException_WhenIncorrectData(string userName, UserProfileRequestInfo userRequestInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => userService.UpdateUserAsync(userName, true, userRequestInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> NonExistingUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "usernotfound@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
                new object[]
                {
                  "user4@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user1@mail.com", "CiiOrg1", 1, new List<int> {1, 2 })
                },
            };

      [Theory]
      [MemberData(nameof(NonExistingUserData))]
      public async Task ThrowsException_WhenUserNotExsists(string userName, UserProfileRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => userService.UpdateUserAsync(userName, true, userRequestInfo));

        });
      }
    }

    public static UserProfileService UserService(IDataContext dataContext)
    {
      IUserProfileHelperService userProfileHelperService = new UserProfileHelperService();
      var service = new UserProfileService(dataContext, userProfileHelperService);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = "INTERNAL_ORGANISATION" });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 1, CcsAccessRoleName = "Administrator" });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 2, CcsAccessRoleName = "Engineer" });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 3, CcsAccessRoleName = "DevOps" });

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "CiiOrg1", OrganisationUri = "Org1Uri", RightToBuy = true });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 1, OrganisationId = 1, UserGroupName = "Admin Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 2, OrganisationId = 1, UserGroupName = "Engineer Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 3, OrganisationId = 1, UserGroupName = "DevOpsEngineer Group" });

      dataContext.GroupAccess.Add(new GroupAccess { Id = 1, OrganisationUserGroupId = 1, CcsAccessRoleId = 1 });
      dataContext.GroupAccess.Add(new GroupAccess { Id = 2, OrganisationUserGroupId = 2, CcsAccessRoleId = 2 });
      dataContext.GroupAccess.Add(new GroupAccess { Id = 3, OrganisationUserGroupId = 3, CcsAccessRoleId = 3 });
      dataContext.GroupAccess.Add(new GroupAccess { Id = 4, OrganisationUserGroupId = 3, CcsAccessRoleId = 2 });

      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "CiiOrg2", OrganisationUri = "Org2Uri", RightToBuy = true });

      #region User1 Admin Group and Engineer Group
      // User 1
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, IdentityProviderId = 1, PartyId = 3, UserName = "user1@mail.com" });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 1, UserId = 1, OrganisationUserGroupId = 1 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 2, UserId = 1, OrganisationUserGroupId = 2 });
      #endregion

      #region User 2 has 1 group(DevOpsEngineer Group) (1 exisiting and 1 deleted(Admin Group))
      // User 2 
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 4, PartyId = 6, OrganisationId = 1, FirstName = "UserFN2", LastName = "UserLN2" });
      dataContext.User.Add(new User { Id = 2, IdentityProviderId = 1, PartyId = 6, UserName = "user2@mail.com" });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 3, UserId = 2, OrganisationUserGroupId = 3 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 4, UserId = 2, OrganisationUserGroupId = 1, IsDeleted = true });
      #endregion

      #region User 3
      // User 3 No group assigned might be invalid scenario
      dataContext.Party.Add(new Party { Id = 8, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 6, PartyId = 8, OrganisationId = 1, FirstName = "UserFN3", LastName = "UserLN3" });
      dataContext.User.Add(new User { Id = 3, IdentityProviderId = 1, PartyId = 8, UserName = "user3@mail.com" });
      #endregion

      #region User 4 deleted
      // User 4
      dataContext.Party.Add(new Party { Id = 9, PartyTypeId = 3, IsDeleted = true });
      dataContext.Person.Add(new Person { Id = 7, PartyId = 9, OrganisationId = 1, FirstName = "UserFN4", LastName = "UserLN4", IsDeleted = true });
      dataContext.User.Add(new User { Id = 4, IdentityProviderId = 1, PartyId = 9, UserName = "user4@mail.com", IsDeleted = true });
      #endregion

      await dataContext.SaveChangesAsync();
    }
  }
}
