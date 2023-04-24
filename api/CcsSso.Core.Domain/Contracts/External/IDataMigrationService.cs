using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Dtos.External;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IDataMigrationService
  {
    Task<DataMigrationStatusResponse> UploadDataMigrationFileAsync(IFormFile file);

    Task<DataMigrationStatusResponse> CheckDataMigrationStatusAsync(string fileKey);
  }
}
