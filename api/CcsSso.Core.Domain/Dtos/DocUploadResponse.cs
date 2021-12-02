using System;

namespace CcsSso.Core.Domain.Dtos
{
  public class DocUploadResponse
  {
    public string Id { get; set; }

    public string SourceApp { get; set; }

    public string State { get; set; }

    public string ClamavMessage { get; set; }

    public DocumentFileResponse DocumentFile { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
  }

  public class DocumentFileResponse
  {
    public string Url { get; set; }
  }
}
