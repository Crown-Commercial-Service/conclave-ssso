using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Shared.Domain
{
  public class EmailInfo
  {
    public string To { get; set; }

    public string TemplateId { get; set; }

    public Dictionary<string, dynamic> BodyContent { get; set; }
  }
}
