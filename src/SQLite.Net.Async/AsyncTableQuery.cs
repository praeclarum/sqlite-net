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
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace SQLite.Net.Async
{
    public class AsyncTableQuery<T>
        where T : class
    {
        [NotNull] private readonly TableQuery<T> _innerQuery;
        private readonly TaskCreationOptions _taskCreationOptions;
        [CanBeNull] private readonly TaskScheduler _taskScheduler;

        /// <summary>
        /// </summary>
        /// <param name="innerQuery"></param>
        /// <param name="taskScheduler">
        ///     If null this parameter will be TaskScheduler.Default (evaluated when used in each method,
        ///     not in ctor)
        /// </param>
        /// <param name="taskCreationOptions">Defaults to DenyChildAttach</param>
        [PublicAPI]
        public AsyncTableQuery([NotNull] TableQuery<T> innerQuery, [CanBeNull] TaskScheduler taskScheduler = null,
            TaskCreationOptions taskCreationOptions = TaskCreationOptions.None)
        {
            if (innerQuery == null)
            {
                throw new ArgumentNullException("innerQuery");
            }
            _innerQuery = innerQuery;
            _taskScheduler = taskScheduler;
            _taskCreationOptions = taskCreationOptions;
        }

        [PublicAPI]
        public AsyncTableQuery<T> Where([NotNull] Expression<Func<T, bool>> predExpr)
        {
            if (predExpr == null)
            {
                throw new ArgumentNullException("predExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.Where(predExpr), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public AsyncTableQuery<T> Skip(int n)
        {
            return new AsyncTableQuery<T>(_innerQuery.Skip(n), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public AsyncTableQuery<T> Take(int n)
        {
            return new AsyncTableQuery<T>(_innerQuery.Take(n), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public AsyncTableQuery<T> OrderBy<TValue>([NotNull] Expression<Func<T, TValue>> orderExpr)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.OrderBy(orderExpr), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public AsyncTableQuery<T> OrderByDescending<TValue>([NotNull] Expression<Func<T, TValue>> orderExpr)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.OrderByDescending(orderExpr), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public AsyncTableQuery<T> ThenBy<TValue>([NotNull] Expression<Func<T, TValue>> orderExpr)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.ThenBy(orderExpr), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public AsyncTableQuery<T> ThenByDescending<TValue>([NotNull] Expression<Func<T, TValue>> orderExpr)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            return new AsyncTableQuery<T>(_innerQuery.ThenByDescending(orderExpr), _taskScheduler ?? TaskScheduler.Default, _taskCreationOptions);
        }

        [PublicAPI]
        public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.ToList();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<int> CountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.Count();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> ElementAtAsync(int index, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    return _innerQuery.ElementAt(index);
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> FirstAsync(CancellationToken cancellationToken = default (CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return _innerQuery.First();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }

        [PublicAPI]
        public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken = default (CancellationToken))
        {
            return Task.Factory.StartNew(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (((SQLiteConnectionWithLock) _innerQuery.Connection).Lock())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return _innerQuery.FirstOrDefault();
                }
            }, cancellationToken, _taskCreationOptions, _taskScheduler ?? TaskScheduler.Default);
        }
    }
}