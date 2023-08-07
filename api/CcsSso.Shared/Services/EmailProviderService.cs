using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class EmailProviderService : IEmailProviderService
  {
    private readonly EmailConfigurationInfo _emailConfigurationInfo;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailProviderService(EmailConfigurationInfo emailConfigurationInfo, IHttpClientFactory httpClientFactory)
    {
      _emailConfigurationInfo = emailConfigurationInfo;
      _httpClientFactory = httpClientFactory;
    }
		public async Task SendEmailAsync(EmailInfo emailInfo)
		{
			try
			{
				var client = _httpClientFactory.CreateClient("NotificationApi");
				string url = $"email";
				var bodyContent = new Dictionary<string, dynamic>();
				if (emailInfo != null && emailInfo.BodyContent == null)
				{
					emailInfo.BodyContent = bodyContent;
				}
				HttpContent data = new StringContent(JsonConvert.SerializeObject(emailInfo, new JsonSerializerSettings
				{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");
				var response = await client.PostAsync(url, data);
				if (!response.IsSuccessStatusCode)
				{
					string errorContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"ERROR SENDING EMAIL NOTIFICATION. Status Code: {response.StatusCode}. Error Content: {errorContent}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("ERROR SENDING EMAIL NOTIFICATION. Status Code" + ex.Message);
			}

		}
	}
}
