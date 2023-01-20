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
  public class WrapperNotificationListner : BackgroundService
  {
    private const string LISTNER_JOB_WRAPPER_NOTIFY = "WrapperNotificationListener";
    private readonly ILogger<WrapperNotificationListner> _logger;
    private readonly SqsListnerAppSetting _appSetting;
    private readonly IAwsSqsService _awsSqsService;
    private readonly IHttpClientFactory _httpClientFactory;

    public WrapperNotificationListner(ILogger<WrapperNotificationListner> logger, SqsListnerAppSetting appSetting, IAwsSqsService awsSqsService, IHttpClientFactory httpClientFactory)
    {
      _logger = logger;
      _appSetting = appSetting;
      _awsSqsService = awsSqsService;
      _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        _logger.LogInformation($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} running at: {DateTime.UtcNow}");
        Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: job started at: {DateTime.UtcNow}");
        await PerformJobAsync();
        Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: job ended at: {DateTime.UtcNow}");
        await Task.Delay(_appSetting.SqsListnerJobSetting.JobSchedulerExecutionFrequencyInMinutes * 60000, stoppingToken);
      }
    }

    private async Task PerformJobAsync()
    {
      var msgs = await _awsSqsService.ReceiveMessagesAsync(_appSetting.QueueUrlInfo.AdaptorNotificationQueueUrl);
      Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} ::{msgs.Count} messages received at {DateTime.UtcNow}");
      List<Task> taskList = new List<Task>();
      msgs.ForEach((msg) =>
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: Message with id {msg.MessageId} received at {DateTime.UtcNow}");
        taskList.Add(PostNotificationToAdapterAsync(msg));
      });
      await Task.WhenAll(taskList);
    }

    private async Task PostNotificationToAdapterAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        var client = _httpClientFactory.CreateClient("AdaptorApi");
        var url = "push/receive-push-data";
        client.DefaultRequestHeaders.Add("X-Consumer-ClientId", sqsMessageResponseDto.MessageBody);
        HttpContent data = new StringContent(JsonConvert.SerializeObject(sqsMessageResponseDto, new JsonSerializerSettings
        { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, data);

        if (response.IsSuccessStatusCode)
        {
          Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: Message processing succeeded at: {DateTime.UtcNow}");
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
        else
        {
          var responseContent = await response.Content.ReadAsStringAsync();
          _logger.LogError($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: Message processing error at: {DateTime.UtcNow}");
          _logger.LogError($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: MessageId: {sqsMessageResponseDto.MessageId}, ErroreCode: {response.StatusCode}, Error: {responseContent}");
          if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
          {
            Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow}");
            // TODO delete and send to deadletter queue
            await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: Message processing error at: {DateTime.UtcNow} for message {sqsMessageResponseDto.MessageId}");
        if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
        {
          Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow}");
          // TODO delete and send to deadletter queue
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
      }
    }

    private async Task DeleteMessageFromQueueAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: Deleteing message from queue. MessageId: {sqsMessageResponseDto.MessageId}");
        await _awsSqsService.DeleteMessageAsync(_appSetting.QueueUrlInfo.AdaptorNotificationQueueUrl, sqsMessageResponseDto.ReceiptHandle);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: Message deleting error at: {DateTime.UtcNow}");
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_WRAPPER_NOTIFY} :: SQS url: {_appSetting.QueueUrlInfo.AdaptorNotificationQueueUrl}");

      }
    }
  }
}
