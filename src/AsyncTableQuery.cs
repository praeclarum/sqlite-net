using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public class AsyncTableQuery<T> : IEnumerable<T>
        where T : new()
    {
        private TableQuery<T> InnerQuery { get; set; }

        public AsyncTableQuery(TableQuery<T> innerQuery)
        {
            this.InnerQuery = innerQuery;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.InnerQuery.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public AsyncTableQuery<T> Clone()
        {
            return new AsyncTableQuery<T>(this.InnerQuery.Clone());
        }

        public Task<List<T>> ToListAsync()
        {
            return Task<List<T>>.Factory.StartNew(() =>
            {
                // load the items from the underlying store...
                return new List<T>(this);
            });
        }

        public AsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            var newQuery = this.InnerQuery.Where(predExpr);
            return new AsyncTableQuery<T>(newQuery);
        }

        public Task<int> CountAsync()
        {
            return Task<int>.Factory.StartNew(() =>
            {
                return this.InnerQuery.Count();
            });
        }

        public Task<T> ElementAtAsync(int index)
        {
            return Task<T>.Factory.StartNew(() =>
            {
                return this.InnerQuery.ElementAt(index);

            });
        }

        public AsyncTableQuery<T> Skip(int n)
        {
            var newQuery = this.InnerQuery.Skip(n);
            return new AsyncTableQuery<T>(newQuery);
        }

        public AsyncTableQuery<T> Take(int n)
        {
            var newQuery = this.InnerQuery.Take(n);
            return new AsyncTableQuery<T>(newQuery);
        }

        public AsyncTableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr)
        {
            var newQuery = this.InnerQuery.OrderBy<U>(orderExpr);
            return new AsyncTableQuery<T>(newQuery);
        }

        public AsyncTableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr)
        {
            var newQuery = this.InnerQuery.OrderByDescending<U>(orderExpr);
            return new AsyncTableQuery<T>(newQuery);
        }
    }
}
