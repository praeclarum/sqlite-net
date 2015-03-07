// Author: Prasanna V. Loganathar
// Created: 2:37 PM 07-03-2015
// Project: SQLite.Net
// License: See project license

using System;
using System.Linq;

namespace SQLite.Net
{
    internal class ActiveInsertCommand
    {
        private readonly TableMapping _tableMapping;
        private PreparedSqlLiteInsertCommand _insertCommand;
        private string _insertCommandExtra;

        public ActiveInsertCommand(TableMapping tableMapping)
        {
            _tableMapping = tableMapping;
        }

        public PreparedSqlLiteInsertCommand GetCommand(SQLiteConnection conn, string extra)
        {
            if (_insertCommand == null)
            {
                _insertCommand = CreateCommand(conn, extra);
                _insertCommandExtra = extra;
            }
            else if (_insertCommandExtra != extra)
            {
                _insertCommand.Dispose();
                _insertCommand = CreateCommand(conn, extra);
                _insertCommandExtra = extra;
            }
            return _insertCommand;
        }

        protected internal void Dispose()
        {
            if (_insertCommand != null)
            {
                _insertCommand.Dispose();
                _insertCommand = null;
            }
        }

        private PreparedSqlLiteInsertCommand CreateCommand(SQLiteConnection conn, string extra)
        {
            var cols = _tableMapping.InsertColumns;
            string insertSql;
            if (!cols.Any() && _tableMapping.Columns.Count() == 1 && _tableMapping.Columns[0].IsAutoInc)
            {
                insertSql = string.Format("insert {1} into \"{0}\" default values", _tableMapping.TableName, extra);
            }
            else
            {
                var replacing = string.Compare(extra, "OR REPLACE", StringComparison.OrdinalIgnoreCase) == 0;

                if (replacing)
                {
                    cols = _tableMapping.Columns;
                }

                insertSql = string.Format("insert {3} into \"{0}\"({1}) values ({2})", _tableMapping.TableName,
                    string.Join(",", (from c in cols
                        select "\"" + c.Name + "\"").ToArray()),
                    string.Join(",", (from c in cols
                        select "?").ToArray()), extra);
            }
            
            return new PreparedSqlLiteInsertCommand(conn)
            {
                CommandText = insertSql
            };
        }
    }
}
