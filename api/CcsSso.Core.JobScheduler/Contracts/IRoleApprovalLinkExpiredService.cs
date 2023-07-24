﻿using CcsSso.Core.Domain.Dtos.External;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Core.JobScheduler.Contracts
{
  public interface IRoleApprovalLinkExpiredService
  {
    Task PerformJobAsync(List<UserAccessRolePendingDetailsInfo> organisations);
  }
}