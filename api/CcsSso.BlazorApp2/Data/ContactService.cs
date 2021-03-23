// using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CcsSso.BlazorApp2.Data
{
    public class ContactService
    {
        private HttpClient _client;

        public ContactService(HttpClient client)
        {
          _client = client;
        }

        public async Task<ContactResponse> Get(int id)
        {
          try
          {
            using var responseStream = await _client.GetStreamAsync("https://dev-api-core.london.cloudapps.digital/contact/" + id);
            return await JsonSerializer.DeserializeAsync<ContactResponse>(responseStream);
          }
          catch (Exception e)
          {
            return null;
          }
        }

    public async Task Post(ContactModel model)
    {
      try
      {
        var body = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
          name = model.name,
          email = model.email,
          phoneNumber = model.phoneNumber,
          contactType = 1,
          organisationId = 1//model.organisationId,
        });
        using var responseStream = await _client.PostAsync("https://dev-api-core.london.cloudapps.digital/contact", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Console.WriteLine(responseStream.StatusCode);
      }
      catch (Exception e)
      {
        //return null;
      }
    }

    public async Task Put(ContactModel model)
    {
      try
      {
        var body = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
          contactId = model.id,
          name = model.name,
          email = model.email,
          phoneNumber = model.phoneNumber,
          // address = null,
          // contactReason = null,
          contactType = 1,
          // fax = null,
          organisationId = 1,
          partyId = model.partyId,
        });
        using var responseStream = await _client.PutAsync("https://dev-api-core.london.cloudapps.digital/contact/" + model.id, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Console.WriteLine(responseStream.StatusCode);
      }
      catch (Exception e)
      {
        //return null;
      }
    }

    public async Task<ContactResponse[]> GetByOrgId(int id)
        {
          try
          {
            using var responseStream = await _client.GetStreamAsync("https://dev-api-core.london.cloudapps.digital/contact?organisationId="+ id);
            return await JsonSerializer.DeserializeAsync<ContactResponse[]>(responseStream);
          }
          catch (Exception e)
          {
            return null;
          }
        }
    }

  public class ContactResponse
  {
    [JsonPropertyName("address")]
    public Address address { get; set; }

    [JsonPropertyName("contactId")]
    public int contactId { get; set; }

    [JsonPropertyName("partyId")]
    public int partyId { get; set; }

    [JsonPropertyName("organisationId")]
    public int organisationId { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("email")]
    public string email { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string phoneNumber { get; set; }

    [JsonPropertyName("teamName")]
    public string teamName { get; set; }

    public class Address
    {
      [JsonPropertyName("streetAddress")]
      public string streetAddress { get; set; }

      [JsonPropertyName("locality")]
      public string locality { get; set; }

      [JsonPropertyName("region")]
      public string region { get; set; }

      [JsonPropertyName("postalCode")]
      public string postalCode { get; set; }

      [JsonPropertyName("countryCode")]
      public string countryCode { get; set; }

      [JsonPropertyName("uprn")]
      public string uprn { get; set; }
    }
  }

  public class ContactModel
  {
    public int id { get; set; }

    [Required]
    public string name { get; set; }

    [Required]
    public string email { get; set; }

    [Required]
    public string phoneNumber { get; set; }

    public string address { get; set; }
    public string contactReason { get; set; }
    public int contactType { get; set; }
    public string fax { get; set; }
    public int organisationId { get; set; }
    public int partyId { get; set; }
    public string teamName { get; set; }
    public string webUrl { get; set; }
  }
}
