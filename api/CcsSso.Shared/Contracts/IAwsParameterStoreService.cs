using Amazon.SimpleSystemsManagement.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CcsSso.Shared.Contracts
{
  public interface IAwsParameterStoreService
  {
    Task<List<Parameter>> GetParameters(string path);

    string FindParameterByName(List<Parameter> parameters, string name);
  }
}
