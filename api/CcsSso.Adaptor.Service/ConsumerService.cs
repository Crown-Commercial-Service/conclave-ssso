using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Dtos;
using CcsSso.Shared.Cache.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Service
{
  public class ConsumerService : IConsumerService
  {
    private readonly IDataContext _dataContext;
    private readonly ILocalCacheService _localCacheService;
    private readonly AppSetting _appSetting;
    public ConsumerService(IDataContext dataContext, ILocalCacheService localCacheService, AppSetting appSetting)
    {
      _dataContext = dataContext;
      _localCacheService = localCacheService;
      _appSetting = appSetting;
    }

    public async Task<ConsumerDetail> GetConsumerByClientId(string consumerClientId)
    { 
      return await _localCacheService.GetOrSetValueAsync(consumerClientId, async () =>
      {
        var consumer = await _dataContext.AdapterConsumers
        .FirstOrDefaultAsync(c => !c.IsDeleted && c.ClientId == consumerClientId);
        return consumer != null ?
          new ConsumerDetail { Id = consumer.Id, Name = consumer.Name, ClientId = consumer.ClientId } : null;
      }, _appSetting.InMemoryCacheExpirationInMinutes);
    }

    public async Task<ConsumerSubscriptionDetail> GetSubscriptionDetail(int consumerId, string conclaveEntityName)
    {
      return await _localCacheService.GetOrSetValueAsync($"SUB-{consumerId}-{conclaveEntityName}", async () =>
      {
        var subscription = await _dataContext.AdapterSubscriptions
        .Include(s => s.AdapterConsumer).ThenInclude(c => c.AdapterConsumerSubscriptionAuthMethod)
        .Include(s => s.AdapterFormat)
        .FirstOrDefaultAsync(s => !s.IsDeleted && s.AdapterConsumer.Id == consumerId && s.ConclaveEntity.Name == conclaveEntityName);
        return subscription != null ?
          new ConsumerSubscriptionDetail
          {
            ConsumerId = consumerId,
            ConsumerClientId = subscription.AdapterConsumer.ClientId,
            SubscriptionType = subscription.SubscriptionType,
            SubscriptionUrl = subscription.SubscriptionUrl,
            FomatFileType = subscription.AdapterFormat.FomatFileType,
            SubscriptionApiKey = subscription.AdapterConsumer.AdapterConsumerSubscriptionAuthMethod.APIKey
          } : null;
      }, _appSetting.InMemoryCacheExpirationInMinutes);
    }
  }
}
