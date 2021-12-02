using CcsSso.Core.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IDocUploadService
  {
    Task<DocUploadResponse> UploadFileAsync(string typeValidation, int sizeValidation, IFormFile file = null, string filePath = null);

    Task<DocUploadResponse> GetFileStatusAsync(string docId);
  }
}
