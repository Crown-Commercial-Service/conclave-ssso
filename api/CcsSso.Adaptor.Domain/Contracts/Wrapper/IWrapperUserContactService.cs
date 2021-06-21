using CcsSso.Adaptor.Domain.Dtos.Wrapper;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Domain.Contracts.Wrapper
{
  public interface IWrapperUserContactService
  {
    Task<WrapperUserContactInfo> GetUserContactPointAsync(string userName, int contactPointId);

    Task<WrapperUserContactInfoList> GetUserContactPointsAsync(string userName);

    Task<int> CreateUserContactPointAsync(string userName, WrapperContactPointRequest wrapperContactPointRequest);

    Task UpdateUserContactPointAsync(string userName, int contactPointId, WrapperContactPointRequest wrapperContactPointRequest);
  }
}
