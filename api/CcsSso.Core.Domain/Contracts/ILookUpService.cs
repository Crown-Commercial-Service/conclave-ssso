using System.Threading.Tasks;

namespace CcsSso.Core.Domain.Contracts
{
    // #Auto validation
    public interface ILookUpService
    {
        Task<bool> IsDomainValidForAutoValidation(string emailId);
    }
}
