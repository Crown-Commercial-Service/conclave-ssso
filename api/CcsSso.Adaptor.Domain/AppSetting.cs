namespace CcsSso.Adaptor.Domain
{
  public class AppSetting
  {
    public string ApiKey { get; set; }

    public int InMemoryCacheExpirationInMinutes { get; set; }

    public int OrganisationUserRequestPageSize { get; set; }

    public RedisCacheSetting RedisCacheSettings { get; set; }

    public QueueUrlInfo QueueUrlInfo { get; set; }
  }

  public class RedisCacheSetting
  {
    public string ConnectionString { get; set; }

    public bool IsEnabled { get; set; }

    public int CacheExpirationInMinutes { get; set; }
  }

  public class QueueUrlInfo
  {
    public string PushDataQueueUrl { get; set; }
  }
}
