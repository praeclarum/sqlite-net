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

        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks, IBlobSerializer serializer)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p, storeDateTimeAsTicks, serializer) + " ";

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

        public static string SqlType(TableMapping.Column p, bool storeDateTimeAsTicks, IBlobSerializer serializer)
        {
            Type clrType = p.ColumnType;
            var interfaces = clrType.GetTypeInfo().ImplementedInterfaces.ToList();

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
                interfaces.Contains(typeof(ISerializable<Int64>)))
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
                int len = p.MaxStringLength;
                return "varchar(" + len + ")";
            }
            if (clrType == typeof(TimeSpan) || interfaces.Contains(typeof(ISerializable<TimeSpan>)))
            {
                return "bigint";
            }
            if (clrType == typeof(DateTime) || interfaces.Contains(typeof(ISerializable<DateTime>)))
            {
                return storeDateTimeAsTicks ? "bigint" : "datetime";
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

        public static bool IsPK(MemberInfo p)
        {
            foreach (var attribute in p.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(PrimaryKeyAttribute))
                    return true;
            }
            return false;
        }

        public static string Collation(MemberInfo p)
        {
            foreach (var attribute in p.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(CollationAttribute))
                    return (string)attribute.ConstructorArguments[0].Value;
            }
            return string.Empty;
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            foreach (var attribute in p.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(AutoIncrementAttribute))
                    return true;
            }
            return false;
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var indexedAttributes = new List<IndexedAttribute>();
            foreach (var attribute in p.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(IndexedAttribute))
                {
                    bool hasNamedArgumentName = attribute.NamedArguments.Any(a => a.MemberName == "Name");
                    bool hasNamedArgumentOrder = attribute.NamedArguments.Any(a => a.MemberName == "Order");
                    bool hasNamedArgumentUnique = attribute.NamedArguments.Any(a => a.MemberName == "Unique");

                    var namedArgumentName = attribute.NamedArguments.FirstOrDefault(a => a.MemberName == "Name");
                    var namedArgumentOrder = attribute.NamedArguments.FirstOrDefault(a => a.MemberName == "Order");
                    var namedArgumentUnique = attribute.NamedArguments.FirstOrDefault(a => a.MemberName == "Unique");

                    var arguments = attribute.ConstructorArguments;
                    bool hasPositionalArgumentName = arguments.Count > 0;
                    bool hasPositionalArgumentOrder = arguments.Count > 1;

                    var name =  hasNamedArgumentName ? (string)namedArgumentName.TypedValue.Value : hasPositionalArgumentName ? (string)arguments[0].Value : null;
                    var order = hasNamedArgumentOrder ? (int)namedArgumentOrder.TypedValue.Value : hasPositionalArgumentOrder ? (int)arguments[1].Value : 0;
                    var unique = hasNamedArgumentUnique ? (bool)namedArgumentUnique.TypedValue.Value : false;
                    indexedAttributes.Add(new IndexedAttribute(name, order) { Unique = unique });
                }
            }
            return indexedAttributes;
        }

        public static int MaxStringLength(PropertyInfo p)
        {
            foreach (var attribute in p.CustomAttributes)
            {
                if (attribute.AttributeType == typeof(MaxLengthAttribute))
                    return (int)attribute.ConstructorArguments[0].Value;
            }
            return DefaultMaxStringLength;
        }
    }
}