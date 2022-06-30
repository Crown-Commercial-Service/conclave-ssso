using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Shared.Domain.Dto
{
  public class AzureResponse
  {
    public string responseMessage { get; set; }
    public string responseFileName { get; set; }
    public bool responseStatus { get; set; }
  }
}
