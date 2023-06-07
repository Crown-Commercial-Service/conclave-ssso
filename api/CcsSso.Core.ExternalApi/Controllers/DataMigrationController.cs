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

    /// <summary>
    /// Allows a user upload file for data migration
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// 
    /// Sample request:
    ///
    ///  POST /datamigration/upload
    ///     
    /// </remarks>
    [HttpPost("upload")]
    [ClaimAuthorise("DATA_MIGRATION")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Tags = new[] { "Data Migration" })]
    public async Task<DataMigrationStatusResponse> UploadDataMigrationFile(IFormFile file)
    {
      var result = await _dataMigrationService.UploadDataMigrationFileAsync(file);
      return result;
    }

    /// <summary>
    /// Allows a user to check status of file
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// 
    /// Sample request:
    ///
    ///     GET /datamigration/status?id=a77bbf38-8d24-4835-920d-1533a349f0c1
    ///     
    /// </remarks>
    [HttpGet("status")]
    [ClaimAuthorise("DATA_MIGRATION")]
    [SwaggerOperation(Tags = new[] { "Data Migration" })]
    public async Task<DataMigrationStatusResponse> CheckDataMigrationStatus(string id)
    {
      var result = await _dataMigrationService.CheckDataMigrationStatusAsync(id);
      return result;
    }

    /// <summary>
    /// Allows a user to retrieve history of uploaded files
    /// </summary>
    /// <response  code="200">Ok</response>
    /// <response  code="401">Unauthorised</response>
    /// <response  code="403">Forbidden</response>
    /// <response  code="404">Not found</response>
    /// <remarks>
    /// NOTE:- query params page-size, current-page
    /// Sample request:
    ///
    ///     GET /datamigration/files?page-size=10,current-page=1
    ///     
    /// </remarks>
    [HttpGet("files")]
    [ClaimAuthorise("DATA_MIGRATION")]
    [SwaggerOperation(Tags = new[] { "Data Migration" })]
    [ProducesResponseType(typeof(DataMigrationListResponse), 200)]
    public async Task<DataMigrationListResponse> GetAll([FromQuery] ResultSetCriteria resultSetCriteria)
    {
      resultSetCriteria ??= new ResultSetCriteria
      {
        CurrentPage = 1,
        PageSize = 10
      };
      resultSetCriteria.CurrentPage = resultSetCriteria.CurrentPage <= 0 ? 1 : resultSetCriteria.CurrentPage;
      resultSetCriteria.PageSize = resultSetCriteria.PageSize <= 0 ? 10 : resultSetCriteria.PageSize;
      return await _dataMigrationService.GetAllAsync(resultSetCriteria);
    }
  }
}
