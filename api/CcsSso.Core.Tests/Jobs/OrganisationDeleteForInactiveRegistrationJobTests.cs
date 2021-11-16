using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.JobScheduler;
using CcsSso.Core.JobScheduler.Contracts;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.Jobs
{
  public class OrganisationDeleteForInactiveRegistrationJobTests
  {
    [Fact]
    public async Task ReturnsOrganisationIds()
    {
      await DataContextHelper.ScopeAsync(async dataContext =>
      {
        var dateTimeMock = new Mock<IDateTimeService>();
        dateTimeMock.Setup(d => d.GetUTCNow()).Returns(new DateTime(2021, 01, 01, 1, 0, 0));
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var appSettings = new AppSettings()
        {
          ScheduleJobSettings = new ScheduleJobSettings()
          {
            OrganizationRegistrationExpiredThresholdInMinutes = 10,
            InactiveOrganisationDeletionJobExecutionFrequencyInMinutes = 10
          }
        };
        var orjDeleteJob = await GetOrganisationDeleteForInactiveRegistrationServiceAsync(dataContext, dateTimeMock.Object, appSettings, httpClientFactoryMock.Object);
        var results = await orjDeleteJob.GetExpiredOrganisationIdsAsync();
        Assert.NotNull(results);
      });
    }

    private static async Task<OrganisationDeleteForInactiveRegistrationJob> GetOrganisationDeleteForInactiveRegistrationServiceAsync(IDataContext dataContext, IDateTimeService dateTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
      await SetupTestDataAsync(dataContext);
      //var serviceScopeFactory = new Mock<IServiceScopeFactory>();
      try
      {
        //serviceScopeFactory
        //    .Setup(x => x.CreateScope().ServiceProvider.GetRequiredService<IDataContext>())
        //    .Returns(dataContext);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IDataContext)))
            .Returns(dataContext);

        var serviceScope = new Mock<IServiceScope>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        serviceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(serviceScope.Object);

        var mockCacheInvalidateService = new Mock<ICacheInvalidateService>();
        var mockIdamSupportService = new Mock<IIdamSupportService>();

        serviceProvider
    .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
    .Returns(serviceScopeFactory.Object);
        return new OrganisationDeleteForInactiveRegistrationJob(serviceScopeFactory.Object, dateTimeService, appSettings,
          httpClientFactory, mockCacheInvalidateService.Object, mockIdamSupportService.Object);
      }
      catch(Exception e)
      {

      }
      return null;
    }

    private static async Task SetupTestDataAsync(IDataContext dataContext)
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
