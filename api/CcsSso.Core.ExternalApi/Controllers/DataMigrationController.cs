using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.ExternalApi.Authorisation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;

namespace CcsSso.Core.ExternalApi.Controllers
{
  [Route("datamigration")]
  [ApiController]
  public class DataMigrationController : ControllerBase
  {
    private readonly IDataMigrationService _dataMigrationService;
    public DataMigrationController(IDataMigrationService dataMigrationService)
    {
      _dataMigrationService = dataMigrationService;
    }

    [HttpPost("upload")]
    [ClaimAuthorise("DATA_MIGRATION")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Tags = new[] { "Data Migration" })]
    public async Task<DataMigrationStatusResponse> UploadDataMigrationFile(IFormFile file)
    {
      var result = await _dataMigrationService.UploadDataMigrationFileAsync(file);
      return result;
    }

    [HttpGet("status")]
    [ClaimAuthorise("DATA_MIGRATION")]
    [SwaggerOperation(Tags = new[] { "Data Migration" })]
    public async Task<DataMigrationStatusResponse> CheckDataMigrationStatus(string id)
    {
      var result = await _dataMigrationService.CheckDataMigrationStatusAsync(id);
      return result;
    }
  }
}
