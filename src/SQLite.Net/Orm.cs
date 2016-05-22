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
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SQLite.Net.Attributes;
using NotNullAttribute = SQLite.Net.Attributes.NotNullAttribute;

namespace SQLite.Net
{
    internal static class Orm
    {
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

		private static IColumnInformationProvider _columnInformationProvider = new DefaultColumnInformationProvider();
		public static IColumnInformationProvider ColumnInformationProvider 
		{
			get { return _columnInformationProvider; }
			set { _columnInformationProvider = value; }
		}

		internal static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks, IBlobSerializer serializer,
			IDictionary<Type, string> extraTypeMappings)
        {
            var decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks, serializer, extraTypeMappings) + " ";

            if (p.IsPK)
            {
                decl += "primary key ";
            }
            if (p.IsAutoInc)
            {
                decl += "autoincrement ";
            }
            if (!p.IsNullable)
            {
                decl += "not null ";
            }
            if (!string.IsNullOrEmpty(p.Collation))
            {
                decl += "collate " + p.Collation + " ";
            }
            if (p.DefaultValue != null)
            {
                decl += "default('" + p.DefaultValue + "') ";
            }

            return decl;
        }

        private static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks,
            IBlobSerializer serializer,
            IDictionary<Type, string> extraTypeMappings)
        {
            var clrType = p.ColumnType;
            var interfaces = clrType.GetTypeInfo().ImplementedInterfaces.ToList();

            string extraMapping;
            if (extraTypeMappings.TryGetValue(clrType, out extraMapping))
            {
                return extraMapping;
            }

            if (clrType == typeof (bool) || clrType == typeof (byte) || clrType == typeof (ushort) ||
                clrType == typeof (sbyte) || clrType == typeof (short) || clrType == typeof (int) ||
                clrType == typeof (uint) || clrType == typeof (long) ||
                interfaces.Contains(typeof (ISerializable<bool>)) ||
                interfaces.Contains(typeof (ISerializable<byte>)) ||
                interfaces.Contains(typeof (ISerializable<ushort>)) ||
                interfaces.Contains(typeof (ISerializable<sbyte>)) ||
                interfaces.Contains(typeof (ISerializable<short>)) ||
                interfaces.Contains(typeof (ISerializable<int>)) ||
                interfaces.Contains(typeof (ISerializable<uint>)) ||
                interfaces.Contains(typeof (ISerializable<long>)) ||
                interfaces.Contains(typeof (ISerializable<ulong>)))
            {
                return "integer";
            }
            if (clrType == typeof (float) || clrType == typeof (double) || clrType == typeof (decimal) ||
                interfaces.Contains(typeof (ISerializable<float>)) ||
                interfaces.Contains(typeof (ISerializable<double>)) ||
                interfaces.Contains(typeof (ISerializable<decimal>)))
            {
                return "float";
            }
            if (clrType == typeof (string) || interfaces.Contains(typeof (ISerializable<string>)))
            {
                var len = p.MaxStringLength;

                if (len.HasValue)
                {
                    return "varchar(" + len.Value + ")";
                }

                return "varchar";
            }
            if (clrType == typeof (TimeSpan) || interfaces.Contains(typeof (ISerializable<TimeSpan>)))
            {
                return "bigint";
            }
            if (clrType == typeof (DateTime) || interfaces.Contains(typeof (ISerializable<DateTime>)))
            {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
            }
            if (clrType == typeof (DateTimeOffset))
            {
                return "bigint";
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof (byte[]) || interfaces.Contains(typeof (ISerializable<byte[]>)))
            {
                return "blob";
            }
            if (clrType == typeof (Guid) || interfaces.Contains(typeof (ISerializable<Guid>)))
            {
                return "varchar(36)";
            }
            if (serializer != null && serializer.CanDeserialize(clrType))
            {
                return "blob";
            }
            throw new NotSupportedException("Don't know about " + clrType);
        }

        internal static bool IsPK(MemberInfo p)
        {
			return ColumnInformationProvider.IsPK (p);
        }

        internal static string Collation(MemberInfo p)
        {
			return ColumnInformationProvider.Collation (p);
        }

        internal static bool IsAutoInc(MemberInfo p)
        {
			return ColumnInformationProvider.IsAutoInc (p);
        }

        internal static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
			return ColumnInformationProvider.GetIndices (p);
        }

        [CanBeNull]
        internal static int? MaxStringLength(PropertyInfo p)
        {
			return ColumnInformationProvider.MaxStringLength (p);
        }

        [CanBeNull]
        internal static object GetDefaultValue(PropertyInfo p)
        {
			return ColumnInformationProvider.GetDefaultValue (p);
        }

        internal static bool IsMarkedNotNull(MemberInfo p)
        {
			return ColumnInformationProvider.IsMarkedNotNull (p);
        }
    }
}