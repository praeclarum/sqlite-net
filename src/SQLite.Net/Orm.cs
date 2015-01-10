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
    internal static class Orm
    {
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        internal static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks, IBlobSerializer serializer,
                                     IDictionary<Type, string> extraTypeMappings)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks, serializer, extraTypeMappings) + " ";

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
            Type clrType = p.ColumnType;
            var interfaces = clrType.GetTypeInfo().ImplementedInterfaces.ToList();

            string extraMapping;
            if (extraTypeMappings.TryGetValue(clrType, out extraMapping))
            {
                return extraMapping;
            }

            if (clrType == typeof(Boolean) || clrType == typeof(Byte) || clrType == typeof(UInt16) ||
                clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32) ||
                interfaces.Contains(typeof(ISerializable<Boolean>)) ||
                interfaces.Contains(typeof(ISerializable<Byte>)) ||
                interfaces.Contains(typeof(ISerializable<UInt16>)) ||
                interfaces.Contains(typeof(ISerializable<SByte>)) ||
                interfaces.Contains(typeof(ISerializable<Int16>)) ||
                interfaces.Contains(typeof(ISerializable<Int32>)))
            {
                return "integer";
            }
            if (clrType == typeof(UInt32) || clrType == typeof(Int64) ||
                interfaces.Contains(typeof(ISerializable<UInt32>)) ||
                interfaces.Contains(typeof(ISerializable<Int64>)) ||
                interfaces.Contains(typeof(ISerializable<UInt64>)))
            {
                return "bigint";
            }
            if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal) ||
                interfaces.Contains(typeof(ISerializable<Single>)) ||
                interfaces.Contains(typeof(ISerializable<Double>)) ||
                interfaces.Contains(typeof(ISerializable<Decimal>)))
            {
                return "float";
            }
            if (clrType == typeof(String) || interfaces.Contains(typeof(ISerializable<String>)))
            {
                int? len = p.MaxStringLength;

                if (len.HasValue)
                    return "varchar(" + len.Value + ")";

                return "varchar";
            }
            if (clrType == typeof(TimeSpan) || interfaces.Contains(typeof(ISerializable<TimeSpan>)))
            {
                return "bigint";
            }
            if (clrType == typeof(DateTime) || interfaces.Contains(typeof(ISerializable<DateTime>)))
            {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
            }
            if (clrType == typeof(DateTimeOffset))
            {
                return "bigint";
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof(byte[]) || interfaces.Contains(typeof(ISerializable<byte[]>)))
            {
                return "blob";
            }
            if (clrType == typeof(Guid) || interfaces.Contains(typeof(ISerializable<Guid>)))
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
            return p.GetCustomAttributes<PrimaryKeyAttribute>().Any();
        }

        internal static string Collation(MemberInfo p)
        {
            foreach (var attribute in p.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(CollationAttribute)))
            {
                return (string)attribute.ConstructorArguments[0].Value;
            }
            return string.Empty;
        }

        internal static bool IsAutoInc(MemberInfo p)
        {
            return p.GetCustomAttributes<AutoIncrementAttribute>().Any();
        }

        internal static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            return p.GetCustomAttributes<IndexedAttribute>();
        }

        internal static int? MaxStringLength(PropertyInfo p)
        {
            foreach (var attribute in p.CustomAttributes.Where(a=>a.AttributeType==typeof(MaxLengthAttribute)))
            {
                return (int) attribute.ConstructorArguments[0].Value;
            }
            return null;
        }

        internal static object GetDefaultValue(PropertyInfo p)
        {
            foreach (var attribute in p.CustomAttributes.Where(a => a.AttributeType == typeof (DefaultAttribute)))
            {
                try
                {
                    var useProp = (bool) attribute.ConstructorArguments[0].Value;

                    if (!useProp)
                        return Convert.ChangeType(attribute.ConstructorArguments[0].Value, p.PropertyType);

                    var obj = Activator.CreateInstance(p.DeclaringType);
                    return p.GetValue(obj);

                }
                catch (Exception exception)
                {
                    throw new Exception("Unable to convert " + attribute.ConstructorArguments[0].Value + " to type " + p.PropertyType, exception);
                }
            }
            return null;
        }

        internal static bool IsMarkedNotNull(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes<NotNullAttribute>(true);
            return attrs.Any();
        }

    }
}