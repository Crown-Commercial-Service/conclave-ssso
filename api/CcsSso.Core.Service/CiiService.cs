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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataContext _dataContext;

    public CiiService(CiiConfig config, IHttpClientFactory httpClientFactory, IDataContext dataContext)
    {
      _config = config;
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
      if (!string.IsNullOrEmpty(token))
      {
        client.DefaultRequestHeaders.Add("Authorization", token);
      }      
      //var body = JsonConvert.SerializeObject(model);
      var response = await client.PutAsync($"identities/organisations/{ciiOrganisationId}/schemes/{scheme}/identifiers/{identifier}", new StringContent("", System.Text.Encoding.UTF8, "application/json"));
      if (response.IsSuccessStatusCode)
      {
        //await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryAdd, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, Scheme:{scheme}, Id:{identifier}");
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
        //await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryRemove, AuditLogApplication.ManageOrganisation, $"OrgId:{ciiOrganisationId}, Scheme:{scheme}, Id:{identifier}");
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

  }
}
