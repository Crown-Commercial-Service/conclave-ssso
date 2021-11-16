using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos.External;
using CcsSso.Domain.Exceptions;
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
  public class OrganisationSiteContactServiceTest
  {
    public class CreateOrganisationSiteContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", "+551155256325", "url.com")
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateSiteContactSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          var createdContactId = await contactService.CreateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactInfo);

          var createdSiteContact = await dataContext.SiteContact.FirstOrDefaultAsync(sc => sc.Id == createdContactId);

          var createdContactData = await dataContext.ContactPoint.Where(c => c.Id == createdSiteContact.ContactId)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);
          var name = contactInfo.ContactPointName;
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Trim().Split(" ");
            Assert.Equal(nameArray[0], createdContactData.Party.Person.FirstName);
            if (nameArray.Length >= 2)
            {
              Assert.Equal(nameArray[nameArray.Length - 1], createdContactData.Party.Person.LastName);
            }
            else
            {
              Assert.Empty(createdContactData.Party.Person.LastName);
            }
          }
          else
          {
            Assert.Empty(createdContactData.Party.Person.FirstName);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSite =>
            new List<object[]>
            {
                new object[]
                {
                  1,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  2,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  3,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  4,
                  3,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSite))]
      public async Task ThrowsResourceNotFoundException_WhenOrgSiteDoesnotExists(string ciiOrganisationId, int siteId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.CreateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactInfo));
        });
      }
    }

    public class DeleteOrganisationSiteContact
    {
      [Theory]
      [InlineData("1", 2, 1)]
      public async Task DeleteOrganisationSiteContactSuccessfully(string ciiOrganisationId, int siteId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteOrganisationSiteContactAsync(ciiOrganisationId, siteId, deletingContactId);

          var deletedContact = await dataContext.SiteContact.FirstOrDefaultAsync(c => c.Id == deletingContactId);
          Assert.True(deletedContact.IsDeleted);
        });
      }

      [Theory]
      [InlineData("1", 2, 4)] // Deleted
      [InlineData("1", 2, 3)] // Assigned
      [InlineData("1", 3, 3)]
      [InlineData("2", 3, 3)]
      [InlineData("4", 3, 3)]
      public async Task ThrowsException_WhenOrgSiteContactDoesntExists(string ciiOrganisationId, int siteId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.DeleteOrganisationSiteContactAsync(ciiOrganisationId, siteId, deletingContactId));
        });
      }
    }

    public class GetOrganisationSiteContact
    {
      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  1,
                  new OrganisationSiteContactInfo { ContactPointId = 1,
                    Detail = new SiteDetailInfo
                    {
                      OrganisationId = "1", SiteId = 2
                    },
                    ContactPointReason = ContactReasonType.Billing,
                    ContactPointName = "Site1C1FN Site1C1LN",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "site1c1@mail.com" },
                        new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345671" },
                      }
                  }
                },
                new object[]
                {
                  "1",
                  2,
                  3,
                  new OrganisationSiteContactInfo { ContactPointId = 3,
                    Detail = new SiteDetailInfo
                    {
                      OrganisationId = "1", SiteId = 2
                    },
                    ContactPointReason = ContactReasonType.Shipping,
                    ContactPointName = "User1C4S1FN User1C4S1LN",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "user1c4s1@mail.com" }
                      }
                  }
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectSiteContact(string ciiOrganisationId, int siteId, int contactId, OrganisationSiteContactInfo expectedSiteContact)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId);

          Assert.NotNull(result);
          Assert.Equal(expectedSiteContact.ContactPointId, result.ContactPointId);
          Assert.Equal(expectedSiteContact.Detail.OrganisationId, result.Detail.OrganisationId);
          Assert.Equal(expectedSiteContact.Detail.SiteId, result.Detail.SiteId);
          Assert.Equal(expectedSiteContact.ContactPointReason, result.ContactPointReason);
          Assert.Equal(expectedSiteContact.ContactPointName, result.ContactPointName);

          var expectedEmailContact = expectedSiteContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Email);
          var expectedPhoneContact = expectedSiteContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Phone);
          var expectedFaxContact = expectedSiteContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Fax);
          var expectedUrlContact = expectedSiteContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Url);

          if (expectedEmailContact != null)
          {
            Assert.Equal(expectedEmailContact.ContactValue, result.Contacts.First(c => c.ContactType == VirtualContactTypeName.Email).ContactValue);
          }
          if (expectedPhoneContact != null)
          {
            Assert.Equal(expectedPhoneContact.ContactValue, result.Contacts.First(c => c.ContactType == VirtualContactTypeName.Phone).ContactValue);
          }
          if (expectedFaxContact != null)
          {
            Assert.Equal(expectedFaxContact.ContactValue, result.Contacts.First(c => c.ContactType == VirtualContactTypeName.Fax).ContactValue);
          }
          if (expectedUrlContact != null)
          {
            Assert.Equal(expectedUrlContact.ContactValue, result.Contacts.First(c => c.ContactType == VirtualContactTypeName.Url).ContactValue);
          }

        });
      }

      [Theory]
      [InlineData("1", 2, 4)]
      [InlineData("1", 2, 5)]
      [InlineData("2", 2, 2)]
      [InlineData("4", 2, 2)]
      public async Task ThrowsException_WhenNotExsists(string ciiOrganisationId, int siteId, int contactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId));

        });
      }
    }

    public class GetOrganisationSiteContactsList
    {

      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  ContactAssignedStatus.All,
                  new OrganisationSiteContactInfoList
                  {
                    Detail = new SiteDetailInfo
                    {
                      OrganisationId = "1",
                      SiteId = 2
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 1, ContactPointReason = ContactReasonType.Billing, ContactPointName = "Site1C1FN Site1C1LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "site1c1@mail.com" },
                          new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345671" },
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 2, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "Site1C2FN Site1C2LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 3, ContactType = VirtualContactTypeName.Email, ContactValue = "site1c2@mail.com" },
                          new ContactResponseDetail { ContactId = 4, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345672" }
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 3, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "User1C4S1FN User1C4S1LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 8, ContactType = VirtualContactTypeName.Email, ContactValue = "user1c4s1@mail.com" }
                        }
                      }
                    }
                  }
                },
                new object[]
                {
                  "1",
                  2,
                  ContactAssignedStatus.Original,
                  new OrganisationSiteContactInfoList
                  {
                    Detail = new SiteDetailInfo
                    {
                      OrganisationId = "1",
                      SiteId = 2
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 1, ContactPointReason = ContactReasonType.Billing, ContactPointName = "Site1C1FN Site1C1LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "site1c1@mail.com" },
                          new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345671" },
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 2, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "Site1C2FN Site1C2LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 3, ContactType = VirtualContactTypeName.Email, ContactValue = "site1c2@mail.com" },
                          new ContactResponseDetail { ContactId = 4, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345672" }
                        }
                      }
                    }
                  }
                },
                new object[]
                {
                  "1",
                  2,
                  ContactAssignedStatus.Assigned,
                  new OrganisationSiteContactInfoList
                  {
                    Detail = new SiteDetailInfo
                    {
                      OrganisationId = "1",
                      SiteId = 2
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 3, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "User1C4S1FN User1C4S1LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 8, ContactType = VirtualContactTypeName.Email, ContactValue = "user1c4s1@mail.com" }
                        }
                      }
                    }
                  }
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectList(string ciiOrganisationId, int siteId, ContactAssignedStatus contactAssignedStatus,OrganisationSiteContactInfoList expectedResult)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationSiteContactsListAsync(ciiOrganisationId, siteId, null, contactAssignedStatus);

          Assert.NotNull(result);
          Assert.Equal(expectedResult.ContactPoints.Count, result.ContactPoints.Count);

          foreach (var expectedContactResponse in expectedResult.ContactPoints)
          {
            var actualContactResponse = result.ContactPoints.First(c => c.ContactPointId == expectedContactResponse.ContactPointId);

            Assert.Equal(expectedContactResponse.ContactPointId, actualContactResponse.ContactPointId);
            Assert.Equal(expectedContactResponse.ContactPointReason, actualContactResponse.ContactPointReason);
            Assert.Equal(expectedContactResponse.ContactPointName, actualContactResponse.ContactPointName);

            var expectedEmailContact = expectedContactResponse.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Email);
            var expectedPhoneContact = expectedContactResponse.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Phone);
            var expectedFaxContact = expectedContactResponse.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Fax);
            var expectedUrlContact = expectedContactResponse.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Url);

            if (expectedEmailContact != null)
            {
              Assert.Equal(expectedEmailContact.ContactValue, actualContactResponse.Contacts.First(c => c.ContactType == VirtualContactTypeName.Email).ContactValue);
            }
            if (expectedPhoneContact != null)
            {
              Assert.Equal(expectedPhoneContact.ContactValue, actualContactResponse.Contacts.First(c => c.ContactType == VirtualContactTypeName.Phone).ContactValue);
            }
            if (expectedFaxContact != null)
            {
              Assert.Equal(expectedFaxContact.ContactValue, actualContactResponse.Contacts.First(c => c.ContactType == VirtualContactTypeName.Fax).ContactValue);
            }
            if (expectedUrlContact != null)
            {
              Assert.Equal(expectedUrlContact.ContactValue, actualContactResponse.Contacts.First(c => c.ContactType == VirtualContactTypeName.Url).ContactValue);
            } 
          }
        });
      }

      [Theory]
      [InlineData("1", 4)]
      [InlineData("2", 4)]
      public async Task ThrowsException_WhenOrgSiteNotExsists(string ciiOrganisationId, int siteId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationSiteContactsListAsync(ciiOrganisationId, siteId));

        });
      }
    }

    public class UpdateOrganisationSiteContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", "+551155256325", "url.com")
                },
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateSiteContactSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, int contactId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await contactService.UpdateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId, contactInfo);

          var updatedSiteContact = await dataContext.SiteContact.FirstOrDefaultAsync(c => c.Id == contactId);

          var updatedContactData = await dataContext.ContactPoint.Where(c => c.Id == updatedSiteContact.ContactId
            && c.PartyType.PartyTypeName == PartyTypeName.NonUser)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.VirtualAddresses).ThenInclude(v => v.VirtualAddressType)
            .Include(cp => cp.Party)
            .ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(updatedContactData);

          var name = contactInfo.ContactPointName;
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Trim().Split(" ");
            Assert.Equal(nameArray[0], updatedContactData.Party.Person.FirstName);
            if (nameArray.Length >= 2)
            {
              Assert.Equal(nameArray[nameArray.Length - 1], updatedContactData.Party.Person.LastName);
            }
            else
            {
              Assert.Empty(updatedContactData.Party.Person.LastName);
            }
          }
          else
          {
            Assert.Empty(updatedContactData.Party.Person.FirstName);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSiteContact =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  4,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", "+551155256325", "url.com")
                },
                new object[]
                {
                  "1",
                  2,
                  5,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "2",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", " Test User ", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "4",
                  2,
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSiteContact))]
      public async Task ThrowsException_WhenSiteDoesnotExists(string ciiOrganisationId, int siteId, int contactId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateOrganisationSiteContactAsync(ciiOrganisationId, siteId, contactId, contactInfo));
        });
      }
    }

    public class AssignContactsToSite
    {
      public static IEnumerable<object[]> CorrectAssignmentData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9, 10 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                }
            };

      [Theory]
      [MemberData(nameof(CorrectAssignmentData))]
      public async Task AssignSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, ContactAssignmentInfo contactAssignmentInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var results = await contactService.AssignContactsToSiteAsync(ciiOrganisationId, siteId, contactAssignmentInfo);

          foreach (var assignedId in results)
          {
            var siteContact = await contactService.GetOrganisationSiteContactAsync(ciiOrganisationId, siteId, assignedId);
            Assert.NotNull(siteContact);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSite =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  1,
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 9, 10 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSite))]
      public async Task ThrowsResourceNotFoundException_WhenSiteDoesnotExists(string ciiOrganisationId, int siteId, ContactAssignmentInfo contactAssignmentInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.AssignContactsToSiteAsync(ciiOrganisationId, siteId, contactAssignmentInfo));
        });
      }

      public static IEnumerable<object[]> ContactDataDuplicate =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 14 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "user1@mail.com"
                  },
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataDuplicate))]
      public async Task ThrowsException_WhenDuplicateAssginments(string ciiOrganisationId, int siteId, ContactAssignmentInfo contactAssignmentInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.AssignContactsToSiteAsync(ciiOrganisationId, siteId, contactAssignmentInfo));

          Assert.Equal(ErrorConstant.ErrorDuplicateContactAssignment, ex.Message);
        });
      }
    }

    public class UnassignSiteContacts
    {
      public static IEnumerable<object[]> CorrectUnassignmentData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  new List<int> { 3 },
                }
            };

      [Theory]
      [MemberData(nameof(CorrectUnassignmentData))]
      public async Task UnassignSuccessfully_WhenCorrectData(string ciiOrganisationId, int siteId, List<int> unassigningContactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.UnassignSiteContactsAsync(ciiOrganisationId, siteId, unassigningContactPointIds);

          foreach (var unassignedId in unassigningContactPointIds)
          {
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationSiteContactAsync(ciiOrganisationId, siteId, unassignedId));
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSite =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  1,
                  new List<int> { 10 },
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSite))]
      public async Task ThrowsResourceNotFoundException_WhenSiteDoesnotExists(string ciiOrganisationId, int siteId, List<int> unassigningContactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UnassignSiteContactsAsync(ciiOrganisationId, siteId, unassigningContactPointIds));
        });
      }

      public static IEnumerable<object[]> ContactDataNotExists =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  2,
                  new List<int> { }
                },
                new object[]
                {
                  "1",
                  2,
                  new List<int> { 10 }
                },
                new object[]
                {
                  "1",
                  2,
                  new List<int> { 2 }
                },
                new object[]
                {
                  "1",
                  2,
                  new List<int> { 9, 5 }
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataNotExists))]
      public async Task ThrowsException_WhenUnAssginingContactNotExists(string ciiOrganisationId, int siteId, List<int> unassigningContactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.UnassignSiteContactsAsync(ciiOrganisationId, siteId, unassigningContactPointIds));

          Assert.Equal(ErrorConstant.ErrorInvalidUnassigningContactIds, ex.Message);
        });
      }
    }

    public static OrganisationSiteContactService ContactService(IDataContext dataContext)
    {
      Mock<ILocalCacheService> mockLocalCacheService = new();
      mockLocalCacheService.Setup(s => s.GetOrSetValueAsync<List<ContactPointReason>>("CONTACT_POINT_REASONS", It.IsAny<Func<Task<List<ContactPointReason>>>>(), It.IsAny<int>()))
        .ReturnsAsync(new List<ContactPointReason> {
          new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" },
          new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" },
          new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" },
          new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" },
          new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" }
        });
      IContactsHelperService contactsHelperService = new ContactsHelperService(dataContext, mockLocalCacheService.Object);
      var mockAdaptorNotificationService = new Mock<IAdaptorNotificationService>();
      var mockWrapperCacheService = new Mock<IWrapperCacheService>();
      var service = new OrganisationSiteContactService(dataContext, contactsHelperService, mockAdaptorNotificationService.Object, mockWrapperCacheService.Object);
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
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 3, Name = VirtualContactTypeName.Fax, Description = "fax" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 4, Name = VirtualContactTypeName.Url, Description = "url" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = ContactReasonType.Other, Description = "Other" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 2, Name = ContactReasonType.Shipping, Description = "Shipping" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 3, Name = ContactReasonType.Billing, Description = "Billing" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 4, Name = ContactReasonType.Site, Description = "Site" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 5, Name = ContactReasonType.Unspecified, Description = "Unspecified" });
      dataContext.IdentityProvider.Add(new IdentityProvider { Id = 1, IdpName = "IDP", IdpUri = "IDP" });

      //Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", LegalName = "Org1", OrganisationUri = "Org1Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });
      //Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 1, ContactDetailId = 1, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 1 });
      //Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 2, ContactDetailId = 2, StreetAddress = "streetsite1", Locality = "localitysite1", Region = "regionsite1", PostalCode = "postalcodesite1", CountryCode = "countrycodesite1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 1, PartyTypeId = 1, IsSite = true, SiteName = "Org1Site1", ContactPointReasonId = 2, ContactDetailId = 2 });
      dataContext.SiteContact.Add(new SiteContact { Id = 1, ContactPointId = 2, ContactId = 5 });
      dataContext.SiteContact.Add(new SiteContact { Id = 2, ContactPointId = 2, ContactId = 6 });

      #region Org 2
      // Org2 No user contacts no site contacts
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true, IsDeleted = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 2, IdentityProviderId = 1 });
      // Registered
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 3, ContactDetailId = 3, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 1, ContactDetailId = 3 });
      // Site
      dataContext.ContactDetail.Add(new ContactDetail { Id = 4, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 4, ContactDetailId = 4, StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "postalcode", CountryCode = "countrycode" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 2, PartyTypeId = 1, IsSite = true, SiteName = "Org2Site1", IsDeleted = true, ContactPointReasonId = 1, ContactDetailId = 4 });
      #endregion

      // Org1 site 1 contact
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "Site1C1FN", LastName = "Site1C1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 5, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 5, VirtualAddressTypeId = 1, VirtualAddressValue = "site1c1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 5, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 5, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 5 });

      // Org1 site 1 contact2
      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "Site1C2FN", LastName = "Site1C2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 6, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 6, VirtualAddressTypeId = 1, VirtualAddressValue = "site1c2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 6, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 6, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 6 });

      // Org1 user 1
      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "User1FN", LastName = "User1LN" });
      dataContext.User.Add(new User { Id = 1, PartyId = 5, UserName = "user1@mail.com" });

      // Org1 user 1 contact1
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 4, PartyId = 6, OrganisationId = 1, FirstName = "User1C1FN", LastName = "User1C1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 7, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 5, ContactDetailId = 7, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 7, PartyId = 6, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 7 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 9, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 7 }); // User contact

      // Org1 user 1 contact2
      dataContext.Party.Add(new Party { Id = 7, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 5, PartyId = 7, OrganisationId = 1, FirstName = "User1C2FN", LastName = "User1C2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 8, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 6, ContactDetailId = 8, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 8, PartyId = 7, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 8 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 10, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 8 }); // User contact

      // Org1 user 1 contact3 deleted
      dataContext.Party.Add(new Party { Id = 8, PartyTypeId = 2, IsDeleted = true });
      dataContext.Person.Add(new Person { Id = 6, PartyId = 8, OrganisationId = 1, FirstName = "User1C3FN", LastName = "User1C3LN", IsDeleted = true });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 9, EffectiveFrom = DateTime.UtcNow, IsDeleted = true });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 7, ContactDetailId = 9, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c3@mail.com", IsDeleted = true });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 11, PartyId = 8, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 9, IsDeleted = true });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 12, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 9, IsDeleted = true }); // User contact

      // Org1 user 1 contact4 assigned to site
      dataContext.Party.Add(new Party { Id = 9, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 7, PartyId = 9, OrganisationId = 1, FirstName = "User1C4S1FN", LastName = "User1C4S1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 10, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 8, ContactDetailId = 10, VirtualAddressTypeId = 1, VirtualAddressValue = "user1c4s1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 13, PartyId = 9, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 10 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 14, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 10 }); // User contact
      dataContext.SiteContact.Add(new SiteContact { Id = 3, ContactPointId = 2, ContactId = 14, OriginalContactId = 14, AssignedContactType = AssignedContactType.User }); // Assigned to site 1

      // Org1 site 1 contact4 deleted (since site1 contact3 is assigned from user1 contact4)
      dataContext.Party.Add(new Party { Id = 10, PartyTypeId = 2, IsDeleted = true });
      dataContext.Person.Add(new Person { Id = 8, PartyId = 10, OrganisationId = 1, FirstName = "Site1C3FN", LastName = "Site1C3LN", IsDeleted = true });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 11, EffectiveFrom = DateTime.UtcNow, IsDeleted = true });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 9, ContactDetailId = 11, VirtualAddressTypeId = 1, VirtualAddressValue = "site1c3@mail.com", IsDeleted = true });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 15, PartyId = 10, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 11, IsDeleted = true });
      dataContext.SiteContact.Add(new SiteContact { Id = 4, ContactPointId = 2, ContactId = 15, IsDeleted = true }); // Site 1 Contact 4

      //Org3
      dataContext.Party.Add(new Party { Id = 11, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 3, PartyId = 6, CiiOrganisationId = "3", LegalName = "Org3", OrganisationUri = "Org3Uri", RightToBuy = true, IsActivated = true, IsSme = true, IsVcse = true });

      await dataContext.SaveChangesAsync();
    }
  }
}
