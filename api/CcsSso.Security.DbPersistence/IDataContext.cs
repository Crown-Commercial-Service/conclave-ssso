using CcsSso.Security.DbPersistence.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CcsSso.Security.DbPersistence
{
  public interface IDataContext
  {
    DbSet<RelyingParty> RelyingParties { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  }
}
