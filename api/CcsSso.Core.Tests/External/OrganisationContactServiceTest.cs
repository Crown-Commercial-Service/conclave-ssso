using CcsSso.Core.DbModel.Constants;
using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.External;
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
  public class OrganisationContactServiceTest
  {
    public class CreateOrganisationContact
    {

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "2",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetContactInfo("OTHER", null, "tuser@mail.com", "+551155256325", null, null)
                },
                new object[]
                {
                  "1",
                  DtoHelper.GetContactInfo("OTHER", "Test Middle User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateContactSuccessfully_WhenCorrectData(string ciiOrganisationId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          var result = await contactService.CreateOrganisationContactAsync(ciiOrganisationId, contactInfo);

          var personPartyTypeId = (await dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;
          var createdContactData = await dataContext.ContactPoint
            .Where(c => !c.IsDeleted && c.Id == result &&
              !c.Party.Organisation.IsDeleted && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
            .Include(c => c.ContactPointReason)
            .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
            .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);
          var name = contactInfo.ContactPointName;
          var personContactPoint = createdContactData.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Split(" ");
            Assert.Equal(nameArray[0], personContactPoint.Party.Person.FirstName);
            if (nameArray.Length >= 2)
            {
              Assert.Equal(nameArray[nameArray.Length - 1], personContactPoint.Party.Person.LastName);
            }
            else
            {
              Assert.Empty(personContactPoint.Party.Person.LastName);
            }
          }
          else
          {
            Assert.Empty(personContactPoint.Party.Person.FirstName);
            Assert.Empty(personContactPoint.Party.Person.LastName);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrganisation =>
            new List<object[]>
            {
                new object[]
                {
                  "3",
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrganisation))]
      public async Task ThrowsException_WhenOrganisationDoesntExists(string ciiOrganisationId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.CreateOrganisationContactAsync(ciiOrganisationId, contactInfo));
        });
      }
    }

    public class GetOrganisationContact
    {
      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  1,
                  new OrganisationContactInfo { ContactPointId = 1,
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "1",
                    },
                    ContactPointReason = ContactReasonType.Shipping,
                    ContactPointName = "PesronFN1 LN1",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "email1@mail.com" },
                        new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345671" },
                      }
                  }
                },
                new object[]
                {
                  "2",
                  5,
                  new OrganisationContactInfo { ContactPointId = 5,
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPointReason = ContactReasonType.Shipping,
                    ContactPointName = "O2PesronFN1 O2LN1",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "emailO2C1@mail.com" },
                        new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+941123456721" },
                      }
                  }
                },
                new object[]
                {
                  "2",
                  7,
                  new OrganisationContactInfo { ContactPointId = 7,
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPointReason = ContactReasonType.Billing,
                    ContactPointName = "O2PesronFN2 O2LN2",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "emailO2C2@mail.com" },
                        new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+941123456722" },
                      }
                  }
                },
                new object[]
                {
                  "2",
                  20,
                  new OrganisationContactInfo { ContactPointId = 20,
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPointReason = ContactReasonType.Shipping,
                    ContactPointName = "O2U1C4O2FN O2U1C4O2LN",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 12, ContactType = VirtualContactTypeName.Email, ContactValue = "o2u1c4o2@mail.com" }
                      }
                  }
                },
                new object[]
                {
                  "2",
                  23,
                  new OrganisationContactInfo { ContactPointId = 23,
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPointReason = ContactReasonType.Shipping,
                    ContactPointName = "O2U1C5O2S1FN O2U1C5O2S1LN",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 13, ContactType = VirtualContactTypeName.Email, ContactValue = "o2u1c5o2s1@mail.com" }
                      }
                  }
                },
                new object[]
                {
                  "2",
                  27,
                  new OrganisationContactInfo { ContactPointId = 27,
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPointReason = ContactReasonType.Shipping,
                    ContactPointName = "O2S1C3O2FN O2S1C3O2LN",
                    Contacts = new List<ContactResponseDetail>
                      {
                        new ContactResponseDetail { ContactId = 16, ContactType = VirtualContactTypeName.Email, ContactValue = "o2s1c3o2@mail.com" }
                      }
                  }
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectContactsIncludingName(string ciiOrganisationId, int contactId, OrganisationContactInfo expectedOrgContact)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationContactAsync(ciiOrganisationId, contactId);

          Assert.NotNull(result);
          Assert.Equal(expectedOrgContact.ContactPointId, result.ContactPointId);
          Assert.Equal(expectedOrgContact.Detail.OrganisationId, result.Detail.OrganisationId);
          Assert.Equal(expectedOrgContact.ContactPointReason, result.ContactPointReason);
          Assert.Equal(expectedOrgContact.ContactPointName, result.ContactPointName);

          var expectedEmailContact = expectedOrgContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Email);
          var expectedPhoneContact = expectedOrgContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Phone);
          var expectedFaxContact = expectedOrgContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Fax);
          var expectedUrlContact = expectedOrgContact.Contacts.FirstOrDefault(c => c.ContactType == VirtualContactTypeName.Url);

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
      [InlineData("1", 10)]
      [InlineData("1", 3)]
      [InlineData("2", 3)]
      [InlineData("nocontactsorg", 1)]
      [InlineData("invalidorg", 1)]
      public async Task ThrowsException_WhenNotExsists(string ciiOrganisationId, int contactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationContactAsync(ciiOrganisationId, contactId));

        });
      }
    }

    public class GetOrganisationContactsList
    {
      public static IEnumerable<object[]> ExpectedContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  ContactAssignedStatus.All,
                  new OrganisationContactInfoList
                  {
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "1",
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 1, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "PesronFN1 LN1",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 1, ContactType = VirtualContactTypeName.Email, ContactValue = "email1@mail.com" },
                          new ContactResponseDetail { ContactId = 2, ContactType = VirtualContactTypeName.Phone, ContactValue = "+94112345671" },
                        }
                      }
                    }
                  }
                },
                new object[]
                {
                  "2",
                  ContactAssignedStatus.All,
                  new OrganisationContactInfoList
                  {
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 5, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2PesronFN1 O2LN1",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 5, ContactType = VirtualContactTypeName.Email, ContactValue = "emailO2C1@mail.com" },
                          new ContactResponseDetail { ContactId = 6, ContactType = VirtualContactTypeName.Phone, ContactValue = "+941123456721" },
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 7, ContactPointReason = ContactReasonType.Billing, ContactPointName = "O2PesronFN2 O2LN2",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 7, ContactType = VirtualContactTypeName.Email, ContactValue = "emailO2C2@mail.com" },
                          new ContactResponseDetail { ContactId = 8, ContactType = VirtualContactTypeName.Phone, ContactValue = "+941123456722" }
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 20, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2U1C4O2FN O2U1C4O2LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 12, ContactType = VirtualContactTypeName.Email, ContactValue = "o2u1c4o2@mail.com" }
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 23, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2U1C5O2S1FN O2U1C5O2S1LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 13, ContactType = VirtualContactTypeName.Email, ContactValue = "o2u1c5o2s1@mail.com" }
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 27, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2S1C3O2FN O2S1C3O2LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 16, ContactType = VirtualContactTypeName.Email, ContactValue = "o2s1c3o2@mail.com" }
                        }
                      }
                    }
                  }
                },
                new object[]
                {
                  "2",
                  ContactAssignedStatus.Original,
                  new OrganisationContactInfoList
                  {
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 5, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2PesronFN1 O2LN1",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 5, ContactType = VirtualContactTypeName.Email, ContactValue = "emailO2C1@mail.com" },
                          new ContactResponseDetail { ContactId = 6, ContactType = VirtualContactTypeName.Phone, ContactValue = "+941123456721" },
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 7, ContactPointReason = ContactReasonType.Billing, ContactPointName = "O2PesronFN2 O2LN2",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 7, ContactType = VirtualContactTypeName.Email, ContactValue = "emailO2C2@mail.com" },
                          new ContactResponseDetail { ContactId = 8, ContactType = VirtualContactTypeName.Phone, ContactValue = "+941123456722" }
                        }
                      }
                    }
                  }
                },
                new object[]
                {
                  "2",
                  ContactAssignedStatus.Assigned,
                  new OrganisationContactInfoList
                  {
                    Detail = new OrganisationDetailInfo
                    {
                      OrganisationId = "2",
                    },
                    ContactPoints = new List<ContactResponseInfo>
                    {
                      new ContactResponseInfo { ContactPointId = 20, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2U1C4O2FN O2U1C4O2LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 12, ContactType = VirtualContactTypeName.Email, ContactValue = "o2u1c4o2@mail.com" }
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 23, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2U1C5O2S1FN O2U1C5O2S1LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 13, ContactType = VirtualContactTypeName.Email, ContactValue = "o2u1c5o2s1@mail.com" }
                        }
                      },
                      new ContactResponseInfo { ContactPointId = 27, ContactPointReason = ContactReasonType.Shipping, ContactPointName = "O2S1C3O2FN O2S1C3O2LN",
                        Contacts = new List<ContactResponseDetail>
                        {
                          new ContactResponseDetail { ContactId = 16, ContactType = VirtualContactTypeName.Email, ContactValue = "o2s1c3o2@mail.com" }
                        }
                      }
                    }
                  }
                }
            };

      [Theory]
      [MemberData(nameof(ExpectedContactData))]
      public async Task ReturnsCorrectList(string ciiOrganisationId, ContactAssignedStatus contactAssignedStatus,OrganisationContactInfoList expectedResult)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationContactsListAsync(ciiOrganisationId, null, contactAssignedStatus);

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
      [InlineData("nocontactsorg")]
      public async Task ReturnsEmpty_WhenNotExsists(string ciiOrganisationId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetOrganisationContactsListAsync(ciiOrganisationId);

          Assert.Empty(result.ContactPoints);

        });
      }

      [Theory]
      [InlineData("3")]
      public async Task ThrowsException_WhenOrganisationNotExsists(string ciiOrganisationId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationContactsListAsync(ciiOrganisationId));

        });
      }
    }

    public class DeleteOrganisationContact
    {
      [Theory]
      [InlineData("1", 1)]
      public async Task DeleteOrganisationContactSuccessfully(string ciiOrganisationId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteOrganisationContactAsync(ciiOrganisationId, deletingContactId);

          var deletedContact = await dataContext.ContactPoint.FirstOrDefaultAsync(c => c.Id == deletingContactId);

          Assert.True(deletedContact.IsDeleted);
        });
      }

      [Theory]
      [InlineData("1", 2)]
      [InlineData("1", 3)]
      [InlineData("3", 2)]
      public async Task ThrowsException_WhenContactDoesntExists(string ciiOrganisationId, int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.DeleteOrganisationContactAsync(ciiOrganisationId, deletingContactId));
        });
      }
    }

    public class UpdateOrganisationContact
    {
      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                  "1",
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mailup.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactSuccessfully_WhenCorrectData(string ciiOrganisationId, int contactId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.UpdateOrganisationContactAsync(ciiOrganisationId, contactId, contactInfo);

          var personPartyTypeId = (await dataContext.PartyType.FirstOrDefaultAsync(t => t.PartyTypeName == PartyTypeName.NonUser)).Id;
          var updatedContactData = await dataContext.ContactPoint
            .Where(c => !c.IsDeleted && c.Id == contactId &&
              !c.Party.Organisation.IsDeleted && c.Party.Organisation.CiiOrganisationId == ciiOrganisationId)
            .Include(c => c.ContactPointReason)
            .Include(c => c.ContactDetail).ThenInclude(cd => cd.VirtualAddresses)
            .Include(c => c.ContactDetail).ThenInclude(cd => cd.ContactPoints.Where(cp => cp.PartyTypeId == personPartyTypeId)).ThenInclude(cp => cp.Party).ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(updatedContactData);
          var name = contactInfo.ContactPointName;
          var personContactPoint = updatedContactData.ContactDetail.ContactPoints.First(cp => cp.PartyTypeId == personPartyTypeId);
          if (!string.IsNullOrEmpty(name))
          {
            var nameArray = name.Split(" ");
            Assert.Equal(nameArray[0], personContactPoint.Party.Person.FirstName);
            if (nameArray.Length >= 2)
            {
              Assert.Equal(nameArray[nameArray.Length - 1], personContactPoint.Party.Person.LastName);
            }
            else
            {
              Assert.Empty(personContactPoint.Party.Person.LastName);
            }
          }
          else
          {
            Assert.Empty(personContactPoint.Party.Person.FirstName);
            Assert.Empty(personContactPoint.Party.Person.LastName);
          }

          Assert.Equal(contactInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Email).ContactValue, personContactPoint.ContactDetail.VirtualAddresses.First(va => va.VirtualAddressTypeId == 1).VirtualAddressValue);
          Assert.Equal(contactInfo.Contacts.First(c => c.ContactType == VirtualContactTypeName.Phone).ContactValue, personContactPoint.ContactDetail.VirtualAddresses.First(va => va.VirtualAddressTypeId == 2).VirtualAddressValue);
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrganisation =>
            new List<object[]>
            {
                new object[]
                {
                  "3",
                  1,
                  DtoHelper.GetContactInfo("OTHER", "Test User", "tuser@mail.com", "+551155256325", null, null)
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrganisation))]
      public async Task ThrowsException_WhenOrganisationDoesntExists(string ciiOrganisationId, int contactId, ContactRequestInfo contactInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);

          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UpdateOrganisationContactAsync(ciiOrganisationId, contactId, contactInfo));
        });
      }
    }

    public class AssignContactsToOrganisation
    {
      public static IEnumerable<object[]> CorrectAssignmentData =>
            new List<object[]>
            {
                new object[]
                {
                  "2",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 13, 15 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "org2user1@mail.com"
                  },
                },
                new object[]
                {
                  "2",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 3, 4 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = 10
                  },
                }
            };

      [Theory]
      [MemberData(nameof(CorrectAssignmentData))]
      public async Task AssignSuccessfully_WhenCorrectData(string ciiOrganisationId, ContactAssignmentInfo contactAssignmentInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var results = await contactService.AssignContactsToOrganisationAsync(ciiOrganisationId, contactAssignmentInfo);

          foreach (var assignedId in results)
          {
            var siteContact = await contactService.GetOrganisationContactAsync(ciiOrganisationId, assignedId);
            Assert.NotNull(siteContact);
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSite =>
            new List<object[]>
            {
                new object[]
                {
                  "4",
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
      public async Task ThrowsResourceNotFoundException_WhenOrgDoesnotExists(string ciiOrganisationId, ContactAssignmentInfo contactAssignmentInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.AssignContactsToOrganisationAsync(ciiOrganisationId, contactAssignmentInfo));
        });
      }

      public static IEnumerable<object[]> ContactDataDuplicate =>
            new List<object[]>
            {
                new object[]
                {
                  "2",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 19 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "org2user1@mail.com"
                  },
                },
                new object[]
                {
                  "2",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 22 },
                    AssigningContactType = AssignedContactType.User,
                    AssigningContactsUserId = "org2user1@mail.com"
                  },
                },
                new object[]
                {
                  "2",
                  new ContactAssignmentInfo
                  {
                    AssigningContactPointIds = new List<int> { 5 },
                    AssigningContactType = AssignedContactType.Site,
                    AssigningContactsSiteId = 10
                  },
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataDuplicate))]
      public async Task ThrowsException_WhenDuplicateAssginments(string ciiOrganisationId, ContactAssignmentInfo contactAssignmentInfo)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.AssignContactsToOrganisationAsync(ciiOrganisationId, contactAssignmentInfo));

          Assert.Equal(ErrorConstant.ErrorDuplicateContactAssignment, ex.Message);
        });
      }
    }

    public class UnassignOrganisationContacts
    {
      public static IEnumerable<object[]> CorrectUnassignmentData =>
            new List<object[]>
            {
                new object[]
                {
                  "2",
                  new List<int> { 20, 23, 27 },
                }
            };

      [Theory]
      [MemberData(nameof(CorrectUnassignmentData))]
      public async Task UnassignSuccessfully_WhenCorrectData(string ciiOrganisationId, List<int> unassigningContactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.UnassignOrganisationContactsAsync(ciiOrganisationId, unassigningContactPointIds);

          foreach (var unassignedId in unassigningContactPointIds)
          {
            await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.GetOrganisationContactAsync(ciiOrganisationId, unassignedId));
          }
        });
      }

      public static IEnumerable<object[]> ContactDataInvalidOrgSite =>
            new List<object[]>
            {
                new object[]
                {
                  "4",
                  new List<int> { 10 },
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataInvalidOrgSite))]
      public async Task ThrowsResourceNotFoundException_WhenOrgDoesnotExists(string ciiOrganisationId, List<int> unassigningContactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await Assert.ThrowsAsync<ResourceNotFoundException>(() => contactService.UnassignOrganisationContactsAsync(ciiOrganisationId, unassigningContactPointIds));
        });
      }

      public static IEnumerable<object[]> ContactDataNotExists =>
            new List<object[]>
            {
                new object[]
                {
                  "2",
                  new List<int> { }
                },
                new object[]
                {
                  "2",
                  new List<int> { 10 }
                },
                new object[]
                {
                  "2",
                  new List<int> { 13 }
                }
            };

      [Theory]
      [MemberData(nameof(ContactDataNotExists))]
      public async Task ThrowsException_WhenUnAssginingContactNotExists(string ciiOrganisationId, List<int> unassigningContactPointIds)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.UnassignOrganisationContactsAsync(ciiOrganisationId, unassigningContactPointIds));

          Assert.Equal(ErrorConstant.ErrorInvalidUnassigningContactIds, ex.Message);
        });
      }
    }

    public static OrganisationContactService ContactService(IDataContext dataContext)
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
      var mockAuditLoginService = new Mock<IAuditLoginService>();
      var service = new OrganisationContactService(dataContext, contactsHelperService, mockAdaptorNotificationService.Object, mockWrapperCacheService.Object,
        mockAuditLoginService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = PartyTypeName.InternalOrgnaisation });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.PartyType.Add(new PartyType { Id = 3, PartyTypeName = PartyTypeName.User });
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

      #region Org1
      // Org1
      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, CiiOrganisationId = "1", OrganisationUri = "Org1Uri", RightToBuy = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 1, OrganisationId = 1, IdentityProviderId = 1 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 1, PartyTypeId = 1, ContactPointReasonId = 2, ContactDetailId = 1 });

      // Org1 org contact ID=1
      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 1, VirtualAddressTypeId = 1, VirtualAddressValue = "email1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 1, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345671" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 1 });

      // Org1 user1 user contact ID=4
      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 4, OrganisationId = 1, FirstName = "PesronFN2", LastName = "LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "+94112345672" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 3, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 2, IsDeleted = true });

      // Org1 User
      dataContext.Party.Add(new Party { Id = 5, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 3, PartyId = 5, OrganisationId = 1, FirstName = "UserFN1", LastName = "UserLN1" });
      dataContext.User.Add(new User { Id = 1, PartyId = 5, UserName = "user1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 4, PartyId = 5, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 2 });
      #endregion


      // Org2
      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, CiiOrganisationId = "2", OrganisationUri = "Org2Uri", RightToBuy = true });
      dataContext.OrganisationEligibleIdentityProvider.Add(new OrganisationEligibleIdentityProvider { Id = 2, OrganisationId = 2, IdentityProviderId = 1 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 5, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 2, ContactDetailId = 3 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 7, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 3, ContactDetailId = 4 });

      // Org2 org contact 1 ID=8
      dataContext.Party.Add(new Party { Id = 6, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 4, PartyId = 6, OrganisationId = 2, FirstName = "O2PesronFN1", LastName = "O2LN1" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 3, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 5, ContactDetailId = 3, VirtualAddressTypeId = 1, VirtualAddressValue = "emailO2C1@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 6, ContactDetailId = 3, VirtualAddressTypeId = 2, VirtualAddressValue = "+941123456721" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 8, PartyId = 6, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 3 });

      // Org2 org contact 2 ID=9
      dataContext.Party.Add(new Party { Id = 7, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 5, PartyId = 7, OrganisationId = 2, FirstName = "O2PesronFN2", LastName = "O2LN2" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 4, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 7, ContactDetailId = 4, VirtualAddressTypeId = 1, VirtualAddressValue = "emailO2C2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 8, ContactDetailId = 4, VirtualAddressTypeId = 2, VirtualAddressValue = "+941123456722" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 9, PartyId = 7, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 4 });

      // Org2 site1 ID=10
      dataContext.ContactDetail.Add(new ContactDetail { Id = 5, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 1, ContactDetailId = 5, StreetAddress = "Site Street" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 10, PartyId = 2, PartyTypeId = 2, ContactPointReasonId = 4, ContactDetailId = 5, IsSite = true });

      // Org2 Registered address
      dataContext.ContactDetail.Add(new ContactDetail { Id = 6, EffectiveFrom = DateTime.UtcNow });
      dataContext.PhysicalAddress.Add(new PhysicalAddress { Id = 2, ContactDetailId = 6, StreetAddress = "Registered Street" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 11, PartyId = 2, PartyTypeId = 2, ContactPointReasonId = 1, ContactDetailId = 6 });

      // Org2 User1 ID=2 UserName="org2user1@mail.com"
      dataContext.Party.Add(new Party { Id = 8, PartyTypeId = 3 });
      dataContext.Person.Add(new Person { Id = 6, PartyId = 8, OrganisationId = 2, FirstName = "Org2User1FN", LastName = "Org2User1LN" });
      dataContext.User.Add(new User { Id = 2, PartyId = 8, UserName = "org2user1@mail.com" });

      // Org2 User1 Contact1 ID=13
      dataContext.Party.Add(new Party { Id = 9, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 7, PartyId = 9, OrganisationId = 2, FirstName = "O2U1C1FN", LastName = "O2U1C1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 7, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 9, ContactDetailId = 7, VirtualAddressTypeId = 1, VirtualAddressValue = "o2u1c1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 12, PartyId = 9, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 7 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 13, PartyId = 8, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 7 }); //User1Contact1

      // Org2 User1 Contact2 ID=15
      dataContext.Party.Add(new Party { Id = 10, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 8, PartyId = 10, OrganisationId = 2, FirstName = "O2U1C2FN", LastName = "O2U1C2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 8, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 10, ContactDetailId = 8, VirtualAddressTypeId = 1, VirtualAddressValue = "o2u1c2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 14, PartyId = 10, PartyTypeId = 2, ContactPointReasonId = 3, ContactDetailId = 8 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 15, PartyId = 8, PartyTypeId = 3, ContactPointReasonId = 3, ContactDetailId = 8 }); //User1Contact2

      // Org2 User1 Contact3 assigned to site1 ID=17
      dataContext.Party.Add(new Party { Id = 11, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 9, PartyId = 11, OrganisationId = 2, FirstName = "O2U1C3S1FN", LastName = "O2U1C3S1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 9, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 11, ContactDetailId = 9, VirtualAddressTypeId = 1, VirtualAddressValue = "o2u1c3s1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 16, PartyId = 11, PartyTypeId = 2, ContactPointReasonId = 5, ContactDetailId = 9 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 17, PartyId = 8, PartyTypeId = 3, ContactPointReasonId = 5, ContactDetailId = 9 }); //User1Contact3
      dataContext.SiteContact.Add(new SiteContact { Id = 1, ContactPointId = 10, ContactId = 16, OriginalContactId = 17, AssignedContactType = AssignedContactType.User });  // Assigned to site1

      // Org2 User1 Contact4 assigned to Org2 ID=19
      dataContext.Party.Add(new Party { Id = 12, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 10, PartyId = 12, OrganisationId = 2, FirstName = "O2U1C4O2FN", LastName = "O2U1C4O2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 10, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 12, ContactDetailId = 10, VirtualAddressTypeId = 1, VirtualAddressValue = "o2u1c4o2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 18, PartyId = 12, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 10 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 19, PartyId = 8, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 10 }); //User1Contact4
      dataContext.ContactPoint.Add(new ContactPoint { Id = 20, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 2, ContactDetailId = 10, OriginalContactPointId = 19, AssignedContactType = AssignedContactType.User }); //Assigned to org2

      // Org2 User1 Contact5 assigned to Org2 and Site1 ID=22
      dataContext.Party.Add(new Party { Id = 13, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 11, PartyId = 13, OrganisationId = 2, FirstName = "O2U1C5O2S1FN", LastName = "O2U1C5O2S1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 11, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 13, ContactDetailId = 11, VirtualAddressTypeId = 1, VirtualAddressValue = "o2u1c5o2s1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 21, PartyId = 13, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 11 });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 22, PartyId = 8, PartyTypeId = 3, ContactPointReasonId = 2, ContactDetailId = 11 }); //User1Contact5
      dataContext.ContactPoint.Add(new ContactPoint { Id = 23, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 2, ContactDetailId = 11, OriginalContactPointId = 22, AssignedContactType = AssignedContactType.User }); //Assigned to org2
      dataContext.SiteContact.Add(new SiteContact { Id = 2, ContactPointId = 10, ContactId = 21, OriginalContactId = 22, AssignedContactType = AssignedContactType.User });  // Assigned to site1

      // Org2 Site1 Contact1  ID=3 (since above 2 assigned from user to site) 
      dataContext.Party.Add(new Party { Id = 14, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 12, PartyId = 14, OrganisationId = 2, FirstName = "O2S1C1FN", LastName = "O2S1C1LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 12, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 14, ContactDetailId = 12, VirtualAddressTypeId = 1, VirtualAddressValue = "o2s1c1@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 24, PartyId = 14, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 12 });
      dataContext.SiteContact.Add(new SiteContact { Id = 3, ContactPointId = 10, ContactId = 24 });  // Site1 Contact1

      // Org2 Site1 Contact2  ID=4
      dataContext.Party.Add(new Party { Id = 15, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 13, PartyId = 15, OrganisationId = 2, FirstName = "O2S1C2FN", LastName = "O2S1C2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 13, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 15, ContactDetailId = 13, VirtualAddressTypeId = 1, VirtualAddressValue = "o2s1c2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 25, PartyId = 15, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 13 });
      dataContext.SiteContact.Add(new SiteContact { Id = 4, ContactPointId = 10, ContactId = 25 });  // Site1 Contact2

      // Org2 Site1 Contact3 assigned to Org2  ID=5
      dataContext.Party.Add(new Party { Id = 16, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 14, PartyId = 16, OrganisationId = 2, FirstName = "O2S1C3O2FN", LastName = "O2S1C3O2LN" });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 14, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 16, ContactDetailId = 14, VirtualAddressTypeId = 1, VirtualAddressValue = "o2s1c3o2@mail.com" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 26, PartyId = 16, PartyTypeId = 2, ContactPointReasonId = 2, ContactDetailId = 14 });
      dataContext.SiteContact.Add(new SiteContact { Id = 5, ContactPointId = 10, ContactId = 26 });  // Site1 Contact2
      dataContext.ContactPoint.Add(new ContactPoint { Id = 27, PartyId = 2, PartyTypeId = 1, ContactPointReasonId = 2, ContactDetailId = 14, OriginalContactPointId = 26, AssignedContactType = AssignedContactType.Site }); // Asssigned to org2

      // Org3 no contacts
      dataContext.Party.Add(new Party { Id = 17, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 3, PartyId = 17, CiiOrganisationId = "nocontactsorg", OrganisationUri = "Org3Uri", RightToBuy = true });

      await dataContext.SaveChangesAsync();
    }
  }
}
