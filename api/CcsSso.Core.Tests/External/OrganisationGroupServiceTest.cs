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
using CcsSso.Shared.Domain.Contexts;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class OrganisationGroupServiceTest
  {

    public class CreateGroup
    {
      public static IEnumerable<object[]> CorrectGroupData =>
            new List<object[]>
            {
                new object[]
                {
                  "CiiOrg1",
                  DtoHelper.GetOrganisationGroupNameInfo("NewGroup")
                }
            };

      [Theory]
      [MemberData(nameof(CorrectGroupData))]
      public async Task CreateGroup_WhenCorrectInfo(string ciiOrgId, OrganisationGroupNameInfo groupNameInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          var result = await groupService.CreateGroupAsync(ciiOrgId, groupNameInfo);

          var createdGroup = await dataContext.OrganisationUserGroup.FirstOrDefaultAsync(g => g.Id == result);
          Assert.NotNull(createdGroup);
          Assert.Equal(groupNameInfo.GroupName, createdGroup.UserGroupName);
        });
      }

      public static IEnumerable<object[]> InvalidGroupData =>
            new List<object[]>
            {
                new object[]
                {
                  "CiiOrg1",
                  DtoHelper.GetOrganisationGroupNameInfo(""),
                  ErrorConstant.ErrorInvalidGroupName
                },
                new object[]
                {
                  "CiiOrg1",
                  DtoHelper.GetOrganisationGroupNameInfo(" "),
                  ErrorConstant.ErrorInvalidGroupName
                },
                new object[]
                {
                  "CiiOrg1",
                  DtoHelper.GetOrganisationGroupNameInfo(null),
                  ErrorConstant.ErrorInvalidGroupName
                }
            };

      [Theory]
      [MemberData(nameof(InvalidGroupData))]
      public async Task ThrowsException_WhenInvalidGroupData(string ciiOrgId, OrganisationGroupNameInfo groupNameInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => groupService.CreateGroupAsync(ciiOrgId, groupNameInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> InvalidOrgGroupData =>
            new List<object[]>
            {
                new object[]
                {
                  "invalidorg",
                  DtoHelper.GetOrganisationGroupNameInfo("Admin Group")
                }
            };

      [Theory]
      [MemberData(nameof(InvalidOrgGroupData))]
      public async Task ThrowsException_WhenInvalidOrg(string ciiOrgId, OrganisationGroupNameInfo groupNameInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => groupService.CreateGroupAsync(ciiOrgId, groupNameInfo));

        });
      }

      public static IEnumerable<object[]> AlreadyExistingGroupData =>
            new List<object[]>
            {
                new object[]
                {
                  "CiiOrg1",
                  DtoHelper.GetOrganisationGroupNameInfo("Admin Group")
                }
            };

      [Theory]
      [MemberData(nameof(AlreadyExistingGroupData))]
      public async Task ThrowsException_WhenGroupAlreadyExsists(string ciiOrgId, OrganisationGroupNameInfo groupNameInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceAlreadyExistsException>(() => groupService.CreateGroupAsync(ciiOrgId, groupNameInfo));

        });
      }
    }

    public class DeleteGroup
    {
      [Theory]
      [InlineData("CiiOrg1", 1)]
      public async Task DeleteGroup_WhenCorrectInfo(string ciiOrgId, int groupId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await groupService.DeleteGroupAsync(ciiOrgId, groupId);

          var deletedGroup = await dataContext.OrganisationUserGroup.FirstOrDefaultAsync(g => g.Id == groupId);
          Assert.NotNull(deletedGroup);
        });
      }

      [Theory]
      [InlineData("invalidOrg", 1)]
      public async Task ThrowsException_WhenInvalidOrg(string ciiOrgId, int groupId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => groupService.DeleteGroupAsync(ciiOrgId, groupId));

        });
      }
    }

    public class GetGroup
    {
      public static IEnumerable<object[]> CorrectGroupData =>
      new List<object[]>
      {
          new object[]
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[]
          {
            "CiiOrg2", 6,
            DtoHelper.GetOrganisationGroupResponse("CiiOrg2", 6, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(5, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user5@mail.com", "UserFN5 UserLN5"),
                DtoHelper.GetGroupUser("user6@mail.com", "UserFN6 UserLN6")
              },
              DateTime.UtcNow.Date)
          },
          new object[]
          {
            "CiiOrg2", 8,
            DtoHelper.GetOrganisationGroupResponse("CiiOrg2", 8, "Other Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(6, "Organisation User"),
                DtoHelper.GetGroupRole(7, "Other")
              },
              new List<GroupUser>
              {
              },
              DateTime.UtcNow.Date)
          },
          new object[]
          {
            "CiiOrg2", 9,
            DtoHelper.GetOrganisationGroupResponse("CiiOrg2", 9, "No Role User Group",
              new List<GroupRole>
              { },
              new List<GroupUser>
              { },
              DateTime.UtcNow.Date)
          }
      };

      [Theory]
      [MemberData(nameof(CorrectGroupData))]
      public async Task ReturnGroup_WhenCorrectInfo(string ciiOrgId, int groupId, OrganisationGroupResponseInfo expectedResponse)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          var result = await groupService.GetGroupAsync(ciiOrgId, groupId);

          Assert.NotNull(result);
          Assert.Equal(expectedResponse.OrganisationId, result.OrganisationId);
          Assert.Equal(expectedResponse.GroupId, result.GroupId);
          Assert.Equal(expectedResponse.GroupName, result.GroupName);

          if (expectedResponse.Roles.Any())
          {
            expectedResponse.Roles.ForEach((expectedRole) =>
            {
              var resultRole = result.Roles.FirstOrDefault(r => r.Id == expectedRole.Id);
              Assert.NotNull(resultRole);
              Assert.Equal(expectedRole.Name, resultRole.Name);
            });
          }
          else
          {
            Assert.Empty(result.Roles);
          }

          if (expectedResponse.Users.Any())
          {
            expectedResponse.Users.ForEach((expectedUser) =>
            {
              var resultUser = result.Users.FirstOrDefault(r => r.UserId == expectedUser.UserId);
              Assert.NotNull(resultUser);
              Assert.Equal(expectedUser.Name, resultUser.Name);
            });
          }
          else
          {
            Assert.Empty(result.Users);
          }
        });
      }

      [Theory]
      [InlineData("invalidOrg", 1)]
      [InlineData("CiiOrg1", 5)]
      [InlineData("CiiOrg1", 50)]
      public async Task ThrowsException_WhenInvalidOrgOrGroup(string ciiOrgId, int groupId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => groupService.GetGroupAsync(ciiOrgId, groupId));
        });
      }
    }

    public class GetGroups
    {
      public static IEnumerable<object[]> CorrectGroupData =>
      new List<object[]>
      {
          new object[]
          {
            "CiiOrg1", "",
            DtoHelper.GetOrganisationGroupListObject("CiiOrg1",
              new List<OrganisationGroupInfo>
              {
                DtoHelper.GetOrganisationGroupInfo(1, "Admin Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(2, "User Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(3, "Other Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(4, "Digits Group", DateTime.UtcNow.Date),
              })
          },
          new object[]
          {
            "CiiOrg1", "Group",
            DtoHelper.GetOrganisationGroupListObject("CiiOrg1",
              new List<OrganisationGroupInfo>
              {
                DtoHelper.GetOrganisationGroupInfo(1, "Admin Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(2, "User Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(3, "Other Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(4, "Digits Group", DateTime.UtcNow.Date),
              })
          },
          new object[]
          {
            "CiiOrg1", "Admin",
            DtoHelper.GetOrganisationGroupListObject("CiiOrg1",
              new List<OrganisationGroupInfo>
              {
                DtoHelper.GetOrganisationGroupInfo(1, "Admin Group", DateTime.UtcNow.Date)
              })
          },
           new object[]
          {
            "CiiOrg1", "Admin Group",
            DtoHelper.GetOrganisationGroupListObject("CiiOrg1",
              new List<OrganisationGroupInfo>
              {
                DtoHelper.GetOrganisationGroupInfo(1, "Admin Group", DateTime.UtcNow.Date)
              })
          },
          new object[]
          {
            "CiiOrg2", "",
            DtoHelper.GetOrganisationGroupListObject("CiiOrg2",
              new List<OrganisationGroupInfo>
              {
                DtoHelper.GetOrganisationGroupInfo(6, "Admin Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(7, "User Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(8, "Other Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(9, "No Role User Group", DateTime.UtcNow.Date),
              })
          },
          new object[]
          {
            "CiiOrg2", "User",
            DtoHelper.GetOrganisationGroupListObject("CiiOrg2",
              new List<OrganisationGroupInfo>
              {
                DtoHelper.GetOrganisationGroupInfo(7, "User Group", DateTime.UtcNow.Date),
                DtoHelper.GetOrganisationGroupInfo(9, "No Role User Group", DateTime.UtcNow.Date),
              })
          }
      };

      [Theory]
      [MemberData(nameof(CorrectGroupData))]
      public async Task ReturnGroup_WhenCorrectInfo(string ciiOrgId, string serachString, OrganisationGroupList expectedResponse)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          var result = await groupService.GetGroupsAsync(ciiOrgId, serachString);

          Assert.NotNull(result);
          Assert.Equal(expectedResponse.OrganisationId, result.OrganisationId);
          Assert.Equal(expectedResponse.GroupList.Count, result.GroupList.Count);

          expectedResponse.GroupList.ForEach((expectedGroup) =>
          {
            var resultGroup = result.GroupList.FirstOrDefault(g => g.GroupId == expectedGroup.GroupId);
            Assert.NotNull(resultGroup);
            Assert.Equal(expectedGroup.GroupId, resultGroup.GroupId);
            Assert.Equal(expectedGroup.GroupName, resultGroup.GroupName);
          });
        });
      }

      [Theory]
      [InlineData("invalidOrg", "")]
      public async Task ThrowsException_WhenInvalidOrg(string ciiOrgId, string serachString)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => groupService.GetGroupsAsync(ciiOrgId, serachString));
        });
      }
    }

    public class UpdateGroup
    {
      public static IEnumerable<object[]> CorrectGroupData =>
      new List<object[]>
      {
          new object[] // Nothing changed
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Nothing changed
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              null,
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Nothing changed
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              null),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Name changed
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group updated",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group updated",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Nothing change since no name
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo(" ",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Add role
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo(" ",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { 2 }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator"),
                DtoHelper.GetGroupRole(2, "Organisation User")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Add and remove same role
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo(" ",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { 2 }, new List<int> { 2 }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Add and remove different roles
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo(" ",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { 2 }, new List<int> { 1 }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(2, "Organisation User")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Remove role
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo(" ",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { 1 }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Add user
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { "user2@mail.com" },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1"),
                DtoHelper.GetGroupUser("user2@mail.com", "UserFN2 UserLN2")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Add and remove same user
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { "user2@mail.com" },
                new List<string> { "user2@mail.com" })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Add and remove different users
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { "user2@mail.com" },
                new List<string> { "user1@mail.com" })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user2@mail.com", "UserFN2 UserLN2")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Remove user
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> {},
                new List<string> { "user1@mail.com" })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Check duplicate not happening
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { 2, 2 , 1 }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { "user2@mail.com", "user2@mail.com",  "user1@mail.com" },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator"),
                DtoHelper.GetGroupRole(2, "Organisation User")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1"),
                DtoHelper.GetGroupUser("user2@mail.com", "UserFN2 UserLN2"),
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Check duplicate not happening
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { 1 , 1 }, new List<int> { }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { "user1@mail.com", "user1@mail.com" },
                new List<string> { })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
          new object[] // Check duplicate not happening
          {
            "CiiOrg1", 1,
            DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
              DtoHelper.GetOrganisationGroupRolePatchInfo(
                new List<int> { }, new List<int> { 2, 2 }),
              DtoHelper.GetOrganisationGroupUserPatchInfo(
                new List<string> { },
                new List<string> {  "user2@mail.com", "user2@mail.com" })),
            DtoHelper.GetOrganisationGroupResponse("CiiOrg1", 1, "Admin Group",
              new List<GroupRole>
              {
                DtoHelper.GetGroupRole(1, "Organisation Administrator")
              },
              new List<GroupUser>
              {
                DtoHelper.GetGroupUser("user1@mail.com", "UserFN1 UserLN1")
              },
              DateTime.UtcNow.Date)
          },
      };

      [Theory]
      [MemberData(nameof(CorrectGroupData))]
      public async Task ReturnGroup_WhenCorrectInfo(string ciiOrgId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo, OrganisationGroupResponseInfo expectedResponse)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await groupService.UpdateGroupAsync(ciiOrgId, groupId, organisationGroupRequestInfo);

          var result = await groupService.GetGroupAsync(ciiOrgId, groupId);

          Assert.NotNull(result);
          Assert.Equal(expectedResponse.OrganisationId, result.OrganisationId);
          Assert.Equal(expectedResponse.GroupId, result.GroupId);
          Assert.Equal(expectedResponse.GroupName, result.GroupName);

          if (expectedResponse.Roles.Any())
          {
            expectedResponse.Roles.ForEach((expectedRole) =>
            {
              var resultRole = result.Roles.FirstOrDefault(r => r.Id == expectedRole.Id);
              Assert.NotNull(resultRole);
              Assert.Equal(expectedRole.Name, resultRole.Name);
            });
          }
          else
          {
            Assert.Empty(result.Roles);
          }

          if (expectedResponse.Users.Any())
          {
            expectedResponse.Users.ForEach((expectedUser) =>
            {
              var resultUser = result.Users.FirstOrDefault(r => r.UserId == expectedUser.UserId);
              Assert.NotNull(resultUser);
              Assert.Equal(expectedUser.Name, resultUser.Name);
            });
          }
          else
          {
            Assert.Empty(result.Users);
          }
        });
      }


      public static IEnumerable<object[]> NotExistGroupData =>
         new List<object[]>
         {
              new object[]
              {
                "CiiOrg1", 5,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { }))
              },
              new object[]
              {
                "CiiOrg1", 50,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { }))
              },
              new object[]
              {
                "Invalidorg", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { }))
              }
         };

      [Theory]
      [MemberData(nameof(NotExistGroupData))]
      public async Task ThrowsException_WhenNotExists(string ciiOrgId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => groupService.UpdateGroupAsync(ciiOrgId, groupId, organisationGroupRequestInfo));
        });
      }

      public static IEnumerable<object[]> AlreadyExistGroupData =>
         new List<object[]>
         {
              new object[]
              {
                "CiiOrg1", 2,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { }))
              }
         };

      [Theory]
      [MemberData(nameof(AlreadyExistGroupData))]
      public async Task ThrowsException_WhenGroupNameExists(string ciiOrgId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          await Assert.ThrowsAsync<ResourceAlreadyExistsException>(() => groupService.UpdateGroupAsync(ciiOrgId, groupId, organisationGroupRequestInfo));
        });
      }

      public static IEnumerable<object[]> InCorrectGroupData =>
         new List<object[]>
         {
              new object[] // Role null
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    null, null),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { })),
                ErrorConstant.ErrorInvalidRoleInfo
              },
              new object[] // Invalid added roles
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { 10, 100 }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { })),
                ErrorConstant.ErrorInvalidRoleInfo
              },
              new object[] // Invalid removed roles
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> {  }, new List<int> { 10, 100 }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { })),
                ErrorConstant.ErrorInvalidRoleInfo
              },
              new object[] // User null
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    null, null)),
                ErrorConstant.ErrorInvalidUserInfo
              },
              new object[] // Invalid added users
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { "user60@mail.com",  "user61@mail.com" },
                    new List<string> { })),
                ErrorConstant.ErrorInvalidUserInfo
              },
              new object[] // Invalid removed users
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { "user60@mail.com",  "user61@mail.com" })),
                ErrorConstant.ErrorInvalidUserInfo
              },
              new object[] // Invalid username format added users
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { "invalidEmail" },
                    new List<string> { })),
                ErrorConstant.ErrorInvalidUserId
              },
              new object[] // Invalid username format removed users
              {
                "CiiOrg1", 1,
                DtoHelper.GetOrganisationGroupRequestInfo("Admin Group",
                  DtoHelper.GetOrganisationGroupRolePatchInfo(
                    new List<int> { }, new List<int> { }),
                  DtoHelper.GetOrganisationGroupUserPatchInfo(
                    new List<string> { },
                    new List<string> { "invalidEmail" })),
                ErrorConstant.ErrorInvalidUserId
              },
         };

      [Theory]
      [MemberData(nameof(InCorrectGroupData))]
      public async Task ThrowsException_WhenInCorrectData(string ciiOrgId, int groupId, OrganisationGroupRequestInfo organisationGroupRequestInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var groupService = UserService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => groupService.UpdateGroupAsync(ciiOrgId, groupId, organisationGroupRequestInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }
    }

    public static OrganisationGroupService UserService(IDataContext dataContext)
    {
      UserProfileHelperService userProfileHelperService = new UserProfileHelperService();
      var mockAuditLoginService = new Mock<IAuditLoginService>();
      var mockEmailService = new Mock<ICcsSsoEmailService>();
      var mockCacheService = new Mock<IWrapperCacheService>();
      ApplicationConfigurationInfo applicationConfigurationInfo = new();
      var mockRolesToServiceRoleGroupMapperService = new Mock<IServiceRoleGroupMapperService>();
      var mockOrganisationProfileService = new Mock<IOrganisationProfileService>();

      var service = new OrganisationGroupService(dataContext, userProfileHelperService, mockAuditLoginService.Object, mockEmailService.Object,
        mockCacheService.Object, applicationConfigurationInfo, mockRolesToServiceRoleGroupMapperService.Object, mockOrganisationProfileService.Object);
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

      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 1, CcsAccessRoleName = "Organisation Administrator", CcsAccessRoleNameKey = "ORG_ADMINISTRATOR" });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 2, CcsAccessRoleName = "Organisation User", CcsAccessRoleNameKey = "DEFAULT_ORG_USER" });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 3, CcsAccessRoleName = "Other", CcsAccessRoleNameKey = "OTHER" });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 4, CcsAccessRoleName = "Digits", CcsAccessRoleNameKey = "DIGITS" });
      dataContext.CcsAccessRole.Add(new CcsAccessRole { Id = 5, CcsAccessRoleName = "Test", CcsAccessRoleNameKey = "TEST" });

      #region Org1
      //Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "CiiOrg1", OrganisationUri = "Org1Uri", RightToBuy = true });

      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 1, OrganisationId = 1, UserGroupName = "Admin Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 2, OrganisationId = 1, UserGroupName = "User Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 3, OrganisationId = 1, UserGroupName = "Other Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 4, OrganisationId = 1, UserGroupName = "Digits Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 5, OrganisationId = 1, UserGroupName = "Test Group", IsDeleted = true });

      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 1, IdentityProviderId = 2 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 3, OrganisationId = 1, IdentityProviderId = 3 });

      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 1, OrganisationId = 1, CcsAccessRoleId = 1 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 2, OrganisationId = 1, CcsAccessRoleId = 2 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 3, OrganisationId = 1, CcsAccessRoleId = 3 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 4, OrganisationId = 1, CcsAccessRoleId = 4 });

      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 1, OrganisationUserGroupId = 1, OrganisationEligibleRoleId = 1 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 2, OrganisationUserGroupId = 2, OrganisationEligibleRoleId = 2 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 3, OrganisationUserGroupId = 3, OrganisationEligibleRoleId = 3 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 4, OrganisationUserGroupId = 3, OrganisationEligibleRoleId = 2 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 5, OrganisationUserGroupId = 3, OrganisationEligibleRoleId = 3 });
      #endregion

      #region Org2
      //Org2
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "CiiOrg2", OrganisationUri = "Org2Uri", RightToBuy = true });

      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 6, OrganisationId = 2, UserGroupName = "Admin Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 7, OrganisationId = 2, UserGroupName = "User Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 8, OrganisationId = 2, UserGroupName = "Other Group" });
      dataContext.OrganisationUserGroup.Add(new OrganisationUserGroup { Id = 9, OrganisationId = 2, UserGroupName = "No Role User Group" });

      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 4, OrganisationId = 2, IdentityProviderId = 1 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 5, OrganisationId = 2, IdentityProviderId = 2 });

      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 5, OrganisationId = 2, CcsAccessRoleId = 1 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 6, OrganisationId = 2, CcsAccessRoleId = 2 });
      dataContext.OrganisationEligibleRole.Add(new OrganisationEligibleRole { Id = 7, OrganisationId = 2, CcsAccessRoleId = 3 });

      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 6, OrganisationUserGroupId = 6, OrganisationEligibleRoleId = 5 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 7, OrganisationUserGroupId = 7, OrganisationEligibleRoleId = 6 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 8, OrganisationUserGroupId = 8, OrganisationEligibleRoleId = 7 });
      dataContext.OrganisationGroupEligibleRole.Add(new OrganisationGroupEligibleRole { Id = 9, OrganisationUserGroupId = 8, OrganisationEligibleRoleId = 6 });
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
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 5, UserId = 2, OrganisationUserGroupId = 1, IsDeleted = true });
      #endregion
      #endregion

      #region Org2 users
      #region User5 Admin Group and Engineer Group
      // User 5
      dataContext.Party.Add(new Party { Id = 10, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 8, PartyId = 10, OrganisationId = 2, FirstName = "UserFN5", LastName = "UserLN5" });
      dataContext.User.Add(new User { Id = 5, PartyId = 10, UserName = "user5@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 5, UserId = 5, OrganisationEligibleIdentityProviderId = 4 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 6, UserId = 5, OrganisationUserGroupId = 6 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 7, UserId = 5, OrganisationUserGroupId = 7 });
      #endregion
      #region User6 Admin Group
      // User 6
      dataContext.Party.Add(new Party { Id = 11, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 9, PartyId = 11, OrganisationId = 2, FirstName = "UserFN6", LastName = "UserLN6" });
      dataContext.User.Add(new User { Id = 6, PartyId = 11, UserName = "user6@mail.com" });
      dataContext.UserIdentityProvider.Add(new UserIdentityProvider { Id = 6, UserId = 6, OrganisationEligibleIdentityProviderId = 4 });
      dataContext.UserGroupMembership.Add(new UserGroupMembership { Id = 8, UserId = 6, OrganisationUserGroupId = 6 });
      #endregion
      #endregion

      await dataContext.SaveChangesAsync();
    }
  }
}
