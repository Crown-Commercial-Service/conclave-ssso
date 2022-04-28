using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Dtos
{
  public class ConsumerDetail
  {
    public int Id { get; set; }

    public string Name { get; set; }

    public string ClientId { get; set; }
  }

  public class ConsumerSubscriptionDetail
  {
    public int ConsumerId { get; set; }

    public string ConsumerClientId { get; set; }

    public string SubscriptionType { get; set; }

    public string SubscriptionUrl { get; set; }

    public string SubscriptionApiKey { get; set; }

    public string FomatFileType { get; set; }
  }
}
