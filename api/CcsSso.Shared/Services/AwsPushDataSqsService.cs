using Amazon.SQS;
using Amazon.SQS.Model;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public class AwsPushDataSqsService : IAwsPushDataSqsService
  {
    // TODO This class and AwsSqsService are same except the constructor where PushDataAccessKeyId and PushDataAccessSecretKey
    // are different from AwsSqsService. This needs to be updated to have same service but handles both configuration settings.

    private const string StringValueType = "String";
    private const string NumberValueType = "Number";

    public AmazonSQSClient _sqsClient;
    private readonly SqsConfiguration _sqsConfiguration;
    public AwsPushDataSqsService(SqsConfiguration sqsConfiguration)
    {
      _sqsConfiguration = sqsConfiguration;
      var sqsConfig = new AmazonSQSConfig
      {
        ServiceURL = sqsConfiguration.ServiceUrl
      };
      _sqsClient = new AmazonSQSClient(sqsConfiguration.PushDataAccessKeyId, sqsConfiguration.PushDataAccessSecretKey, sqsConfig);
    }

    /// <summary>
    /// Delete message from queue
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="messageReceiptHandle"></param>
    /// <returns></returns>
    public async Task DeleteMessageAsync(string queueUrl, string messageReceiptHandle)
    {
      await _sqsClient.DeleteMessageAsync(queueUrl, messageReceiptHandle);
    }


    /// <summary>
    /// Recieve messages
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="maxMessages"></param>
    /// <param name="waitTimeSeconds"></param>
    /// <returns></returns>
    public async Task<List<SqsMessageResponseDto>> ReceiveMessagesAsync(string queueUrl, int? maxMessages = null, int? waitTimeSeconds = null)
    {
      List<SqsMessageResponseDto> sqsMessageResponseDtos = new List<SqsMessageResponseDto>();
      var receiveMessageRequest = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
      {
        QueueUrl = queueUrl,
        MaxNumberOfMessages = maxMessages ?? _sqsConfiguration.RecieveMessagesMaxCount,
        WaitTimeSeconds = waitTimeSeconds ?? _sqsConfiguration.RecieveWaitTimeInSeconds,
        AttributeNames = new List<string> { "All" },
        MessageAttributeNames = new List<string> { "*" }
      });

      receiveMessageRequest?.Messages.ForEach((msg) =>
      {
        var stringValueAttributes = msg.MessageAttributes
        .Where(ma => ma.Value.DataType == StringValueType)
        .Select(ma => new KeyValuePair<string, string>(ma.Key, ma.Value.StringValue)).ToDictionary(a => a.Key, a => a.Value);

        var numberValueAttributes = msg.MessageAttributes
        .Where(ma => ma.Value.DataType == NumberValueType)
        .Select(ma => new KeyValuePair<string, int>(ma.Key, int.Parse(ma.Value.StringValue))).ToDictionary(a => a.Key, a => a.Value);

        sqsMessageResponseDtos.Add(new SqsMessageResponseDto
        {
          MessageBody = msg.Body,
          MessageId = msg.MessageId,
          ReceiptHandle = msg.ReceiptHandle,
          StringCustomAttributes = stringValueAttributes,
          NumberCustomAttributes = numberValueAttributes,
          ReceiveCount = int.Parse(msg.Attributes.First(a => a.Key == "ApproximateReceiveCount").Value)
        });
      });

      return sqsMessageResponseDtos;
    }

    /// <summary>
    /// Send message with only a message body
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="messageBody"></param>
    /// <returns></returns>
    public async Task SendMessageAsync(string queueUrl, string messageGroupId, string messageBody)
    {
      await _sqsClient.SendMessageAsync(queueUrl, messageBody);
    }

    /// <summary>
    /// Send message with additional attributes
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="sqsMessageDto"></param>
    /// <returns></returns>
    public async Task SendMessageAsync(string queueUrl, string messageGroupId, SqsMessageDto sqsMessageDto)
    {
      SendMessageRequest messageRequest = CreateMessage(queueUrl, sqsMessageDto, true, messageGroupId);

      // TODO check how the socket connection managed (whether its managed with AmazonSQSClient or SendMessageRequest)
      // AmazonSQSClient has the service url
      // SendMessageRequest has the queue url
      var result = await _sqsClient.SendMessageAsync(messageRequest);
    }

    /// <summary>
    /// Send a batch of messages with attributes if atleast one message available in the list
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="sqsMessageDtoList"></param>
    /// <param name="messageGroupId"></param>
    /// <returns></returns>
    public async Task SendMessageBatchAsync(string queueUrl, string messageGroupId, List<SqsMessageDto> sqsMessageDtoList)
    {
      {
        IEnumerable<SqsMessageDto[]> _mesageList;
        List<Task> taskList = new List<Task>();
        // Here we are sending 10 messages for each batch call due to the restirction given by the AWSSDK.SQS(3.7.1.14)
        // https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SendMessageBatch.html
        if (sqsMessageDtoList.Any())
        {
          if (sqsMessageDtoList.Count > 10)
          {
            _mesageList = sqsMessageDtoList.Chunk(10);
            foreach (var eachSqlMessageBatch in _mesageList)
            {
              SendMessageBatchRequest messageBatchRequest = CreateMessageBatch(queueUrl, eachSqlMessageBatch.ToList(), true, messageGroupId);
              taskList.Add(_sqsClient.SendMessageBatchAsync(messageBatchRequest));
            }
            await Task.WhenAll(taskList);
          }
          else
          {
            SendMessageBatchRequest messageBatchRequest = CreateMessageBatch(queueUrl, sqsMessageDtoList, true, messageGroupId);
            await _sqsClient.SendMessageBatchAsync(messageBatchRequest);

          }

        }
      }
    }
      

    /// <summary>
    /// Create message send request object
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="sqsMessageDto"></param>
    /// <returns></returns>
    private SendMessageRequest CreateMessage(string queueUrl, SqsMessageDto sqsMessageDto, bool isFifoQueue = true, string messageGroupId = "")
    {
      if (string.IsNullOrWhiteSpace(sqsMessageDto.MessageBody))
      {
        throw new Exception("ERROR_NULL_OR_EMPTY_SQS_MESSAGE_BODY");
      }

      SendMessageRequest messageRequest = new SendMessageRequest
      {
        QueueUrl = queueUrl,
        MessageBody = sqsMessageDto.MessageBody
      };

      if (isFifoQueue)
      {
        messageRequest.MessageGroupId = messageGroupId;
        messageRequest.MessageDeduplicationId = Guid.NewGuid().ToString();
      }

      // Handle string attributes
      if (sqsMessageDto.StringCustomAttributes != null)
      {
        GetStringMessageAttributes(sqsMessageDto.StringCustomAttributes).ForEach((atttributeKeyValue) =>
        {
          messageRequest.MessageAttributes.Add(atttributeKeyValue.Key, atttributeKeyValue.Value);
        });
      }

      // Handle number attributes
      if (sqsMessageDto.NumberCustomAttributes != null)
      {
        GetNumberMessageAttributes(sqsMessageDto.NumberCustomAttributes).ForEach((atttributeKeyValue) =>
        {
          messageRequest.MessageAttributes.Add(atttributeKeyValue.Key, atttributeKeyValue.Value);
        });
      }

      // TODO Handle other data type attributes

      return messageRequest;
    }

    /// <summary>
    /// Create message batch request object
    /// </summary>
    /// <param name="queueUrl"></param>
    /// <param name="sqsMessageDtoList"></param>
    /// <param name="messageGroupId"></param>
    /// <returns></returns>
    private SendMessageBatchRequest CreateMessageBatch(string queueUrl, List<SqsMessageDto> sqsMessageDtoList, bool isFifoQueue = true, string messageGroupId = "")
    {
      SendMessageBatchRequest sendMessageBatchRequest = new SendMessageBatchRequest
      {
        QueueUrl = queueUrl,
        Entries = new List<SendMessageBatchRequestEntry>()
      };

      for (int i = 0; i < sqsMessageDtoList.Count; i++)
      {
        var test = CreateMessageBatchRequestEntry(sqsMessageDtoList[i], i.ToString(), isFifoQueue, messageGroupId);
        sendMessageBatchRequest.Entries.Add(test);
      }

      return sendMessageBatchRequest;
    }

    /// <summary>
    /// Create messages in message batch
    /// </summary>
    /// <param name="sqsMessageDto"></param>
    /// <param name="messageGroupId"></param>
    /// <returns></returns>
    private SendMessageBatchRequestEntry CreateMessageBatchRequestEntry(SqsMessageDto sqsMessageDto, string msgId, bool isFifoQueue = true, string messageGroupId = "")
    {
      if (string.IsNullOrWhiteSpace(sqsMessageDto.MessageBody))
      {
        throw new Exception("ERROR_NULL_OR_EMPTY_SQS_MESSAGE_BODY");
      }

      SendMessageBatchRequestEntry sendMessageBatchRequestEntry = new SendMessageBatchRequestEntry
      {
        Id = msgId,
        MessageBody = sqsMessageDto.MessageBody
      };

      if (isFifoQueue)
      {
        sendMessageBatchRequestEntry.MessageGroupId = messageGroupId;
        sendMessageBatchRequestEntry.MessageDeduplicationId = $"{msgId}{messageGroupId}-{Guid.NewGuid()}";
      }

      // Handle string attributes
      if (sqsMessageDto.StringCustomAttributes != null)
      {
        GetStringMessageAttributes(sqsMessageDto.StringCustomAttributes).ForEach((atttributeKeyValue) =>
        {
          sendMessageBatchRequestEntry.MessageAttributes.Add(atttributeKeyValue.Key, atttributeKeyValue.Value);
        });
      }

      // Handle number attributes
      if (sqsMessageDto.NumberCustomAttributes != null)
      {
        GetNumberMessageAttributes(sqsMessageDto.NumberCustomAttributes).ForEach((atttributeKeyValue) =>
        {
          sendMessageBatchRequestEntry.MessageAttributes.Add(atttributeKeyValue.Key, atttributeKeyValue.Value);
        });
      }

      return sendMessageBatchRequestEntry;
    }

    /// <summary>
    /// Get message attributes with string values (DataType = String)
    /// </summary>
    /// <param name="stringCustomAttributes"></param>
    /// <returns></returns>
    private List<KeyValuePair<string, MessageAttributeValue>> GetStringMessageAttributes(Dictionary<string, string> stringCustomAttributes)
    {
      List<KeyValuePair<string, MessageAttributeValue>> messageAttributeValues = new();

      foreach (KeyValuePair<string, string> property in stringCustomAttributes)
      {
        messageAttributeValues.Add(
          new KeyValuePair<string, MessageAttributeValue>(property.Key, new MessageAttributeValue
          {
            DataType = StringValueType,
            StringValue = property.Value
          }));
      }

      return messageAttributeValues;
    }

    /// <summary>
    /// Get message attributes with numeric values (DataType = Number)
    /// </summary>
    /// <param name="numberCustomAttributes"></param>
    /// <returns></returns>
    private List<KeyValuePair<string, MessageAttributeValue>> GetNumberMessageAttributes(Dictionary<string, int> numberCustomAttributes)
    {
      List<KeyValuePair<string, MessageAttributeValue>> messageAttributeValues = new();

      foreach (KeyValuePair<string, int> property in numberCustomAttributes)
      {
        messageAttributeValues.Add(
          new KeyValuePair<string, MessageAttributeValue>(property.Key, new MessageAttributeValue
          {
            DataType = NumberValueType,
            StringValue = property.Value.ToString()
          }));
      }

      return messageAttributeValues;
    }
  }
}