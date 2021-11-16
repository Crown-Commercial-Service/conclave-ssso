using CcsSso.DbPersistence;
using CcsSso.Domain.Contracts;
using CcsSso.Shared.Contracts;
using CcsSso.Shared.Domain.Contexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CcsSso.Core.Tests.Infrastructure
{
  internal class DataContextHelper
  {
    public static async Task ScopeAsync(Func<IDataContext, Task> action, IDateTimeService dateTimeService = null)
    {
      // In-memory database only exists while the connection is open
      using (var dbConnection = new SqliteConnection("DataSource=:memory:"))
      {
        dbConnection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(dbConnection)
            .Options;

        using (var dataContext = new DataContext(options, new RequestContext { UserId = 0 }, dateTimeService))
        {
          dataContext.Database.EnsureCreated();
          await action(dataContext);
        }
      }
    }
  }
}
