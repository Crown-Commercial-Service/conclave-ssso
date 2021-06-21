using CcsSso.Core.Service;
using CcsSso.Core.Tests.Infrastructure;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CcsSso.Tests
{
  public class AuditLoginServiceTest
  {
    public class CreateLog
    {

      public static IEnumerable<object[]> LogData =>
        new List<object[]>
        {
          new object[]
          {
            "TestEvent", "TestApp", "TestReference", new RequestContext { UserId = 1, Device = "TestDevice", IpAddress="127.0.0.1" }
          }
        };

      [Theory]
      [MemberData(nameof(LogData))]
      public async Task CreateLogSuccessfully(string eventName, string appName, string referenceData, RequestContext requestContext)
      {
        await DataContextHelper.ScopeAsync(async dataContext =>
        {
          var mockDateTimeService = new Mock<IDateTimeService>();
          var utcNow = DateTime.UtcNow;
          mockDateTimeService.Setup(s => s.GetUTCNow()).Returns(utcNow);
          var service = AuditLoginService(dataContext, requestContext, mockDateTimeService);

          await service.CreateLogAsync(eventName, appName, referenceData);

          var log = await dataContext.AuditLog.FirstOrDefaultAsync();

          Assert.NotNull(log);
          Assert.Equal(eventName, log.Event);
          Assert.Equal(appName, log.Application);
          Assert.Equal(referenceData, log.ReferenceData);
          Assert.Equal(requestContext.UserId, log.UserId);
          Assert.Equal(requestContext.Device, log.Device);
          Assert.Equal(requestContext.IpAddress, log.IpAddress);
          Assert.Equal(utcNow, log.EventTimeUtc);
        });
      }

      public static AuditLoginService AuditLoginService(IDataContext dataContext, RequestContext requestContext, Mock<IDateTimeService> mockDateTimeService)
      {
        return new AuditLoginService(dataContext, requestContext, mockDateTimeService.Object);
      }
    }
  }
}
