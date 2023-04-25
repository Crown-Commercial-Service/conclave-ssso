using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.EntityFrameworkCore;
using Moq;
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
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "newuser@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }, new List<int> {1})
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP ", " UserLN1UP", "newuser@mail.com", "CiiOrg1", new List<int>{ 2 }, UserTitle.Doctor, new List<int> {1, 2 })
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo(" UserFN1UP ", " UserLN1UP ", "newuser@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 })
                }
            };

      [Theory]
      [MemberData(nameof(CorrectUserData))]
      public async Task CreateUser_WhenCorrectInfo(UserProfileEditRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          Mock<IIdamService> mockIdamService = new Mock<IIdamService>();
          Mock<ICcsSsoEmailService> mockEmailService = new Mock<ICcsSsoEmailService>();
          var userService = UserService(dataContext, mockIdamService, mockEmailService);

          var result = await userService.CreateUserAsync(userRequestInfo);

          Assert.Equal(userRequestInfo.Detail.IdentityProviderIds.Contains(1), result.IsRegisteredInIdam);

          var createdUser = await dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
          .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
          .Include(u => u.UserIdentityProviders)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == result.UserId);

          Assert.NotNull(createdUser);
          Assert.Equal(userRequestInfo.FirstName.Trim(), createdUser.Party.Person.FirstName);
          Assert.Equal(userRequestInfo.LastName.Trim(), createdUser.Party.Person.LastName);
          Assert.Equal(userRequestInfo.UserName, createdUser.UserName);
          Assert.Equal(userRequestInfo.OrganisationId, createdUser.Party.Person.Organisation.CiiOrganisationId);
          Assert.Equal(userRequestInfo.Detail.IdentityProviderIds.OrderBy(i => i), createdUser.UserIdentityProviders.Select(i => i.OrganisationEligibleIdentityProviderId).OrderBy(id => id));
          userRequestInfo.Detail.GroupIds.Sort();
          var userGroupIds = createdUser.UserGroupMemberships.Select(gm => gm.OrganisationUserGroupId).ToList();
          userGroupIds.Sort();
          Assert.Equal(userRequestInfo.Detail.GroupIds, userGroupIds);
          if (userRequestInfo.Detail.IdentityProviderIds.Contains(1))
          {
            mockIdamService.Verify(i => i.RegisterUserInIdamAsync(It.IsAny<SecurityApiUserInfo>()), Times.Once());
          }
          else
          {
            mockEmailService.Verify(e => e.SendUserWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
          }
        });
      }

      public static IEnumerable<object[]> InCorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo(null, "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", null, "usernew@mail.com", "CiiOrg1",new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo(" ", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", " ", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 0 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ }, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", null, UserTitle.Doctor, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {0, 1, 2 }, null),
                  ErrorConstant.ErrorInvalidUserGroup
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, null, new List<int> {0, 1, 2 }),
                  ErrorConstant.ErrorInvalidUserRole
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {0, 1, 2 }),
                  ErrorConstant.ErrorInvalidUserGroup
                },
                new object[]
                {
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "usernew@mail.com", "CiiOrg1", new List<int>{ 1 }, (UserTitle)8, new List<int> {1, 2 }),
                  ErrorConstant.ErrorInvalidTitle
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectUserData))]
      public async Task ThrowsException_WhenIncorrectData(UserProfileEditRequestInfo userRequestInfo, string expectedError)
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
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "usernew@mail.com", "WrongOrgId", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 })
                }
            };

      [Theory]
      [MemberData(nameof(NonExistingOrgForUserData))]
      public async Task ThrowsException_WhenOrganisationNotExsists(UserProfileEditRequestInfo userRequestInfo)
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
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 })
                }
            };

      [Theory]
      [MemberData(nameof(AlreadyExistingUserData))]
      public async Task ThrowsException_WhenUserAlreadyExsists(UserProfileEditRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceAlreadyExistsException>(() => userService.CreateUserAsync(userRequestInfo));

        });
      }
    }

    public class DeleteUser
    {
      [Theory]
      [InlineData("user5@mail.com")]
      public async Task DeleteSuccessfully_WhenUserExsists(string userName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          Mock<IIdamService> mockIdamService = new Mock<IIdamService>();
          var userService = UserService(dataContext, mockIdamService);

          await userService.DeleteUserAsync(userName);

          var deletedUser = await dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person).ThenInclude(pr => pr.Organisation)
          .Include(u => u.UserGroupMemberships).ThenInclude(ugm => ugm.OrganisationUserGroup)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

          Assert.Null(deletedUser);

          mockIdamService.Verify(i => i.DeleteUserInIdamAsync(It.IsAny<string>()), Times.Once());
        });
      }

      [Theory]
      [InlineData("user1@mail.com")]
      public async Task ThrowsException_WhenTryingToDeleteLastAdminUser(string userName)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => userService.DeleteUserAsync(userName));
          Assert.Equal(ErrorConstant.ErrorCannotDeleteLastOrgAdmin, ex.Message);
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

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => userService.DeleteUserAsync(userName));

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
                      DtoHelper.GetGroupAccessRole("Admin Group", "Organisation Administrator", "Organisation Administrator"),
                      DtoHelper.GetGroupAccessRole("User Group", "Organisation User", "Organisation User"),
                    })
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetUserProfileResponseInfo("UserFN2", "UserLN2", "user2@mail.com", "CiiOrg1",
                    new List<GroupAccessRole>
                    {
                      DtoHelper.GetGroupAccessRole("Other Group", "Other",  "Other"),
                      DtoHelper.GetGroupAccessRole("Other Group", "Organisation User", "Organisation User"),
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
  Assert.Equal(expectedUserInfo.Detail.UserGroups.Count, result.Detail.UserGroups.Count);

  foreach (var expectedGroupRole in expectedUserInfo.Detail.UserGroups)
  {
    Assert.Contains(result.Detail.UserGroups, gar => gar.Group == expectedGroupRole.Group && gar.AccessRoleName == expectedGroupRole.AccessRoleName);
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
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "UserFN1 UserLN1",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 1, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "userfn1",
                  DtoHelper.GetUserListResponse("CiiOrg1", 1, 1, 1, new List<UserListInfo>
                  {
                    DtoHelper.GetUserListInfo("UserFN1 UserLN1", "user1@mail.com")
                  })
                },
                new object[]
                {
                  "CiiOrg1",
                  new ResultSetCriteria{ CurrentPage =1, PageSize =3 },
                  "userfn",
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
                  "userfn ",
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
      public async Task ReturnsCorrectUserListInfo_WhenExsists(string organisationId, ResultSetCriteria resultSetCriteria, UserFilterCriteria userFilterCriteria,
        UserListResponse expectedUserInfo)
      {
        // todo: userFilterCriteria has been added to avoid build error. But it looks like most of the unit test has been broken. 
        // Need to create new tickets to update it 
        await DataContextHelper.ScopeAsync(async dataContext =>
                {
                  await SetupTestDataAsync(dataContext);
                  var userService = UserService(dataContext);

                  var result = await userService.GetUsersAsync(organisationId, resultSetCriteria, userFilterCriteria);

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
      public static IEnumerable<object[]> CorrectMyAccountData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, null, null, null)
                },
                new object[]
                {
                  "user2@mail.com", 2,
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user2@mail.com", "CiiOrg1", new List<int>{ 1 }, null, null, null)
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo(" UserFN1UP", "UserLN1UP ", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, null, null, null)
                },
                new object[]
                {
                  "user2@mail.com", 2,
                  DtoHelper.GetUserProfileRequestInfo(" UserFN2UP ", "  UserLN2UP ", "user2@mail.com", "CiiOrg1", new List<int>{ 1 }, null, null, null)
                },
            };

      [Theory]
      [MemberData(nameof(CorrectMyAccountData))]
      public async Task UpdateMyAccount_WhenCorrectInfo(string userName, int userId, UserProfileEditRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          Mock<IIdamService> mockIdamService = new Mock<IIdamService>();
          var requestContext = new RequestContext
          {
            UserId = userId,
            CiiOrganisationId = userRequestInfo.OrganisationId
          };
          var userService = UserService(dataContext, mockIdamService, null, requestContext);

          var result = await userService.UpdateUserAsync(userName, userRequestInfo);

          Assert.False(result.IsRegisteredInIdam);

          var updatedUser = await dataContext.User
          .Include(u => u.Party).ThenInclude(p => p.Person)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

          Assert.NotNull(updatedUser);
          Assert.Equal(userRequestInfo.FirstName.Trim(), updatedUser.Party.Person.FirstName);
          Assert.Equal(userRequestInfo.LastName.Trim(), updatedUser.Party.Person.LastName);
          mockIdamService.Verify(i => i.RegisterUserInIdamAsync(It.IsAny<SecurityApiUserInfo>()), Times.Never());
          mockIdamService.Verify(i => i.DeleteUserInIdamAsync(It.IsAny<string>()), Times.Never());
        });
      }

      public static IEnumerable<object[]> CorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  false, false
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user2@mail.com",  "CiiOrg1", new List<int>{ 1, 2 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, false
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(" UserFN1UP", "UserLN1UP ", "user1@mail.com", "CiiOrg1", new List<int>{ 1, 2 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  false, false
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(" UserFN1UP", "UserLN1UP ", "user1@mail.com", "CiiOrg1", new List<int>{ 2 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  false, true
                },
                new object[]
                {
                  "user2@mail.com",
                  DtoHelper.GetUserProfileRequestInfo(" UserFN2UP ", "  UserLN2UP ", "user2@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, false
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, null, new List<int> {1, 2 }),
                  false, false
                },
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }, null),
                  false, false
                },
            };

      [Theory]
      [MemberData(nameof(CorrectUserData))]
      public async Task UpdateUser_WhenCorrectInfo(string userName, UserProfileEditRequestInfo userRequestInfo, bool isRegisteredInIdam, bool isDeletedFromIdam)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          Mock<IIdamService> mockIdamService = new Mock<IIdamService>();
          var userService = UserService(dataContext, mockIdamService);

          var result = await userService.UpdateUserAsync(userName, userRequestInfo);

          Assert.Equal(isRegisteredInIdam, result.IsRegisteredInIdam);

          var updatedUser = await dataContext.User
          .Include(u => u.UserGroupMemberships)
          .Include(u => u.UserAccessRoles)
          .Include(u => u.Party).ThenInclude(p => p.Person)
          .Include(u => u.UserIdentityProviders)
          .FirstOrDefaultAsync(u => !u.IsDeleted && u.UserName == userName);

          Assert.NotNull(updatedUser);
          Assert.Equal(userRequestInfo.FirstName.Trim(), updatedUser.Party.Person.FirstName);
          Assert.Equal(userRequestInfo.LastName.Trim(), updatedUser.Party.Person.LastName);
          Assert.Equal(userRequestInfo.Detail.IdentityProviderIds.OrderBy(i => i), updatedUser.UserIdentityProviders.Where(i => !i.IsDeleted).Select(idp => idp.OrganisationEligibleIdentityProviderId).OrderBy(i => i));
          if (userRequestInfo.Detail.GroupIds != null)
          {
            var updatedUserGroups = updatedUser.UserGroupMemberships.Where(ugm => !ugm.IsDeleted).Select(ugm => ugm.OrganisationUserGroupId).OrderBy(id => id).ToList();
            userRequestInfo.Detail.GroupIds.Sort();
            Assert.Equal(userRequestInfo.Detail.GroupIds, updatedUserGroups);
          }
          if (userRequestInfo.Detail.RoleIds != null)
          {
            var updatedUserRoles = updatedUser.UserAccessRoles.Where(ur => !ur.IsDeleted).Select(ur => ur.OrganisationEligibleRoleId).OrderBy(id => id).ToList();
            userRequestInfo.Detail.RoleIds.Sort();
            Assert.Equal(userRequestInfo.Detail.RoleIds, updatedUserRoles);
          }
          if (isRegisteredInIdam)
          {
            mockIdamService.Verify(i => i.RegisterUserInIdamAsync(It.IsAny<SecurityApiUserInfo>()), Times.Once());
          }
          if (isDeletedFromIdam)
          {
            mockIdamService.Verify(i => i.DeleteUserInIdamAsync(It.IsAny<string>()), Times.Once());
          }
        });
      }

      public static IEnumerable<object[]> InCorrectUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo(null, "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", null, "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo(" ", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, ErrorConstant.ErrorInvalidFirstName
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", " ", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  true, ErrorConstant.ErrorInvalidLastName
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 0 }, UserTitle.Doctor, new List<int> {1, 2 }),
                  false, ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{  }, UserTitle.Doctor, new List<int> {1, 2 }),
                  false, ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", null, UserTitle.Doctor, new List<int> {1, 2 }),
                  false, ErrorConstant.ErrorInvalidIdentityProvider
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, (UserTitle)8, new List<int> {1, 2 }),
                  false, ErrorConstant.ErrorInvalidTitle
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {0, 1, 2 }, null),
                  false, ErrorConstant.ErrorInvalidUserGroup
                },
                new object[]
                {
                  "user1@mail.com", 1,
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, null, new List<int> {0, 1, 2 }),
                  false, ErrorConstant.ErrorInvalidUserRole
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectUserData))]
      public async Task ThrowsException_WhenIncorrectData(string userName, int userId, UserProfileEditRequestInfo userRequestInfo, bool isMyAccount, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var requestContext = new RequestContext
          {
            UserId = isMyAccount ? userId : 0,
            CiiOrganisationId = userRequestInfo.OrganisationId
          };
          var userService = UserService(dataContext, null, null, requestContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => userService.UpdateUserAsync(userName, userRequestInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> RemoveAdminGroupUserData =>
           new List<object[]>
           {
                new object[]
                {
                  "user1@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN1UP", "UserLN1UP", "user1@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> { 2 })
                },
           };

      [Theory]
      [MemberData(nameof(RemoveAdminGroupUserData))]
      public async Task ThrowsException_WhenRemovingAdminRoleOfLastAdmin(string userName, UserProfileEditRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => userService.UpdateUserAsync(userName, userRequestInfo));
          Assert.Equal(ErrorConstant.ErrorCannotRemoveAdminRoleGroupLastOrgAdmin, ex.Message);
        });
      }

      public static IEnumerable<object[]> NonExistingUserData =>
            new List<object[]>
            {
                new object[]
                {
                  "usernotfound@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "usernotfound@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 })
                },
                new object[]
                {
                  "user4@mail.com",
                  DtoHelper.GetUserProfileRequestInfo("UserFN2UP", "UserLN2UP", "user4@mail.com", "CiiOrg1", new List<int>{ 1 }, UserTitle.Doctor, new List<int> {1, 2 })
                },
            };

      [Theory]
      [MemberData(nameof(NonExistingUserData))]
      public async Task ThrowsException_WhenUserNotExsists(string userName, UserProfileEditRequestInfo userRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var userService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => userService.UpdateUserAsync(userName, userRequestInfo));

        });
      }
    }

    public static UserProfileService UserService(IDataContext dataContext, Mock<IIdamService> mockIdamService = null,
      Mock<ICcsSsoEmailService> mockEmailService = null, RequestContext requestContext = null)
    {
      mockIdamService ??= new Mock<IIdamService>();
      IUserProfileHelperService userProfileHelperService = new UserProfileHelperService();
      requestContext ??= new RequestContext();

      mockEmailService ??= new Mock<ICcsSsoEmailService>();
      Mock<IAdaptorNotificationService> mockAdapterNotificationService = new Mock<IAdaptorNotificationService>();
      var mockWrapperCacheService = new Mock<IWrapperCacheService>();
      var mockAuditLoginService = new Mock<IAuditLoginService>();
      var mockRemoteCacheService = new Mock<IRemoteCacheService>();
      var mockCacheInvalidateService = new Mock<ICacheInvalidateService>();
      var mockCryptographyService = new Mock<ICryptographyService>();
      var mockApplicationConfigurationInfo = new Mock<ApplicationConfigurationInfo>();
      var mockLookUpService = new Mock<ILookUpService>();
      var mockWrapperApiService = new Mock<IWrapperApiService>();
      var mockUserProfileRoleApprovalService = new Mock<IUserProfileRoleApprovalService>();
      var mockServiceRoleGroupMapperService = new Mock<IServiceRoleGroupMapperService>();
      var mockOrganisationGroupService = new Mock<IOrganisationGroupService>();

      var service = new UserProfileService(dataContext, userProfileHelperService, requestContext, mockIdamService.Object,
 mockEmailService.Object, mockAdapterNotificationService.Object, mockWrapperCacheService.Object, mockAuditLoginService.Object, mockRemoteCacheService.Object,
 mockCacheInvalidateService.Object, mockCryptographyService.Object, mockApplicationConfigurationInfo.Object, mockLookUpService.Object, mockWrapperApiService.Object,
 mockUserProfileRoleApprovalService.Object, mockServiceRoleGroupMapperService.Object, mockOrganisationGroupService.Object);
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
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "Username Password", IdpConnectionName = Contstant.ConclaveIdamConnectionName, IdpUri = "IDP" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 2, IdpName = "Google", IdpConnectionName = "google", IdpUri = "IDP_google" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 3, IdpName = "Microsoft 365", IdpConnectionName = "microsoft365", IdpUri = "IDP_microsoft" });

      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 1, CcsAccessRoleName = "Organisation Administrator", CcsAccessRoleNameKey = Contstant.OrgAdminRoleNameKey });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 2, CcsAccessRoleName = "Organisation User", CcsAccessRoleNameKey = Contstant.DefaultUserRoleNameKey });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 3, CcsAccessRoleName = "Other", CcsAccessRoleNameKey = "OTHER" });

      #region Org1
      //Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "CiiOrg1", OrganisationUri = "Org1Uri", RightToBuy = true });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 1, OrganisationId = 1, UserGroupName = "Admin Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 2, OrganisationId = 1, UserGroupName = "User Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 3, OrganisationId = 1, UserGroupName = "Other Group" });

      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 1, IdentityProviderId = 2 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 3, OrganisationId = 1, IdentityProviderId = 3 });

      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 1, OrganisationId = 1, CcsAccessRoleId = 1 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 2, OrganisationId = 1, CcsAccessRoleId = 2 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 3, OrganisationId = 1, CcsAccessRoleId = 3 });

      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 1, OrganisationUserGroupId = 1, OrganisationEligibleRoleId = 1 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 2, OrganisationUserGroupId = 2, OrganisationEligibleRoleId = 2 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 3, OrganisationUserGroupId = 3, OrganisationEligibleRoleId = 3 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 4, OrganisationUserGroupId = 3, OrganisationEligibleRoleId = 2 });
      #endregion

      #region Org2
      //Org2
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "CiiOrg2", OrganisationUri = "Org2Uri", RightToBuy = true });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 4, OrganisationId = 2, UserGroupName = "Admin Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 5, OrganisationId = 2, UserGroupName = "User Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 6, OrganisationId = 2, UserGroupName = "Other Group" });

      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 4, OrganisationId = 2, IdentityProviderId = 1 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 5, OrganisationId = 2, IdentityProviderId = 2 });

      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 4, OrganisationId = 2, CcsAccessRoleId = 1 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 5, OrganisationId = 2, CcsAccessRoleId = 2 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 6, OrganisationId = 2, CcsAccessRoleId = 3 });

      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 5, OrganisationUserGroupId = 4, OrganisationEligibleRoleId = 4 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 6, OrganisationUserGroupId = 5, OrganisationEligibleRoleId = 5 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 7, OrganisationUserGroupId = 6, OrganisationEligibleRoleId = 6 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 8, OrganisationUserGroupId = 6, OrganisationEligibleRoleId = 5 });
      #endregion

      #region Org1 users
      #region User1 Admin Group and Engineer Group
      // User 1
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, PartyId = 3, UserName = "user1@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 1, UserId = 1, OrganisationEligibleIdentityProviderId = 1 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 1, UserId = 1, OrganisationUserGroupId = 1 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 2, UserId = 1, OrganisationUserGroupId = 2 });
      #endregion

      #region User 2 has 1 group(DevOpsEngineer Group) (1 exisiting and 1 deleted(Admin Group))
      // User 2 
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 4, PartyId = 6, OrganisationId = 1, FirstName = "UserFN2", LastName = "UserLN2" });
      dataContext.User.Add(new User { Id = 2, PartyId = 6, UserName = "user2@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 2, UserId = 2, OrganisationEligibleIdentityProviderId = 2 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 3, UserId = 2, OrganisationUserGroupId = 3 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 4, UserId = 2, OrganisationUserGroupId = 1, IsDeleted = true });
      #endregion

      #region User 3
      // User 3 No group assigned might be invalid scenario
      dataContext.Party.Add(new Party { Id = 8, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 6, PartyId = 8, OrganisationId = 1, FirstName = "UserFN3", LastName = "UserLN3" });
      dataContext.User.Add(new User { Id = 3, PartyId = 8, UserName = "user3@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 3, UserId = 3, OrganisationEligibleIdentityProviderId = 1 });
      #endregion

      #region User 4 deleted
      // User 4
      dataContext.Party.Add(new Party { Id = 9, PartyTypeId = 3, IsDeleted = true });
      dataContext.Person.Add(new Person { Id = 7, PartyId = 9, OrganisationId = 1, FirstName = "UserFN4", LastName = "UserLN4", IsDeleted = true });
      dataContext.User.Add(new User { Id = 4, PartyId = 9, UserName = "user4@mail.com", IsDeleted = true });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 4, UserId = 4, OrganisationEligibleIdentityProviderId = 1 });
      #endregion
      #endregion

      #region Org2 users
      #region User5 Admin Group and Engineer Group
      // User 5
      dataContext.Party.Add(new Party { Id = 10, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 8, PartyId = 10, OrganisationId = 2, FirstName = "UserFN5", LastName = "UserLN5" });
      dataContext.User.Add(new User { Id = 5, PartyId = 10, UserName = "user5@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 5, UserId = 5, OrganisationEligibleIdentityProviderId = 4 });

      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 5, UserId = 5, OrganisationUserGroupId = 4 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 6, UserId = 5, OrganisationUserGroupId = 5 });
      #endregion
      #region User6 Admin Group
      // User 6
      dataContext.Party.Add(new Party { Id = 11, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 9, PartyId = 11, OrganisationId = 2, FirstName = "UserFN6", LastName = "UserLN6" });
      dataContext.User.Add(new User { Id = 6, PartyId = 11, UserName = "user6@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 6, UserId = 6, OrganisationEligibleIdentityProviderId = 4 });

      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 7, UserId = 6, OrganisationUserGroupId = 4 });
      #endregion
      #endregion

      await dataContext.SaveChangesAsync();
    }
  }
}
