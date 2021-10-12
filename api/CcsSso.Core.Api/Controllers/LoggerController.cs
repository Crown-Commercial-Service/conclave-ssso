using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace CcsSso.Core.Api.Controllers
{
  [Route("logs")]
  [ApiController]
  public class LoggerController : ControllerBase
  {
    private readonly IAuditLoginService _auditLoginService;
    public LoggerController(IAuditLoginService auditLoginService)
    {
      _auditLoginService = auditLoginService;
    }

    [HttpPost]
    [SwaggerOperation(Tags = new[] { "logs" })]
    [ProducesResponseType(typeof(int), 200)]
    [ProducesResponseType(401)]
    public async Task CreateLog(LogInfo logInfo)
    {
      await _auditLoginService.CreateLogAsync(logInfo.EventName, logInfo.ApplicationName, logInfo.ReferenceData);
    }
  }
}
