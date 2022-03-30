using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.DbDomain.Entity;
using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Adaptor.Service;
using CcsSso.Adaptor.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Adaptor.Tests
{
  public class AttributeMappingServiceTest
  {

    public class GetMappedAttributeDictionary
    {
      public static IEnumerable<object[]> AttrbuteMappingDictionaryData =>
            new List<object[]>
            {
                new object[]
                {
                  "User",
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new Dictionary<string, Dictionary<string, string>>
                  {
                    { "UserProfile",
                      new Dictionary<string, string>
                      {
                        { "FirstName", "Name" },
                        { "LastName", "FamilyName" },
                        { "Detail.RolePermissionInfo", "Roles" },
                      }
                    },
                    { "OrgProfile",
                      new Dictionary<string, string>
                      {
                        { "Identifier.LegalName", "OrganisationName" }
                      }
                    }
                  }
                },
                new object[]
                {
                  "Organisation",
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new Dictionary<string, Dictionary<string, string>>
                  {
                    { "OrgProfile",
                      new Dictionary<string, string>
                      {
                        { "Identifier.LegalName", "OrganisationName" },
                        { "Identifier.Uri", "OrganisationUri" },
                      }
                    }
                  }
                },
            };

      [Theory]
      [MemberData(nameof(AttrbuteMappingDictionaryData))]
      public async Task ReturnsCorrectDictionary_WhenExsists(string consumerEntityName, AdaptorRequestContext requestContext,
        Dictionary<string, Dictionary<string, string>> expectedMappings)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = AttributeMappingService(dataContext, requestContext);

          var result = await contactService.GetMappedAttributeDictionaryAsync(consumerEntityName);

          Assert.NotNull(result);
          foreach (var expectedMappingEntity in expectedMappings)
          {
            var resultMappingEntity = result[expectedMappingEntity.Key];
            Assert.NotNull(resultMappingEntity);
            foreach (var expectedMappingAttribute in expectedMappingEntity.Value)
            {
              var resultAttribute = resultMappingEntity[expectedMappingAttribute.Key];
              Assert.NotNull(resultAttribute);
              Assert.Equal(expectedMappingAttribute.Value, resultAttribute);
            }
          }
        });
      }
    }

    public class GetMappedDataDictionary
    {
      public static IEnumerable<object[]> AttributeDataMappingDetails =>
            new List<object[]>
            {
                new object[]
                {
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new WrapperUserResponse
                  {
                    UserName = "user1@mail.com",
                    FirstName = "UserFN",
                    LastName = "UserLN",
                    OrganisationId = "CiiOrgId1",
                    Title = "",
                    Detail = new UserResponseDetail
                    {
                      Id = 1,
                      IdentityProviders = new List<UserIdentityProvider> { new UserIdentityProvider { IdentityProviderId= 1, IdentityProviderDisplayName= "Username and Password" } },
                      RolePermissionInfo = new List<RolePermissionInfo>
                      {
                        new RolePermissionInfo
                        {
                          RoleId = 1,
                          RoleKey = "ADMIN_ROLE",
                          RoleName = "Admin Role"
                        },
                        new RolePermissionInfo
                        {
                          RoleId = 2,
                          RoleKey = "USER_ROLE",
                          RoleName = "User Role"
                        }
                      }
                    }
                  },
                  new Dictionary<string, string>
                  {
                    { "FirstName", "Name" },
                    { "LastName", "FamilyName" },
                    { "Detail.RolePermissionInfo", "Roles" },
                  },
                  new Dictionary<string, object>
                  {
                    { "Name", "UserFN" },
                    { "FamilyName", "UserLN" },
                    { "Roles", new List<RolePermissionInfo>
                      {
                        new RolePermissionInfo
                        {
                          RoleId = 1,
                          RoleKey = "ADMIN_ROLE",
                          RoleName = "Admin Role"
                        },
                        new RolePermissionInfo
                        {
                          RoleId = 2,
                          RoleKey = "USER_ROLE",
                          RoleName = "User Role"
                        }
                      }
                    },
                  }
                },
                new object[]
                {
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new WrapperOrganisationResponse{
                    Identifier = new OrganisationIdentifier
                    {
                      LegalName = "Organisation One",
                      Uri = "orgone.com"
                    }
                  },
                  new Dictionary<string, string>
                  {
                    { "Identifier.LegalName", "OrganisationName" },
                    { "Identifier.Uri", "OrganisationUri" }
                  },
                  new Dictionary<string, object>
                  {
                    { "OrganisationName", "Organisation One" },
                    { "OrganisationUri", "orgone.com" },
                  }
                },
            };

      [Theory]
      [MemberData(nameof(AttributeDataMappingDetails))]
      public async Task ReturnsCorrectAttributeDataMappingDictionary(AdaptorRequestContext requestContext,
        object dataObject, Dictionary<string, string> attributeMappings, Dictionary<string, object> expectedDataMappings)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = AttributeMappingService(dataContext, requestContext);

          var result = contactService.GetMappedDataDictionary(dataObject, attributeMappings);

          Assert.NotNull(result);
          foreach (var expectedMappingEntity in expectedDataMappings)
          {
            var resultMappingEntity = result[expectedMappingEntity.Key];
            Assert.NotNull(resultMappingEntity);
            if (expectedMappingEntity.Key == "Roles")
            {
              var expectedRoles = expectedMappingEntity.Value as List<RolePermissionInfo>;
              var actualRoles = resultMappingEntity as List<RolePermissionInfo>;
              Assert.Equal(expectedRoles.Count, actualRoles.Count);
            }
            else
            {
              Assert.Equal(expectedMappingEntity.Value, resultMappingEntity);
            }
          }
        });
      }
    }

    public class GetMappedContactDataDictionaryFromContactPoints
    {
      public static IEnumerable<object[]> AttributeDataMappingDetails =>
            new List<object[]>
            {
                new object[]
                {
                  new AdaptorRequestContext{ ConsumerId = 1 },
                  new List<WrapperContactPoint>
                  {
                    new WrapperContactPoint
                    {
                      ContactPointId = 1,
                      Contacts = new List<WrapperContactResponse>
                      {
                        new WrapperContactResponse { ContactId = 1, ContactType = ContactType.Email, ContactValue = "one@mail.com" },
                        new WrapperContactResponse { ContactId = 2, ContactType = ContactType.Phone, ContactValue = "+5684274" },
                        new WrapperContactResponse { ContactId = 3, ContactType = ContactType.Fax, ContactValue = "+9568743" },
                      }
                    },
                    new WrapperContactPoint
                    {
                      ContactPointId = 2,
                      Contacts = new List<WrapperContactResponse>
                      {
                        new WrapperContactResponse { ContactId = 4, ContactType = ContactType.Url, ContactValue = "one.com" },
                        new WrapperContactResponse { ContactId = 5, ContactType = ContactType.Email, ContactValue = "two@mail.com" },
                      }
                    }
                  },
                  new Dictionary<string, string>
                  {
                    { ContactType.Email, "Email" },
                    { ContactType.Phone, "Mobile" },
                    { ContactType.Fax, "Fax" },
                    { ContactType.Url, "WebAddress" },
                  },
                  new Dictionary<string, object>
                  {
                    { "Email", new List<string>{ "one@mail.com", "two@mail.com" } },
                    { "Mobile", new List<string>{ "+5684274", } },
                    { "Fax", new List<string>{ "+9568743", } },
                    { "WebAddress", new List<string>{ "one.com", } },
                  }
                },
            };

      [Theory]
      [MemberData(nameof(AttributeDataMappingDetails))]
      public async Task ReturnsCorrectContactAttributeDataMappingDictionary(AdaptorRequestContext requestContext,
        List<WrapperContactPoint> contactPoints, Dictionary<string, string> attributeMappings, Dictionary<string, object> expectedDataMappings)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = AttributeMappingService(dataContext, requestContext);

          var result = contactService.GetMappedContactDataDictionaryFromContactPoints(contactPoints, attributeMappings);

          Assert.NotNull(result);
          foreach (var expectedMappingEntity in expectedDataMappings)
          {
            var resultMappingEntity = result[expectedMappingEntity.Key];
            Assert.NotNull(resultMappingEntity);
            Assert.Equal(expectedMappingEntity.Value, resultMappingEntity);
          }
        });
      }
    }

    public class GetMergedResultDictionary
    {
      public static IEnumerable<object[]> MerginDictionaryData =>
            new List<object[]>
            {
                new object[]
                {
                  new List<Dictionary<string, object>>
                  {
                    new Dictionary<string, object>
                    {
                      { "Property1", "string"},
                      { "Property2", 1},
                    },
                    new Dictionary<string, object>
                    {
                      { "Property3", new List<string> { "a", "b", "c"} },
                    }
                  },
                  new Dictionary<string, object>
                  {
                    { "Property1", "string"},
                    { "Property2", 1},
                    { "Property3", new List<string> { "a", "b", "c"} },
                  }
                },
            };

      [Theory]
      [MemberData(nameof(MerginDictionaryData))]
      public async Task ReturnsCorrectMergedDictionary(List<Dictionary<string, object>> dictionaryList,
        Dictionary<string, object> expectedDictionary)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = AttributeMappingService(dataContext);

          var result = contactService.GetMergedResultDictionary(dictionaryList);

          Assert.NotNull(result);
          foreach (var expectedAttribute in expectedDictionary)
          {
            var resultMappingEntity = result[expectedAttribute.Key];
            Assert.NotNull(resultMappingEntity);
            Assert.Equal(expectedAttribute.Value, resultMappingEntity);
          }
        });
      }
    }

    public static AttributeMappingService AttributeMappingService(IDataContext dataContext, AdaptorRequestContext requestContext = null)
    {
      requestContext ??= new AdaptorRequestContext
      {
        ConsumerId = 1
      };
      var service = new AttributeMappingService(dataContext, requestContext);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "Digits" });
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.Organisation, AdapterConsumerId = 1 });
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 2, Name = ConsumerEntityNames.User, AdapterConsumerId = 1 });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "OrganisationName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 2, AdapterConsumerEntityId = 1, AttributeName = "OrganisationUri" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 3, AdapterConsumerEntityId = 2, AttributeName = "Name" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 4, AdapterConsumerEntityId = 2, AttributeName = "FamilyName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 5, AdapterConsumerEntityId = 2, AttributeName = "Roles" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 6, AdapterConsumerEntityId = 2, AttributeName = "MobileNumber" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 7, AdapterConsumerEntityId = 2, AttributeName = "OrganisationName" });

      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.OrgProfile });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 2, Name = ConclaveEntityNames.UserProfile });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 3, Name = ConclaveEntityNames.UserContact });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 4, Name = ConclaveEntityNames.Site });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "Identifier.LegalName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "Identifier.Uri" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 3, ConclaveEntityId = 2, AttributeName = "FirstName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 4, ConclaveEntityId = 2, AttributeName = "LastName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 5, ConclaveEntityId = 2, AttributeName = "Title" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 6, ConclaveEntityId = 2, AttributeName = "Detail.RolePermissionInfo" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 7, ConclaveEntityId = 2, AttributeName = "Detail.UserGroups" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 8, ConclaveEntityId = 2, AttributeName = "IdentityProviderDisplayName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 9, ConclaveEntityId = 3, AttributeName = "ContactReason" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 10, ConclaveEntityId = 3, AttributeName = "Contacts" });

      // Org Mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });

      // User mapping :- first name, last name, roles, organisation name
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 3, AdapterConsumerEntityAttributeId = 3, ConclaveEntityAttributeId = 3 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 4, AdapterConsumerEntityAttributeId = 4, ConclaveEntityAttributeId = 4 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 5, AdapterConsumerEntityAttributeId = 5, ConclaveEntityAttributeId = 6 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 6, AdapterConsumerEntityAttributeId = 7, ConclaveEntityAttributeId = 1 });

      await dataContext.SaveChangesAsync();
    }
  }
}

