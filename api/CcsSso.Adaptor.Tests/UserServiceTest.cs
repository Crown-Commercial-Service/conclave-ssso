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
  public class UserServiceTest
  {

    public class GetUser
    {
      public static IEnumerable<object[]> CorrectUserGetData =>
            new List<object[]>
            {
                new object[]
                {
                  "user1mail.com",
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
                  new WrapperOrganisationResponse{
                    Identifier = new OrganisationIdentifier
                    {
                      LegalName = "Organisation One",
                      Uri = "orgone.com"
                    }
                  },
                  new WrapperUserContactInfoList
                  {
                    Detail =  new UserDetailInfo
                    {
                      UserId = "user1@mail.com"
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
                    { "Name", "UserFN" },
                    { "FamilyName", "UserLN" },
                    { "OrganisationName", "Organisation One" },
                    { "Mobile", new List<string> { "+156842741", "+256842742" } },
                    { "FaxNumber", new List<string> { "+9568743" } },
                    { "WebAddress", new List<string> { "one.com" } },
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
            };

      [Theory]
      [MemberData(nameof(CorrectUserGetData))]
      public async Task ReturnsCorrectResultDictionary_WhenUserExists(string userName, AdaptorRequestContext requestContext,
        WrapperUserResponse wrapperUserResponse, WrapperOrganisationResponse wrapperOrganisationResponse,
        WrapperUserContactInfoList wrapperUserContactInfoList, Dictionary<string, object> expectedDataDictionary)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var mockWrapperUserService = new Mock<IWrapperUserService>();
          var mockWrapperOrgService = new Mock<IWrapperOrganisationService>();
          var mockWrapperUserContactService = new Mock<IWrapperUserContactService>();


          mockWrapperUserService.Setup(s => s.GetUserAsync(userName))
          .ReturnsAsync(wrapperUserResponse);

          mockWrapperOrgService.Setup(s => s.GetOrganisationAsync(wrapperUserResponse.OrganisationId))
          .ReturnsAsync(wrapperOrganisationResponse);

          mockWrapperUserContactService.Setup(s => s.GetUserContactPointsAsync(userName))
          .ReturnsAsync(wrapperUserContactInfoList);

          var userService = UserService(dataContext, requestContext, mockWrapperUserService, mockWrapperOrgService, mockWrapperUserContactService);

          var result = await userService.GetUserAsync(userName);

          Assert.NotNull(result);
          foreach (var expectedProperty in expectedDataDictionary)
          {
            var propertyValue = result[expectedProperty.Key];
            Assert.NotNull(propertyValue);
            if (expectedProperty.Key == "Roles")
            {
              var expectedRoles = expectedProperty.Value as List<RolePermissionInfo>;
              var actualRoles = propertyValue as List<RolePermissionInfo>;
              Assert.Equal(expectedRoles.Count, actualRoles.Count);
            }
            else
            {
              Assert.Equal(expectedProperty.Value, propertyValue);
            }
          }
        });
      }

      public static IEnumerable<object[]> CorrectUserGetDataForNoOrganisation =>
            new List<object[]>
            {
                new object[]
                {
                  "user1mail.com",
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
            };

      [Theory]
      [MemberData(nameof(CorrectUserGetDataForNoOrganisation))]
      public async Task ReturnsCorrectResultDictionary_WhenUserExistsButNoOrganisation(string userName, AdaptorRequestContext requestContext,
        WrapperUserResponse wrapperUserResponse, Dictionary<string, object> expectedDataDictionary)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var mockWrapperUserService = new Mock<IWrapperUserService>();
          var mockWrapperOrgService = new Mock<IWrapperOrganisationService>();

          mockWrapperUserService.Setup(s => s.GetUserAsync(userName))
          .ReturnsAsync(wrapperUserResponse);

          mockWrapperOrgService.Setup(s => s.GetOrganisationAsync(wrapperUserResponse.OrganisationId))
          .ThrowsAsync(new ResourceNotFoundException());

          var mockWrapperUserContactService = new Mock<IWrapperUserContactService>();
          mockWrapperUserContactService.Setup(s => s.GetUserContactPointsAsync(userName))
           .ReturnsAsync(new WrapperUserContactInfoList { ContactPoints = new List<WrapperContactPoint>() });

          var userService = UserService(dataContext, requestContext, mockWrapperUserService, mockWrapperOrgService, mockWrapperUserContactService);

          var result = await userService.GetUserAsync(userName);

          Assert.NotNull(result);
          foreach (var expectedProperty in expectedDataDictionary)
          {
            var propertyValue = result[expectedProperty.Key];
            Assert.NotNull(propertyValue);
            if (expectedProperty.Key == "Roles")
            {
              var expectedRoles = expectedProperty.Value as List<RolePermissionInfo>;
              var actualRoles = propertyValue as List<RolePermissionInfo>;
              Assert.Equal(expectedRoles.Count, actualRoles.Count);
            }
            else
            {
              Assert.Equal(expectedProperty.Value, propertyValue);
            }
          }
        });
      }

      public static IEnumerable<object[]> NoConfigurationDataDetails =>
            new List<object[]>
            {
                new object[]
                {
                  "user@mail.com",
                  new AdaptorRequestContext{ ConsumerId = 2 },
                },
            };

      [Theory]
      [MemberData(nameof(NoConfigurationDataDetails))]
      public async Task ThrowsException_WhenConfigurationNotExists(string userName, AdaptorRequestContext requestContext)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var userService = UserService(dataContext, requestContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => userService.GetUserAsync(userName));
          Assert.Equal(ErrorConstant.NoConfigurationFound, ex.Message);
        });
      }

      public static IEnumerable<object[]> InCorrectUserGetData =>
            new List<object[]>
            {
                new object[]
                {
                  "nouser@mail.com",
                  new AdaptorRequestContext{ ConsumerId = 1 },
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectUserGetData))]
      public async Task ThrowsException_WhenUserNotExists(string userName, AdaptorRequestContext requestContext)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var mockWrapperUserService = new Mock<IWrapperUserService>();

          mockWrapperUserService.Setup(s => s.GetUserAsync(userName))
          .ThrowsAsync(new ResourceNotFoundException());

          var userService = UserService(dataContext, requestContext, mockWrapperUserService);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => userService.GetUserAsync(userName));
        });
      }
    }

    public static UserService UserService(IDataContext dataContext, AdaptorRequestContext requestContext = null,
      Mock<IWrapperUserService> mockWrapperUserService = null, Mock<IWrapperOrganisationService> mockWrapperOrgService = null,
      Mock<IWrapperUserContactService> mockWrapperUserContactService = null, Mock<ICiiService> mockCiiService = null)
    {
      requestContext ??= new AdaptorRequestContext
      {
        ConsumerId = 1
      };
      var atrributeMappingService = new AttributeMappingService(dataContext, requestContext);

      mockWrapperUserService ??= new Mock<IWrapperUserService>();
      mockWrapperOrgService ??= new Mock<IWrapperOrganisationService>();
      mockWrapperUserContactService ??= new Mock<IWrapperUserContactService>();
      mockCiiService ??= new Mock<ICiiService>();

      var service = new UserService(atrributeMappingService, mockWrapperUserService.Object, mockWrapperOrgService.Object, mockWrapperUserContactService.Object, mockCiiService.Object);
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

      // Adaptor User attributes
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 3, AdapterConsumerEntityId = 2, AttributeName = "Name" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 4, AdapterConsumerEntityId = 2, AttributeName = "FamilyName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 5, AdapterConsumerEntityId = 2, AttributeName = "Roles" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 6, AdapterConsumerEntityId = 2, AttributeName = "MobileNumber" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 7, AdapterConsumerEntityId = 2, AttributeName = "OrganisationName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 8, AdapterConsumerEntityId = 2, AttributeName = "Mobile" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 9, AdapterConsumerEntityId = 2, AttributeName = "FaxNumber" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 10, AdapterConsumerEntityId = 2, AttributeName = "WebAddress" });


      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.OrgProfile });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 2, Name = ConclaveEntityNames.UserProfile });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 3, Name = ConclaveEntityNames.UserContact });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 4, Name = ConclaveEntityNames.Site });

      // Conclave Org profile attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "Identifier.LegalName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "Identifier.Uri" });

      // Conclave User profile attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 3, ConclaveEntityId = 2, AttributeName = "FirstName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 4, ConclaveEntityId = 2, AttributeName = "LastName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 5, ConclaveEntityId = 2, AttributeName = "Title" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 6, ConclaveEntityId = 2, AttributeName = "Detail.RolePermissionInfo" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 7, ConclaveEntityId = 2, AttributeName = "Detail.UserGroups" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 8, ConclaveEntityId = 2, AttributeName = "IdentityProviderDisplayName" });

      // Conclave User Contacts attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 9, ConclaveEntityId = 3, AttributeName = ContactType.Phone });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 10, ConclaveEntityId = 3, AttributeName = ContactType.Fax });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 11, ConclaveEntityId = 3, AttributeName = ContactType.Url });

      // Org Mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });

      // User mapping :- first name, last name, roles, organisation name, mobile, fax, web address
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 3, AdapterConsumerEntityAttributeId = 3, ConclaveEntityAttributeId = 3 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 4, AdapterConsumerEntityAttributeId = 4, ConclaveEntityAttributeId = 4 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 5, AdapterConsumerEntityAttributeId = 5, ConclaveEntityAttributeId = 6 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 6, AdapterConsumerEntityAttributeId = 7, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 7, AdapterConsumerEntityAttributeId = 8, ConclaveEntityAttributeId = 9 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 8, AdapterConsumerEntityAttributeId = 9, ConclaveEntityAttributeId = 10 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 9, AdapterConsumerEntityAttributeId = 10, ConclaveEntityAttributeId = 11 });

      await dataContext.SaveChangesAsync();
    }
  }
}


