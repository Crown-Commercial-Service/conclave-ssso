using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos.Exceptions;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CcsSso.Shared.Services;

namespace CcsSso.Service
{
  public class Cii
  {
    public string url { get; set; }
    public string token { get; set; }
  }
  public class CiiService : ICiiService
  {
    private readonly CiiConfig _config;
    private readonly IAuditLoginService _auditLoginService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataContext _dataContext;

    public CiiService(CiiConfig config, IAuditLoginService auditLoginService, IHttpClientFactory httpClientFactory, IDataContext dataContext)
    {
      _config = config;
      _auditLoginService = auditLoginService;
      _httpClientFactory = httpClientFactory;
      _dataContext = dataContext;
    }

    /// <summary>
    /// Add an additional registry to an organisation
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task AddSchemeAsync(string ciiOrganisationId, string scheme, string identifier, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      client.DefaultRequestHeaders.Add("Authorization", token);
      //var body = JsonConvert.SerializeObject(model);
      var response = await client.PutAsync($"identities/organisations/{ciiOrganisationId}/schemes/{scheme}/identifiers/{identifier}", new StringContent("", System.Text.Encoding.UTF8, "application/json"));
      if (response.IsSuccessStatusCode)
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryAdd, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, Scheme:{scheme}, Id:{identifier}");
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else
      {
        throw new CcsSsoException("ERROR_ADDING_IDENTIFIER");
      }
    }

    /// <summary>
    /// Delete the orgaisation from CII
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <returns></returns>
    public async Task DeleteOrgAsync(string ciiOrganisationId)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      client.DefaultRequestHeaders.Remove("x-api-key");
      client.DefaultRequestHeaders.Add("x-api-key", _config.deleteToken);
      var response = await client.DeleteAsync("identities/organisations/" + ciiOrganisationId);
      if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (!response.IsSuccessStatusCode)
      {
        throw new CcsSsoException("ERROR_DELETING_CII_ORGANISATION");
      }
    }

