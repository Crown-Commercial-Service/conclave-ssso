using CcsSso.Adaptor.DbDomain.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.DbDomain
{
  public interface IDataContext
  {
    DbSet<AdapterConclaveAttributeMapping> AdapterConclaveAttributeMappings { get; set; }

    DbSet<AdapterConsumer> AdapterConsumers { get; set; }

    DbSet<AdapterConsumerEntity> AdapterConsumerEntities { get; set; }

    DbSet<AdapterConsumerEntityAttribute> AdapterConsumerEntityAttributes { get; set; }

    DbSet<AdapterFormat> AdapterFormats { get; set; }

    DbSet<AdapterSubscription> AdapterSubscriptions { get; set; }

    DbSet<ConclaveEntity> ConclaveEntities { get; set; }

    DbSet<ConclaveEntityAttribute> ConclaveEntityAttributes { get; set; }

    DbSet<AdapterConsumerSubscriptionAuthMethod> AdapterConsumerSubscriptionAuthMethods { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  }
}
