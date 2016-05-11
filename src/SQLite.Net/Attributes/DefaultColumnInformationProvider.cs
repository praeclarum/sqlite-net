using System;
using System.Linq;
using System.Reflection;
using SQLite.Net.Attributes;
using System.Collections.Generic;

namespace SQLite.Net
{
	public class DefaultColumnInformationProvider : IColumnInformationProvider
	{
		#region IColumnInformationProvider implementation

		public string GetColumnName(PropertyInfo p)
		{
			var colAttr = p.GetCustomAttributes<ColumnAttribute>(true).FirstOrDefault();
			return colAttr == null ? p.Name : colAttr.Name;
		}

		public bool IsIgnored(PropertyInfo p)
		{
			return p.IsDefined(typeof (IgnoreAttribute), true);
		}

		public IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
		{
			return p.GetCustomAttributes<IndexedAttribute>();
		}

		public bool IsPK(MemberInfo m)
		{
			return m.GetCustomAttributes<PrimaryKeyAttribute>().Any();
		}

		public string Collation(MemberInfo m)
		{
			foreach (var attribute in m.GetCustomAttributes<CollationAttribute>())
			{
				return attribute.Value;
			}
			return string.Empty;
		}

		public bool IsAutoInc(MemberInfo m)
		{
			return m.GetCustomAttributes<AutoIncrementAttribute>().Any();
		}

		public int? MaxStringLength(PropertyInfo p)
		{
			foreach (var attribute in p.GetCustomAttributes<MaxLengthAttribute>())
			{
				return attribute.Value;
			}
			return null;
		}

		public object GetDefaultValue(PropertyInfo p)
		{
			foreach (var attribute in p.GetCustomAttributes<DefaultAttribute>())
			{
				try
				{
					if (!attribute.UseProperty)
					{
						return Convert.ChangeType(attribute.Value, p.PropertyType);
					}

					var obj = Activator.CreateInstance(p.DeclaringType);
					return p.GetValue(obj);
				}
				catch (Exception exception)
				{
					throw new Exception("Unable to convert " + attribute.Value + " to type " + p.PropertyType, exception);
				}
			}
			return null;
		}

		public bool IsMarkedNotNull(MemberInfo p)
		{
			var attrs = p.GetCustomAttributes<NotNullAttribute>(true);
			return attrs.Any();
		}

		#endregion
	}
}

