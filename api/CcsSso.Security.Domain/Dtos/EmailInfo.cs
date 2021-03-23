using System;
using System.Collections.Generic;
using System.Text;

namespace CcsSso.Security.Domain.Dtos
{
  public class EmailInfo
  {
    public string To { get; set; }

    public string TemplateId { get; set; }

    public Dictionary<string, dynamic> BodyContent { get; set; }
  }
}
