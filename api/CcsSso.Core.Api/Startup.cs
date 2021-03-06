using CcsSso.Api.Middleware;
using CcsSso.Core.Api.Middleware;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Service;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Service;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace CcsSso.Api
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton(s =>
      {
        bool.TryParse(Configuration["QueueInfo:EnableAdaptorNotifications"], out bool enableAdaptorNotifications);
        bool.TryParse(Configuration["RedisCacheSettings:IsEnabled"], out bool isRedisEnabled);
        int.TryParse(Configuration["RedisCacheSettings:CacheExpirationInMinutes"], out int cacheExpirationInMinutes);
        bool.TryParse(Configuration["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);

        ApplicationConfigurationInfo appConfigInfo = new ApplicationConfigurationInfo()
        {
          JwtTokenValidationInfo = new JwtTokenValidationConfigurationInfo()
          {
            IdamClienId = Configuration["JwtTokenValidationInfo:IdamClienId"],
            Issuer = Configuration["JwtTokenValidationInfo:Issuer"],
            JwksUrl = isApiGatewayEnabled ? Configuration["JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl"] : Configuration["JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl"]
          },
          SecurityApiDetails = new SecurityApiDetails()
          {
            ApiKey = Configuration["SecurityApiSettings:ApiKey"],
            Url = Configuration["SecurityApiSettings:Url"]
          },
          QueueUrlInfo = new QueueUrlInfo
          {
            AdaptorNotificationQueueUrl = Configuration["QueueInfo:AdaptorNotificationQueueUrl"]
          },
          RedisCacheSettings = new RedisCacheSetting()
          {
            ConnectionString = Configuration["RedisCacheSettings:ConnectionString"],
            IsEnabled = isRedisEnabled,
            CacheExpirationInMinutes = cacheExpirationInMinutes
          },
        };
        return appConfigInfo;
      });
      services.AddSingleton(s =>
      {
        Dtos.Domain.Models.CiiConfig ciiConfigInfo = new Dtos.Domain.Models.CiiConfig()
        {
          url = Configuration["Cii:Url"],
          token = Configuration["Cii:Token"],
          deleteToken = Configuration["Cii:Delete_Token"],
          clientId = Configuration["JwtTokenValidationInfo:IdamClienId"]
        };
        return ciiConfigInfo;
      });
      services.AddSingleton(s =>
      {
        int.TryParse(Configuration["QueueInfo:RecieveMessagesMaxCount"], out int recieveMessagesMaxCount);
        recieveMessagesMaxCount = recieveMessagesMaxCount == 0 ? 10 : recieveMessagesMaxCount;

        int.TryParse(Configuration["QueueInfo:RecieveWaitTimeInSeconds"], out int recieveWaitTimeInSeconds); // Default value 0

        var sqsConfiguration = new SqsConfiguration
        {
          ServiceUrl = Configuration["QueueInfo:ServiceUrl"],
          AccessKeyId = Configuration["QueueInfo:AccessKeyId"],
          AccessSecretKey = Configuration["QueueInfo:AccessSecretKey"],
          RecieveMessagesMaxCount = recieveMessagesMaxCount,
          RecieveWaitTimeInSeconds = recieveWaitTimeInSeconds
        };

        return sqsConfiguration;
      });
      services.AddControllers();
      services.AddSingleton<IAwsSqsService, AwsSqsService>();
      services.AddSingleton<ITokenService, TokenService>();
      services.AddSingleton<IRemoteCacheService, RedisCacheService>();
      services.AddSingleton<RedisConnectionPoolService>(_ =>
        new RedisConnectionPoolService(Configuration["RedisCacheSettings:ConnectionString"])
      );
      services.AddSingleton<ILocalCacheService, InMemoryCacheService>();
      services.AddMemoryCache();
      services.AddSingleton<IWrapperCacheService, WrapperCacheService>();
      services.AddDbContext<DataContext>(options => options.UseNpgsql(Configuration["DbConnection"]), ServiceLifetime.Transient);
      services.AddScoped<IDataContext>(s => s.GetRequiredService<DataContext>());
      services.AddHttpClient<ICiiService, CiiService>().ConfigureHttpClient((serviceProvider, httpClient) =>
      {
        httpClient.BaseAddress = new Uri(Configuration["Cii:Url"]);
        httpClient.DefaultRequestHeaders.Add("Apikey", Configuration["Cii:Token"]);
        httpClient.DefaultRequestHeaders.Add("x-api-key", Configuration["Cii:Token"]);
      });
      services.AddHttpClient("default");
      services.AddScoped<IAuthService, AuthService>();
      services.AddScoped<RequestContext>();
      services.AddScoped<IContactService, ContactService>();
      services.AddScoped<IOrganisationService, OrganisationService>();
      services.AddScoped<IUserService, UserService>();
      services.AddScoped<ICiiService, CiiService>();
      services.AddScoped<IAdaptorNotificationService, AdaptorNotificationService>();
      services.AddScoped<IAuditLoginService, AuditLoginService>();
      services.AddScoped<IDateTimeService, DateTimeService>();
      services.AddHttpContextAccessor();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CcsSso.Api", Version = "v1" });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        c.EnableAnnotations();
      });
      services.AddAntiforgery(op =>
      {
        // Instruct to refer http header called "x-xsrf-tokenN"
        op.HeaderName = "x-xsrf-token";
      });
      services.AddMvc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAntiforgery antiforgery)
    {
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.Api v1"));
      app.UseHttpsRedirection();

      app.Use(async (context, next) =>
      {
        context.Response.Headers.Add(
            "Cache-Control",
            "no-cache");
        context.Response.Headers.Add(
            "Pragma",
            "no-cache");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        var tokens = antiforgery.GetAndStoreTokens(context);
        // [ValidateAntiForgeryToken]
        // Cookie will be read by angular client and attach to http header
        context.Response.Cookies.Append("XSRF-TOKEN", tokens.CookieToken,
            new CookieOptions() { SameSite = SameSiteMode.None, Secure = true, HttpOnly = false });

        await next();
      });

      app.UseRouting();
      var _cors = Configuration.GetSection("CorsDomains").Get<string[]>();
      app.UseCors(builder => builder.WithOrigins(_cors)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
      );

      app.UseForwardedHeaders(new ForwardedHeadersOptions
      {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor
      });

      app.UseMiddleware<CommonExceptionHandlerMiddleware>();
      app.UseMiddleware<AuthenticationMiddleware>();


      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
