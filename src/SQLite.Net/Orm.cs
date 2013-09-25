//
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
//

#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SQLite
{
    public static class Orm
    {
        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        public static string SqlDecl (TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            string decl = "\"" + p.Name + "\" " + SqlType (p, storeDateTimeAsTicks) + " ";
			
            if (p.IsPK) {
                decl += "primary key ";
            }
            if (p.IsAutoInc) {
                decl += "autoincrement ";
            }
            if (!p.IsNullable) {
                decl += "not null ";
            }
            if (!string.IsNullOrEmpty (p.Collation)) {
                decl += "collate " + p.Collation + " ";
            }
			
            return decl;
        }

        public static string SqlType (TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            var clrType = p.ColumnType;
            if (clrType == typeof(Boolean) || clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32)) {
                return "integer";
            } else if (clrType == typeof(UInt32) || clrType == typeof(Int64)) {
                return "bigint";
            } else if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal)) {
                return "float";
            } else if (clrType == typeof(String)) {
                int len = p.MaxStringLength;
                return "varchar(" + len + ")";
            } else if (clrType == typeof(DateTime)) {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
#if !NETFX_CORE
            } else if (clrType.IsEnum) {
#else
			} else if (clrType.GetTypeInfo().IsEnum) {
#endif
                return "integer";
            } else if (clrType == typeof(byte[])) {
                return "blob";
            } else if (clrType == typeof(Guid)) {
                return "varchar(36)";
            } else {
                throw new NotSupportedException ("Don't know about " + clrType);
            }
        }

        public static bool IsPK (MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(PrimaryKeyAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
			return attrs.Count() > 0;
#endif
        }

        public static string Collation (MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(CollationAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0) {
                return ((CollationAttribute)attrs [0]).Value;
#else
			if (attrs.Count() > 0) {
                return ((CollationAttribute)attrs.First()).Value;
#endif
            } else {
                return string.Empty;
            }
        }

        public static bool IsAutoInc (MemberInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(AutoIncrementAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
			return attrs.Count() > 0;
#endif
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }
		
        public static int MaxStringLength(PropertyInfo p)
        {
            var attrs = p.GetCustomAttributes (typeof(MaxLengthAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0) {
                return ((MaxLengthAttribute)attrs [0]).Value;
#else
			if (attrs.Count() > 0) {
				return ((MaxLengthAttribute)attrs.First()).Value;
#endif
            } else {
                return DefaultMaxStringLength;
            }
        }
    }
}