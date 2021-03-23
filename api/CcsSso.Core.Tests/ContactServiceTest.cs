using CcsSso.Core.Tests.Infrastructure;
using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Service;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Tests
{
  public class ContactServiceTest
  {
    public class Create
    {

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactDetailDto(0, 0, 2, ContactType.OrganisationPerson, "FirstName LastName", "user@email.com", "0112345")
                },
                new object[]
                {
                    DtoHelper.GetContactDetailDto(0, 1, 2, ContactType.Organisation, "", "", "", new Address {StreetAddress = "street", Locality = "locality", Region = "region", PostalCode = "pcode", CountryCode = "ccode", Uprn = "uprn" })
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task CreateContactSuccessfully_WhenCorrectData(ContactDetailDto contactDetailDto)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.CreateAsync(contactDetailDto);
          Assert.NotEqual(0, result);

          var createdContactData = await dataContext.ContactPoint.Where(c => c.Id == result)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.PhysicalAddress)
            .Include(cd => cd.ContactDetail.VirtualAddresses)
            .Include(cp => cp.Party)
            .ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(createdContactData);

          if (contactDetailDto.ContactType == ContactType.OrganisationPerson)
          {
            Assert.Equal(contactDetailDto.Name, $"{createdContactData.Party.Person.FirstName} {createdContactData.Party.Person.LastName}");
          Assert.Equal(contactDetailDto.Email, createdContactData.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressType.Id == 1).VirtualAddressValue);
          Assert.Equal(contactDetailDto.PhoneNumber, createdContactData.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressType.Id == 2).VirtualAddressValue);
          }
          else
          {
            Assert.Equal(contactDetailDto.Address.StreetAddress, createdContactData.ContactDetail.PhysicalAddress.StreetAddress);
            Assert.Equal(contactDetailDto.Address.Region, createdContactData.ContactDetail.PhysicalAddress.Region);
            Assert.Equal(contactDetailDto.Address.Locality, createdContactData.ContactDetail.PhysicalAddress.Locality);
            Assert.Equal(contactDetailDto.Address.PostalCode, createdContactData.ContactDetail.PhysicalAddress.PostalCode);
            Assert.Equal(contactDetailDto.Address.CountryCode, createdContactData.ContactDetail.PhysicalAddress.CountryCode);
            Assert.Equal(contactDetailDto.Address.Uprn, createdContactData.ContactDetail.PhysicalAddress.Uprn);
          }
        });
      }

      public static IEnumerable<object[]> InCorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactDetailDto(0, 0, 2, ContactType.OrganisationPerson, "FirstName LastName", "useremail.com", "0112345"),
                    ErrorConstant.ErrorInvalidEmail
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectContactData))]
      public async Task ThrowsException_WhenInCorrectData(ContactDetailDto contactDetailDto, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.CreateAsync(contactDetailDto));

          Assert.Equal(expectedError, ex.Message);
        });
      }
    }

    public class Delete
    {
      [Theory]
      [InlineData(1)]
      public async Task DeleteContactSuccessfully(int deletingContactId)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          await contactService.DeleteAsync(deletingContactId);

          var deletedContact = await dataContext.ContactPoint.FirstOrDefaultAsync(c => c.Id == deletingContactId);

          Assert.True(deletedContact.IsDeleted);

        });
      }
    }

    public class Get
    {
      public static IEnumerable<object[]> ContactRequestFilterData =>
            new List<object[]>
            {
                new object[]
                {
                    new ContactRequestFilter { OrganisationId = 1}, 1
                },
                new object[]
                {
                    new ContactRequestFilter { OrganisationId = 2}, 0
                }
            };

      [Theory]
      [MemberData(nameof(ContactRequestFilterData))]
      public async Task ReturnsCorrectList(ContactRequestFilter contactRequestFilter, int expectedCount)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetAsync(contactRequestFilter);

          Assert.Equal(expectedCount, result.Count);

        });
      }

      [Theory]
      [InlineData(1)]
      public async Task ReturnsContact_WhenExsists(int id)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetAsync(id);

          Assert.NotNull(result);

        });
      }

      [Theory]
      [InlineData(2)]
      [InlineData(3)]
      public async Task ReturnsNull_WhenNotExsists(int id)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.GetAsync(id);

          Assert.Null(result);

        });
      }
    }

    public class Update
    {

      public static IEnumerable<object[]> CorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactDetailDto(1, 1, 1, ContactType.OrganisationPerson, "FirstName LastName", "user@email.com", "0112345")
                }
            };

      [Theory]
      [MemberData(nameof(CorrectContactData))]
      public async Task UpdateContactSuccessfully_WhenCorrectData(ContactDetailDto contactDetailDto)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var result = await contactService.UpdateAsync(contactDetailDto.ContactId, contactDetailDto);
          Assert.NotEqual(0, result);

          var updatedContactData = await dataContext.ContactPoint.Where(c => c.Id == result)
            .Include(cp => cp.ContactDetail)
            .Include(cd => cd.ContactDetail.PhysicalAddress)
            .Include(cd => cd.ContactDetail.VirtualAddresses)
            .Include(cp => cp.Party)
            .ThenInclude(p => p.Person)
            .FirstOrDefaultAsync();

          Assert.NotNull(updatedContactData);
          Assert.Equal(contactDetailDto.Name, $"{updatedContactData.Party.Person.FirstName} {updatedContactData.Party.Person.LastName}");
          Assert.Equal(contactDetailDto.Email, updatedContactData.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressType.Id == 1).VirtualAddressValue);
          Assert.Equal(contactDetailDto.PhoneNumber, updatedContactData.ContactDetail.VirtualAddresses.FirstOrDefault(v => v.VirtualAddressType.Id == 2).VirtualAddressValue);
        });
      }

      public static IEnumerable<object[]> InCorrectContactData =>
            new List<object[]>
            {
                new object[]
                {
                    DtoHelper.GetContactDetailDto(1, 1, 1, ContactType.OrganisationPerson, "FirstName LastName", "useremail.com", "0112345"),
                    ErrorConstant.ErrorInvalidEmail
                },
            };

      [Theory]
      [MemberData(nameof(InCorrectContactData))]
      public async Task ThrowsException_WhenInCorrectData(ContactDetailDto contactDetailDto, string expectedError)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          await SetupTestDataAsync(dataContext);
          var contactService = ContactService(dataContext);

          var ex = await Assert.ThrowsAsync<CcsSsoException>(() => contactService.UpdateAsync(contactDetailDto.ContactId, contactDetailDto));

          Assert.Equal(expectedError, ex.Message);
        });
      }
    }



    public static ContactService ContactService(IDataContext dataContext)
    {
      var service = new ContactService(dataContext);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.PartyType.Add(new PartyType { Id = 1, PartyTypeName = PartyTypeName.InternalOrgnaisation });
      dataContext.PartyType.Add(new PartyType { Id = 2, PartyTypeName = PartyTypeName.NonUser });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 1, Name = VirtualContactTypeName.Email, Description = "email" });
      dataContext.VirtualAddressType.Add(new VirtualAddressType { Id = 2, Name = VirtualContactTypeName.Phone, Description = "phone" });
      dataContext.ContactPointReason.Add(new ContactPointReason { Id = 1, Name = "OTHER", Description = "Other" });

      dataContext.Party.Add(new Party { Id = 1, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 1, PartyId = 1, OrganisationUri = "Org1Uri", RightToBuy = true });

      dataContext.Party.Add(new Party { Id = 2, PartyTypeId = 1 });
      dataContext.Organisation.Add(new Organisation { Id = 2, PartyId = 2, OrganisationUri = "Org2Uri", RightToBuy = true });

      dataContext.Party.Add(new Party { Id = 3, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 1, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 1, PartyId = 3, PartyTypeId = 2, ContactPointReasonId = 1, ContactDetailId = 1 });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 1, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 1, ContactDetailId = 1, VirtualAddressTypeId = 1, VirtualAddressValue = "email@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 2, ContactDetailId = 1, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345671" });

      dataContext.Party.Add(new Party { Id = 4, PartyTypeId = 2 });
      dataContext.Person.Add(new Person { Id = 2, PartyId = 3, OrganisationId = 1, FirstName = "PesronFN1", LastName = "LN1" });
      dataContext.ContactPoint.Add(new ContactPoint { Id = 2, PartyId = 4, PartyTypeId = 2, ContactPointReasonId = 1, ContactDetailId = 2, IsDeleted = true });
      dataContext.ContactDetail.Add(new ContactDetail { Id = 2, EffectiveFrom = DateTime.UtcNow });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 3, ContactDetailId = 2, VirtualAddressTypeId = 1, VirtualAddressValue = "email2@mail.com" });
      dataContext.VirtualAddress.Add(new VirtualAddress { Id = 4, ContactDetailId = 2, VirtualAddressTypeId = 2, VirtualAddressValue = "94112345672" });


      await dataContext.SaveChangesAsync();
    }
  }
}
