using CcsSso.Shared.Domain.Dto;
using System.IO;
using System.Threading.Tasks;

namespace CcsSso.Shared.Services
{
  public interface IFileUploadToCloud
  {
    Task<AzureResponse> FileUploadToAzureBlobAsync(byte[] stream, string inputFileType);
  }
}