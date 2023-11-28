using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Model
{
  public class UserListDetails
  {
    public int Start { get; set; }
    public int Limit { get; set; }
    public int Length { get; set; }
    public int Total { get; set; }
    public List<UserInfo> Users { get; set; }
  }

  public class UserInfo
  {
    public string Email { get; set; }

    [JsonProperty("last_login")]
    public string Last_Login { get; set; }
  }
}
