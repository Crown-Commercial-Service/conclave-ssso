using CcsSso.Core.Domain.Contracts;
using CcsSso.Domain.Exceptions;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CcsSso.Core.Service
{
  // #Auto validation
  public class LookUpService : ILookUpService
  {
    private readonly IHttpClientFactory _httpClientFactory;

    public LookUpService(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Search in lookup to check whether domain is valid or not
    /// </summary>
    /// <param name="emailId"></param>
    /// <returns></returns>
    public async Task<bool> IsDomainValidForAutoValidation(string emailId)
    {
      //MailAddress address = new MailAddress(emailId);
      //string domain = address.Host;
      string domain = emailId.Split('@')?[1];

      var client = _httpClientFactory.CreateClient("LookupApi");
      string url = $"?domainName={domain}";
      if (client.DefaultRequestHeaders.Any(x => x.Key == "x-api-key"))
      {
        client.DefaultRequestHeaders.Remove("x-api-key");
      }

      using var response = await client.GetAsync(url);
      var responseString =  await response.Content.ReadAsStringAsync();

      if (response.IsSuccessStatusCode)
      {
        return Convert.ToBoolean(responseString);
      }
      else if (response.StatusCode == HttpStatusCode.NotFound)
      {
        return false;
      }
      else if (response.StatusCode == HttpStatusCode.Unauthorized)
      {
        throw new UnauthorizedAccessException();
      }
      else
      {
        throw new CcsSsoException("ERROR_RETRIEVING_LOOKUP_DETAILS");
      }
    }
  }
}
