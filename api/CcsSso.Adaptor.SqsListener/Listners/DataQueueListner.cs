using CcsSso.Adaptor.Domain.Dtos.Security;
using CcsSso.Adaptor.Domain.SqsListener;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
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

  public class DataQueueListner : BackgroundService
  {
    private const string LISTNER_JOB_NAME = "DataQueueListner";
    private readonly ILogger<DataQueueListner> _logger;
    private readonly SqsListnerAppSetting _appSetting;
    private readonly IAwsDataSqsService _awsDataSqsService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailProviderService _emaillProviderService;

    public DataQueueListner(ILogger<DataQueueListner> logger, SqsListnerAppSetting appSetting, IAwsDataSqsService AwsDataSqsService,
      IHttpClientFactory httpClientFactory, IEmailProviderService emaillProviderService)
    {
      _logger = logger;
      _appSetting = appSetting;
      _awsDataSqsService = AwsDataSqsService;
      _httpClientFactory = httpClientFactory;
      _emaillProviderService = emaillProviderService;
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
      var msgs = await _awsDataSqsService.ReceiveMessagesAsync(_appSetting.QueueUrlInfo.DataQueueUrl);
      Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: {msgs.Count} messages received at {DateTime.UtcNow}");
      List<Task> taskList = new List<Task>();
      msgs.ForEach((msg) =>
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: Message with id {msg.MessageId} received at {DateTime.UtcNow}");
        taskList.Add(ProcessMessageAsync(msg));
      });
      await Task.WhenAll(taskList);
    }

    private async Task ProcessMessageAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        var destination = sqsMessageResponseDto.StringCustomAttributes["Destination"];
        var action = sqsMessageResponseDto.StringCustomAttributes["Action"];

        if (destination == "Security" && action == "POST")
        {
          await CreateUser(sqsMessageResponseDto);
        }
        else if (destination == "Security" && action == "DELETE")
        {
          await DeleteUser(sqsMessageResponseDto);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_NAME} :: Message processing error at: {DateTime.UtcNow} for message {sqsMessageResponseDto.MessageId}");
        if (sqsMessageResponseDto.ReceiveCount > _appSetting.SqsListnerJobSetting.MessageReadThreshold)
        {
          Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: MessageId {sqsMessageResponseDto.MessageId} receive count exceeded at {DateTime.UtcNow}");
          await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
        }
      }
    }

    private async Task CreateUser(SqsMessageResponseDto sqsMessageResponseDto, int retryCount = 0)
    {
      await Task.Delay(_appSetting.DataQueueSettings.DelayInSeconds * 1000);

      var url = "security/users";

      var client = _httpClientFactory.CreateClient("SecurityApi");

      HttpContent data = new StringContent(sqsMessageResponseDto.MessageBody);
      var response = await client.PostAsync(new Uri(url), data);

      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine($"WorkerScuccess: {LISTNER_JOB_NAME} :: Message processing succeeded for url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}");
        await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
      }
      else
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"WorkerError: {LISTNER_JOB_NAME} :: Message processing error for MessageId: {sqsMessageResponseDto.MessageId}, url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}, ErroreCode: {response.StatusCode}, Error: {JsonConvert.SerializeObject(responseContent)}");
        _logger.LogError($"Worker: {LISTNER_JOB_NAME} :: MessageId: {sqsMessageResponseDto.MessageId}, ErroreCode: {response.StatusCode}, Error: {responseContent}");

        if (retryCount <= _appSetting.DataQueueSettings.RetryMaxCount)
        {
          Console.WriteLine($"WorkerScuccess: {LISTNER_JOB_NAME} :: Message processing retry: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}");
          await CreateUser(sqsMessageResponseDto, retryCount++);
        }
        else
        {
          var user = JsonConvert.DeserializeObject<UserInfo>(sqsMessageResponseDto.MessageBody);
          await SendCreateUserErrorNotification(user.Email);
          Console.WriteLine($"WorkerError: {LISTNER_JOB_NAME} :: Message processing retry failed for MessageId: {sqsMessageResponseDto.MessageId}, url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}, ErroreCode: {response.StatusCode}, Error: {JsonConvert.SerializeObject(responseContent)}");
        }
      }
    }

    private async Task DeleteUser(SqsMessageResponseDto sqsMessageResponseDto, int retryCount = 0)
    {
      await Task.Delay(_appSetting.DataQueueSettings.DelayInSeconds * 1000);

      var client = _httpClientFactory.CreateClient("SecurityApi");

      var email = sqsMessageResponseDto.MessageBody;

      var url = "/security/users?email=" + email;

      var response = await client.GetAsync(new Uri(url));

      if (response.IsSuccessStatusCode)
      {
        Console.WriteLine($"WorkerScuccess: {LISTNER_JOB_NAME} :: Message processing succeeded for url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}");
        await DeleteMessageFromQueueAsync(sqsMessageResponseDto);
      }
      else
      {
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"WorkerError: {LISTNER_JOB_NAME} :: Message processing error for MessageId: {sqsMessageResponseDto.MessageId}, url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}, ErroreCode: {response.StatusCode}, Error: {JsonConvert.SerializeObject(responseContent)}");
        _logger.LogError($"Worker: {LISTNER_JOB_NAME} :: MessageId: {sqsMessageResponseDto.MessageId}, ErroreCode: {response.StatusCode}, Error: {responseContent}");

        if (retryCount <= _appSetting.DataQueueSettings.RetryMaxCount)
        {
          Console.WriteLine($"WorkerScuccess: {LISTNER_JOB_NAME} :: Message processing retry: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}");
          await DeleteUser(sqsMessageResponseDto, retryCount++);
        }
        else
        {
          await SendDeleteUserErrorNotification(email);
          Console.WriteLine($"WorkerError: {LISTNER_JOB_NAME} :: Message processing retry failed for MessageId: {sqsMessageResponseDto.MessageId}, url: {url}, data: {JsonConvert.SerializeObject(sqsMessageResponseDto.MessageBody)}, at: {DateTime.UtcNow}, ErroreCode: {response.StatusCode}, Error: {JsonConvert.SerializeObject(responseContent)}");
        }
      }
    }

    private async Task DeleteMessageFromQueueAsync(SqsMessageResponseDto sqsMessageResponseDto)
    {
      try
      {
        Console.WriteLine($"Worker: {LISTNER_JOB_NAME} :: Deleteing message from queue. MessageId: {sqsMessageResponseDto.MessageId}");
        await _awsDataSqsService.DeleteMessageAsync(_appSetting.QueueUrlInfo.DataQueueUrl, sqsMessageResponseDto.ReceiptHandle);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_NAME} :: Message deleting error at: {DateTime.UtcNow}");
        _logger.LogError(ex, $"Worker: {LISTNER_JOB_NAME} :: SQS url: {_appSetting.QueueUrlInfo.DataQueueUrl}");
      }
    }

    private async Task SendCreateUserErrorNotification(string email)
    {
      if (!_appSetting.EmailSettings.SendNotificationsEnabled) return;

      var data = new Dictionary<string, dynamic>
        {
          { "emailaddress", email }
        };

      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in _appSetting.EmailSettings.SendDataQueueErrorNotificationToEmailIds)
      {
        var emailInfo = GetEmailInfo(toEmail, _appSetting.EmailSettings.Auth0CreateUserErrorNotificationTemplateId, data);

        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }
      await Task.WhenAll(emailTaskList);
    }

    private async Task SendDeleteUserErrorNotification(string email)
    {
      if (!_appSetting.EmailSettings.SendNotificationsEnabled) return;

      var data = new Dictionary<string, dynamic>
        {
          { "emailaddress", email }
        };

      List<Task> emailTaskList = new List<Task>();
      foreach (var toEmail in _appSetting.EmailSettings.SendDataQueueErrorNotificationToEmailIds)
      {
        var emailInfo = GetEmailInfo(toEmail, _appSetting.EmailSettings.Auth0DeleteUserErrorNotificationTemplateId, data);

        emailTaskList.Add(_emaillProviderService.SendEmailAsync(emailInfo));
      }
      await Task.WhenAll(emailTaskList);
    }

    private EmailInfo GetEmailInfo(string toEmail, string templateId, Dictionary<string, dynamic> data)
    {
      var emailInfo = new EmailInfo
      {
        To = toEmail,
        TemplateId = templateId,
        BodyContent = data
      };

      return emailInfo;
    }
  }
}
