using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
  public interface IAuditLoginService
  {
    Task CreateLogAsync(string eventName, string applicationName, string referenceData);
  }
}
