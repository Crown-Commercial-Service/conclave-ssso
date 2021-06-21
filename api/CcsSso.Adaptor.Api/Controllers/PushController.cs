using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Shared.Domain.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Api.Controllers
{
  [ApiExplorerSettings(IgnoreApi = true)]
  [Route("push")]
  [ApiController]
  public class PushController : ControllerBase
  {
    private readonly IPushService _pushService;
    public PushController(IPushService pushService)
    {
      _pushService = pushService;
    }

    [HttpPost("receive-push-data")]
    public async Task ReceivePushData(SqsMessageResponseDto sqsMessageResponseDto)
    {
      await _pushService.PublishPushDataAsync(sqsMessageResponseDto);
    }
  }
}
