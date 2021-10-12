using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Dtos.External
{
  public class ResultSetCriteria
  {
    [JsonProperty(PropertyName = "page-size")]
    public int PageSize { get; set; }

    [JsonProperty(PropertyName = "current-page")]
    public int CurrentPage { get; set; }
  }
}
