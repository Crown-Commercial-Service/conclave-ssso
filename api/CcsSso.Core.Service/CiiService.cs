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
using System.Web;

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

    public CiiService(CiiConfig config, IAuditLoginService auditLoginService, IHttpClientFactory httpClientFactory)
    {
      _config = config;
      _auditLoginService = auditLoginService;
      _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Submits a json payload to CII
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>  
    public async Task<string> PostAsync(CiiDto model)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      var body = JsonConvert.SerializeObject(model);
      var response = await client.PostAsync("/identities/schemes/organisation", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CiiPostResponceDto[]>(content);
        return result.First().CcsOrgId;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
      {
        throw new ResourceAlreadyExistsException();
      }
      else
      {
        throw new CcsSsoException("ERROR_CREATING_ORGANISATION");
      }
    }

    /// <summary>
    /// Submits a json payload to CII via http Put
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task PutAsync(CiiPutDto model, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      if (!String.IsNullOrEmpty(token))
      {
        client.DefaultRequestHeaders.Add("Authorization", token);
      }
      client.DefaultRequestHeaders.Add("clientid", _config.clientId);
      var body = JsonConvert.SerializeObject(model);
      var response = await client.PutAsync("/identities/schemes/organisation" + "?clientid=" + _config.clientId, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
      if (response.IsSuccessStatusCode)
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryAdd, AuditLogApplication.ManageOrganisation, $"OrgId:{model.ccsOrgId}, Scheme:{model.identifier.Scheme}, Id:{model.identifier.Id}");
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
    /// Submits a json payload to CII via http delete
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task DeleteOrgAsync(string id)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      client.DefaultRequestHeaders.Remove("x-api-key");
      client.DefaultRequestHeaders.Add("x-api-key", _config.deleteToken);
      var response = await client.DeleteAsync("/identities/organisation?ccs_org_id=" + HttpUtility.UrlEncode(id));
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
    /// Submits a json payload to CII via http delete
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task DeleteSchemeAsync(string orgId, string scheme, string id, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      if (!String.IsNullOrEmpty(token))
      {
        client.DefaultRequestHeaders.Add("Authorization", token);
      }
      client.DefaultRequestHeaders.Add("clientid", _config.clientId);
      var response = await client.DeleteAsync("/identities/schemes/organisation?ccs_org_id=" + HttpUtility.UrlEncode(orgId) + "&identifier[scheme]=" + HttpUtility.UrlEncode(scheme) + "&identifier[id]=" + HttpUtility.UrlEncode(id) + "&clientid=" + _config.clientId);
      if (response.IsSuccessStatusCode)
      {
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryRemove, AuditLogApplication.ManageOrganisation, $"OrgId:{orgId}, Scheme:{scheme}, Id:{id}");
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
    /// Retrieves a payload from CII
    /// </summary>
    /// <param name="scheme"></param>
    /// <param name="companyNumber"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetAsync(string scheme, string companyNumber, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      var response = await client.GetAsync("/identities/schemes/organisation?scheme=" + HttpUtility.UrlEncode(scheme) + "&id=" + HttpUtility.UrlEncode(companyNumber));
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
      else if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
      {
        throw new ResourceAlreadyExistsException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_ORGANISATIONS_BY_COMPANY_NUMBER");
      }
    }

    /// <summary>
    /// Retrieves all the schemas from CII
    /// </summary>
    /// <returns></returns>
    public async Task<CiiSchemeDto[]> GetSchemesAsync(string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      if (!String.IsNullOrEmpty(token))
      {
        client.DefaultRequestHeaders.Add("Authorization", token);
      }
      client.DefaultRequestHeaders.Add("clientid", _config.clientId);
      var response = await client.GetAsync("/identities/schemes" + "?clientid=" + _config.clientId);
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
    /// Get cii details by org id (CII returns a list)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<CiiDto[]> GetOrgsAsync(string id, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      if (!String.IsNullOrEmpty(token))
      {
        client.DefaultRequestHeaders.Add("Authorization", token);
      }
      client.DefaultRequestHeaders.Add("clientid", _config.clientId);
      using var response = await client.GetAsync("/identities/schemes/organisations?ccs_org_id=" + HttpUtility.UrlEncode(id) + "&clientid=" + _config.clientId);
      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CiiDto[]>(content);
        return result;
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        throw new ResourceNotFoundException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_ORGANISATIONS");
      }
    }

    /// <summary>
    /// Retrieves org info using scheme and id from CII
    /// </summary>
    /// <param name="scheme"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetIdentifiersAsync(string orgId, string scheme, string id, string token)
    {
      var client = _httpClientFactory.CreateClient("CiiApi");
      if (!String.IsNullOrEmpty(token))
      {
        client.DefaultRequestHeaders.Add("Authorization", token);
      }
      client.DefaultRequestHeaders.Add("clientid", _config.clientId);
      using var response = await client.GetAsync("/identities/schemes/manageidentifiers?ccs_org_id=" + HttpUtility.UrlEncode(orgId) + "&scheme=" + HttpUtility.UrlEncode(scheme) + "&id=" + HttpUtility.UrlEncode(id) + "&clientid=" + _config.clientId);
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
      else if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
      {
        throw new ResourceAlreadyExistsException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_IDENTIFIERS");
      }
    }

  }
}
