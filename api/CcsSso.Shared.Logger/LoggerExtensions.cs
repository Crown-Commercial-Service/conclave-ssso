using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rollbar;
using Rollbar.NetCore.AspNet;

namespace CcsSso.Logs.Extensions
{
  public static class RollbarLoggerExtensions
  {
    public static IApplicationBuilder AddRollbarMiddleware(this IApplicationBuilder app)
    {
      app.UseRollbarMiddleware();
      return app;
    }

    public static void AddRollbarLoggerServices(this IServiceCollection services, string rollbarAccessToken, string rollbarEnvironment)
    {
      RollbarInfrastructureConfig config = new RollbarInfrastructureConfig(
              rollbarAccessToken, rollbarEnvironment);

      RollbarMiddleware.ConfigureServices(services, LogLevel.Trace, config);

      services.AddRollbarLogger(loggerOptions =>
      {
        loggerOptions.Filter = (loggerName, loglevel) => loglevel >= LogLevel.Trace;
      });
    }    
  }
}
