using System.Threading.Tasks;
using CcsSso.Core.Domain.Jobs;

namespace CcsSso.Core.Domain.Contracts.Wrapper
{
    public interface IWrapperSecurityService
    {
        Task<IdamUser> GetUserByEmail(string email);
    }
}
