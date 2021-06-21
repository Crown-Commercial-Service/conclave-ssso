using CcsSso.Shared.Domain.Dto;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts
{
  public interface IPushService
  {
    Task PublishPushDataAsync(SqsMessageResponseDto sqsMessageResponseDto);
  }
}
