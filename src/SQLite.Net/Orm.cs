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
using SQLite.Net.Attributes;

namespace SQLite.Net
{
    public static class Orm
    {
        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks) + " ";

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

            return decl;
        }

        public static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            Type clrType = p.ColumnType;
            if (clrType == typeof (Boolean) || clrType == typeof (Byte) || clrType == typeof (UInt16) ||
                clrType == typeof (SByte) || clrType == typeof (Int16) || clrType == typeof (Int32))
            {
                return "integer";
            }
            if (clrType == typeof (UInt32) || clrType == typeof (Int64))
            {
                return "bigint";
            }
            if (clrType == typeof (Single) || clrType == typeof (Double) || clrType == typeof (Decimal))
            {
                return "float";
            }
            if (clrType == typeof (String))
            {
                int len = p.MaxStringLength;
                return "varchar(" + len + ")";
            }
            if (clrType == typeof (DateTime))
            {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
            }
            if (clrType.IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof (byte[]))
            {
                return "blob";
            }
            if (clrType == typeof (Guid))
            {
                return "varchar(36)";
            }
            throw new NotSupportedException("Don't know about " + clrType);
        }

        public static bool IsPK(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof (PrimaryKeyAttribute), true);
            return attrs.Length > 0;
        }

        public static string Collation(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof (CollationAttribute), true);
            if (attrs.Length > 0)
            {
                return ((CollationAttribute) attrs[0]).Value;
            }
            return string.Empty;
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof (AutoIncrementAttribute), true);
            return attrs.Length > 0;
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof (IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int MaxStringLength(PropertyInfo p)
        {
            object[] attrs = p.GetCustomAttributes(typeof (MaxLengthAttribute), true);
            if (attrs.Length > 0)
            {
                return ((MaxLengthAttribute) attrs[0]).Value;
            }
            return DefaultMaxStringLength;
        }
    }
}