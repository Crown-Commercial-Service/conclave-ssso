using CcsSso.DbModel.Entity;
using CcsSso.Domain.Constants;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Exceptions;
using CcsSso.Dtos.Domain.Models;
using CcsSso.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CcsSso.Core.Domain.Contracts;

namespace CcsSso.Service
{
  public class Cii
  {
    public string url { get; set; }
    public string token { get; set; }
  }
  public class CiiService : ICiiService
  {
    private HttpClient _client;
    private readonly CiiConfig _config;
    private readonly IAuditLoginService _auditLoginService;

    //IConfiguration configuration
    //public CiiService(HttpClient client, IOptions<Cii> appSettings)
    public CiiService(HttpClient client, CiiConfig config, IAuditLoginService auditLoginService)
    {
      _client = client;
      _config = config;
      _auditLoginService = auditLoginService;

      _client.DefaultRequestHeaders.Add("Apikey", config.token);
      _client.DefaultRequestHeaders.Add("x-api-key", config.token);
      _client.BaseAddress = new Uri(config.url);
    }

    /// <summary>
    /// Submits a json payload to CII
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>  
    public async Task<string> PostAsync(CiiDto model, string token)
    {
      try
      {
        var body = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var response = await _client.PostAsync("/identities/schemes/organisation", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        return await response.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Submits a json payload to CII via http Put
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> PutAsync(CiiPutDto model, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("clientid", _config.clientId);
        var body = Newtonsoft.Json.JsonConvert.SerializeObject(model);
        var response = await _client.PutAsync("/identities/schemes/organisation" + "?clientid=" + _config.clientId, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryAdd, AuditLogApplication.ManageOrganisation, $"OrgId:{model.ccsOrgId}, Scheme:{model.identifier.scheme}, Id:{model.identifier.id}");
        return await response.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Submits a json payload to CII via http delete
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> DeleteAsync(string id, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("x-api-key", _config.deleteToken);
        _client.DefaultRequestHeaders.Add("clientid", _config.clientId);
        var response = await _client.DeleteAsync("/api/v1/testing/identities/schemes/organisation?org_ccs_id=" + id + "&clientid=" + _config.clientId);
        return await response.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Submits a json payload to CII via http delete
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> DeleteOrgAsync(string id, string token)
    {
      try
      {
        _client.DefaultRequestHeaders.Add("x-api-key", _config.deleteToken);
        var response = await _client.DeleteAsync("/identities/organisation?ccs_org_id=" + id);
        return await response.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Submits a json payload to CII via http delete
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> DeleteSchemeAsync(string orgId, string scheme, string id, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("clientid", _config.clientId);
        var response = await _client.DeleteAsync("/identities/schemes/organisation?ccs_org_id=" + orgId + "&identifier[scheme]=" + scheme + "&identifier[id]=" + id + "&clientid=" + _config.clientId);
        await _auditLoginService.CreateLogAsync(AuditLogEvent.OrgRegistryRemove, AuditLogApplication.ManageOrganisation, $"OrgId:{orgId}, Scheme:{scheme}, Id:{id}");
        return await response.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Submits a json payload to CII via http delete
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<string> DeleteAsyncWithBody(CiiDto model, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("x-api-key", _config.deleteToken);
        var request = new HttpRequestMessage  {
          Method = HttpMethod.Delete,
          RequestUri = new Uri("/api/v1/testing/identities/schemes/organisation?org_ccs_id=" + model.identifier.id),
          Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(model), System.Text.Encoding.UTF8, "application/json")
        };
        var response = await _client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
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
      try
      {
        using var responseStream = await _client.GetStreamAsync("/identities/schemes/organisation?scheme=" + scheme + "&id=" + companyNumber);
        return await JsonSerializer.DeserializeAsync<CiiDto>(responseStream);
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if(ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Retrieves all the schemas from CII
    /// </summary>
    /// <returns></returns>
    public async Task<CiiSchemeDto[]> GetSchemesAsync(string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("clientid", _config.clientId);
        using var responseStream = await _client.GetStreamAsync("/identities/schemes" + "?clientid=" + _config.clientId);
        var result = await JsonSerializer.DeserializeAsync<CiiSchemeDto[]>(responseStream);
        return result;
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        throw;
      }
    }

    /// <summary>
    /// Retrieves a payload from CII
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetOrgAsync(string id, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        using var responseStream = await _client.GetStreamAsync("/api/v1/testing/search/identities/schemes/organisation?id=" + id);
        return await JsonSerializer.DeserializeAsync<CiiDto>(responseStream);
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    public async Task<CiiDto[]> GetOrgsAsync(string id, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("clientid", _config.clientId);
        using var responseStream = await _client.GetStreamAsync("/identities/schemes/organisations?ccs_org_id=" + id + "&clientid=" + _config.clientId);
        var result = await JsonSerializer.DeserializeAsync<CiiDto[]>(responseStream);
        return result;
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

    /// <summary>
    /// Retrieves a payload from CII
    /// </summary>
    /// <param name="scheme"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CiiDto> GetIdentifiersAsync(string orgId, string scheme, string id, string token)
    {
      try
      {
        if (!String.IsNullOrEmpty(token))
        {
          _client.DefaultRequestHeaders.Add("Authorization", token);
        }
        _client.DefaultRequestHeaders.Add("clientid", _config.clientId);
        using var responseStream = await _client.GetStreamAsync("/identities/schemes/manageidentifiers?ccs_org_id=" + orgId + "&scheme=" + scheme + "&id=" + id + "&clientid=" + _config.clientId);
        var result = await JsonSerializer.DeserializeAsync<CiiDto>(responseStream);
        return result;
      }
      catch (Exception ex)
      {
        Console.Write(ex);
        if (ex.Message == "Response status code does not indicate success: 405 (Method Not Allowed).")
        {
          throw new MethodNotAllowedException(ex);
        }
        if (ex.Message == "Response status code does not indicate success: 404 (Not Found).")
        {
          throw new ResourceNotFoundException();
        }
        throw;
      }
    }

  }
}
