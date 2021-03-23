using CcsSso.Core.Jobs;
using System;
using System.Threading.Tasks;

namespace CcsSso.Core.JobManager
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var dIContainer = new DIContainer();
      Console.WriteLine("Back Ground job started");
      await dIContainer.RegisterDependenciesAsync();
      await dIContainer.RegisterStatupJobsAsync();
      Console.WriteLine("Back Ground job ended");
    }
  }
}
