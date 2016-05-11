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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using SQLite.Net.Attributes;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    public class TableMapping
    {
        private readonly Column _autoPk;
        private Column[] _insertColumns;

        [PublicAPI]
		public TableMapping(Type type, IEnumerable<PropertyInfo> properties, CreateFlags createFlags = CreateFlags.None, IColumnInformationProvider infoProvider = null)
        {
			if (infoProvider == null)
			{
				infoProvider = new DefaultColumnInformationProvider ();
			}

            MappedType = type;

            var tableAttr = type.GetTypeInfo().GetCustomAttributes<TableAttribute>().FirstOrDefault();

            TableName = tableAttr != null ?  tableAttr.Name : MappedType.Name;

            var props = properties;

            var cols = new List<Column>();
            foreach (var p in props)
            {
				var ignore = infoProvider.IsIgnored (p);

                if (p.CanWrite && !ignore)
                {
                    cols.Add(new Column(p, createFlags));
                }
            }
            Columns = cols.ToArray();
            foreach (var c in Columns)
            {
                if (c.IsAutoInc && c.IsPK)
                {
                    _autoPk = c;
                }
                if (c.IsPK)
                {
                    PK = c;
                }
            }

            HasAutoIncPK = _autoPk != null;

            if (PK != null)
            {
                GetByPrimaryKeySql = string.Format("select * from \"{0}\" where \"{1}\" = ?", TableName, PK.Name);
            }
            else
            {
                // People should not be calling Get/Find without a PK
                GetByPrimaryKeySql = string.Format("select * from \"{0}\" limit 1", TableName);
            }
        }

        [PublicAPI]
        public Type MappedType { get; private set; }

        [PublicAPI]
        public string TableName { get; private set; }

        [PublicAPI]
        public Column[] Columns { get; private set; }

        [PublicAPI]
        public Column PK { get; private set; }

        [PublicAPI]
        public string GetByPrimaryKeySql { get; private set; }

        [PublicAPI]
        public bool HasAutoIncPK { get; private set; }

        [PublicAPI]
        public Column[] InsertColumns
        {
            get { return _insertColumns ?? (_insertColumns = Columns.Where(c => !c.IsAutoInc).ToArray()); }
        }

        [PublicAPI]
        public void SetAutoIncPK(object obj, long id)
        {
            if (_autoPk != null)
            {
                _autoPk.SetValue(obj, Convert.ChangeType(id, _autoPk.ColumnType, null));
            }
        }

        [PublicAPI]
        public Column FindColumnWithPropertyName(string propertyName)
        {
            var exact = Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            return exact;
        }

        [PublicAPI]
        public Column FindColumn(string columnName)
        {
            var exact = Columns.FirstOrDefault(c => c.Name == columnName);
            return exact;
        }

        public class Column
        {
            private readonly PropertyInfo _prop;

            [PublicAPI]
			public Column(PropertyInfo prop, CreateFlags createFlags = CreateFlags.None, IColumnInformationProvider infoProvider = null)
            {
				if (infoProvider == null)
				{
					infoProvider = new DefaultColumnInformationProvider();
				}

                _prop = prop;
				Name = infoProvider.GetColumnName(prop);
                //If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
                ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                Collation = Orm.Collation(prop);

                IsPK = Orm.IsPK(prop) ||
                       (((createFlags & CreateFlags.ImplicitPK) == CreateFlags.ImplicitPK) &&
                        string.Compare(prop.Name, Orm.ImplicitPkName, StringComparison.OrdinalIgnoreCase) == 0);

                var isAuto = Orm.IsAutoInc(prop) ||
                             (IsPK && ((createFlags & CreateFlags.AutoIncPK) == CreateFlags.AutoIncPK));
                IsAutoGuid = isAuto && ColumnType == typeof (Guid);
                IsAutoInc = isAuto && !IsAutoGuid;

                DefaultValue = Orm.GetDefaultValue(prop);

                Indices = Orm.GetIndices(prop);
                if (!Indices.Any()
                    && !IsPK
                    && ((createFlags & CreateFlags.ImplicitIndex) == CreateFlags.ImplicitIndex)
                    && Name.EndsWith(Orm.ImplicitIndexSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    Indices = new[] {new IndexedAttribute()};
                }
                IsNullable = !(IsPK || Orm.IsMarkedNotNull(prop));
                MaxStringLength = Orm.MaxStringLength(prop);
            }

            [PublicAPI]
            public string Name { get; private set; }

            [PublicAPI]
            public string PropertyName
            {
                get { return _prop.Name; }
            }

            [PublicAPI]
            public Type ColumnType { get; private set; }

            [PublicAPI]
            public string Collation { get; private set; }

            [PublicAPI]
            public bool IsAutoInc { get; private set; }

            [PublicAPI]
            public bool IsAutoGuid { get; private set; }

            [PublicAPI]
            public bool IsPK { get; private set; }

            [PublicAPI]
            public IEnumerable<IndexedAttribute> Indices { get; set; }

            [PublicAPI]
            public bool IsNullable { get; private set; }

            [PublicAPI]
            public int? MaxStringLength { get; private set; }

            [PublicAPI]
            public object DefaultValue { get; private set; }

            /// <summary>
            ///     Set column value.
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="val"></param>
            [PublicAPI]
            public void SetValue(object obj, [CanBeNull] object val)
            {
                var propType = _prop.PropertyType;
                var typeInfo = propType.GetTypeInfo();

                if (typeInfo.IsGenericType && propType.GetGenericTypeDefinition() == typeof (Nullable<>))
                {
                    var typeCol = propType.GetTypeInfo().GenericTypeArguments;
                    if (typeCol.Length > 0)
                    {
                        var nullableType = typeCol[0];
                        var baseType = nullableType.GetTypeInfo().BaseType;
                        if (baseType == typeof (Enum))
                        {
                            SetEnumValue(obj, nullableType, val);
                        }
                        else
                        {
                            _prop.SetValue(obj, val, null);
                        }
                    }
                }
                else if (typeInfo.BaseType == typeof (Enum))
                {
                    SetEnumValue(obj, propType, val);
                }
                else
                {
                    _prop.SetValue(obj, val, null);
                }
            }

            private void SetEnumValue(object obj, Type type, object value)
            {
                var result = value;
                if (result != null)
                {
                    result = Enum.ToObject(type, result);
                    _prop.SetValue(obj, result, null);
                }
            }

            [PublicAPI]
            public object GetValue(object obj)
            {
                return _prop.GetValue(obj, null);
            }
        }
    }
}