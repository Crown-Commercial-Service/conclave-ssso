using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Service;
using CcsSso.Core.Service.External;
using CcsSso.DbPersistence;
using CcsSso.Domain;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Contracts.External;
using CcsSso.Domain.Dtos;
using CcsSso.ExternalApi.Middleware;
using CcsSso.Service;
using CcsSso.Service.External;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain;
using CcsSso.Shared.Domain.Contexts;
using CcsSso.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
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

      services.AddControllers();
      services.AddSingleton<ITokenService, TokenService>();
      services.AddSingleton(s =>
      {
        bool.TryParse(Configuration["Email:SendNotificationsEnabled"], out bool sendNotificationsEnabled);
        ApplicationConfigurationInfo appConfigInfo = new ApplicationConfigurationInfo()
        {
          ApiKey = Configuration["ApiKey"],
          ConclaveLoginUrl = Configuration["ConclaveLoginUrl"],
          JwtTokenValidationInfo = new JwtTokenValidationConfigurationInfo()
          {
            IdamClienId = Configuration["JwtTokenValidationInfo:IdamClienId"],
            Issuer = Configuration["JwtTokenValidationInfo:Issuer"],
            JwksUrl = Configuration["JwtTokenValidationInfo:JwksUrl"]
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

      services.AddSingleton<IEmailProviderService, EmailProviderService>();
      services.AddSingleton<ICcsSsoEmailService, CcsSsoEmailService>();

      services.AddDbContext<DataContext>(options => options.UseNpgsql(Configuration["DbConnection"]));
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
      services.AddScoped<IConfigurationDetailService, ConfigurationDetailService>();
      services.AddScoped<IOrganisationGroupService, OrganisationGroupService>();
      services.AddScoped<IIdamService, IdamService>();
      services.AddScoped<ICiiService, CiiService>();
      services.AddHttpClient<ICiiService, CiiService>().ConfigureHttpClient((serviceProvider, httpClient) =>
      {
        httpClient.BaseAddress = new Uri(Configuration["Cii:Url"]);
        httpClient.DefaultRequestHeaders.Add("Apikey", Configuration["Cii:Token"]);
      });
      services.AddHttpClient();

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
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.ExternalApi v1"));
      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseCors(builder => builder.WithOrigins(JsonConvert.DeserializeObject<string[]>(Configuration["CorsDomains"]))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
      );

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
