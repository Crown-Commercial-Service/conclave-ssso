using CcsSso.Adaptor.DbDomain;
using CcsSso.Adaptor.DbPersistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace CcsSso.Adaptor.Tests.Infrastructure
{
  internal class DataContextHelper
  {
    public static async Task ScopeAsync(Func<IDataContext, Task> action)
    {
      // In-memory database only exists while the connection is open
      using (var dbConnection = new SqliteConnection("DataSource=:memory:"))
      {
        dbConnection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(dbConnection)
            .Options;

        using (var dataContext = new DataContext(options))
        {
          dataContext.Database.EnsureCreated();
          await action(dataContext);
        }
      }
    }
  }
}