    /// <summary>
    /// Delete an identifier of a registered organisation
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="scheme"></param>
    /// <param name="identifier"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task DeleteSchemeAsync(string ciiOrganisationId, string scheme, string identifier, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      client.DefaultRequestHeaders.Add("Authorization", token);
      var response = await client.DeleteAsync($"identities/organisations/{ciiOrganisationId}/schemes/{scheme}/identifiers/{identifier}");
      if (response.IsSuccessStatusCode)
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryRemove, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, Scheme:{scheme}, Id:{identifier}");
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else
      {
        throw new CcsSsoException("ERROR_DELETING_IDENTIFIER");
      }
    }

    /// <summary>
    /// Retrieves organisation details from CII by scheme and identifier
    /// And also checks whther this identifier has already been used
    /// </summary>rd
    /// <param name="scheme"></param>
    /// <param name="identifier"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetIdentifierDetailsAsync(string scheme, string identifier)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      var response = await client.GetAsync($"identities/schemes/{scheme}/identifiers/{identifier}");
      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CiiDto>(content);

        string CountryCode = string.Empty;
        if (result.Address.CountryName != null && result.Address.CountryName != "")
        {
          CountryCode = (await _dataContext.CountryDetails.FirstOrDefaultAsync(x => x.IsDeleted == false && x.Name.ToLower() == result.Address.CountryName.ToLower()))?.Code;
        }
        result.Address.CountryCode = CountryCode;
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (response.StatusCode == HttpStatusCode.Conflict)
      {
        var conflictResultContent = await response.Content.ReadAsStringAsync();
        throw new ResourceAlreadyExistsException(conflictResultContent);
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_IDENTIFIER_DETAILS");
      }
    }

    /// <summary>
    /// Retrieves identifier info using scheme and identifier from CII
    /// This check the given dentifier is valid, already exists for other organisations in the CII
    /// This is required to call before adding additional identifiers to an existing organisation
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="scheme"></param>
    /// <param name="identifier"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetOrganisationIdentifierDetailsAsync(string ciiOrganisationId, string scheme, string identifier, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      client.DefaultRequestHeaders.Add("Authorization", token);
      using var response = await client.GetAsync($"identities/organisations/{ciiOrganisationId}/schemes/{scheme}/identifiers/{identifier}");
      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CiiDto>(content);
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (response.StatusCode == HttpStatusCode.Conflict)
      {
        throw new ResourceAlreadyExistsException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_ORGANISATIONS_IDENTIFIER");
      }
    }

    /// <summary>
    /// Get cii details by org id (CII returns a list)
    /// </summary>
    /// <param name="ciiOrganisationId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetOrgDetailsAsync(string ciiOrganisationId, string token = null, bool includeHiddenIdentifiers = false)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      string url = $"identities/organisations/{ciiOrganisationId}";
      if (includeHiddenIdentifiers)
      {
        if (!string.IsNullOrEmpty(token))
        {
          client.DefaultRequestHeaders.Add("Authorization", token);

          // #1453
          // Token based authenication is not working in local with CII API, it will wokr on server.
          // Below condition to exclude this for local machine. 
          // Send x-api-key only when token is not available
#if !DEBUG
          if (client.DefaultRequestHeaders.Any(x => x.Key == "x-api-key"))
          {
            client.DefaultRequestHeaders.Remove("x-api-key");
          }
#endif
        }
        else
        {
          url += "/all";
        }
      }
      using var response = await client.GetAsync(url);
      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var ciiInfo = JsonConvert.DeserializeObject<CiiDto>(content);

        var orgDetails = await GetOrgDetails(ciiOrganisationId);
        if (orgDetails != null && orgDetails.Address != null)
        {
          ciiInfo.Address = new CiiAddress()
          {
            CountryName = GetCountryNameByCode(orgDetails.Address.CountryCode),
            CountryCode = orgDetails.Address.CountryCode,
            PostalCode = orgDetails.Address.PostalCode,
            Region = orgDetails.Address.Region,
            StreetAddress = orgDetails.Address.StreetAddress,
            Locality = orgDetails.Address.Locality
          };
        }
        return ciiInfo;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (response.StatusCode == HttpStatusCode.Unauthorized) // This CII endpoints requires a access token
      {
        throw new UnauthorizedAccessException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_ORGANISATIONS");
      }
    }

    /// <summary>
    /// Retrieves CountryName based on country code
    /// </summary>
    /// <returns></returns>
    public string GetCountryNameByCode(string countyCode)
    {
      try
      {
        string CountryName = string.Empty;
        if (!string.IsNullOrEmpty(countyCode))
        {
          CountryName = _dataContext.CountryDetails.FirstOrDefault(x => x.IsDeleted == false && x.Code == countyCode).Name;
        }
        return CountryName;
      }
      catch (ArgumentException)
      {
      }
      return null;
    }

    /// <summary>
    /// Retrieves all the schemas from CII
    /// </summary>
    /// <returns></returns>
    public async Task<CiiSchemeDto[]> GetSchemesAsync()
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      var response = await client.GetAsync("identities/schemes");
      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CiiSchemeDto[]>(content);
        return result;
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_SCHEMES");
      }
    }


    /// <summary>
    /// Register an Organisation in CII
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>  
    public async Task<string> PostAsync(CiiDto model)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      var body = JsonConvert.SerializeObject(model);
      var response = await client.PostAsync("identities/organisations", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CiiPostResponceDto>(content);
        return result.OrganisationId;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (response.StatusCode == HttpStatusCode.Conflict)
      {
        throw new ResourceAlreadyExistsException();
      }
      else
      {
        throw new CcsSsoException("ERROR_CREATING_ORGANISATION");
      }
    }

    private async Task<OrganisationDto> GetOrgDetails(string id)
    {
      var organisation = await _dataContext.Organisation
        .Where(x => x.CiiOrganisationId == id && x.IsDeleted == false)
        .FirstOrDefaultAsync();
      if (organisation != null)
      {
        var orgInfo = new OrganisationDto();
        var contactPoint = await _dataContext.ContactPoint
          .Include(c => c.ContactDetail)
          .ThenInclude(c => c.PhysicalAddress)
        .Where(x => x.PartyId == organisation.PartyId)
        .FirstOrDefaultAsync();

        var physicalAddress = contactPoint?.ContactDetail?.PhysicalAddress;

        if (physicalAddress != null)
        {
          string CountryName = string.Empty;
          if (physicalAddress.CountryCode != null)
          {
            CountryName = GetCountryNameByCode(physicalAddress.CountryCode);
          }

          orgInfo.Address = new Address
          {
            StreetAddress = physicalAddress.StreetAddress,
            Region = physicalAddress.Region,
            PostalCode = physicalAddress.PostalCode,
            Locality = physicalAddress.Locality,
            CountryCode = physicalAddress.CountryCode,
            CountryName = CountryName.ToString(),
            Uprn = physicalAddress.Uprn,
          };
        }
        return orgInfo;
      }
      return null;
    }

  }
}
