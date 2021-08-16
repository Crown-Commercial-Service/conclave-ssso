using CcsSso.Adaptor.Domain.Dtos.Cii;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Cii
{
  public interface ICiiService
  {
    Task<CiiIdentifierAllDto> GetOrgIdentifierInfoAsync(string ciiOrgId);
  }
}
