using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CcsSso.BlazorApp.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            // builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped(sp => new HttpClient{});
            await builder.Build().RunAsync();
        }
    }

  public static class NavigationManagerExtensions
  {
    public static bool TryGetQueryString<T>(this NavigationManager navManager, string key, out T value)
    {
      var uri = navManager.ToAbsoluteUri(navManager.Uri);

      if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString))
      {
        if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
        {
          value = (T)(object)valueAsInt;
          return true;
        }

        if (typeof(T) == typeof(string))
        {
          value = (T)(object)valueFromQueryString.ToString();
          return true;
        }

        if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
        {
          value = (T)(object)valueAsDecimal;
          return true;
        }
      }

      value = default;
      return false;
    }
  }
}
