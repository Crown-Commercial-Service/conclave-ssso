using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Core.PPONScheduler.Model
{
  public class Identifier
  {
    public string Id { get; set; }

    [JsonProperty("id-type")]
    public string IdType { get; set; }

    public bool Persisted { get; set; }
  }

  public class PPONDetails
  {
    public List<Identifier> Identifiers { get; set; }
  }  
}
