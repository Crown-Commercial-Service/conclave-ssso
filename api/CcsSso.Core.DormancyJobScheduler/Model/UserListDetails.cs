using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CcsSso.Core.DormancyJobScheduler.Model
{
  public class UserMetadataInfo
  {
    public bool? UseMfa { get; set; }
    public DateTime? DeactivatedOn { get; set; }
    public DateTime? ReactivatedOn { get; set; }
    public bool? IsDeactivated { get; set; }
    public bool? IsReactivated { get; set; }
  }

  public class UserDataList
  {
    public int Start { get; set; }
    public int Limit { get; set; }
    public int Length { get; set; }
    public int Total { get; set; }
    public List<UserDataInfo> Users { get; set; }
  }

  public class UserDataInfo
  {
    public DateTime? CreatedAt { get; set; }
    public string Email { get; set; }
    public DateTime? LastLogin { get; set; }
    public UserMetadataInfo UserMetadata { get; set; }
  }
}
