using CcsSso.Core.DbModel.Entity;
using CcsSso.Core.Domain.Contracts.External;
using CcsSso.Core.Domain.Dtos.External;
using CcsSso.Core.Service.External;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.Domain.Contracts;
using CcsSso.Domain.Dtos;
using CcsSso.Domain.Dtos.External;
using CcsSso.Shared.Cache.Contracts;
using CcsSso.Shared.Cache.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Core.Tests.External
{
  public class ConfigurationServiceTests
  {
    public static IEnumerable<object[]> CorrectCcsServiceData =>
            new List<object[]>
            {
                new object[]
                {
                  new CcsServiceInfo() { Id = 1, Code = "DIGITS", Description = "digits", Name = "Digit Service", Url = "http://localhost:4000" }
                },
                new object[]
                {
                  new CcsServiceInfo() { Id = 2, Code = "CAT", Description = "CAT", Name = "CAT", Url = "http://localhost:4001" }
                }
            };

    [Theory]
    [MemberData(nameof(CorrectCcsServiceData))]
    public async Task CreateContactSuccessfully_WhenCorrectData(CcsServiceInfo CcsServicesExpected)
    {
      await DataContextHelper.ScopeAsync(async dataContext =>
      {
        await SetupTestDataAsync(dataContext);

        var configurationDetailService = GetConfigurationDetailService(dataContext);

        var ccsServices = await configurationDetailService.GetCcsServicesAsync();

        Assert.NotNull(ccsServices);
        Assert.Equal(2, ccsServices.Count);
        var ccsService = ccsServices.Find(c => c.Id == CcsServicesExpected.Id);
        Assert.Equal(CcsServicesExpected.Name, ccsService.Name);
        Assert.Equal(CcsServicesExpected.Description, ccsService.Description);
        Assert.Equal(CcsServicesExpected.Url, ccsService.Url);
        Assert.Equal(CcsServicesExpected.Code, ccsService.Code);

      });
    }

    public static ConfigurationDetailService GetConfigurationDetailService(IDataContext dataContext)
    {
      var memCacheService = GetLocalCache();
      ApplicationConfigurationInfo applicationConfigurationInfo = new();
      var mockRolesToServiceRoleGroupMapperService = new Mock<IServiceRoleGroupMapperService>();

      var service = new ConfigurationDetailService(dataContext, memCacheService, applicationConfigurationInfo, mockRolesToServiceRoleGroupMapperService.Object);
      return service;
    }

    public static async Task SetupTestDataAsync(IDataContext dataContext)
    {
      dataContext.CcsService.Add(new CcsService { Id = 1, ServiceName = "Digit Service", Description = "digits", ServiceUrl = "http://localhost:4000", ServiceCode = "DIGITS", IsDeleted = false });
      dataContext.CcsService.Add(new CcsService { Id = 2, ServiceName = "CAT", Description = "CAT", ServiceUrl = "http://localhost:4001", ServiceCode = "CAT", IsDeleted = false });
      dataContext.CcsService.Add(new CcsService { Id = 3, ServiceName = "Dashboard", Description = "Dashboard", ServiceUrl = "http://localhost:4003", ServiceCode = "DASHBOARD", IsDeleted = true });
      await dataContext.SaveChangesAsync();
    }

    private static ILocalCacheService GetLocalCache()
    {
      var services = new ServiceCollection();
      services.AddMemoryCache();
      var serviceProvider = services.BuildServiceProvider();

      var memoryCache = serviceProvider.GetService<IMemoryCache>();
      var localCache = new InMemoryCacheService(memoryCache);
      return localCache;
    }
  }
}
