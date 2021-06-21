using CcsSso.Adaptor.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts
{
  public interface IConsumerService
  {
    Task<ConsumerDetail> GetConsumerByClientId(string consumerClientId);

    Task<ConsumerSubscriptionDetail> GetSubscriptionDetail(int consumerId, string conclaveEntityName);
  }
}
