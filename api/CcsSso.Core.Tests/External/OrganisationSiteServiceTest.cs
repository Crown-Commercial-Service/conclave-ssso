using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Exceptions;
using CcsSso.Service.External;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class OrganisationSiteServiceTest
  {
    public class CreateSite
    {

      public static IEnumerable<object[]> CorrectOrgData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo("Org1Site2", "street3", "local3", "region3", "pcode3", "ccode3")
                }
            };

      [Theory]
      [MemberData(nameof(CorrectOrgData))]
      public async Task CreateOrganisationSiteSuccessfully_WhenCorrectData(string ciiOrganisationId, OrganisationSiteInfo organisationSiteInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgSiteService = OrganisationSiteService(dataContext);

          var createdOrgId = await orgSiteService.CreateSiteAsync(ciiOrganisationId, organisationSiteInfo);

          var createdSiteContactpoint = await dataContext.ContactPoint
            .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
            .Include(cp => cp.ContactPointReason)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == createdOrgId);

          Assert.NotNull(createdSiteContactpoint);
          Assert.True(createdSiteContactpoint.IsSite);
          Assert.Equal(organisationSiteInfo.SiteName, createdSiteContactpoint.SiteName);

          Assert.Equal(organisationSiteInfo.StreetAddress, createdSiteContactpoint.ContactDetail.PhysicalAddress.StreetAddress);
          Assert.Equal(organisationSiteInfo.Region, createdSiteContactpoint.ContactDetail.PhysicalAddress.Region);
          Assert.Equal(organisationSiteInfo.Locality, createdSiteContactpoint.ContactDetail.PhysicalAddress.Locality);
          Assert.Equal(organisationSiteInfo.PostalCode, createdSiteContactpoint.ContactDetail.PhysicalAddress.PostalCode);
          Assert.Equal(organisationSiteInfo.CountryCode, createdSiteContactpoint.ContactDetail.PhysicalAddress.CountryCode);
        });
      }

      public static IEnumerable<object[]> InvalidOrganisationSiteData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo(null, "street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo("","street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo(" ", "street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo("site1", "", "", "", "", ""),
                  ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo("site1", " ", " ", " ", " ", " "),
                  ErrorConstant.ErrorInsufficientDetails
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetOrganisationSiteInfo("site1", null, null, null, null, null),
                  ErrorConstant.ErrorInsufficientDetails
                },
            };

      [Theory]
      [MemberData(nameof(InvalidOrganisationSiteData))]
      public async Task ThrowsException_WhenInCorrectDataForCreation(string ciiOrganisationId, OrganisationSiteInfo organisationSiteInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgSiteService = OrganisationSiteService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => orgSiteService.CreateSiteAsync(ciiOrganisationId, organisationSiteInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> InvalidOrganisationSiteDataWithInvalidOrg =>
            new List<object[]>
            {
                new object[]
                {
                  "4",
                  DtoHelper.GetOrganisationSiteInfo("Org3Site1", "street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
            };

      [Theory]
      [MemberData(nameof(InvalidOrganisationSiteDataWithInvalidOrg))]
      public async Task ThrowsResourceNotFoundException_WhenInCorrectOrganisation(string ciiOrganisationId, OrganisationSiteInfo organisationSiteInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgSiteService = OrganisationSiteService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => orgSiteService.CreateSiteAsync(ciiOrganisationId, organisationSiteInfo));
        });
      }
    }

    public class DeleteSite
    {
      [Theory]
      [InlineData("1", 2)]
      public async Task DeleteOrganisationContactSuccessfully(string ciiOrganisationId, int deletingSiteId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          await orgSiteService.DeleteSiteAsync(ciiOrganisationId, deletingSiteId);

          var deletedContact = await dataContext.ContactPoint.FirstOrDefaultAsync(c => c.Id == deletingSiteId);

          Assert.True(deletedContact.IsDeleted);
        });
      }

      [Theory]
      [InlineData("1", 1)]
      [InlineData("2", 4)]
      [InlineData("2", 5)]
      [InlineData("3", 5)]
      public async Task ThrowsException_WhenContactDoesntExists(string ciiOrganisationId, int deletingSiteId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgSiteService = OrganisationSiteService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => orgSiteService.DeleteSiteAsync(ciiOrganisationId, deletingSiteId));
        });
      }
    }

    public class GetOrganisationSites
    {

      [Theory]
      [InlineData("1", 1)]
      [InlineData("2", 0)]
      [InlineData("3", 0)]
      public async Task ReturnsCorrectOrganisationSiteList_WhenExists(string ciiOrganisationId, int expectedCount)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          var result = await orgSiteService.GetOrganisationSitesAsync(ciiOrganisationId);

          Assert.NotNull(result);
          Assert.Equal(expectedCount, result.Sites.Count);
        });
      }

      [Theory]
      [InlineData("4")]
      public async Task ThrowsException_WhenNotExsistsOrDeleted(string ciiOrganisationId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => orgSiteService.GetOrganisationSitesAsync(ciiOrganisationId));

        });
      }
    }

    public class GetSite
    {
      public static IEnumerable<object[]> ExpectedOrgSiteData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetOrganisationSiteInfo("Org1Site1", "streetsite1", "localitysite1", string.Empty, "postalcodesite1", "countrycodesite1")
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedOrgSiteData))]
      public async Task ReturnsCorrectOrganisationSiteData_WhenExists(string ciiOrganisationId, int siteId, OrganisationSiteInfo expectedOrganisationSiteInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          var result = await orgSiteService.GetSiteAsync(ciiOrganisationId, siteId);

          Assert.NotNull(result);
          Assert.Equal(expectedOrganisationSiteInfo.SiteName, result.SiteName);

          Assert.Equal(expectedOrganisationSiteInfo.StreetAddress, result.StreetAddress);
          Assert.Equal(expectedOrganisationSiteInfo.Region, result.Region);
          Assert.Equal(expectedOrganisationSiteInfo.Locality, result.Locality);
          Assert.Equal(expectedOrganisationSiteInfo.PostalCode, result.PostalCode);
          Assert.Equal(expectedOrganisationSiteInfo.CountryCode, result.CountryCode);

        });
      }

      [Theory]
      [InlineData("1", 1)]
      [InlineData("2", 2)]
      [InlineData("3", 2)]
      [InlineData("4", 2)]
      public async Task ThrowsException_WhenNotExsistsOrDeleted(string ciiOrganisationId, int siteId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => orgSiteService.GetSiteAsync(ciiOrganisationId, siteId));

        });
      }
    }

    public class UpdateSite
    {
      public static IEnumerable<object[]> CorrectOrgSiteData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetOrganisationSiteInfo("Org1Site1up", "street3up", "local3up", "region3up", "pcode3up", "ccode3up")
                }
            };

      [Theory]
      [MemberData(nameof(CorrectOrgSiteData))]
      public async Task UpdateOrganisationSiteSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, OrganisationSiteInfo organisationSiteInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          await orgSiteService.UpdateSiteAsync(ciiOrganisationId, siteId, organisationSiteInfo);

          var updatedSiteContactpoint = await dataContext.ContactPoint
            .Include(cp => cp.ContactDetail).ThenInclude(cd => cd.PhysicalAddress)
            .Include(cp => cp.ContactPointReason)
            .FirstOrDefaultAsync(o => !o.IsDeleted && o.Id == siteId);

          Assert.NotNull(updatedSiteContactpoint);
          Assert.True(updatedSiteContactpoint.IsSite);
          Assert.Equal(organisationSiteInfo.SiteName, updatedSiteContactpoint.SiteName);

          Assert.Equal(organisationSiteInfo.StreetAddress, updatedSiteContactpoint.ContactDetail.PhysicalAddress.StreetAddress);
          Assert.Equal(organisationSiteInfo.Region, updatedSiteContactpoint.ContactDetail.PhysicalAddress.Region);
          Assert.Equal(organisationSiteInfo.Locality, updatedSiteContactpoint.ContactDetail.PhysicalAddress.Locality);
          Assert.Equal(organisationSiteInfo.PostalCode, updatedSiteContactpoint.ContactDetail.PhysicalAddress.PostalCode);
          Assert.Equal(organisationSiteInfo.CountryCode, updatedSiteContactpoint.ContactDetail.PhysicalAddress.CountryCode);
        });
      }

      public static IEnumerable<object[]> InvalidOrganisationSiteData =>
           new List<object[]>
           {
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetOrganisationSiteInfo(null, "street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetOrganisationSiteInfo("", "street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetOrganisationSiteInfo(" ", "street3", "local3", "region3", "pcode3", "ccode3"),
                  ErrorConstant.ErrorInvalidSiteName
                },
           };

      [Theory]
      [MemberData(nameof(InvalidOrganisationSiteData))]
      public async Task ThrowsException_WhenInCorrectDataForUpdate(string ciiOrganisationId, int siteId, OrganisationSiteInfo organisationSiteInfo, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var orgSiteService = OrganisationSiteService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => orgSiteService.UpdateSiteAsync(ciiOrganisationId, siteId, organisationSiteInfo));
          Assert.Equal(expectedError, ex.Message);
        });
      }

      public static IEnumerable<object[]> UpdateOrgSiteDataForInvalidOrganisation =>
           new List<object[]>
           {
             new object[]
                {
                  "1",
                  1,
                  DtoHelper.GetOrganisationSiteInfo("Org1Site1", "street3", "local3", "region3", "pcode3", "ccode3"),
                },
                new object[]
                {
                  "2",
                  2,
                  DtoHelper.GetOrganisationSiteInfo("Org1Site1", "street3", "local3", "region3", "pcode3", "ccode3"),
                },
                new object[]
                {
                  "3",
                  2,
                  DtoHelper.GetOrganisationSiteInfo("Org1Site1", "street3", "local3", "region3", "pcode3", "ccode3"),
                },
                new object[]
                {
                  "4",
                  2,
                  DtoHelper.GetOrganisationSiteInfo("Org1Site1", "street3", "local3", "region3", "pcode3", "ccode3"),
                }
           };

      [Theory]
      [MemberData(nameof(UpdateOrgSiteDataForInvalidOrganisation))]
      public async Task ThrowsException_WhenNotExsistsOrDeleted(string ciiOrganisationId, int siteId, OrganisationSiteInfo organisationSiteInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var orgSiteService = OrganisationSiteService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => orgSiteService.UpdateSiteAsync(ciiOrganisationId, siteId, organisationSiteInfo));
        });
      }
    }

    public static OrganisationSiteService OrganisationSiteService(IDataContext dataContext)
    {
      IContactsHelperService contactsHelperService = new ContactsHelperService(dataContext);
      var service = new OrganisationSiteService(dataContext, contactsHelperService);
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

      //Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", LegalName = "Org1", OrganisationUri = "Org1Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });
      //Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 1, ContactDetailId = 1, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 1 });
      //Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 2, ContactDetailId = 2, StreetAddress = "streetsite1", Locality = "localitysite1", PostalCode = "postalcodesite1", CountryCode = "countrycodesite1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 1, PartyTypeId = 1, IsSite = true, SiteName = "Org1Site1", ContactPointReasonId = 2, ContactDetailId = 2 });


      //Org2
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true, IsDeleted = true });
      //Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 3, ContactDetailId = 3, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 3 });
      //Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 4, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 4, ContactDetailId = 4, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 2, PartyTypeId = 1, IsSite = true, SiteName = "Org2Site1", IsDeleted = true, ContactPointReasonId = 1, ContactDetailId = 4 });

      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 5, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 5, VirtualAddressTypeId = 1, VirtualAddressValue = "email@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 5, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 5, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 5 });

      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "PesronFN2", LastName = "LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 6, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 6, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 6, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 6, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 6, IsDeleted = true });

      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, IdentityProviderId = 1, PartyId = 5, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 7, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 6 });

      //Org3
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 3, PartyId = 6, CiiOrganisationId = "3", LegalName = "Org3", OrganisationUri = "Org3Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });

      await dataContext.SaveChangesAsync();
    }
  }
}
