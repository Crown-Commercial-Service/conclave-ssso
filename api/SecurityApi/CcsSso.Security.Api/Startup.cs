using CcsSso.Security.Api.Middleware;
using CcsSso.Security.Domain.Contracts;
using CcsSso.Security.Domain.Dtos;
using CcsSso.Security.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Net;
using Microsoft.AspNetCore.DataProtection;

namespace CcsSso.Security.Api
{
  public class Startup
  {
    private readonly IDataProtector _protector;

    public Startup(IConfiguration configuration)
    {
      var provider = DataProtectionProvider.Create(new System.IO.DirectoryInfo("C:\\keys"));
      _protector = provider.CreateProtector("ccs-sso-api-protector");

      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }
    

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddDataProtection();
      services.AddControllers();
      services.AddSingleton(s =>
      {
        ApplicationConfigurationInfo appConfigInfo = new ApplicationConfigurationInfo()
        {
          AWSRegion = _protector.Unprotect(Configuration["AWSCognito:Region"]),
          AWSPoolId = _protector.Unprotect(Configuration["AWSCognito:PoolId"]),
          AWSAppClientId = _protector.Unprotect(Configuration["AWSCognito:AppClientId"]),
          AWSAccessKeyId = _protector.Unprotect(Configuration["AWSCognito:AccessKeyId"]),
          AWSAccessSecretKey = _protector.Unprotect(Configuration["AWSCognito:AccessSecretKey"])
        };
        return appConfigInfo;
      });
      services.AddSingleton<IIdentityProviderService, AwsIdentityProviderService>();
      services.AddScoped<ISecurityService, SecurityService>();
      services.AddScoped<IUserManagerService, UserManagerService>();

      var jwtTokenInfo = Configuration.GetSection("JwtTokenInfo");
      JwtSettings jwtSettings = new JwtSettings();
      jwtTokenInfo.Bind(jwtSettings);

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

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CcsSso.Security.Api", Version = "v1" });
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.Security.Api v1"));
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseMiddleware<CommonExceptionHandlerMiddleware>();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
