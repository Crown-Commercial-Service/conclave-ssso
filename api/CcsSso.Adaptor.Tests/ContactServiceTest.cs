using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.DbDomain.Entity;
using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Constants;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using CcsSso.Adaptor.Service;
using CcsSso.Adaptor.Tests.Infrastructure;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Adaptor.Tests
{
  public class ContactServiceTest
  {

    public class GetContact
    {

      public static WrapperContactResponse WrapperContactResponse = new()
      {
        ContactId = 1,
        ContactType = "PHONE",
        ContactValue = "+568424753"
      };

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  1,
                  1,
                  WrapperContactResponse,
                  new Dictionary<string, object>
                  {
                    { "ContactsObject", WrapperContactResponse },
                  }
                },
                new object[]
                {
                  1,
                  2,
                  WrapperContactResponse,
                  new Dictionary<string, object>
                  {
                    { "ContactType", "PHONE"},
                    { "ContactValue", "+568424753"}
                  }
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task ReturnsCorrectResultDictionary_ForContacts(int contactId, int testDataScenarioNumber,
        WrapperContactResponse wrapperContactResponse, Dictionary<string, object> expectedDataDictionary)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext, testDataScenarioNumber);

          var mockWrapperContactService = new Mock<IWrapperContactService>();

          mockWrapperContactService.Setup(s => s.GetContactAsync(contactId))
          .ReturnsAsync(wrapperContactResponse);

          var contactService = ContactService(dataContext, mockWrapperContactService);

          var result = await contactService.GetContactAsync(contactId);

          Assert.NotNull(result);
          foreach (var expectedProperty in expectedDataDictionary)
          {
            var propertyValue = result[expectedProperty.Key];
            Assert.NotNull(propertyValue);
            Assert.Equal(expectedProperty.Value, propertyValue);
          }
        });
      }
    }

    public class GetUserContact
    {

      public static WrapperContactResponse ExpectedWrapperContactResponse = new()
      {
        ContactId = 1,
        ContactType = "PHONE",
        ContactValue = "+568424753"
      };

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[] // User contact whole contac object
                {
                  1,
                  "u1@mail.com",
                  3,
                  new WrapperUserContactInfoList
                  {
                    ContactPoints =  new List<WrapperContactPoint>
                    {
                      new WrapperContactPoint
                      {
                        ContactPointId = 1,
                        ContactPointName = "CP1",
                        ContactPointReason = "Reason1",
                        Contacts = new List<WrapperContactResponse>
                        {
                          ExpectedWrapperContactResponse,
                          new WrapperContactResponse
                          {
                            ContactId = 2,
                            ContactType = "EMAIL",
                            ContactValue = "c1@mail.com"
                          }
                        }
                      },
                      new WrapperContactPoint
                      {
                        ContactPointId = 2,
                        ContactPointName = "CP2",
                        ContactPointReason = "Reason2",
                        Contacts = new List<WrapperContactResponse>
                        {
                          new WrapperContactResponse
                          {
                            ContactId = 3,
                            ContactType = "PHONE",
                            ContactValue = "+9784638236"
                          },
                          new WrapperContactResponse
                          {
                            ContactId = 4,
                            ContactType = "EMAIL",
                            ContactValue = "c4@mail.com"
                          }
                        }
                      }
                    }
                  },
                  new Dictionary<string, object>
                  {
                    { "ContactsObject", ExpectedWrapperContactResponse },
                  },
                  null
                },
                new object[] // User contact mapped fields of contact object
                {
                  1,
                  "u1@mail.com",
                  4,
                  new WrapperUserContactInfoList
                  {
                    ContactPoints =  new List<WrapperContactPoint>
                    {
                      new WrapperContactPoint
                      {
                        ContactPointId = 1,
                        ContactPointName = "CP1",
                        ContactPointReason = "Reason1",
                        Contacts = new List<WrapperContactResponse>
                        {
                          ExpectedWrapperContactResponse,
                          new WrapperContactResponse
                          {
                            ContactId = 2,
                            ContactType = "EMAIL",
                            ContactValue = "c1@mail.com"
                          }
                        }
                      },
                      new WrapperContactPoint
                      {
                        ContactPointId = 2,
                        ContactPointName = "CP2",
                        ContactPointReason = "Reason2",
                        Contacts = new List<WrapperContactResponse>
                        {
                          new WrapperContactResponse
                          {
                            ContactId = 3,
                            ContactType = "PHONE",
                            ContactValue = "+9784638236"
                          },
                          new WrapperContactResponse
                          {
                            ContactId = 4,
                            ContactType = "EMAIL",
                            ContactValue = "c4@mail.com"
                          }
                        }
                      }
                    }
                  },
                  new Dictionary<string, object>
                  {
                    { "ContactType", "PHONE"},
                    { "ContactValue", "+568424753"}
                  },
                  null
                },
                new object[] // User contact with contact point info
                {
                  1,
                  "u1@mail.com",
                  5,
                  new WrapperUserContactInfoList
                  {
                    ContactPoints =  new List<WrapperContactPoint>
                    {
                      new WrapperContactPoint
                      {
                        ContactPointId = 1,
                        ContactPointName = "CP1",
                        ContactPointReason = "Reason1",
                        Contacts = new List<WrapperContactResponse>
                        {
                          ExpectedWrapperContactResponse,
                          new WrapperContactResponse
                          {
                            ContactId = 2,
                            ContactType = "EMAIL",
                            ContactValue = "c1@mail.com"
                          }
                        }
                      },
                      new WrapperContactPoint
                      {
                        ContactPointId = 2,
                        ContactPointName = "CP2",
                        ContactPointReason = "Reason2",
                        Contacts = new List<WrapperContactResponse>
                        {
                          new WrapperContactResponse
                          {
                            ContactId = 3,
                            ContactType = "PHONE",
                            ContactValue = "+9784638236"
                          },
                          new WrapperContactResponse
                          {
                            ContactId = 4,
                            ContactType = "EMAIL",
                            ContactValue = "c4@mail.com"
                          }
                        }
                      }
                    }
                  },
                  new Dictionary<string, object>
                  {
                    { "ContactType", "PHONE"},
                    { "ContactValue", "+568424753" },
                    { "ContactPointName", "CP1" },
                    { "ContactPointReason", "Reason1" },
                  },
                  null
                },
                new object[] // User contact with contact point and user info
                {
                  1,
                  "u1@mail.com",
                  6,
                  new WrapperUserContactInfoList
                  {
                    ContactPoints =  new List<WrapperContactPoint>
                    {
                      new WrapperContactPoint
                      {
                        ContactPointId = 1,
                        ContactPointName = "CP1",
                        ContactPointReason = "Reason1",
                        Contacts = new List<WrapperContactResponse>
                        {
                          ExpectedWrapperContactResponse,
                          new WrapperContactResponse
                          {
                            ContactId = 2,
                            ContactType = "EMAIL",
                            ContactValue = "c1@mail.com"
                          }
                        }
                      },
                      new WrapperContactPoint
                      {
                        ContactPointId = 2,
                        ContactPointName = "CP2",
                        ContactPointReason = "Reason2",
                        Contacts = new List<WrapperContactResponse>
                        {
                          new WrapperContactResponse
                          {
                            ContactId = 3,
                            ContactType = "PHONE",
                            ContactValue = "+9784638236"
                          },
                          new WrapperContactResponse
                          {
                            ContactId = 4,
                            ContactType = "EMAIL",
                            ContactValue = "c4@mail.com"
                          }
                        }
                      }
                    }
                  },
                  new Dictionary<string, object>
                  {
                    { "ContactType", "PHONE"},
                    { "ContactValue", "+568424753" },
                    { "ContactPointName", "CP1" },
                    { "ContactPointReason", "Reason1" },
                    { "UserFirstName", "U1FirstName" },
                    { "UserLastName", "U1LastName" },
                  },
                  new WrapperUserResponse
                  {
                    FirstName = "U1FirstName",
                    LastName = "U1LastName",
                  }
                },
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task ReturnsCorrectResultDictionary_ForContacts(int contactId, string userName, int testDataScenarioNumber,
        WrapperUserContactInfoList wrapperContactResponse, Dictionary<string, object> expectedDataDictionary,
        WrapperUserResponse wrapperUserResponse = null)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext, testDataScenarioNumber);

          var mockUserWrapperContactService = new Mock<IWrapperUserContactService>();

          mockUserWrapperContactService.Setup(s => s.GetUserContactPointsAsync(userName))
          .ReturnsAsync(wrapperContactResponse);

          Mock<IWrapperUserService> mockWrapperUserService = null;
          if (wrapperUserResponse != null)
          {
            mockWrapperUserService = new Mock<IWrapperUserService>();
            mockWrapperUserService.Setup(s => s.GetUserAsync(userName)).ReturnsAsync(wrapperUserResponse);
          }

          var contactService = ContactService(dataContext, null, mockUserWrapperContactService, null, null, mockWrapperUserService);

          var result = await contactService.GetUserContactAsync(contactId, userName);

          Assert.NotNull(result);
          foreach (var expectedProperty in expectedDataDictionary)
          {
            var propertyValue = result[expectedProperty.Key];
            Assert.NotNull(propertyValue);
            Assert.Equal(expectedProperty.Value, propertyValue);
          }
        });
      }
    }

    public static ContactService ContactService(IDataContext dataContext,
      Mock<IWrapperContactService> mockWrapperContactService = null, Mock<IWrapperUserContactService> mockWrapperUserContactService = null,
      Mock<IWrapperOrganisationContactService> mockWrapperOrgContactService = null, Mock<IWrapperSiteContactService> mockWrapperSiteContactService = null,
      Mock<IWrapperUserService> mockWrapperUserService = null, Mock<IWrapperOrganisationService> mockWrapperOrgService = null,
      Mock<IWrapperSiteService> mockWrapperSiteService = null)
    {
      AdaptorRequestContext requestContext = new AdaptorRequestContext
      {
        ConsumerId = 1
      };
      var atrributeMappingService = new AttributeMappingService(dataContext, requestContext);
      mockWrapperContactService ??= new Mock<IWrapperContactService>();
      mockWrapperUserContactService ??= new Mock<IWrapperUserContactService>();
      mockWrapperOrgContactService ??= new Mock<IWrapperOrganisationContactService>();
      mockWrapperSiteContactService ??= new Mock<IWrapperSiteContactService>();
      mockWrapperUserService ??= new Mock<IWrapperUserService>();
      mockWrapperOrgService ??= new Mock<IWrapperOrganisationService>();
      mockWrapperSiteService ??= new Mock<IWrapperSiteService>();

      var service = new ContactService(atrributeMappingService, mockWrapperContactService.Object, mockWrapperUserContactService.Object,
        mockWrapperOrgContactService.Object, mockWrapperSiteContactService.Object, mockWrapperUserService.Object, mockWrapperOrgService.Object,
        mockWrapperSiteService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext, int consumerId)
    {
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "ContactObject" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 2, Name = "ContactObjectMapped" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 3, Name = "UserContactObject" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 4, Name = "UserContactObjectMapped" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 5, Name = "UserContactObjectMappedContactPoint" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 6, Name = "UserContactObjectMappedUser" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 7, Name = "OrgContactObject" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 8, Name = "OrgContactObjectMapped" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 9, Name = "OrgContactObjectMappedContactPoint" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 10, Name = "OrgContactObjectMappedOrganisation" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 11, Name = "SiteContactObject" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 12, Name = "SiteContactObjectMapped" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 13, Name = "SiteContactObjectMappedContactPoint" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 14, Name = "SiteContactObjectMappedSite" });
      //dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 15, Name = "SiteContactObjectMappedOrganisation" });

      switch (consumerId)
      {
        case 1:
          {
            SetDataContextScenario1(dataContext);
            break;
          }
        case 2:
          {
            SetDataContextScenario2(dataContext);
            break;
          }
        case 3:
          {
            SetDataContextScenario3(dataContext);
            break;
          }
        case 4:
          {
            SetDataContextScenario4(dataContext);
            break;
          }
        case 5:
          {
            SetDataContextScenario5(dataContext);
            break;
          }
        case 6:
          {
            SetDataContextScenario6(dataContext);
            break;
          }
        default:
          {
            break;
          }
      }

      await dataContext.SaveChangesAsync();
    }

    private static void SetDataContextScenario1(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "ContactObject" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.Contact, AdapterConsumerId = 1 });

      // Consumer attribute
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "ContactsObject" });

      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.Contact });

      // Conclave Contact attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = ConclaveAttributeNames.ContactObject });

      // Contact object mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
    }

    private static void SetDataContextScenario2(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "ContactObjectMapped" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.Contact, AdapterConsumerId = 1 });

      // Consumer attribute
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "ContactType" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 2, AdapterConsumerEntityId = 1, AttributeName = "ContactValue" });

      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.Contact });

      // Conclave Contact attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "ContactType" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "ContactValue" });

      // Contact object mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });
    }

    private static void SetDataContextScenario3(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "UserContactObject" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.UserContact, AdapterConsumerId = 1 });

      // Consumer attribute
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "ContactsObject" });

      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.Contact });

      // Conclave Contact attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = ConclaveAttributeNames.ContactObject });

      // Contact object mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
    }

    private static void SetDataContextScenario4(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "UserContactObjectMapped" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.UserContact, AdapterConsumerId = 1 });

      // Consumer attribute
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "ContactType" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 2, AdapterConsumerEntityId = 1, AttributeName = "ContactValue" });

      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.Contact });

      // Conclave Contact attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "ContactType" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "ContactValue" });

      // Contact object mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });
    }

    private static void SetDataContextScenario5(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "UserContactObjectMappedContactPoint" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.UserContact, AdapterConsumerId = 1 });

      // Consumer attribute
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "ContactType" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 2, AdapterConsumerEntityId = 1, AttributeName = "ContactValue" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 3, AdapterConsumerEntityId = 1, AttributeName = "ContactPointName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 4, AdapterConsumerEntityId = 1, AttributeName = "ContactPointReason" });

      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.Contact });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 2, Name = ConclaveEntityNames.UserContact });

      // Conclave Contact attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "ContactType" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "ContactValue" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 3, ConclaveEntityId = 2, AttributeName = "ContactPoints.ContactPointName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 4, ConclaveEntityId = 2, AttributeName = "ContactPoints.ContactPointReason" });

      // Contact object mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 3, AdapterConsumerEntityAttributeId = 3, ConclaveEntityAttributeId = 3 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 4, AdapterConsumerEntityAttributeId = 4, ConclaveEntityAttributeId = 4 });
    }

    private static void SetDataContextScenario6(IDataContext dataContext)
    {
      dataContext.AdapterConsumers.Add(new AdapterConsumer { Id = 1, Name = "UserContactObjectMappedContactPoint" });

      // Adaptor Entities
      dataContext.AdapterConsumerEntities.Add(new AdapterConsumerEntity { Id = 1, Name = ConsumerEntityNames.UserContact, AdapterConsumerId = 1 });

      // Consumer attribute
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 1, AdapterConsumerEntityId = 1, AttributeName = "ContactType" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 2, AdapterConsumerEntityId = 1, AttributeName = "ContactValue" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 3, AdapterConsumerEntityId = 1, AttributeName = "ContactPointName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 4, AdapterConsumerEntityId = 1, AttributeName = "ContactPointReason" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 5, AdapterConsumerEntityId = 1, AttributeName = "UserFirstName" });
      dataContext.AdapterConsumerEntityAttributes.Add(new AdapterConsumerEntityAttribute { Id = 6, AdapterConsumerEntityId = 1, AttributeName = "UserLastName" });

      // Conclave Entities
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 1, Name = ConclaveEntityNames.Contact });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 2, Name = ConclaveEntityNames.UserContact });
      dataContext.ConclaveEntities.Add(new ConclaveEntity { Id = 3, Name = ConclaveEntityNames.UserProfile });

      // Conclave Contact attributes
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 1, ConclaveEntityId = 1, AttributeName = "ContactType" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 2, ConclaveEntityId = 1, AttributeName = "ContactValue" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 3, ConclaveEntityId = 2, AttributeName = "ContactPoints.ContactPointName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 4, ConclaveEntityId = 2, AttributeName = "ContactPoints.ContactPointReason" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 5, ConclaveEntityId = 3, AttributeName = "FirstName" });
      dataContext.ConclaveEntityAttributes.Add(new ConclaveEntityAttribute { Id = 6, ConclaveEntityId = 3, AttributeName = "LastName" });

      // Contact object mapping
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 1, AdapterConsumerEntityAttributeId = 1, ConclaveEntityAttributeId = 1 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 2, AdapterConsumerEntityAttributeId = 2, ConclaveEntityAttributeId = 2 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 3, AdapterConsumerEntityAttributeId = 3, ConclaveEntityAttributeId = 3 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 4, AdapterConsumerEntityAttributeId = 4, ConclaveEntityAttributeId = 4 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 5, AdapterConsumerEntityAttributeId = 5, ConclaveEntityAttributeId = 5 });
      dataContext.AdapterConclaveAttributeMappings.Add(new AdapterConclaveAttributeMapping { Id = 6, AdapterConsumerEntityAttributeId = 6, ConclaveEntityAttributeId = 6 });
    }
  }
}




