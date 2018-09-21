using LstmLgBackend.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LstmLgBackend.Test
{
    public class TestDbSet<T> : DbSet<T>, IQueryable, IEnumerable<T>
        where T : class
    {
        ObservableCollection<T> _data;
        IQueryable _query;

        public TestDbSet()
        {
            _data = new ObservableCollection<T>();
            _query = _data.AsQueryable();
        }

        public override T Add(T item)
        {
            _data.Add(item);
            return item;
        }

        public override T Remove(T item)
        {
            _data.Remove(item);
            return item;
        }

        public override T Attach(T item)
        {
            _data.Add(item);
            return item;
        }

        public override T Create()
        {
            return Activator.CreateInstance<T>();
        }

        public override TDerivedEntity Create<TDerivedEntity>()
        {
            return Activator.CreateInstance<TDerivedEntity>();
        }

        public override ObservableCollection<T> Local
        {
            get { return new ObservableCollection<T>(_data); }
        }

        Type IQueryable.ElementType
        {
            get { return _query.ElementType; }
        }

        System.Linq.Expressions.Expression IQueryable.Expression
        {
            get { return _query.Expression; }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return _query.Provider; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }


    }


    public class TestScenarioDbSet : TestDbSet<Scenario>
    {

    }

    public class TestIntentDbSet : TestDbSet<Intent>
    {

    }

    public class TestSampleDbSet : TestDbSet<Sample>
    {

    }

    public class TestLogDbSet : TestDbSet<Log>
    {

    }
    public class TestLstmLgBackendContext : ILstmLgBackendContext
    {
        public TestLstmLgBackendContext()
        {
            this.Scenarios = new TestScenarioDbSet();
            this.Intents = new TestIntentDbSet();
            this.Samples = new TestSampleDbSet();
            this.Logs = new TestLogDbSet();
    
        }

        public DbSet<Scenario> Scenarios { get; set; }

        public DbSet<Intent> Intents { get; set; }
        public DbSet<Sample> Samples { get; set; }
        public DbSet<Log> Logs { get; set; }


        public void MarkAsModified(Scenario item) { }

        public void MarkAsModified(Intent item) { }
        public void MarkAsModified(Sample item) { }
        public void MarkAsModified(Log item) { }

        public int SaveChanges()
        {
            return 0;
        }

        public Task<int> SaveChangesAsync()
        {
            return null;
        }

        public void Dispose()
        {

        }
      
    }
}
