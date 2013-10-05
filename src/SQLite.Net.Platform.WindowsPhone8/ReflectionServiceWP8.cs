using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Net.Interop;

namespace SQLite.Net.Platform.WindowsPhone8
{
    public class ReflectionServiceWP8 : IReflectionService
    {
        public IEnumerable<PropertyInfo> GetPublicInstanceProperties(Type mappedType)
        {
            if (mappedType == null)
            {
                throw new ArgumentNullException("mappedType");
            }
            return from p in mappedType.GetRuntimeProperties()
                where
                    ((p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic) ||
                     (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic))
                select p;
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
                return Expression.Lambda(expr).Compile().DynamicInvoke();
            }
            throw new NotSupportedException("MemberExpr: " + member.MemberType);
        }
    }
}