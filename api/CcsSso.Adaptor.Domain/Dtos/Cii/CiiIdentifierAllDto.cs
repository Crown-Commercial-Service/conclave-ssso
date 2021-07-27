using System.Collections.Generic;

namespace CcsSso.Adaptor.Domain.Dtos.Cii
{
  public class CiiIdentifierAllDto
  {
    public IdentifierDto Identifier { get; set; }

    public List<IdentifierDto> AdditionalIdentifiers { get; set; }
  }

  public class IdentifierDto
  {
    public string Scheme { get; set; }

    public string Id { get; set; }

    public string LegalName { get; set; }

    public string Uri { get; set; }

    public bool Hidden { get; set; }
  }
}
