using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LstmLgBackend.Models
{
    public interface ILstmLgBackendContext : IDisposable
    {
        void MarkAsModified(Scenario item);
        void MarkAsModified(Intent item);
        void MarkAsModified(Sample item);
        void MarkAsModified(Log item);
        int SaveChanges();
        Task<int> SaveChangesAsync();
        System.Data.Entity.DbSet<LstmLgBackend.Models.Scenario> Scenarios { get; }
        System.Data.Entity.DbSet<LstmLgBackend.Models.Intent> Intents { get; }
        System.Data.Entity.DbSet<LstmLgBackend.Models.Sample> Samples { get; }
        System.Data.Entity.DbSet<LstmLgBackend.Models.Log> Logs { get; }
    }
}
