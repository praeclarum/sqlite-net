//
// Copyright (c) 2012 Krueger Systems, Inc.
// Copyright (c) 2013 Øystein Krog (oystein.krog@gmail.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JetBrains.Annotations;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    public class TableQuery<T> : BaseTableQuery, IEnumerable<T>
    {
        private readonly ISQLitePlatform _sqlitePlatform;
        private bool _deferred;
        private BaseTableQuery _joinInner;
        private Expression _joinInnerKeySelector;
        private BaseTableQuery _joinOuter;
        private Expression _joinOuterKeySelector;
        private Expression _joinSelector;
        private int? _limit;
        private int? _offset;
        private List<Ordering> _orderBys;
        private Expression _where;

        private TableQuery(ISQLitePlatform platformImplementation, SQLiteConnection conn, TableMapping table)
        {
            _sqlitePlatform = platformImplementation;
            Connection = conn;
            Table = table;
        }

        [PublicAPI]
        public TableQuery(ISQLitePlatform platformImplementation, SQLiteConnection conn)
        {
            _sqlitePlatform = platformImplementation;
            Connection = conn;
            Table = Connection.GetMapping(typeof (T));
        }

        [PublicAPI]
        public SQLiteConnection Connection { get; private set; }

        [PublicAPI]
        public TableMapping Table { get; private set; }

        [PublicAPI]
        public IEnumerator<T> GetEnumerator()
        {
            if (!_deferred)
            {
                return GenerateCommand("*").ExecuteQuery<T>().GetEnumerator();
            }

            return GenerateCommand("*").ExecuteDeferredQuery<T>().GetEnumerator();
        }

        [PublicAPI]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PublicAPI]
        public TableQuery<U> Clone<U>()
        {
            return new TableQuery<U>(_sqlitePlatform, Connection, Table)
            {
                _where = _where,
                _deferred = _deferred,
                _limit = _limit,
                _offset = _offset,
                _joinInner = _joinInner,
                _joinInnerKeySelector = _joinInnerKeySelector,
                _joinOuter = _joinOuter,
                _joinOuterKeySelector = _joinOuterKeySelector,
                _joinSelector = _joinSelector,
                _orderBys = _orderBys == null ? null : new List<Ordering>(_orderBys)
            };
        }

        [PublicAPI]
        public TableQuery<T> Where([NotNull] Expression<Func<T, bool>> predExpr)
        {
            if (predExpr == null)
            {
                throw new ArgumentNullException("predExpr");
            }
            if (predExpr.NodeType != ExpressionType.Lambda)
            {
                throw new NotSupportedException("Must be a predicate");
            }
            var lambda = (LambdaExpression) predExpr;
            var pred = lambda.Body;
            var q = Clone<T>();
            q.AddWhere(pred);
            return q;
        }

        [PublicAPI]
        public TableQuery<T> Take(int n)
        {
            var q = Clone<T>();

            // If there is already a limit then the limit will be the minimum
            // of the current limit and n.
            q._limit = Math.Min(q._limit ?? int.MaxValue, n);
            return q;
        }

        [PublicAPI]
        public int Delete([NotNull] Expression<Func<T, bool>> predExpr)
        {
            if (predExpr == null)
            {
                throw new ArgumentNullException("predExpr");
            }
            if (predExpr.NodeType != ExpressionType.Lambda)
            {
                throw new NotSupportedException("Must be a predicate");
            }
            if (_limit != null)
            {
                //SQLite provides a limit to deletions so this would be possible to implement in the future
                //You would need to take care that the correct order was being applied.
                throw new NotSupportedException("Cannot delete if a limit has been specified");
            }
            if (_offset != null)
            {
                throw new NotSupportedException("Cannot delete if an offset has been specified");
            }
            var lambda = (LambdaExpression) predExpr;
            var pred = lambda.Body;
            if (_where != null)
            {
                pred = Expression.AndAlso(pred, _where);
            }
            var args = new List<object>();
            var w = CompileExpr(pred, args);
            var cmdText = "delete from \"" + Table.TableName + "\"";
            cmdText += " where " + w.CommandText;
            var command = Connection.CreateCommand(cmdText, args.ToArray());

            var result = command.ExecuteNonQuery();
            return result;
        }

        [PublicAPI]
        public TableQuery<T> Skip(int n)
        {
            var q = Clone<T>();

            q._offset = n + (q._offset ?? 0);
            return q;
        }

        [PublicAPI]
        public T ElementAt(int index)
        {
            return Skip(index).Take(1).First();
        }

        [PublicAPI]
        public TableQuery<T> Deferred()
        {
            var q = Clone<T>();
            q._deferred = true;
            return q;
        }

        [PublicAPI]
        public TableQuery<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            return AddOrderBy(orderExpr, true);
        }

        [PublicAPI]
        public TableQuery<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            return AddOrderBy(orderExpr, false);
        }

        [PublicAPI]
        public TableQuery<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            return AddOrderBy(orderExpr, true);
        }

        [PublicAPI]
        public TableQuery<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpr)
        {
            return AddOrderBy(orderExpr, false);
        }

        private TableQuery<T> AddOrderBy<TValue>([NotNull] Expression<Func<T, TValue>> orderExpr, bool asc)
        {
            if (orderExpr == null)
            {
                throw new ArgumentNullException("orderExpr");
            }
            if (orderExpr.NodeType != ExpressionType.Lambda)
            {
                throw new NotSupportedException("Must be a predicate");
            }
            var lambda = (LambdaExpression) orderExpr;

            MemberExpression mem;

            var unary = lambda.Body as UnaryExpression;
            if (unary != null && unary.NodeType == ExpressionType.Convert)
            {
                mem = unary.Operand as MemberExpression;
            }
            else
            {
                mem = lambda.Body as MemberExpression;
            }

            if (mem == null || (mem.Expression.NodeType != ExpressionType.Parameter))
            {
                throw new NotSupportedException("Order By does not support: " + orderExpr);
            }
            var q = Clone<T>();
            if (q._orderBys == null)
            {
                q._orderBys = new List<Ordering>();
            }
            q._orderBys.Add(new Ordering
            {
                ColumnName = Table.FindColumnWithPropertyName(mem.Member.Name).Name,
                Ascending = asc
            });
            return q;
        }

        private void AddWhere([NotNull] Expression pred)
        {
            if (pred == null)
            {
                throw new ArgumentNullException("pred");
            }
            if (_limit != null || _offset != null)
            {
                throw new NotSupportedException("Cannot call where after a skip or a take");
            }

            if (_where == null)
            {
                _where = pred;
            }
            else
            {
                _where = Expression.AndAlso(_where, pred);
            }
        }

        [PublicAPI]
        public TableQuery<TResult> Join<TInner, TKey, TResult>(
            TableQuery<TInner> inner,
            Expression<Func<T, TKey>> outerKeySelector,
            Expression<Func<TInner, TKey>> innerKeySelector,
            Expression<Func<T, TInner, TResult>> resultSelector)
        {
            var q = new TableQuery<TResult>(_sqlitePlatform, Connection, Connection.GetMapping(typeof (TResult)))
            {
                _joinOuter = this,
                _joinOuterKeySelector = outerKeySelector,
                _joinInner = inner,
                _joinInnerKeySelector = innerKeySelector,
                _joinSelector = resultSelector
            };
            return q;
        }

        private SQLiteCommand GenerateCommand([NotNull] string selectionList)
        {
            if (selectionList == null)
            {
                throw new ArgumentNullException("selectionList");
            }
            if (_joinInner != null && _joinOuter != null)
            {
                throw new NotSupportedException("Joins are not supported.");
            }
            var cmdText = "select " + selectionList + " from \"" + Table.TableName + "\"";
            var args = new List<object>();
            if (_where != null)
            {
                var w = CompileExpr(_where, args);
                cmdText += " where " + w.CommandText;
            }
            if ((_orderBys != null) && (_orderBys.Count > 0))
            {
                var t = string.Join(", ",
                    _orderBys.Select(o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " desc")).ToArray());
                cmdText += " order by " + t;
            }
            if (_limit.HasValue)
            {
                cmdText += " limit " + _limit.Value;
            }
            if (_offset.HasValue)
            {
                if (!_limit.HasValue)
                {
                    cmdText += " limit -1 ";
                }
                cmdText += " offset " + _offset.Value;
            }
            return Connection.CreateCommand(cmdText, args.ToArray());
        }

        private CompileResult CompileExpr([NotNull] Expression expr, List<object> queryArgs)
        {
            if (expr == null)
            {
                throw new NotSupportedException("Expression is NULL");
            }
            if (expr is BinaryExpression)
            {
                var bin = (BinaryExpression) expr;

                var leftr = CompileExpr(bin.Left, queryArgs);
                var rightr = CompileExpr(bin.Right, queryArgs);

                //If either side is a parameter and is null, then handle the other side specially (for "is null"/"is not null")
                string text;
                if (leftr.CommandText == "?" && leftr.Value == null)
                {
                    text = CompileNullBinaryExpression(bin, rightr);
                }
                else if (rightr.CommandText == "?" && rightr.Value == null)
                {
                    text = CompileNullBinaryExpression(bin, leftr);
                }
                else
                {
                    text = "(" + leftr.CommandText + " " + GetSqlName(bin) + " " + rightr.CommandText + ")";
                }
                return new CompileResult
                {
                    CommandText = text
                };
            }
            if (expr.NodeType == ExpressionType.Not)
            {
                var operandExpr = ((UnaryExpression) expr).Operand;
                var opr = CompileExpr(operandExpr, queryArgs);
                var val = opr.Value;
                if (val is bool)
                {
                    val = !((bool) val);
                }
                return new CompileResult
                {
                    CommandText = "NOT(" + opr.CommandText + ")",
                    Value = val
                };
            }
            if (expr.NodeType == ExpressionType.Call)
            {
                var call = (MethodCallExpression) expr;
                var args = new CompileResult[call.Arguments.Count];
                var obj = call.Object != null ? CompileExpr(call.Object, queryArgs) : null;

                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = CompileExpr(call.Arguments[i], queryArgs);
                }

                var sqlCall = "";

                if (call.Method.Name == "Like" && args.Length == 2)
                {
                    sqlCall = "(" + args[0].CommandText + " like " + args[1].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 2)
                {
                    sqlCall = "(" + args[1].CommandText + " in " + args[0].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 1)
                {
                    if (call.Object != null && call.Object.Type == typeof (string))
                    {
                        sqlCall = "(" + obj.CommandText + " like ('%' || " + args[0].CommandText + " || '%'))";
                    }
                    else
                    {
                        sqlCall = "(" + args[0].CommandText + " in " + obj.CommandText + ")";
                    }
                }
                else if (call.Method.Name == "StartsWith" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " like (" + args[0].CommandText + " || '%'))";
                }
                else if (call.Method.Name == "EndsWith" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " like ('%' || " + args[0].CommandText + "))";
                }
                else if (call.Method.Name == "Equals" && args.Length == 1)
                {
                    sqlCall = "(" + obj.CommandText + " = (" + args[0].CommandText + "))";
                }
                else if (call.Method.Name == "ToLower")
                {
                    sqlCall = "(lower(" + obj.CommandText + "))";
                }
                else if (call.Method.Name == "ToUpper")
                {
                    sqlCall = "(upper(" + obj.CommandText + "))";
                }
                else if (call.Method.Name == "Replace" && args.Length == 2)
                {
                    sqlCall = "(replace(" + obj.CommandText + ", " + args[0].CommandText + ", " + args[1].CommandText + "))";
                }
                else
                {
                    sqlCall = call.Method.Name.ToLower() + "(" +
                              string.Join(",", args.Select(a => a.CommandText).ToArray()) + ")";
                }
                return new CompileResult
                {
                    CommandText = sqlCall
                };
            }
            if (expr.NodeType == ExpressionType.Constant)
            {
                var c = (ConstantExpression) expr;
                queryArgs.Add(c.Value);
                return new CompileResult
                {
                    CommandText = "?",
                    Value = c.Value
                };
            }
            if (expr.NodeType == ExpressionType.Convert)
            {
                var u = (UnaryExpression) expr;
                var ty = u.Type;
                var valr = CompileExpr(u.Operand, queryArgs);
                return new CompileResult
                {
                    CommandText = valr.CommandText,
                    Value = valr.Value != null ? ConvertTo(valr.Value, ty) : null
                };
            }
            if (expr.NodeType == ExpressionType.MemberAccess)
            {
                var mem = (MemberExpression) expr;

                if (mem.Expression != null && mem.Expression.NodeType == ExpressionType.Parameter)
                {
                    //
                    // This is a column of our table, output just the column name
                    // Need to translate it if that column name is mapped
                    //
                    var columnName = Table.FindColumnWithPropertyName(mem.Member.Name).Name;
                    return new CompileResult
                    {
                        CommandText = "\"" + columnName + "\""
                    };
                }
                object obj = null;
                if (mem.Expression != null)
                {
                    var r = CompileExpr(mem.Expression, queryArgs);
                    if (r.Value == null)
                    {
                        throw new NotSupportedException("Member access failed to compile expression");
                    }
                    if (r.CommandText == "?")
                    {
                        queryArgs.RemoveAt(queryArgs.Count - 1);
                    }
                    obj = r.Value;
                }

                //
                // Get the member value
                //
                var val = _sqlitePlatform.ReflectionService.GetMemberValue(obj, expr, mem.Member);

                //
                // Work special magic for enumerables
                //
                if (val != null && val is IEnumerable && !(val is string) && !(val is IEnumerable<byte>))
                {
                    var sb = new StringBuilder();
                    sb.Append("(");
                    var head = "";
                    foreach (var a in (IEnumerable) val)
                    {
                        queryArgs.Add(a);
                        sb.Append(head);
                        sb.Append("?");
                        head = ",";
                    }
                    sb.Append(")");
                    return new CompileResult
                    {
                        CommandText = sb.ToString(),
                        Value = val
                    };
                }
                queryArgs.Add(val);
                return new CompileResult
                {
                    CommandText = "?",
                    Value = val
                };
            }
            throw new NotSupportedException("Cannot compile: " + expr.NodeType);
        }

        [CanBeNull]
        private object ConvertTo(object obj, Type t)
        {
            var nut = Nullable.GetUnderlyingType(t);

            if (nut != null)
            {
                if (obj == null)
                {
                    return null;
                }
                return Convert.ChangeType(obj, nut, CultureInfo.CurrentCulture);
            }
            return Convert.ChangeType(obj, t, CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///     Compiles a BinaryExpression where one of the parameters is null.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="parameter">The non-null parameter</param>
        private string CompileNullBinaryExpression(BinaryExpression expression, CompileResult parameter)
        {
            if (expression.NodeType == ExpressionType.Equal)
            {
                return "(" + parameter.CommandText + " is ?)";
            }
            if (expression.NodeType == ExpressionType.NotEqual)
            {
                return "(" + parameter.CommandText + " is not ?)";
            }
            throw new NotSupportedException("Cannot compile Null-BinaryExpression with type " +
                                            expression.NodeType);
        }

        private string GetSqlName(BinaryExpression expr)
        {
            var n = expr.NodeType;
            if (n == ExpressionType.GreaterThan)
            {
                return ">";
            }
            if (n == ExpressionType.GreaterThanOrEqual)
            {
                return ">=";
            }
            if (n == ExpressionType.LessThan)
            {
                return "<";
            }
            if (n == ExpressionType.LessThanOrEqual)
            {
                return "<=";
            }
            if (n == ExpressionType.And)
            {
                return "&";
            }
            if (n == ExpressionType.AndAlso)
            {
                return "and";
            }
            if (n == ExpressionType.Or)
            {
                return "|";
            }
            if (n == ExpressionType.OrElse)
            {
                return "or";
            }
            if (n == ExpressionType.Equal)
            {
                return "=";
            }
            if (n == ExpressionType.NotEqual)
            {
                return "!=";
            }
            if (n == ExpressionType.Add)
            {
                if (expr.Left.Type == typeof(string))
                {
                    return "||";
                }
                return "+";

            }
            if (n == ExpressionType.Subtract)
            {
                return "-";
            }

            throw new NotSupportedException("Cannot get SQL for: " + n);
        }

        [PublicAPI]
        public int Count()
        {
            return GenerateCommand("count(*)").ExecuteScalar<int>();
        }

        [PublicAPI]
        public int Count([NotNull] Expression<Func<T, bool>> predExpr)
        {
            if (predExpr == null)
            {
                throw new ArgumentNullException("predExpr");
            }
            return Where(predExpr).Count();
        }

        [PublicAPI]
        public T First()
        {
            var query = Take(1);
            return query.ToList().First();
        }

        [PublicAPI]
        public T FirstOrDefault()
        {
            var query = Take(1);
            return query.ToList().FirstOrDefault();
        }

        private class CompileResult
        {
            public string CommandText { get; set; }

            [CanBeNull]
            public object Value { get; set; }
        }
    }
}