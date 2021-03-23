using CcsSso.Api.Middleware;
using CcsSso.Core.Domain.Contracts;
using CcsSso.Core.Service;
using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Service;
using Microsoft.AspNetCore.Builder;
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
        ApplicationConfigurationInfo appConfigInfo = new ApplicationConfigurationInfo()
        {
          JwtTokenValidationInfo = new JwtTokenValidationInfo()
          {
            IdamClienId = Configuration["JwtTokenValidationInfo:IdamClienId"],
            Issuer = Configuration["JwtTokenValidationInfo:Issuer"],
            JwksUrl = Configuration["JwtTokenValidationInfo:JwksUrl"]
          }
        };
        return appConfigInfo;
      });

      services.AddControllers();
      services.AddSingleton<IAuthService, AuthService>();
      services.AddDbContext<DataContext>(options => options.UseNpgsql(Configuration["DbConnection"]));
      services.AddScoped<IDataContext>(s => s.GetRequiredService<DataContext>());
      services.AddHttpClient<ICiiService, CiiService>().ConfigureHttpClient((serviceProvider, httpClient) =>
      {
        httpClient.BaseAddress = new Uri(Configuration["Cii:Url"]);
        httpClient.DefaultRequestHeaders.Add("Apikey", Configuration["Cii:ApiKey"]);
      });
      services.AddScoped<IContactService, ContactService>();
      services.AddScoped<IOrganisationService, OrganisationService>();
      services.AddScoped<IUserService, UserService>();
      services.AddScoped<ICiiService, CiiService>();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "CcsSso.Api", Version = "v1" });
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        c.EnableAnnotations();
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CcsSso.Api v1"));
      app.UseHttpsRedirection();

      app.UseRouting();

      app.UseCors(builder => builder.WithOrigins(JsonConvert.DeserializeObject<string[]>(Configuration["CorsDomains"]))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
      );

      app.UseMiddleware<CommonExceptionHandlerMiddleware>();

      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }
  }
}
