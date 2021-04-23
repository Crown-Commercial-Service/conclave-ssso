using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using Notify.Client;
using Notify.Models.Responses;
using System.Net.Http;
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
      var client = _httpClientFactory.CreateClient();
      var httpClientWithProxy = new HttpClientWrapper(client);
      var notificationClient = new NotificationClient(httpClientWithProxy, _emailConfigurationInfo.ApiKey);
      EmailNotificationResponse response = await notificationClient.SendEmailAsync(emailInfo.To,
        emailInfo.TemplateId, emailInfo.BodyContent);
    }
  }
}
