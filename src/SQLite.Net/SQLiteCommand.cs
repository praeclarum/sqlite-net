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
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    public class SQLiteCommand
    {
        private static readonly IntPtr NegativePointer = new IntPtr(-1);

        [NotNull] private readonly List<Binding> _bindings;

        private readonly SQLiteConnection _conn;
        private readonly ISQLitePlatform _sqlitePlatform;
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

        internal SQLiteCommand(ISQLitePlatform platformImplementation, SQLiteConnection conn)
        {
            _sqlitePlatform = platformImplementation;
            _conn = conn;
            _bindings = new List<Binding>();
            CommandText = "";
        }

        [PublicAPI]
        public string CommandText { get; set; }

        [PublicAPI]
        public int ExecuteNonQuery()
        {
            _conn.TraceListener.WriteLine("Executing: {0}", this);

            var stmt = Prepare();
            var r = _sqlitePlatform.SQLiteApi.Step(stmt);
            Finalize(stmt);
            if (r == Result.Done)
            {
                var rowsAffected = _sqlitePlatform.SQLiteApi.Changes(_conn.Handle);
                return rowsAffected;
            }
            if (r == Result.Error)
            {
                var msg = _sqlitePlatform.SQLiteApi.Errmsg16(_conn.Handle);
                throw SQLiteException.New(r, msg);
            }
            if (r == Result.Constraint)
            {
                if (_sqlitePlatform.SQLiteApi.ExtendedErrCode(_conn.Handle) == ExtendedResult.ConstraintNotNull)
                {
                    throw NotNullConstraintViolationException.New(r, _sqlitePlatform.SQLiteApi.Errmsg16(_conn.Handle));
                }
            }
            throw SQLiteException.New(r, r.ToString());
        }

        [PublicAPI]
        public IEnumerable<T> ExecuteDeferredQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof (T)));
        }

        [PublicAPI]
        public List<T> ExecuteQuery<T>()
        {
            return ExecuteDeferredQuery<T>(_conn.GetMapping(typeof (T))).ToList();
        }

        [PublicAPI]
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
        [PublicAPI]
        protected virtual void OnInstanceCreated(object obj)
        {
            // Can be overridden.
        }

        [PublicAPI]
        public IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map)
        {
            _conn.TraceListener.WriteLine("Executing Query: {0}", this);

            var stmt = Prepare();
            try
            {
                var cols = new TableMapping.Column[_sqlitePlatform.SQLiteApi.ColumnCount(stmt)];

                for (var i = 0; i < cols.Length; i++)
                {
                    var name = _sqlitePlatform.SQLiteApi.ColumnName16(stmt, i);
                    cols[i] = map.FindColumn(name);
                }

                while (_sqlitePlatform.SQLiteApi.Step(stmt) == Result.Row)
                {
                    var obj = _conn.Resolver.CreateObject(map.MappedType);
                    for (var i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] == null)
                        {
                            continue;
                        }
                        var colType = _sqlitePlatform.SQLiteApi.ColumnType(stmt, i);
                        var val = ReadCol(stmt, i, colType, cols[i].ColumnType);
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

        [PublicAPI]
        [CanBeNull]
        public T ExecuteScalar<T>()
        {
            _conn.TraceListener.WriteLine("Executing Query: {0}", this);

            var val = default(T);

            var stmt = Prepare();

            try
            {
                var r = _sqlitePlatform.SQLiteApi.Step(stmt);
                if (r == Result.Row)
                {
                    var colType = _sqlitePlatform.SQLiteApi.ColumnType(stmt, 0);
                    var clrType = Nullable.GetUnderlyingType(typeof (T)) ?? typeof (T);
                    if (colType != ColType.Null)
                    {
                        val = (T) ReadCol(stmt, 0, colType, clrType);
                    }
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

        [PublicAPI]
        public void Bind([CanBeNull] string name, [CanBeNull] object val)
        {
            _bindings.Add(new Binding
            {
                Name = name,
                Value = val
            });
        }

        [PublicAPI]
        public void Bind(object val)
        {
            Bind(null, val);
        }

        [PublicAPI]
        public override string ToString()
        {
            var parts = new string[1 + _bindings.Count];
            parts[0] = CommandText;
            var i = 1;
            foreach (var b in _bindings)
            {
                parts[i] = string.Format("  {0}: {1}", i - 1, b.Value);
                i++;
            }
            return string.Join(Environment.NewLine, parts);
        }

        private IDbStatement Prepare()
        {
            var stmt = _sqlitePlatform.SQLiteApi.Prepare2(_conn.Handle, CommandText);
            BindAll(stmt);
            return stmt;
        }

        private void Finalize(IDbStatement stmt)
        {
            _sqlitePlatform.SQLiteApi.Finalize(stmt);
        }

        private void BindAll(IDbStatement stmt)
        {
            var nextIdx = 1;
            foreach (var b in _bindings)
            {
                if (b.Name != null)
                {
                    b.Index = _sqlitePlatform.SQLiteApi.BindParameterIndex(stmt, b.Name);
                }
                else
                {
                    b.Index = nextIdx++;
                }

                BindParameter(_sqlitePlatform.SQLiteApi, stmt, b.Index, b.Value, _conn.StoreDateTimeAsTicks, _conn.Serializer);
            }
        }

        internal static void BindParameter(ISQLiteApi isqLite3Api, IDbStatement stmt, int index, object value, bool storeDateTimeAsTicks,
            IBlobSerializer serializer)
        {
            if (value == null)
            {
                isqLite3Api.BindNull(stmt, index);
            }
            else
            {
                if (value is int)
                {
                    isqLite3Api.BindInt(stmt, index, (int) value);
                }
                else if (value is ISerializable<int>)
                {
                    isqLite3Api.BindInt(stmt, index, ((ISerializable<int>) value).Serialize());
                }
                else if (value is string)
                {
                    isqLite3Api.BindText16(stmt, index, (string) value, -1, NegativePointer);
                }
                else if (value is ISerializable<string>)
                {
                    isqLite3Api.BindText16(stmt, index, ((ISerializable<string>) value).Serialize(), -1, NegativePointer);
                }
                else if (value is byte || value is ushort || value is sbyte || value is short)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is ISerializable<byte>)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(((ISerializable<byte>) value).Serialize()));
                }
                else if (value is ISerializable<ushort>)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(((ISerializable<ushort>) value).Serialize()));
                }
                else if (value is ISerializable<sbyte>)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(((ISerializable<sbyte>) value).Serialize()));
                }
                else if (value is ISerializable<short>)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(((ISerializable<short>) value).Serialize()));
                }
                else if (value is bool)
                {
                    isqLite3Api.BindInt(stmt, index, (bool) value ? 1 : 0);
                }
                else if (value is ISerializable<bool>)
                {
                    isqLite3Api.BindInt(stmt, index, ((ISerializable<bool>) value).Serialize() ? 1 : 0);
                }
                else if (value is uint || value is long)
                {
                    isqLite3Api.BindInt64(stmt, index, Convert.ToInt64(value));
                }
                else if (value is ISerializable<uint>)
                {
                    isqLite3Api.BindInt64(stmt, index, Convert.ToInt64(((ISerializable<uint>) value).Serialize()));
                }
                else if (value is ISerializable<long>)
                {
                    isqLite3Api.BindInt64(stmt, index, Convert.ToInt64(((ISerializable<long>) value).Serialize()));
                }
                else if (value is float || value is double || value is decimal)
                {
                    isqLite3Api.BindDouble(stmt, index, Convert.ToDouble(value));
                }
                else if (value is ISerializable<float>)
                {
                    isqLite3Api.BindDouble(stmt, index, Convert.ToDouble(((ISerializable<float>) value).Serialize()));
                }
                else if (value is ISerializable<double>)
                {
                    isqLite3Api.BindDouble(stmt, index, Convert.ToDouble(((ISerializable<double>) value).Serialize()));
                }
                else if (value is ISerializable<decimal>)
                {
                    isqLite3Api.BindDouble(stmt, index, Convert.ToDouble(((ISerializable<decimal>) value).Serialize()));
                }
                else if (value is TimeSpan)
                {
                    isqLite3Api.BindInt64(stmt, index, ((TimeSpan) value).Ticks);
                }
                else if (value is ISerializable<TimeSpan>)
                {
                    isqLite3Api.BindInt64(stmt, index, ((ISerializable<TimeSpan>) value).Serialize().Ticks);
                }
                else if (value is DateTime)
                {
                    if (storeDateTimeAsTicks)
                    {
                        long ticks = ((DateTime) value).ToUniversalTime().Ticks;
                        isqLite3Api.BindInt64(stmt, index, ticks);
                    }
                    else
                    {
                        string val = ((DateTime) value).ToUniversalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);
                        isqLite3Api.BindText16(stmt, index, val, -1, NegativePointer);
                    }
                }
                else if (value is DateTimeOffset)
                {
                    isqLite3Api.BindInt64(stmt, index, ((DateTimeOffset) value).UtcTicks);
                }
                else if (value is ISerializable<DateTime>)
                {
                    if (storeDateTimeAsTicks)
                    {
                        long ticks = ((ISerializable<DateTime>) value).Serialize().ToUniversalTime().Ticks;
                        isqLite3Api.BindInt64(stmt, index, ticks);
                    }
                    else
                    {
                        string val = ((ISerializable<DateTime>) value).Serialize().ToUniversalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);
                        isqLite3Api.BindText16(stmt, index, val, -1, NegativePointer);
                    }
                }
                else if (value.GetType().GetTypeInfo().IsEnum)
                {
                    isqLite3Api.BindInt(stmt, index, Convert.ToInt32(value));
                }
                else if (value is byte[])
                {
                    isqLite3Api.BindBlob(stmt, index, (byte[]) value, ((byte[]) value).Length, NegativePointer);
                }
                else if (value is ISerializable<byte[]>)
                {
                    isqLite3Api.BindBlob(stmt, index, ((ISerializable<byte[]>) value).Serialize(), ((ISerializable<byte[]>) value).Serialize().Length,
                        NegativePointer);
                }
                else if (value is Guid)
                {
                    isqLite3Api.BindText16(stmt, index, ((Guid) value).ToString(), 72, NegativePointer);
                }
                else if (value is ISerializable<Guid>)
                {
                    isqLite3Api.BindText16(stmt, index, ((ISerializable<Guid>) value).Serialize().ToString(), 72, NegativePointer);
                }
                else if (serializer != null && serializer.CanDeserialize(value.GetType()))
                {
                    var bytes = serializer.Serialize(value);
                    isqLite3Api.BindBlob(stmt, index, bytes, bytes.Length, NegativePointer);
                }
                else
                {
                    throw new NotSupportedException("Cannot store type: " + value.GetType());
                }
            }
        }

        [CanBeNull]
        private object ReadCol(IDbStatement stmt, int index, ColType type, Type clrType)
        {
            var interfaces = clrType.GetTypeInfo().ImplementedInterfaces.ToList();

            if (type == ColType.Null)
            {
                return null;
            }
            if (clrType == typeof (string))
            {
                return _sqlitePlatform.SQLiteApi.ColumnText16(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<string>)))
            {
                var value = _sqlitePlatform.SQLiteApi.ColumnText16(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (int))
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<int>)))
            {
                var value = _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (bool))
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index) == 1;
            }
            if (interfaces.Contains(typeof (ISerializable<bool>)))
            {
                var value = _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index) == 1;
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (double))
            {
                return _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<double>)))
            {
                var value = _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (float))
            {
                return (float) _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<float>)))
            {
                var value = (float) _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (TimeSpan))
            {
                return new TimeSpan(_sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index));
            }
            if (interfaces.Contains(typeof (ISerializable<TimeSpan>)))
            {
                var value = new TimeSpan(_sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index));
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (DateTime))
            {
                if (_conn.StoreDateTimeAsTicks)
                {
                    return new DateTime(_sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index), DateTimeKind.Utc);
                }
                return DateTime.Parse(_sqlitePlatform.SQLiteApi.ColumnText16(stmt, index), CultureInfo.InvariantCulture);
            }
            if (clrType == typeof (DateTimeOffset))
            {
                return new DateTimeOffset(_sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index), TimeSpan.Zero);
            }
            if (interfaces.Contains(typeof (ISerializable<DateTime>)))
            {
                DateTime value;
                if (_conn.StoreDateTimeAsTicks)
                {
                    value = new DateTime(_sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index), DateTimeKind.Utc);
                }
                else
                {
                    value = DateTime.Parse(_sqlitePlatform.SQLiteApi.ColumnText16(stmt, index), CultureInfo.InvariantCulture);
                }
                return Activator.CreateInstance(clrType, value);
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (clrType == typeof (long))
            {
                return _sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<long>)))
            {
                var value = _sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (uint))
            {
                return (uint) _sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<long>)))
            {
                var value = (uint) _sqlitePlatform.SQLiteApi.ColumnInt64(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (decimal))
            {
                return (decimal) _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<decimal>)))
            {
                var value = (decimal) _sqlitePlatform.SQLiteApi.ColumnDouble(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (byte))
            {
                return (byte) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<byte>)))
            {
                var value = (byte) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (ushort))
            {
                return (ushort) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<ushort>)))
            {
                var value = (ushort) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (short))
            {
                return (short) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<short>)))
            {
                var value = (short) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (sbyte))
            {
                return (sbyte) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<sbyte>)))
            {
                var value = (sbyte) _sqlitePlatform.SQLiteApi.ColumnInt(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (byte[]))
            {
                return _sqlitePlatform.SQLiteApi.ColumnByteArray(stmt, index);
            }
            if (interfaces.Contains(typeof (ISerializable<byte[]>)))
            {
                var value = _sqlitePlatform.SQLiteApi.ColumnByteArray(stmt, index);
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (clrType == typeof (Guid))
            {
                return new Guid(_sqlitePlatform.SQLiteApi.ColumnText16(stmt, index));
            }
            if (interfaces.Contains(typeof (ISerializable<Guid>)))
            {
                var value = new Guid(_sqlitePlatform.SQLiteApi.ColumnText16(stmt, index));
                return _conn.Resolver.CreateObject(clrType, new object[] {value});
            }
            if (_conn.Serializer != null && _conn.Serializer.CanDeserialize(clrType))
            {
                var bytes = _sqlitePlatform.SQLiteApi.ColumnByteArray(stmt, index);
                return _conn.Serializer.Deserialize(bytes, clrType);
            }
            throw new NotSupportedException("Don't know how to read " + clrType);
        }

        private class Binding
        {
            [CanBeNull]
            public string Name { get; set; }

            [CanBeNull]
            public object Value { get; set; }

            public int Index { get; set; }
        }
    }
}