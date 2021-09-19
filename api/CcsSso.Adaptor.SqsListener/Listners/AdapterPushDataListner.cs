using CcsSso.Adaptor.Domain.SqsListener;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Dto;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private const string LISTNER_JOB_NAME = "Adapter Push Data Listener";
    private readonly ILogger<AdapterPushDataListner> _logger;
    private readonly SqsListnerAppSetting _appSetting;
    private readonly IAwsSqsService _awsSqsService;
    private readonly IHttpClientFactory _httpClientFactory;

    public AdapterPushDataListner(ILogger<AdapterPushDataListner> logger, SqsListnerAppSetting appSetting, IAwsSqsService awsSqsService, IHttpClientFactory httpClientFactory)
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
        _logger.LogInformation($"Worker: {LISTNER_JOB_NAME} running at: {DateTime.UtcNow}");
        Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: job started at: {DateTime.UtcNow}");
        await PerformJobAsync();
        Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: job ended at: {DateTime.UtcNow}");
        await Task.Delay(_appSetting.SqsListnerJobSetting.JobSchedulerExecutionFrequencyInMinutes * 60000, stoppingToken);
      }
    }

    private async Task PerformJobAsync()
    {
      var msgs = await _awsSqsService.ReceiveMessagesAsync(_appSetting.QueueUrlInfo.PushDataQueueUrl);
      Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: {msgs.Count} messages received at {DateTime.UtcNow}");
      List<Task> taskList = new List<Task>();
      msgs.ForEach((msg) =>
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: Message with id {msg.MessageId} received at {DateTime.UtcNow}");
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

        var client = _httpClientFactory.CreateClient("ConsumerClient");

        HttpContent data = new StringContent(sqsMessageResponseDto.MessageBody, Encoding.UTF8, $"application/{mediaType}");
        var response = await client.PostAsync(new Uri(url), data);

        if (response.IsSuccessStatusCode)
        {
          Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: Message processing succeeded at: {DateTime.UtcNow}");
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
        else
        {
          var responseContent = await response.Content.ReadAsStringAsync();
          _logger.LogError($"Worker: {LISTNER_JOB_NAME} :: Message processing error at: {DateTime.UtcNow}");
          _logger.LogError($"Worker: {LISTNER_JOB_NAME} :: MessageId: {sqsMessageResponseDto.MessageId}, ErroreCode: {response.StatusCode}, Error: {responseContent}");
          if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
          {
            Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow}");
            // TODO delete and send to deadletter queue
            await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_NAME} :: Message processing error at: {DateTime.UtcNow} for message {sqsMessageResponseDto.MessageId}");
        if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
        {
          Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow}");
          // TODO delete and send to deadletter queue
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
      }
    }

    private async Task DeleteMessageFromQueueAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: Deleteing message from queue. MessageId: {sqsMessageResponseDto.MessageId}");
        await _awsSqsService.DeleteMessageAsync(_appSetting.QueueUrlInfo.AdapterNotificationQueueUrl, sqsMessageResponseDto.ReceiptHandle);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_NAME} :: Message deleting error at: {DateTime.UtcNow}");
      }
    }
  }
}
