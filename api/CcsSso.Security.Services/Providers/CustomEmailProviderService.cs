using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using Notify.Client;
using Notify.Models.Responses;
using System.Net.Http;
using System.Threading.Tasks;

namespace CcsSso.Security.Services.Providers
{
  public class CustomEmailProviderService : IEmaillProviderService
  {
    private readonly ApplicationConfigurationInfo _applicationConfigurationInfo;
    private readonly IHttpClientFactory _httpClientFactory;

    public CustomEmailProviderService(ApplicationConfigurationInfo applicationConfigurationInfo, IHttpClientFactory httpClientFactory)
    {
      _applicationConfigurationInfo = applicationConfigurationInfo;
      _httpClientFactory = httpClientFactory;
    }
    public async Task SendEmailAsync(EmailInfo emailInfo)
    {
      var client = _httpClientFactory.CreateClient();
      var httpClientWithProxy = new HttpClientWrapper(client);
      var notificationClient = new NotificationClient(httpClientWithProxy, _applicationConfigurationInfo.EmailConfigurationInfo.ApiKey);
      EmailNotificationResponse response = await notificationClient.SendEmailAsync(emailInfo.To,
        emailInfo.TemplateId, emailInfo.BodyContent);
    }
  }
}
