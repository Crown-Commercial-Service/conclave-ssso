using CcsSso.Core.Domain.Jobs;
using CcsSso.Core.Jobs;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
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
        var dateTimeMock = new Mock<IDataTimeService>();
        dateTimeMock.Setup(d => d.GetUTCNow()).Returns(new DateTime(2021, 01, 01, 1, 0, 0));
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var appSettings = new AppSettings()
        {
          ScheduleJobSettings = new ScheduleJobSettings()
          {
            OrganizationRegistrationExpiredThresholdInMinutes = 10
          }
        };
        var orjDeleteJob = await GetOrganisationDeleteForInactiveRegistrationServiceAsync(dataContext, dateTimeMock.Object, appSettings, httpClientFactoryMock.Object);
        var results = await orjDeleteJob.GetExpiredOrganisationRegistrationsIdsAsync();
        Assert.NotNull(results);
      });
    }

    private static async Task<OrganisationDeleteForInactiveRegistrationJob> GetOrganisationDeleteForInactiveRegistrationServiceAsync(IDataContext dataContext, IDataTimeService dateTimeService,
      AppSettings appSettings, IHttpClientFactory httpClientFactory)
    {
      await SetupTestDataAsync(dataContext, dateTimeService);
      return new OrganisationDeleteForInactiveRegistrationJob(dataContext, dateTimeService, appSettings, httpClientFactory);
    }

    private static async Task SetupTestDataAsync(IDataContext dataContext, IDataTimeService dateTimeService)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = "INTERNAL_ORGANISATION" });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = "OTHER", Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = "SHIPPING", Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = "BILLING", Description = "Billing" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", OrganisationUri = "Org1Uri", RightToBuy = true, IsActivated = false, IsDeleted = false, CreatedOnUtc = dateTimeService.GetUTCNow().AddDays(-1) });

      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true, IsActivated = false, IsDeleted = false, CreatedOnUtc = dateTimeService.GetUTCNow().AddDays(-1) });

      dataContext.Party.Add(new Party { Id = 33, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 3, PartyId = 2, CiiOrganisationId = "3", OrganisationUri = "Org3Uri", RightToBuy = true, IsActivated = true, IsDeleted = true, CreatedOnUtc = dateTimeService.GetUTCNow().AddHours(-38) });

      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 1, VirtualAddressTypeId = 1, VirtualAddressValue = "email@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 1, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 1 });

      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN2", LastName = "LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 2, IsDeleted = true });

      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, IdentityProviderId = 1, PartyId = 5, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 1 });
      await dataContext.SaveChangesAsync();
    }
  }
}
