using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Jobs;
using System.Collections.Generic;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IBulkUploadFileContentService
  {
    List<KeyValuePair<string, string>> ValidateUploadedFile(string fileKey, string fileContentString);

    BulkUploadMigrationResult CheckMigrationStatus(string fileContentString);

    List<BulkUploadFileContentRowDetails> GetFileContentObject(string fileContentString);
  }
}
