using Serilog;
using Serilog.Events;
using Serilog.Sinks.Network;

namespace CcsSso.Logs
{
  public class LogConfigurationManager
  {
    public static void ConfigureLogs(string tcpLinkUrl)
    {
      var urlLogger = new LoggerConfiguration()
    .WriteTo.TCPSink(tcpLinkUrl)
    .MinimumLevel.Error()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .CreateLogger();

      Log.Logger = urlLogger;
    }
  }
}
