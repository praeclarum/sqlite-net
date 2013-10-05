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
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    public class SQLiteCommand
    {
        internal static IntPtr NegativePointer = new IntPtr(-1);
        private readonly List<Binding> _bindings;
        private readonly SQLiteConnection _conn;
        private readonly ISQLitePlatform _sqlitePlatform;

        internal SQLiteCommand(ISQLitePlatform platformImplementation, SQLiteConnection conn)
        {
            _sqlitePlatform = platformImplementation;
            _conn = conn;
            _bindings = new List<Binding>();
            CommandText = "";
        }

        public string CommandText { get; set; }

        public int ExecuteNonQuery()
        {
            if (_conn.Trace)
            {
                Debug.WriteLine("Executing: " + this);
            }

            var r = Result.OK;
            IDbStatement stmt = Prepare();
            r = _sqlitePlatform.SQLiteApi.Step(stmt);
            Finalize(stmt);
            if (r == Result.Done)
            {
                int rowsAffected = _sqlitePlatform.SQLiteApi.Changes(_conn.Handle);
                return rowsAffected;
            }
            if (r == Result.Error)
            {
                string msg = _sqlitePlatform.SQLiteApi.Errmsg16(_conn.Handle);
                throw SQLiteException.New(r, msg);
            }
            throw SQLiteException.New(r, r.ToString());
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof (T)));
        }

        public List<T> ExecuteQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof (T))).ToList();
        }

        public List<T> ExecuteQuery<T>(TableMapping map)
        {
            return ExecuteDeferredQuery<T>(map).ToList();
        }

        /// <summary>
        ///     Invoked every time an instance is loaded from the database.
        /// </summary>
        /// <param name='obj'>
        ///     The newly created object.
        /// </param>
        /// <remarks>
        ///     This can be overridden in combination with the <see cref="SQLiteConnection.NewCommand" />
        ///     method to hook into the life-cycle of objects.
        ///     Type safety is not possible because MonoTouch does not support virtual generic methods.
        /// </remarks>
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            if (_conn.Trace)
            {
                Debug.WriteLine("Executing Query: " + this);
            }

            IDbStatement stmt = Prepare();
            try
            {
                var cols = new TableMapping.Column[_sqlitePlatform.SQLiteApi.ColumnCount(stmt)];

                for (int i = 0; i < cols.Length; i++)
                {
                    string name = _sqlitePlatform.SQLiteApi.ColumnName16(stmt, i);
                    cols[i] = map.FindColumn(name);
                }

                while (_sqlitePlatform.SQLiteApi.Step(stmt) == Result.Row)
                {
                    object obj = Activator.CreateInstance(map.MappedType);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] == null)
                        {
                            continue;
                        }
                        ColType colType = _sqlitePlatform.SQLiteApi.ColumnType(stmt, i);
                        object val = ReadCol(stmt, i, colType, cols[i].ColumnType);
                        cols[i].SetValue(obj, val);
                    }
                    OnInstanceCreated(obj);
                    yield return (T) obj;
                }
            }
            finally
            {
                _sqlitePlatform.SQLiteApi.Finalize(stmt);
            }
        }

        public T ExecuteScalar<T>()
        {
            if (_conn.Trace)
            {
                Debug.WriteLine("Executing Query: " + this);
            }

            T val = default(T);

            IDbStatement stmt = Prepare();

            try
            {
                Result r = _sqlitePlatform.SQLiteApi.Step(stmt);
                if (r == Result.Row)
                {
                    ColType colType = _sqlitePlatform.SQLiteApi.ColumnType(stmt, 0);
                    val = (T) ReadCol(stmt, 0, colType, typeof (T));
                }
                else if (r == Result.Done)
                {
                }
                else
                {
                    throw SQLiteException.New(r, _sqlitePlatform.SQLiteApi.Errmsg16(_conn.Handle));
                }
            }
            finally
            {
                Finalize(stmt);
            }

            return val;
        }

        public void Bind(string name, object val)
        {
            _bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        public void Bind(object val)
        {
            Bind(null, val);
        }

        public override string ToString()
        {
            var parts = new string[1 + _bindings.Count];
            parts[0] = CommandText;
            int i = 1;
            foreach (Binding b in _bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }
            return string.Join(Environment.NewLine, parts);
        }

        private IDbStatement Prepare()
        {
            IDbStatement stmt = _sqlitePlatform.SQLiteApi.Prepare2(_conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }

        private void Finalize(IDbStatement stmt)
        {
            _sqlitePlatform.SQLiteApi.Finalize(stmt);
        }

        private void BindAll(IDbStatement stmt)
        {
            int nextIdx = 1;
            foreach (Binding b in _bindings)
            {
                if (b.Name != null)
                {
                    b.Index = _sqlitePlatform.SQLiteApi.BindParameterIndex(stmt, b.Name);
                }
                else
                {
                    b.Index = nextIdx++;
                }

                BindParameter(_sqlitePlatform.SQLiteApi, stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks);
            }
        }

        internal static void BindParameter(ISQLiteApi isqLite3Api, IDbStatement stmt, int index, object value,
            bool storeDateTimeAsTicks)
        {
            if (value == null)
            {
                isqLite3Api.BindNull(stmt, index);
            }
            else
            {
                if (value is Int32)
                {
                    isqLite3Api.BindInt(stmt, index, (int) value);
                }
                else if (value is String)
                {
                    isqLite3Api.BindText16(stmt, index, (string) value, -1, NegativePointer);
                }
                else if (value is Byte || value is UInt16 || value is SByte || value is Int16)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is Boolean)
                {
                    isqLite3Api.BindInt(stmt, index, (bool) value ? 1 : 0);
                }
                else if (value is UInt32 || value is Int64)
                {
                    isqLite3Api.BindInt64(stmt, index, Convert.ToInt64(value));
                }
                else if (value is Single || value is Double || value is Decimal)
                {
                    isqLite3Api.BindDouble(stmt, index, Convert.ToDouble(value));
                }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks)
                    {
                        isqLite3Api.BindInt64(stmt, index, ((DateTime) value).Ticks);
                    }
                    else
                    {
                        isqLite3Api.BindText16(stmt, index, ((DateTime) value).ToString("yyyy-MM-dd HH:mm:ss"), -1,
                            NegativePointer);
                    }
                }
                else if (value.GetType().IsEnum)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is byte[])
                {
                    isqLite3Api.BindBlob(stmt, index, (byte[]) value, ((byte[]) value).Length, NegativePointer);
                }
                else if (value is Guid)
                {
                    isqLite3Api.BindText16(stmt, index, ((Guid) value).ToString(), 72, NegativePointer);
                }
                else
                {
                    throw new NotSupportedException("Cannot store type: " + value.GetType());
                }
            }
        }

        private object ReadCol(IDbStatement stmt, int index, ColType type, Type clrType)
        {
            if (type == ColType.Null)
            {
                return null;
            }
            if (clrType == typeof (String))
            {
                return _sqlitePlatform.SQLiteApi.ColumnText16(stmt, index);
            }
            if (clrType == typeof (Int32))
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (Boolean))
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index) == 1;
            }
            if (clrType == typeof (double))
            {
                return _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
            }
            if (clrType == typeof (float))
            {
                return (float) _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
            }
            if (clrType == typeof (DateTime))
            {
                if (_conn.StoreDateTimeAsTicks)
                {
                    return new DateTime(_sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index));
                }
                string text = _sqlitePlatform.SQLiteApi.ColumnText16(stmt, index);
                return DateTime.Parse(text);
            }
            if (clrType.IsEnum)
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (Int64))
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index);
            }
            if (clrType == typeof (UInt32))
            {
                return (uint) _sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index);
            }
            if (clrType == typeof (decimal))
            {
                return (decimal) _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
            }
            if (clrType == typeof (Byte))
            {
                return (byte) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (UInt16))
            {
                return (ushort) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (Int16))
            {
                return (short) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (sbyte))
            {
                return (sbyte) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (byte[]))
            {
                return _sqlitePlatform.SQLiteApi.ColumnByteArray(stmt, index);
            }
            if (clrType == typeof (Guid))
            {
                string text = _sqlitePlatform.SQLiteApi.ColumnText16(stmt, index);
                return new Guid(text);
            }
            throw new NotSupportedException("Don't know how to read " + clrType);
        }

        private class Binding
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public int Index { get; set; }
        }
    }
}