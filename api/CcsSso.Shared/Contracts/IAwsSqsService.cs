using CcsSso.Shared.Domain.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Shared.Contracts
{
  public interface IAwsSqsService
  {
    Task DeleteMessageAsync(string queueUrl, string messageReceiptHandle);

    Task<List<SqsMessageResponseDto>> ReceiveMessagesAsync(string queueUrl, int? maxMessages = null, int? waitTimeSeconds = null);

    Task SendMessageAsync(string queueUrl, string messageGroupId, string messageBody);

    Task SendMessageAsync(string queueUrl, string messageGroupId, SqsMessageDto sqsMessageDto);

    Task SendMessageBatchAsync(string queueUrl, string messageGroupId, List<SqsMessageDto> sqsMessageDtoList);
  }
}
