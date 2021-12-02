using CcsSso.Core.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IBulkUploadService
  {
    Task<BulkUploadStatusResponse> BulkUploadUsersAsync(string organisationId, IFormFile file);

    Task<BulkUploadStatusResponse> CheckBulkUploadStatusAsync(string organisationId, string fileKey);
  }
}
