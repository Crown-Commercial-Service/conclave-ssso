using CcsSso.Adaptor.Api.Middlewares;
using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.DbPersistence;
using CcsSso.Adaptor.Domain;
using CcsSso.Adaptor.Domain.Contracts;
using CcsSso.Adaptor.Domain.Contracts.Cii;
using CcsSso.Adaptor.Domain.Contracts.Wrapper;
using CcsSso.Adaptor.Service;
using CcsSso.Adaptor.Service.Cii;
using CcsSso.Adaptor.Service.Wrapper;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CcsSso.Adaptor.Api
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
      services.AddControllers();

      bool.TryParse(Configuration["RedisCacheSettings:IsEnabled"], out bool isRedisEnabled);
      int.TryParse(Configuration["RedisCacheSettings:CacheExpirationInMinutes"], out int cacheExpirationInMinutes);
      int.TryParse(Configuration["InMemoryCacheExpirationInMinutes"], out int inMemoryCacheExpirationInMinutes);
      int.TryParse(Configuration["OrganisationUserRequestPageSize"], out int organisationUserRequestPageSize);

      if (cacheExpirationInMinutes == 0)
      {
        cacheExpirationInMinutes = 10;
      }

      if (inMemoryCacheExpirationInMinutes == 0)
      {
        inMemoryCacheExpirationInMinutes = 10;
      }

      if (organisationUserRequestPageSize == 0)
      {
        organisationUserRequestPageSize = 100;
      }

      services.AddSingleton(s => new AppSetting
      {
        ApiKey = Configuration["ApiKey"],
        InMemoryCacheExpirationInMinutes = inMemoryCacheExpirationInMinutes,
        OrganisationUserRequestPageSize = organisationUserRequestPageSize,
        RedisCacheSettings = new RedisCacheSetting()
        {
          ConnectionString = Configuration["RedisCacheSettings:ConnectionString"],
          IsEnabled = isRedisEnabled,
          CacheExpirationInMinutes = cacheExpirationInMinutes
        },
        QueueUrlInfo = new QueueUrlInfo
        {
          PushDataQueueUrl = Configuration["QueueInfo:PushDataQueueUrl"]
        }
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
      services.AddSingleton<ILocalCacheService, InMemoryCacheService>();
      services.AddMemoryCache();
      services.AddSingleton<IRemoteCacheService, RedisCacheService>();
      services.AddSingleton<RedisConnectionPoolService>(_ =>
        new RedisConnectionPoolService(Configuration["RedisCacheSettings:ConnectionString"])
      );
      services.AddSingleton<IAwsSqsService, AwsSqsService>();
      services.AddSingleton<IWrapperContactService, WrapperContactService>();
      services.AddSingleton<IWrapperOrganisationService, WrapperOrganisationService>();
      services.AddSingleton<IWrapperUserService, WrapperUserService>();
      services.AddSingleton<IWrapperOrganisationContactService, WrapperOrganisationContactService>();
      services.AddSingleton<IWrapperUserContactService, WrapperUserContactService>();
      services.AddSingleton<IWrapperSiteService, WrapperSiteService>();
      services.AddSingleton<IWrapperSiteContactService, WrapperSiteContactService>();
      services.AddSingleton<IWrapperApiService, WrapperApiService>();
      services.AddSingleton<ICiiApiService, CiiApiService>();
      services.AddSingleton<ICiiService, CiiService>();

      services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(Configuration["DbConnection"]));
      services.AddScoped<AdaptorRequestContext>();
      services.AddScoped<IConsumerService, ConsumerService>();
      services.AddScoped<IAttributeMappingService, AttributeMappingService>();
      services.AddScoped<IOrganisationService, OrganisationService>();
      services.AddScoped<IUserService, UserService>();
      services.AddScoped<IContactService, ContactService>();
      services.AddScoped<IPushService, PushService>();

      bool.TryParse(Configuration["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);

      services.AddHttpClient("UserWrapperApi", c =>
      {
        c.BaseAddress = new Uri(isApiGatewayEnabled ? Configuration["WrapperApiSettings:ApiGatewayEnabledUserUrl"] : Configuration["WrapperApiSettings:ApiGatewayDisabledUserUrl"]);
        c.DefaultRequestHeaders.Add("X-API-Key", Configuration["WrapperApiSettings:UserApiKey"]);
      });

      services.AddHttpClient("OrgWrapperApi", c =>
      {
        c.BaseAddress = new Uri(isApiGatewayEnabled ? Configuration["WrapperApiSettings:ApiGatewayEnabledOrgUrl"] : Configuration["WrapperApiSettings:ApiGatewayDisabledOrgUrl"]);
        c.DefaultRequestHeaders.Add("X-API-Key", Configuration["WrapperApiSettings:OrgApiKey"]);
      });

      services.AddHttpClient("ContactWrapperApi", c =>
      {
        c.BaseAddress = new Uri(isApiGatewayEnabled ? Configuration["WrapperApiSettings:ApiGatewayEnabledContactUrl"] : Configuration["WrapperApiSettings:ApiGatewayDisabledContactUrl"]);
        c.DefaultRequestHeaders.Add("X-API-Key", Configuration["WrapperApiSettings:ContactApiKey"]);
      });

      services.AddHttpClient("CiiApi", c =>
      {
        c.BaseAddress = new Uri(Configuration["CiiApiSettings:Url"]);
        c.DefaultRequestHeaders.Add("X-API-Key", Configuration["CiiApiSettings:SpecialToken"]);
      });

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CcsSso.Adaptor.Api", Version = "v1" });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        c.EnableAnnotations();
        c.AddSecurityDefinition("ConsumerClientId", new OpenApiSecurityScheme()
        {
          In = ParameterLocation.Header,
          Name = "X-Consumer-ClientId",
          Type = SecuritySchemeType.ApiKey,
        });
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme()
        {
          In = ParameterLocation.Header,
          Name = "X-API-Key",
          Type = SecuritySchemeType.ApiKey,
        });
        var openApiSecuritySchemaConsumerKey = new OpenApiSecurityScheme()
        {
          Reference = new OpenApiReference
          {
            Type = ReferenceType.SecurityScheme,
            Id = "ConsumerClientId"
          },
          In = ParameterLocation.Header
        };
        var openApiSecuritySchemaConsumerName = new OpenApiSecurityScheme()
        {
          Reference = new OpenApiReference
          {
            Type = ReferenceType.SecurityScheme,
            Id = "ApiKey"
          },
          In = ParameterLocation.Header
        };
        var openApiSecurityRequirement = new OpenApiSecurityRequirement
        {
           { openApiSecuritySchemaConsumerKey, new List<string>() },
           { openApiSecuritySchemaConsumerName, new List<string>() }
        };
        c.AddSecurityRequirement(openApiSecurityRequirement);
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseMiddleware<CommonExceptionHandlerMiddleware>();
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.Adaptor.Api v1"));
      app.UseHsts();
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
        context.Response.Headers.Add("X-Xss-Protection", "1");
        await next();
      });

      app.UseRouting();

      app.UseMiddleware<AuthenticationMiddleware>();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
