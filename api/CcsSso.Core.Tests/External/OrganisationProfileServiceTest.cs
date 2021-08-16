using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Service.External;
using CcsSso.Shared.Cache.Contracts;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class OrganisationProfileServiceTest
  {
    public class CreateOrganisation
    {

      public static IEnumerable<object[]> CorrectOrgData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "Org3", "Org3@web.com", "street3", "local3", "region3", "pcode3", "GB", true, true, true)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectOrgData))]
      public async Task CreateOrganisationSuccessfully_WhenCorrectData(OrganisationProfileInfo organisationInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgService = OrganisationProfileService(dataContext);

          var createdOrgId = await orgService.CreateOrganisationAsync(organisationInfo);

          var createdOrganisation = await dataContext.Organisation
            .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
            .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == createdOrgId);

          Assert.NotNull(createdOrganisation);
          Assert.Equal(organisationInfo.Identifier.LegalName, createdOrganisation.LegalName);
          Assert.Equal(organisationInfo.Identifier.Uri, createdOrganisation.OrganisationUri);
          Assert.Equal(organisationInfo.Detail.IsSme, createdOrganisation.IsSme);
          Assert.Equal(organisationInfo.Detail.IsVcse, createdOrganisation.IsVcse);

          Assert.NotEmpty(createdOrganisation.Party.ContactPoints);
          var physicalContactPoint = createdOrganisation.Party.ContactPoints.FirstOrDefault();
          Assert.Equal(organisationInfo.Address.StreetAddress, physicalContactPoint.ContactDetail.PhysicalAddress.StreetAddress);
          Assert.Equal(organisationInfo.Address.Region, physicalContactPoint.ContactDetail.PhysicalAddress.Region);
          Assert.Equal(organisationInfo.Address.Locality, physicalContactPoint.ContactDetail.PhysicalAddress.Locality);
          Assert.Equal(organisationInfo.Address.PostalCode, physicalContactPoint.ContactDetail.PhysicalAddress.PostalCode);
          Assert.Equal(organisationInfo.Address.CountryCode, physicalContactPoint.ContactDetail.PhysicalAddress.CountryCode);
        });
      }

      public static IEnumerable<object[]> InvalidOrganisationData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "Org3", "Org3@web.com", "street3", "local3", "region3", "pcode3", "ccode3", true, true, true, true),
                  ErrorConstant.ErrorInvalidIdentifier
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", null, "Org3@web.com", "street3", "local3", "region3", "pcode3", "GB", true, true, true),
                  ErrorConstant.ErrorInvalidOrganisationName
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "", "Org3@web.com", "street3", "local3", "region3", "pcode3", "GB", true, true, true),
                  ErrorConstant.ErrorInvalidOrganisationName
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", " ", "Org3@web.com", "street3", "local3", "region3", "pcode3", "GB", true, true, true),
                  ErrorConstant.ErrorInvalidOrganisationName
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "Org3", "uri", "", "local3", "region3", "pcode3", "", true, false, true),
                  ErrorConstant.ErrorInsufficientDetails
                }
            };

      [Theory]
      [MemberData(nameof(InvalidOrganisationData))]
      public async Task ThrowsException_WhenInCorrectDataForCreation(OrganisationProfileInfo organisationInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgService = OrganisationProfileService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => orgService.CreateOrganisationAsync(organisationInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> AlreadyExistsOrganisationData =>
            new List<object[]>
            {
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("1", "orgname", "Org3@web.com", "street3", "local3", "region3", "pcode3", "GB", true, true, true)
                }
            };

      [Theory]
      [MemberData(nameof(AlreadyExistsOrganisationData))]
      public async Task ThrowsException_WhenOrgExists(OrganisationProfileInfo organisationInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgService = OrganisationProfileService(dataContext);

          await Assert.ThrowsAsync<ResourceAlreadyExistsException>(() => orgService.CreateOrganisationAsync(organisationInfo));
        });
      }
    }

    public class GetOrganisation
    {
      public static IEnumerable<object[]> ExpectedOrgData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationProfileInfo("1", "Org1", "Org1Uri", "street", "locality", "region", "postalcode", "countrycode",
                    true, true, true)
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedOrgData))]
      public async Task ReturnsCorrectOrganisationData_WhenExists(string ciiOrganisationId, OrganisationProfileInfo expectedOrganisationInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          Mock<ICiiService> mockCiiService = new Mock<ICiiService>();
          mockCiiService.Setup(s => s.GetOrgsAsync(It.IsAny<string>(),""))
          .ReturnsAsync(new CiiDto[]
          {
            new CiiDto
            {
              Identifier = new CiiIdentifier
              {
                LegalName = expectedOrganisationInfo.Identifier.LegalName,
                Uri = expectedOrganisationInfo.Identifier.Uri
              },
              Address = new CiiAddress
              {
                StreetAddress = expectedOrganisationInfo.Address.StreetAddress,
                Region = expectedOrganisationInfo.Address.Region,
                Locality = expectedOrganisationInfo.Address.Locality,
                PostalCode = expectedOrganisationInfo.Address.PostalCode,
                CountryName = expectedOrganisationInfo.Address.CountryCode,
              }
            }
          });
          var orgService = OrganisationProfileService(dataContext, mockCiiService);

          var result = await orgService.GetOrganisationAsync(ciiOrganisationId);

          Assert.NotNull(result);
          Assert.Equal(expectedOrganisationInfo.Identifier.LegalName, result.Identifier.LegalName);
          Assert.Equal(expectedOrganisationInfo.Identifier.Uri, result.Identifier.Uri);
          Assert.Equal(expectedOrganisationInfo.Detail.IsActive, result.Detail.IsActive);
          Assert.Equal(expectedOrganisationInfo.Detail.IsSme, result.Detail.IsSme);
          Assert.Equal(expectedOrganisationInfo.Detail.IsVcse, result.Detail.IsVcse);

          Assert.Equal(expectedOrganisationInfo.Address.StreetAddress, result.Address.StreetAddress);
          Assert.Equal(expectedOrganisationInfo.Address.Region, result.Address.Region);
          Assert.Equal(expectedOrganisationInfo.Address.Locality, result.Address.Locality);
          Assert.Equal(expectedOrganisationInfo.Address.PostalCode, result.Address.PostalCode);
          Assert.Equal(expectedOrganisationInfo.Address.CountryCode, result.Address.CountryCode);

        });
      }

      [Theory]
      [InlineData("2")]
      [InlineData("3")]
      public async Task ThrowsException_WhenNotExsistsOrDeleted(string ciiOrganisationId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = OrganisationProfileService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationAsync(ciiOrganisationId));

        });
      }
    }

    public class UpdateOrganisation
    {
      public static IEnumerable<object[]> CorrectOrgData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationProfileInfo("1", "Org1up", "Org1up@web.com", "street1up", "local1up", "region1up", "pcode1up", "GB", true, true, true)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectOrgData))]
      public async Task UpdateOrganisationSuccessfully_WhenCorrectData(string ciiOrganisationId, OrganisationProfileInfo organisationInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgService = OrganisationProfileService(dataContext);

          await orgService.UpdateOrganisationAsync(ciiOrganisationId, organisationInfo);

          var updatedOrganisation = await dataContext.Organisation
           .Include(o => o.Party).ThenInclude(p => p.ContactPoints)
           .ThenInclude(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
           .FirstOrDefaultAsync(o => !o.IsDeleted && o.CiiOrganisationId == ciiOrganisationId);

          Assert.NotNull(updatedOrganisation);
          Assert.Equal(organisationInfo.Identifier.LegalName, updatedOrganisation.LegalName);
          Assert.Equal(organisationInfo.Identifier.Uri, updatedOrganisation.OrganisationUri);
          Assert.Equal(organisationInfo.Detail.IsActive, updatedOrganisation.IsActivated);
          Assert.Equal(organisationInfo.Detail.IsSme, updatedOrganisation.IsSme);
          Assert.Equal(organisationInfo.Detail.IsVcse, updatedOrganisation.IsVcse);

          Assert.NotEmpty(updatedOrganisation.Party.ContactPoints);
          var physicalContactPoint = updatedOrganisation.Party.ContactPoints.FirstOrDefault();
          Assert.Equal(organisationInfo.Address.StreetAddress, physicalContactPoint.ContactDetail.PhysicalAddress.StreetAddress);
          Assert.Equal(organisationInfo.Address.Region, physicalContactPoint.ContactDetail.PhysicalAddress.Region);
          Assert.Equal(organisationInfo.Address.Locality, physicalContactPoint.ContactDetail.PhysicalAddress.Locality);
          Assert.Equal(organisationInfo.Address.PostalCode, physicalContactPoint.ContactDetail.PhysicalAddress.PostalCode);
          Assert.Equal(organisationInfo.Address.CountryCode, physicalContactPoint.ContactDetail.PhysicalAddress.CountryCode);
        });
      }

      public static IEnumerable<object[]> InvalidOrganisationData =>
           new List<object[]>
           {
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "Org3", "Org3@web.com", "street3", "local3", "region3", "pcode3", "ccode3", true, true, true, true),
                  ErrorConstant.ErrorInvalidIdentifier
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", null, "Org3@web.com", "street3", "local3", "region3", "pcode3", "ccode3", true, true, true),
                  ErrorConstant.ErrorInvalidOrganisationName
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "", "Org3@web.com", "street3", "local3", "region3", "pcode3", "ccode3", true, true, true),
                  ErrorConstant.ErrorInvalidOrganisationName
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", " ", "Org3@web.com", "street3", "local3", "region3", "pcode3", "ccode3", true, true, true),
                  ErrorConstant.ErrorInvalidOrganisationName
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "Org3", "uri", "", "local3", "region3", "pcode3", "GB", true, false, true),
                  ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                  DtoHelper.GetOrganisationProfileInfo("3", "Org3", "uri", "street3", "local3", "region3", "pcode3", "ccode3", true, false, true),
                  ErrorConstant.ErrorInvalidCountryCode
                }
           };

      [Theory]
      [MemberData(nameof(InvalidOrganisationData))]
      public async Task ThrowsException_WhenInCorrectDataForUpdate(OrganisationProfileInfo organisationInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgService = OrganisationProfileService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => orgService.CreateOrganisationAsync(organisationInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> UpdateOrgDataForInvalidOrganisation =>
           new List<object[]>
           {
                new object[]
                {
                  "2",
                  DtoHelper.GetOrganisationProfileInfo("2", "Org1up", "Org1up@web.com", "street1up", "local1up", "region1up", "pcode1up", "GB", true, true, true)
                }
           };

      [Theory]
      [MemberData(nameof(UpdateOrgDataForInvalidOrganisation))]
      public async Task ThrowsException_WhenNotExsistsOrDeleted(string ciiOrganisationId, OrganisationProfileInfo organisationInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = OrganisationProfileService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateOrganisationAsync(ciiOrganisationId, organisationInfo));
        });
      }
    }

    public static OrganisationProfileService OrganisationProfileService(IDataContext dataContext, Mock<ICiiService> mockCiiService = null)
    {
      Mock<ILocalCacheService> mockLocalCacheService = new();
      mockLocalCacheService.Setup(s => s.GetOrSetValueAsync<List<ContactPointReason>>("CONTACT_POINT_REASONS", It.IsAny<Func<Task<List<ContactPointReason>>>>(), It.IsAny<int>()))
        .ReturnsAsync(new List<ContactPointReason> {
          new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" },
          new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" },
          new ContactPointReason { Id = 3, Name = ContactReasonType.Site, Description = "Billing" },
          new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" },
          new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" }
        });
      IContactsHelperService contactsHelperService = new ContactsHelperService(dataContext, mockLocalCacheService.Object);
      Mock<ICcsSsoEmailService> mockCcsSsoEmailService = new Mock<ICcsSsoEmailService>();
      mockCiiService ??= new Mock<ICiiService>();
      Mock<IAdaptorNotificationService> mockAdapterNotificationService = new Mock<IAdaptorNotificationService>();

      var mockWrapperCacheService = new Mock<IWrapperCacheService>();

       var service = new OrganisationProfileService(dataContext, contactsHelperService, mockCcsSsoEmailService.Object,
        mockCiiService.Object, mockAdapterNotificationService.Object, mockWrapperCacheService.Object, mockLocalCacheService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = PartyTypeName.InternalOrgnaisation });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
      dataContext.PartyType.Add(new PartyType { Id = 4, PartyTypeName = PartyTypeName.ExternalOrgnaisation });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", LegalName = "Org1", OrganisationUri = "Org1Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 1, ContactDetailId = 1, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 1 });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });

      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true, IsDeleted = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 2, IdentityProviderId = 1 });

      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "email@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 2 });

      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "PesronFN2", LastName = "LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 3, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 3, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 3, IsDeleted = true });

      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, OrganisationEligibleIdentityProviderId = 1, PartyId = 5, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 2 });

      await dataContext.SaveChangesAsync();
    }
  }
}
