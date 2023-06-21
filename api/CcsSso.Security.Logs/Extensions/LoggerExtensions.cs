using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.Logs.Extensions
{
  public static class LoggerExtensions
  {
    public static IHostBuilder UseApplicationLog(this IHostBuilder hostBuilder)
    {
      return hostBuilder.UseSerilog();
    }

    public static IApplicationBuilder AddLoggerMiddleware(this IApplicationBuilder app)
    {
      app.UseSerilogRequestLogging();
      return app;
    }
  }
}
