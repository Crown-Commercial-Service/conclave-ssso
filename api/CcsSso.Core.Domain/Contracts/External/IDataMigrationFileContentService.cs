using CcsSso.Core.Domain.Dtos;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Domain.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts.External
{
  public interface IDataMigrationFileContentService
  {
    Task<List<KeyValuePair<string, string>>> ValidateUploadedFile(string fileKey, string fileContentString);

    DataMigrationResult CheckMigrationStatus(string fileContentString);

    List<DataMigrationFileContentRowDetails> GetFileContentObject(string fileContentString);
  }
}
