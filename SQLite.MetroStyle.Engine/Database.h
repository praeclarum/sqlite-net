// This file is part of SQLiteMetro
// For conditions of distribution and use, see copyright notice in license.txt

#pragma once

#include "sqlite3.h"

namespace SQLiteMetro
{
	ref class Statement;

	public ref class Database sealed
	{
		friend Statement;
	public:
		Database(Platform::String^ dbPath);
		~Database();

		Statement^ PrepareStatement(Platform::String^ statement);

		property bool IsReady;

		property int64 LastInsertRowId
		{
			int64 get()
			{
				if (!IsReady) return -1;
				else return sqlite3_last_insert_rowid(m_SQLite);
			}
		}

	private:
		sqlite3* m_SQLite;
	};
}

