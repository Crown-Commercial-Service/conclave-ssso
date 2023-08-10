using CcsSso.Core.ExternalApi.CustomOptions;
using CcsSso.ExternalApi.Api.CustomOptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CcsSso.ExternalApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
          var configBuilder = new ConfigurationBuilder()
                         .AddJsonFile("appsettings.json", optional: false)
                         .Build();
          var builtConfig = config.Build();
          var vaultEnabled = configBuilder.GetValue<bool>("VaultEnabled");
          if (!vaultEnabled)
          {
            config.AddJsonFile("appsecrets.json", optional: false, reloadOnChange: true);
          }
          else
          {
            var source = configBuilder.GetValue<string>("Source");
            if (source.ToUpper() == "AWS")
            {
              config.AddParameterStore();
            }
            else
            {
              config.AddVault(options =>
            {
              var vaultOptions = builtConfig.GetSection("Vault");
              options.Address = vaultOptions["Address"];
            });
            }
          }
          config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
          string accessKeyId = Environment.GetEnvironmentVariable("ACCESSKEYID");
          string accessKeySecret = Environment.GetEnvironmentVariable("ACCESSKEYSECRET");
          string region = Environment.GetEnvironmentVariable("REGION");
          string startupUrl = Environment.GetEnvironmentVariable("STARTUP_URL");
          if (string.IsNullOrWhiteSpace(accessKeyId) || string.IsNullOrWhiteSpace(accessKeySecret) || string.IsNullOrWhiteSpace(region))
          {
            webBuilder.UseStartup<Startup>();
          }
          else
          {
            webBuilder.UseStartup<Startup>().UseUrls(startupUrl);
          }
        });
  }
}
