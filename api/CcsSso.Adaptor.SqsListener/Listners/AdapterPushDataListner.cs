using CcsSso.Adaptor.Domain.SqsListener;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.SqsListener.Listners
{

  public class AdapterPushDataListner : BackgroundService
  {
    private const string LISTNER_JOB_ADAPTER_PUSH = "AdapterPushDataListener";
    private readonly ILogger<AdapterPushDataListner> _logger;
    private readonly SqsListnerAppSetting _appSetting;
    private readonly IAwsPushDataSqsService _awsPushDataSqsService;

    private readonly IHttpClientFactory _httpClientFactory;

    public AdapterPushDataListner(ILogger<AdapterPushDataListner> logger, SqsListnerAppSetting appSetting, IAwsPushDataSqsService AwsPushDataSqsService,
      IHttpClientFactory httpClientFactory)
    {
      _logger = logger;
      _appSetting = appSetting;
      _awsPushDataSqsService = AwsPushDataSqsService;
      _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        _logger.LogInformation($"Worker: {LISTNER_JOB_ADAPTER_PUSH} running at: {DateTime.UtcNow}");
        Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: job started at: {DateTime.UtcNow}");
        await PerformJobAsync();
        Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: job ended at: {DateTime.UtcNow}");
        await Task.Delay(_appSetting.SqsListnerJobSetting.JobSchedulerExecutionFrequencyInMinutes * 60000, stoppingToken);
      }
    }

    private async Task PerformJobAsync()
    {
      var msgs = await _awsPushDataSqsService.ReceiveMessagesAsync(_appSetting.QueueUrlInfo.PushDataQueueUrl);
      Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: {msgs.Count} messages received at {DateTime.UtcNow}");
      List<Task> taskList = new List<Task>();
      msgs.ForEach((msg) =>
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: Message with id {msg.MessageId} received at {DateTime.UtcNow}");
        taskList.Add(PostPushDataToSubscriptionAsync(msg));
      });
      await Task.WhenAll(taskList);
    }

    private async Task PostPushDataToSubscriptionAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        var url = sqsMessageResponseDto.StringCustomAttributes["URL"];
        var mediaType = sqsMessageResponseDto.StringCustomAttributes["FORMAT"];
        var apiKey = sqsMessageResponseDto.StringCustomAttributes["APIKEY"];

        var client = _httpClientFactory.CreateClient("ConsumerClient");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        HttpContent data = new StringContent(sqsMessageResponseDto.MessageBody, Encoding.UTF8, $"{mediaType}");
        var response = await client.PostAsync(new Uri(url), data);

        if (response.IsSuccessStatusCode)
        {
          Console.WriteLine($"WorkerScuccess: {LISTNER_JOB_ADAPTER_PUSH} :: Message processing succeeded for url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}");
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
        else
        {
          var responseContent = await response.Content.ReadAsStringAsync();
          Console.WriteLine($"WorkerError: {LISTNER_JOB_ADAPTER_PUSH} :: Message processing error for MessageId: {sqsMessageResponseDto.MessageId}, url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}, ErroreCode: {response.StatusCode}, Error: {JsonConvert.SerializeObject(responseContent)}");
          _logger.LogError($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: MessageId: {sqsMessageResponseDto.MessageId}, ErroreCode: {response.StatusCode}, Error: {responseContent}");
          if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
          {
            Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow} for, url: {url}");
            // TODO delete and send to deadletter queue
            await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: Message processing error at: {DateTime.UtcNow} for message {sqsMessageResponseDto.MessageId}");
        if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
        {
          Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow}");
          // TODO delete and send to deadletter queue
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
      }
    }

    private async Task DeleteMessageFromQueueAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: Deleteing message from queue. MessageId: {sqsMessageResponseDto.MessageId}");
        await _awsPushDataSqsService.DeleteMessageAsync(_appSetting.QueueUrlInfo.PushDataQueueUrl, sqsMessageResponseDto.ReceiptHandle);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: Message deleting error at: {DateTime.UtcNow}");
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_ADAPTER_PUSH} :: SQS url: {_appSetting.QueueUrlInfo.PushDataQueueUrl}");
      }
    }
  }
}
