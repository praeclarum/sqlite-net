// Copyright (c) 2012 Krueger Systems, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SQLite.Net.Async
{
    public class AsyncTableQuery<T>
        where T : new()
    {
        private readonly TableQuery<T> _innerQuery;
        private readonly TaskFactory _taskFactory;

        public AsyncTableQuery(TableQuery<T> innerQuery, TaskFactory taskFactory)
        {
            if (innerQuery == null)
            {
                throw new ArgumentNullException("innerQuery");
            }
            _innerQuery = innerQuery;
            _taskFactory = taskFactory;
        }

        public AsyncTableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            if (predExpr == null)
            {
                throw new ArgumentNullException("predExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.Where(predExpr), _taskFactory);
        }

        public AsyncTableQuery<T> Skip(int n)
        {
            return new AsyncTableQuery<T>(_innerQuery.Skip(n), _taskFactory);
        }

        public AsyncTableQuery<T> Take(int n)
        {
            return new AsyncTableQuery<T>(_innerQuery.Take(n), _taskFactory);
        }

        public AsyncTableQuery<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.OrderBy(orderExpr), _taskFactory);
        }

        public AsyncTableQuery<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.OrderByDescending(orderExpr), _taskFactory);
        }

        public Task<List<T>> ToListAsync()
        {
            return _taskFactory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.ToList();
                }
            });
        }

        public Task<int> CountAsync()
        {
            return _taskFactory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.Count();
                }
            });
        }

        public Task<T> ElementAtAsync(int index)
        {
            return _taskFactory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.ElementAt(index);
                }
            });
        }

        public Task<T> FirstAsync()
        {
            return _taskFactory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.First();
                }
            });
        }

        public Task<T> FirstOrDefaultAsync()
        {
            return _taskFactory.StartNew(() =>
            {
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.FirstOrDefault();
                }
            });
        }
    }
}