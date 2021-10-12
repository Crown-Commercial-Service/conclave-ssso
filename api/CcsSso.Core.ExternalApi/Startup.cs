using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.ExternalApi.Authorisation;
using CcsSso.Core.ExternalApi.Middleware;
using CcsSso.Core.Service;
using CcsSso.Core.Service.External;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos;
using CcsSso.ExternalApi.Middleware;
using CcsSso.Service;
using CcsSso.Service.External;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CcsSso.ExternalApi
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
      services.AddCors();
      services.AddControllers();
      services.AddSingleton<ITokenService, TokenService>();
      services.AddSingleton(s =>
      {
        bool.TryParse(Configuration["Email:SendNotificationsEnabled"], out bool sendNotificationsEnabled);
        bool.TryParse(Configuration["QueueInfo:EnableAdaptorNotifications"], out bool enableAdaptorNotifications);
        bool.TryParse(Configuration["RedisCacheSettings:IsEnabled"], out bool isRedisEnabled);
        int.TryParse(Configuration["RedisCacheSettings:CacheExpirationInMinutes"], out int cacheExpirationInMinutes);
        int.TryParse(Configuration["InMemoryCacheExpirationInMinutes"], out int inMemoryCacheExpirationInMinutes);
        bool.TryParse(Configuration["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);

        if (cacheExpirationInMinutes == 0)
        {
          cacheExpirationInMinutes = 10;
        }

        if (inMemoryCacheExpirationInMinutes == 0)
        {
          inMemoryCacheExpirationInMinutes = 10;
        }

        ApplicationConfigurationInfo appConfigInfo = new ApplicationConfigurationInfo()
        {
          ApiKey = Configuration["ApiKey"],
          ConclaveLoginUrl = Configuration["ConclaveLoginUrl"],
          EnableAdapterNotifications = enableAdaptorNotifications,
          InMemoryCacheExpirationInMinutes = inMemoryCacheExpirationInMinutes,
          DashboardServiceClientId = Configuration["DashboardServiceClientId"],
          JwtTokenValidationInfo = new JwtTokenValidationConfigurationInfo()
          {
            IdamClienId = Configuration["JwtTokenValidationInfo:IdamClienId"],
            Issuer = Configuration["JwtTokenValidationInfo:Issuer"],
            JwksUrl = isApiGatewayEnabled ? Configuration["JwtTokenValidationInfo:ApiGatewayEnabledJwksUrl"] : Configuration["JwtTokenValidationInfo:ApiGatewayDisabledJwksUrl"]
          },
          SecurityApiDetails = new SecurityApiDetails
          {
            ApiKey = Configuration["SecurityApiSettings:ApiKey"],
            Url = Configuration["SecurityApiSettings:Url"],
          },
          EmailInfo = new CcsEmailInfo
          {
            UserWelcomeEmailTemplateId = Configuration["Email:UserWelcomeEmailTemplateId"],
            OrgProfileUpdateNotificationTemplateId = Configuration["Email:OrgProfileUpdateNotificationTemplateId"],
            UserProfileUpdateNotificationTemplateId = Configuration["Email:UserProfileUpdateNotificationTemplateId"],
            UserContactUpdateNotificationTemplateId = Configuration["Email:UserContactUpdateNotificationTemplateId"],
            UserPermissionUpdateNotificationTemplateId = Configuration["Email:UserPermissionUpdateNotificationTemplateId"],
            SendNotificationsEnabled = sendNotificationsEnabled,
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
          }
        };
        return appConfigInfo;
      });
      services.AddSingleton(s =>
      {
        EmailConfigurationInfo emailConfigurationInfo = new()
        {
          ApiKey = Configuration["Email:ApiKey"],
        };

        return emailConfigurationInfo;
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
      services.AddSingleton<IAwsSqsService, AwsSqsService>();
      services.AddSingleton<IEmailProviderService, EmailProviderService>();
      services.AddSingleton<ICcsSsoEmailService, CcsSsoEmailService>();
      services.AddDbContext<DataContext>(options => options.UseNpgsql(Configuration["DbConnection"]));
      services.AddSingleton<IRemoteCacheService, RedisCacheService>();
      services.AddSingleton<ICacheInvalidateService, CacheInvalidateService>();
      services.AddSingleton<RedisConnectionPoolService>(_ =>
        new RedisConnectionPoolService(Configuration["RedisCacheSettings:ConnectionString"])
      );
      services.AddSingleton<IWrapperCacheService, WrapperCacheService>();
      services.AddSingleton<ILocalCacheService, InMemoryCacheService>();
      services.AddSingleton<IAuthorizationPolicyProvider, ClaimAuthorisationPolicyProvider>();
      services.AddMemoryCache();

      services.AddScoped<IDataContext>(s => s.GetRequiredService<DataContext>());
      services.AddScoped<RequestContext>();
      services.AddScoped<IOrganisationProfileService, OrganisationProfileService>();
      services.AddScoped<IOrganisationService, OrganisationService>();
      services.AddScoped<IOrganisationContactService, OrganisationContactService>();
      services.AddScoped<IOrganisationSiteService, OrganisationSiteService>();
      services.AddScoped<IOrganisationSiteContactService, OrganisationSiteContactService>();
      services.AddScoped<IUserProfileService, UserProfileService>();
      services.AddScoped<IUserContactService, UserContactService>();
      services.AddScoped<IUserProfileHelperService, UserProfileHelperService>();
      services.AddScoped<IContactsHelperService, ContactsHelperService>();
      services.AddScoped<IContactExternalService, ContactExternalService>();
      services.AddScoped<IConfigurationDetailService, ConfigurationDetailService>();
      services.AddScoped<IOrganisationGroupService, OrganisationGroupService>();
      services.AddScoped<IIdamService, IdamService>();
      services.AddScoped<ICiiService, CiiService>();
      services.AddScoped<IAdaptorNotificationService, AdaptorNotificationService>();
      services.AddScoped<IAuditLoginService, AuditLoginService>();
      services.AddScoped<IDateTimeService, DateTimeService>(); 
      services.AddScoped<IUserService, UserService>(); 
      services.AddScoped<IAuthService, AuthService>(); 
      services.AddHttpClient();
      services.AddHttpContextAccessor();

      services.AddHttpClient("CiiApi", c =>
      {
        c.BaseAddress = new Uri(Configuration["Cii:Url"]);
        c.DefaultRequestHeaders.Add("x-api-key", Configuration["Cii:Token"]);
      });
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CcsSso.WrapperApi", Version = "v1" });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        c.EnableAnnotations();
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme()
        {
          In = ParameterLocation.Header,
          Name = "X-API-KEY",
          Type = SecuritySchemeType.ApiKey,
        });
        var openApiSecuritySchema = new OpenApiSecurityScheme()
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
           { openApiSecuritySchema, new List<string>() }
        };
        c.AddSecurityRequirement(openApiSecurityRequirement);
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseMiddleware<CommonExceptionHandlerMiddleware>();
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.ExternalApi v1"));
      app.UseHsts();
      app.UseHttpsRedirection();

      app.Use(async (context, next) =>
      {
        context.Request.EnableBuffering();
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

      app.UseMiddleware<InputValidationMiddleware>();
      app.UseMiddleware<AuthenticatorMiddleware>();
      app.UseMiddleware<RequestOrganisationContextFilterMiddleware>();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
