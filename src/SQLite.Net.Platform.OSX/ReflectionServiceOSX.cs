using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.OSX
{
    public class ReflectionServiceOSX : IReflectionService
    {
        public IEnumerable<PropertyInfo> GetPublicInstanceProperties(Type mappedType)
        {
            return mappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
        }

        public object GetMemberValue(object obj, Expression expr, MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                var m = (PropertyInfo) member;
                return m.GetValue(obj, null);
            }
            if (member.MemberType == MemberTypes.Field)
            {
                var m = (FieldInfo) member;
                return m.GetValue(obj);
            }
            throw new NotSupportedException("MemberExpr: " + member.MemberType);
        }
    }
}
