using CcsSso.Security.Domain.Dtos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Security.Domain.Contracts
{
  public interface IEmaillProviderService
  {
    Task SendEmailAsync(EmailInfo emailInfo);
  }
}
