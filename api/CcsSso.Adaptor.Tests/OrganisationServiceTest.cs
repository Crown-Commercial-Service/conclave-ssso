using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.DbDomain.Entity;
using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Cii;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Adaptor.Service;
using CcsSso.Adaptor.Tests.Infrastructure;
using CcsSso.Shared.Domain.Excecptions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Adaptor.Tests
{
  public class OrganisationServiceTest
  {

    public class GetOrganisation
    {
      public static IEnumerable<object[]> CorrectOrgGetData =>
            new List<object[]>
            {
                new object[]
                {
                  "CiiOrg1",
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new WrapperOrganisationResponse{
                    Identifier = new OrganisationIdentifier
                    {
                      LegalName = "Organisation One",
                      Uri = "orgone.com"
                    },
                    Address = new OrganisationAddress
                    {
                      StreetAddress = "Main Street, London",
                      CountryCode = "UK"
                    },
                    Detail = new OrganisationResponseDetail
                    {
                      SupplierBuyerType = 1,
                      IsSme = true
                    }
                  },
                  new WrapperOrganisationContactInfoList
                  {
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "CiiOrg1"
                    },
                    ContactPoints = new List<WrapperContactPoint>
                    {
                      new WrapperContactPoint
                      {
                        ContactPointId = 1,
                        Contacts = new List<WrapperContactResponse>
                        {
                          new WrapperContactResponse { ContactId = 1, ContactType = ContactType.Email, ContactValue = "one@mail.com" },
                          new WrapperContactResponse { ContactId = 2, ContactType = ContactType.Phone, ContactValue = "+156842741" },
                          new WrapperContactResponse { ContactId = 3, ContactType = ContactType.Fax, ContactValue = "+9568743" },
                        }
                      },
                      new WrapperContactPoint
                      {
                        ContactPointId = 2,
                        Contacts = new List<WrapperContactResponse>
                        {
                          new WrapperContactResponse { ContactId = 4, ContactType = ContactType.Url, ContactValue = "one.com" },
                          new WrapperContactResponse { ContactId = 5, ContactType = ContactType.Phone, ContactValue = "+256842742" },
                        }
                      }
                    }
                  },
                  new Dictionary<string, object>
                  {
                    { "OrganisationName", "Organisation One" },
                    { "OrganisationUri", "orgone.com" },
                    { "StreetAddress", "Main Street, London" },
                    { "CountryCode", "UK" },
                    { "SupplierBuyerType", 1 },
                    { "IsSme", true },
                    { "Mobile", new List<string> { "+156842741", "+256842742" } },
                    { "WebAddress", new List<string> { "one.com" } }
                  }
                },
                new object[] // No Address, No contacts
                {
                  "CiiOrg2",
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new WrapperOrganisationResponse{
                    Identifier = new OrganisationIdentifier
                    {
                      LegalName = "Organisation One",
                      Uri = "orgone.com"
                    },
                    Detail = new OrganisationResponseDetail
                    {
                      SupplierBuyerType = 1,
                      IsSme = true
                    }
                  },
                  new WrapperOrganisationContactInfoList
                  {
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "CiiOrg2"
                    },
                    ContactPoints = new List<WrapperContactPoint>{}
                  },
                  new Dictionary<string, object>
                  {
                    { "OrganisationName", "Organisation One" },
                    { "OrganisationUri", "orgone.com" },
                    { "SupplierBuyerType", 1 },
                    { "IsSme", true },
                    { "Mobile", new List<string> { } },
                    { "WebAddress", new List<string> { } }
                  }
                },
            };

      [Theory]
      [MemberData(nameof(CorrectOrgGetData))]
      public async Task ReturnsCorrectResultDictionary_WhenOrgExists(string organisationId, AdaptorRequestContext requestContext,
        WrapperOrganisationResponse wrapperOrganisationResponse, WrapperOrganisationContactInfoList wrapperOrganisationContactInfoList,
        Dictionary<string, object> expectedDataDictionary)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var mockWrapperOrgService = new Mock<IWrapperOrganisationService>();
          var mockWrapperOrgContactService = new Mock<IWrapperOrganisationContactService>();

          mockWrapperOrgService.Setup(s => s.GetOrganisationAsync(organisationId))
          .ReturnsAsync(wrapperOrganisationResponse);

          mockWrapperOrgContactService.Setup(s => s.GetOrganisationContactsAsync(organisationId))
          .ReturnsAsync(wrapperOrganisationContactInfoList);

          var organisationService = OrganisationService(dataContext, requestContext, mockWrapperOrgService, mockWrapperOrgContactService);

          var result = await organisationService.GetOrganisationAsync(organisationId);

          Assert.NotNull(result);
          foreach (var expectedProperty in expectedDataDictionary)
          {
            var propertyValue = result[expectedProperty.Key];
            Assert.NotNull(propertyValue);
            Assert.Equal(expectedProperty.Value, propertyValue);
          }
        });
      }

      public static IEnumerable<object[]> NoConfigurationDataDetails =>
            new List<object[]>
            {
                new object[]
                {
                  "CIIORG1",
                  new AdaptorRequestContext{ ConsumerId = 2 },
                },
            };

      [Theory]
      [MemberData(nameof(NoConfigurationDataDetails))]
      public async Task ThrowsException_WhenConfigurationNotExists(string organisationId, AdaptorRequestContext requestContext)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var organisationService = OrganisationService(dataContext, requestContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => organisationService.GetOrganisationAsync(organisationId));
          Assert.Equal(ErrorConstant.NoConfigurationFound, ex.Message);
        });
      }

      public static IEnumerable<object[]> InCorrectOrgGetData =>
            new List<object[]>
            {
                new object[]
                {
                  "NO_ORG_CIIORG1",
                  new AdaptorRequestContext{ ConsumerId = 1 },
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectOrgGetData))]
      public async Task ThrowsException_WhenOrgNotExists(string organisationId, AdaptorRequestContext requestContext)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var mockWrapperOrgService = new Mock<IWrapperOrganisationService>();

          mockWrapperOrgService.Setup(s => s.GetOrganisationAsync(organisationId))
          .ThrowsAsync(new ResourceNotFoundException());

          var organisationService = OrganisationService(dataContext, requestContext, mockWrapperOrgService);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => organisationService.GetOrganisationAsync(organisationId));
        });
      }
    }

    public static OrganisationService OrganisationService(IDataContext dataContext, AdaptorRequestContext requestContext = null,
      Mock<IWrapperOrganisationService> mockWrapperOrgService = null, Mock<IWrapperOrganisationContactService> mockWrapperOrgContactService = null,
      Mock<IWrapperSiteService> mockWrapperSiteService = null, Mock<ICiiService> mockCiiService = null)
    {
      requestContext ??= new AdaptorRequestContext
      {
        ConsumerId = 1
      };
      var atrributeMappingService = new AttributeMappingService(dataContext, requestContext);
      mockWrapperOrgService ??= new Mock<IWrapperOrganisationService>();
      mockWrapperOrgContactService ??= new Mock<IWrapperOrganisationContactService>();
      mockWrapperSiteService ??= new Mock<IWrapperSiteService>();
      mockCiiService ??= new Mock<ICiiService>();

      var service = new OrganisationService(atrributeMappingService, mockWrapperOrgService.Object, mockWrapperOrgContactService.Object,
        mockWrapperSiteService.Object, mockCiiService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "Digits" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.Organisation, AdapterConsumerId = 1 });
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 2, Name = ConsumerEntityNames.User, AdapterConsumerId = 1 });

      // Adaptor Org attributes
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "OrganisationName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 2, AdapterConsumerEntityId = 1, AttributeName = "OrganisationUri" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 3, AdapterConsumerEntityId = 1, AttributeName = "StreetAddress" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 4, AdapterConsumerEntityId = 1, AttributeName = "CountryCode" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 5, AdapterConsumerEntityId = 1, AttributeName = "SupplierBuyerType" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 6, AdapterConsumerEntityId = 1, AttributeName = "IsSme" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 7, AdapterConsumerEntityId = 1, AttributeName = "Mobile" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 8, AdapterConsumerEntityId = 1, AttributeName = "WebAddress" });

      // Adaptor User attributes
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 9, AdapterConsumerEntityId = 2, AttributeName = "Name" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 10, AdapterConsumerEntityId = 2, AttributeName = "FamilyName" });


      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.OrgProfile });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 2, Name = ConclaveEntityNames.UserProfile });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 3, Name = ConclaveEntityNames.OrgContact });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 4, Name = ConclaveEntityNames.Site });

      // Conclave Org profile attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "Identifier.LegalName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "Identifier.Uri" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 3, ConclaveEntityId = 1, AttributeName = "Address.StreetAddress" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 4, ConclaveEntityId = 1, AttributeName = "Address.CountryCode" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 5, ConclaveEntityId = 1, AttributeName = "Detail.SupplierBuyerType" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 6, ConclaveEntityId = 1, AttributeName = "Detail.IsSme" });

      // Conclave User profile attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 7, ConclaveEntityId = 2, AttributeName = "FirstName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 8, ConclaveEntityId = 2, AttributeName = "LastName" });


      // Conclave User Contacts attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 9, ConclaveEntityId = 3, AttributeName = ContactType.Phone });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 10, ConclaveEntityId = 3, AttributeName = ContactType.Url });

      // Org Mapping [Identifier.LegalName, Identifier.Uri, Address.StreetAddress, Address.CountryCode, Detail.SupplierBuyerType, Detail.IsSme]
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 3, AdapterConsumerEntityAttributeId = 3, ConclaveEntityAttributeId = 3 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 4, AdapterConsumerEntityAttributeId = 4, ConclaveEntityAttributeId = 4 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 5, AdapterConsumerEntityAttributeId = 5, ConclaveEntityAttributeId = 5 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 6, AdapterConsumerEntityAttributeId = 6, ConclaveEntityAttributeId = 6 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 7, AdapterConsumerEntityAttributeId = 7, ConclaveEntityAttributeId = 9 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 8, AdapterConsumerEntityAttributeId = 8, ConclaveEntityAttributeId = 10 });

      // User mapping :- first name, last name
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 9, AdapterConsumerEntityAttributeId = 9, ConclaveEntityAttributeId = 7 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 10, AdapterConsumerEntityAttributeId = 10, ConclaveEntityAttributeId = 8 });

      await dataContext.SaveChangesAsync();
    }
  }
}



