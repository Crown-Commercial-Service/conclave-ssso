using System.Collections.Generic;

namespace CcsSso.Shared.Domain
{
  public class EmailInfo
  {
    public string To { get; set; }

    public string TemplateId { get; set; }

    public Dictionary<string, dynamic> BodyContent { get; set; }
  }
}
