using CcsSso.Logs.Extensions;
using CcsSso.Security.Api.CustomOptions;
using CcsSso.Security.Api.Middleware;
using CcsSso.Security.DbPersistence;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Services;
using CcsSso.Security.Services.Helpers;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace CcsSso.Security.Api
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
      services.Configure<CustomOptions.VaultOptions>(Configuration.GetSection("Vault"));
      services.AddDataProtection();

      services.AddControllers(opt =>
      {
        // remove formatter that turns nulls into 204 - No Content responses this formatter breaks Angular's Http response JSON parsing
        opt.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.HttpNoContentOutputFormatter>();
      });
      services.AddSingleton(s =>
      {
        bool.TryParse(Configuration["Email:SendNotificationsEnabled"], out bool sendNotificationsEnabled);
        int.TryParse(Configuration["SessionConfig:SessionTimeoutInMinutes"], out int sessionTimeOut);
        if (sessionTimeOut <= 0)
        {
          sessionTimeOut = 2; //default hardcoded value
        }

        int.TryParse(Configuration["SessionConfig:StateExpirationInMinutes"], out int stateExpirationInMinutes);
        if (stateExpirationInMinutes <= 0)
        {
          stateExpirationInMinutes = 10; //default hardcoded value
        }

        int.TryParse(Configuration["JwtTokenConfig:IDTokenExpirationTimeInMinutes"], out int tokenExpirationTimeInMinutes);
        if (tokenExpirationTimeInMinutes <= 0)
        {
          tokenExpirationTimeInMinutes = 30; //default hardcoded value
        }

        int.TryParse(Configuration["MfaSettings:TicketExpirationInMinutes"], out int ticketExpirationInMinutes);
        if (ticketExpirationInMinutes <= 0)
        {
          ticketExpirationInMinutes = 30; //default hardcoded value
        }

        int.TryParse(Configuration["MfaSettings:MFAResetPersistentTicketListExpirationInDays"], out int mfaResetPersistentTicketListExpirationInDays);
        if (mfaResetPersistentTicketListExpirationInDays <= 0)
        {
          mfaResetPersistentTicketListExpirationInDays = 30; //default hardcoded value
        }

        int.TryParse(Configuration["JwtTokenConfig:LogoutTokenExpireTimeInMinutes"], out int logoutTokenExpirationTimeInMinutes);
        if (logoutTokenExpirationTimeInMinutes <= 0)
        {
          logoutTokenExpirationTimeInMinutes = 30; //default hardcoded value
        }

        bool.TryParse(Configuration["PasswordPolicy:LowerAndUpperCaseWithDigits"], out bool lowerAndUpperCaseWithDigits);

        int.TryParse(Configuration["PasswordPolicy:RequiredLength"], out int requiredLength);
        if (requiredLength <= 0)
        {
          requiredLength = 10; //default hardcoded value
        }

        int.TryParse(Configuration["PasswordPolicy:RequiredUniqueChars"], out int requiredUniqueChars);
        bool.TryParse(Configuration["RedisCacheSettings:IsEnabled"], out bool isRedisEnabled);
        bool.TryParse(Configuration["IsApiGatewayEnabled"], out bool isApiGatewayEnabled);
        bool.TryParse(Configuration["QueueInfo:EnableDataQueue"], out bool enableDataQueue);

        ApplicationConfigurationInfo appConfigInfo = new ApplicationConfigurationInfo()
        {
          CustomDomain = Configuration["CustomDomain"],
          AllowedDomains = Configuration.GetSection("AllowedDomains").Get<List<string>>(),
          Auth0ConfigurationInfo = new Auth0Configuration()
          {
            ClientId = Configuration["Auth0:ClientId"],
            ClientSecret = Configuration["Auth0:Secret"],
            Domain = Configuration["Auth0:Domain"],
            DBConnectionName = Configuration["Auth0:DBConnectionName"],
            ManagementApiBaseUrl = Configuration["Auth0:ManagementApiBaseUrl"],
            ManagementApiIdentifier = Configuration["Auth0:ManagementApiIdentifier"],
            DefaultDBConnectionId = Configuration["Auth0:DefaultDBConnectionId"],
            DefaultAudience = Configuration["Auth0:DefaultAudience"]
          },
          AwsCognitoConfigurationInfo = new AwsCognitoConfigurationInfo()
          {
            AWSRegion = Configuration["AWSCognito:Region"],
            AWSPoolId = Configuration["AWSCognito:PoolId"],
            AWSAppClientId = Configuration["AWSCognito:AppClientId"],
            AWSAccessKeyId = Configuration["AWSCognito:AccessKeyId"],
            AWSAccessSecretKey = Configuration["AWSCognito:AccessSecretKey"],
            AWSCognitoURL = Configuration["AWSCognito:AWSCognitoURL"]
          },
          CcsEmailConfigurationInfo = new CcsEmailConfigurationInfo()
          {
            UserActivationEmailTemplateId = Configuration["Email:UserActivationEmailTemplateId"],
            UserActivationLinkTTLInMinutes = int.Parse(Configuration["Email:UserActivationLinkTTLInMinutes"]),
            ResetPasswordLinkTTLInMinutes = int.Parse(Configuration["Email:ResetPasswordLinkTTLInMinutes"]),
            ResetPasswordEmailTemplateId = Configuration["Email:ResetPasswordEmailTemplateId"],
            NominateEmailTemplateId = Configuration["Email:NominateEmailTemplateId"],
            ChangePasswordNotificationTemplateId = Configuration["Email:ChangePasswordNotificationTemplateId"],
            MfaResetEmailTemplateId = Configuration["Email:MfaResetEmailTemplateId"],
            SendNotificationsEnabled = sendNotificationsEnabled
          },
          RollBarConfigurationInfo = new RollBarConfigurationInfo()
          {
            Token = Configuration["RollBarLogger:Token"],
            Environment = Configuration["RollBarLogger:Environment"]
          },
          SessionConfig = new SessionConfig()
          {
            SessionTimeoutInMinutes = sessionTimeOut,
            StateExpirationInMinutes = stateExpirationInMinutes
          },
          JwtTokenConfiguration = new JwtTokenConfiguration()
          {
            Issuer = Configuration["JwtTokenConfig:Issuer"],
            RsaPrivateKey = Configuration["JwtTokenConfig:RsaPrivateKey"],
            RsaPublicKey = Configuration["JwtTokenConfig:RsaPublicKey"],
            IDTokenExpirationTimeInMinutes = tokenExpirationTimeInMinutes,
            LogoutTokenExpireTimeInMinutes = logoutTokenExpirationTimeInMinutes,
            JwksUrl = Configuration["JwtTokenConfig:JwksUrl"],
            IdamClienId = Configuration["JwtTokenConfig:IdamClienId"]
          },
          UserExternalApiDetails = new WrapperApi()
          {
            ApiKey = Configuration["WrapperApi:ApiKey"],
            UserServiceUrl = Configuration["WrapperApi:UserServiceUrl"],
            ConfigurationServiceUrl = Configuration["WrapperApi:ConfigurationServiceUrl"]
          },
          PasswordPolicy = new PasswordPolicy()
          {
            LowerAndUpperCaseWithDigits = lowerAndUpperCaseWithDigits,
            RequiredLength = requiredLength,
            RequiredUniqueChars = requiredUniqueChars
          },
          SecurityApiKeySettings = new SecurityApiKeySettings()
          {
            SecurityApiKey = Configuration["SecurityApiKeySettings:SecurityApiKey"],
            ApiKeyValidationExcludedRoutes = Configuration.GetSection("SecurityApiKeySettings:ApiKeyValidationExcludedRoutes").Get<List<string>>(),
            BearerTokenValidationIncludedRoutes = Configuration.GetSection("SecurityApiKeySettings:BearerTokenValidationIncludedRoutes").Get<List<string>>()
          },
          RedisCacheSettings = new Domain.Dtos.RedisCacheSettings()
          {
            ConnectionString = Configuration["RedisCacheSettings:ConnectionString"],
            IsEnabled = isRedisEnabled
          },
          OpenIdConfigurationSettings = new OpenIdConfigurationSettings()
          {
            Issuer = Configuration["OpenIdConfigurationSettings:Issuer"],
            AuthorizationEndpoint = Configuration["OpenIdConfigurationSettings:AuthorizationEndpoint"],
            TokenEndpoint = Configuration["OpenIdConfigurationSettings:TokenEndpoint"],
            DeviceAuthorizationEndpoint = Configuration["OpenIdConfigurationSettings:DeviceAuthorizationEndpoint"],
            UserinfoEndpoint = Configuration["OpenIdConfigurationSettings:UserinfoEndpoint"],
            MfaChallengeEndpoint = Configuration["OpenIdConfigurationSettings:MfaChallengeEndpoint"],
            JwksUri = Configuration["OpenIdConfigurationSettings:JwksUri"],
            RegistrationEndpoint = Configuration["OpenIdConfigurationSettings:RegistrationEndpoint"],
            RevocationEndpoint = Configuration["OpenIdConfigurationSettings:RevocationEndpoint"],
            ScopesSupported = Configuration.GetSection("OpenIdConfigurationSettings:ScopesSupported").Get<List<string>>() ?? new List<string>(),
            ResponseTypesSupported = Configuration.GetSection("OpenIdConfigurationSettings:ResponseTypesSupported").Get<List<string>>() ?? new List<string>(),
            CodeChallengeMethodsSupported = Configuration.GetSection("OpenIdConfigurationSettings:CodeChallengeMethodsSupported").Get<List<string>>() ?? new List<string>(),
            ResponseModesSupported = Configuration.GetSection("OpenIdConfigurationSettings:ResponseModesSupported").Get<List<string>>() ?? new List<string>(),
            SubjectTypesSupported = Configuration.GetSection("OpenIdConfigurationSettings:SubjectTypesSupported").Get<List<string>>() ?? new List<string>(),
            IdTokenSigningAlgValuesSupported = Configuration.GetSection("OpenIdConfigurationSettings:IdTokenSigningAlgValuesSupported").Get<List<string>>() ?? new List<string>(),
            TokenEndpointAuthMethodsSupported = Configuration.GetSection("OpenIdConfigurationSettings:TokenEndpointAuthMethodsSupported").Get<List<string>>() ?? new List<string>(),
            ClaimsSupported = Configuration.GetSection("OpenIdConfigurationSettings:ClaimsSupported").Get<List<string>>() ?? new List<string>(),
            RequestUriParameterSupported = bool.Parse(Configuration["OpenIdConfigurationSettings:RequestUriParameterSupported"]),
          },
          CryptoSettings = new CryptoSettings()
          {
            CookieEncryptionKey = Configuration["Crypto:CookieEncryptionKey"]
          },
          MfaSetting = new MfaSetting()
          {
            TicketExpirationInMinutes = ticketExpirationInMinutes,
            MfaResetRedirectUri = Configuration["MfaSettings:MfaResetRedirectUri"],
            MFAResetPersistentTicketListExpirationInDays = mfaResetPersistentTicketListExpirationInDays
          },
          MockProvider = new MockProvider()
          {
            LoginUrl = Configuration["MockProvider:LoginUrl"]
          },
          ResetPasswordSettings = new ResetPasswordSettings()
          {
            MaxAllowedAttempts = Configuration["ResetPasswordSettings:MaxAllowedAttempts"],
            MaxAllowedAttemptsThresholdInMinutes = Configuration["ResetPasswordSettings:MaxAllowedAttemptsThresholdInMinutes"],
          },
          QueueInfo = new QueueInfo
          {
            EnableDataQueue = enableDataQueue,
            DataQueueUrl = Configuration["QueueInfo:DataQueueUrl"]
          },
        };
        return appConfigInfo;
      });

      services.AddSingleton(s =>
      {
        EmailConfigurationInfo emailConfigurationInfo = new EmailConfigurationInfo
        {
          ApiKey = Configuration["Email:ApiKey"],
        };

        return emailConfigurationInfo;
      });

      services.AddSingleton(s =>
      {
        int.TryParse(Configuration["QueueInfo:DataQueueRecieveMessagesMaxCount"], out int dataQueueRecieveMessagesMaxCount);
        dataQueueRecieveMessagesMaxCount = dataQueueRecieveMessagesMaxCount == 0 ? 10 : dataQueueRecieveMessagesMaxCount;

        int.TryParse(Configuration["QueueInfo:DataQueueRecieveWaitTimeInSeconds"], out int dataQueueRecieveWaitTimeInSeconds); // Default value 0

        var sqsConfiguration = new SqsConfiguration
        {
          ServiceUrl = Configuration["QueueInfo:ServiceUrl"],
          DataQueueAccessKeyId = Configuration["QueueInfo:DataQueueAccessKeyId"],
          DataQueueAccessSecretKey = Configuration["QueueInfo:DataQueueAccessSecretKey"],
          DataQueueRecieveMessagesMaxCount = dataQueueRecieveMessagesMaxCount,
          DataQueueRecieveWaitTimeInSeconds = dataQueueRecieveWaitTimeInSeconds
        };

        return sqsConfiguration;
      });

      if (!string.IsNullOrEmpty(Configuration["RollBarLogger:Token"]) && !string.IsNullOrEmpty(Configuration["RollBarLogger:Environment"]))
      {
        services.AddRollbarLoggerServices(Configuration["RollBarLogger:Token"], Configuration["RollBarLogger:Environment"]);
      }

      services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
      if (Configuration["IdentityProvider"] == "AUTH0")
      {
        services.AddSingleton<IIdentityProviderService, Auth0IdentityProviderService>();
      }
      else if (Configuration["IdentityProvider"] == "MOCK")
      {
        services.AddSingleton<IIdentityProviderService, MockIdentityProviderService>();
      }

      services.AddSingleton<TokenHelper>();
      services.AddSingleton<ITokenService, TokenService>();
      services.AddSingleton<ISecurityCacheService, SecurityCacheService>();
      services.AddSingleton<IJwtTokenHandler, JwtTokenHandler>();
      services.AddSingleton<IEmailProviderService, EmailProviderService>();
      services.AddSingleton<ICcsSsoEmailService, CcsSsoEmailService>();
      services.AddSingleton<ILocalCacheService, InMemoryCacheService>();
      services.AddMemoryCache();
      services.AddSingleton<IRemoteCacheService, RedisCacheService>();
      services.AddSingleton<ICryptographyService, CryptographyService>();
      services.AddSingleton<IAwsDataSqsService, AwsDataSqsService>();
      services.AddSingleton<RedisConnectionPoolService>(_ =>
        new RedisConnectionPoolService(Configuration["RedisCacheSettings:ConnectionString"])
      );
      services.AddDbContext<IDataContext, DataContext>(options => options.UseNpgsql(Configuration["SecurityDbConnection"]));
      services.AddScoped<RequestContext>();
      services.AddScoped<ISecurityService, SecurityService>();
      services.AddScoped<IUserManagerService, UserManagerService>();
      services.AddHttpClient("default").ConfigurePrimaryHttpMessageHandler(() =>
      {
        return new HttpClientHandler()
        {
          AllowAutoRedirect = true,
          UseDefaultCredentials = true
        };
      });

      services.AddCors();

      var jwtTokenInfo = Configuration.GetSection("JwtTokenInfo");
      JwtSettings jwtSettings = new JwtSettings();
      jwtTokenInfo.Bind(jwtSettings);

      // Moved to separate method to solve code climate issue
      ConfigureJwt(services, jwtSettings);

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CcsSso.Security.Api", Version = "v1" });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        c.EnableAnnotations();
      });
    }

    private static void ConfigureJwt(IServiceCollection services, JwtSettings jwtSettings)
    {
      services
                .AddAuthentication(options =>
                {
                  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                  options.SaveToken = true;
                  options.TokenValidationParameters = new TokenValidationParameters
                  {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
                    {
                      // Get JsonWebKeySet from AWS
                      var json = new WebClient().DownloadString(jwtSettings.JWTKeyEndpoint);
                      // Serialize the result
                      return JsonConvert.DeserializeObject<JsonWebKeySet>(json).Keys;
                    },
                    ValidateIssuer = jwtSettings.ValidateIssuer,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateLifetime = true,
                    LifetimeValidator = (before, expires, token, param) => expires > DateTime.UtcNow,
                    ValidateAudience = jwtSettings.ValidateAudience,
                    ValidAudience = jwtSettings.Audience,
                  };
                });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.AddLoggerMiddleware();// Registers the logger configured on the core library
      app.UseHsts();
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

      if (!string.IsNullOrEmpty(Configuration["RollBarLogger:Token"]) && !string.IsNullOrEmpty(Configuration["RollBarLogger:Environment"]))
      {
        app.AddRollbarMiddleware();
      }
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.Security.Api v1"));

      app.UseHttpsRedirection();
      app.UseRouting();
      var _cors = Configuration.GetSection("CorsDomains").Get<string[]>();
      app.UseCors(builder => builder.WithOrigins(_cors)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
      );

      bool additionalLog = Configuration.GetSection("EnableAdditionalLogs").Get<bool>();
      if (additionalLog)
      {
        app.UseMiddleware<RequestLogMiddleware>();
      }
      app.UseMiddleware<CommonExceptionHandlerMiddleware>();
      app.UseMiddleware<AuthenticatorMiddleware>();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
