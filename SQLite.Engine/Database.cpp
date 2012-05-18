// This file is part of SQLiteMetro
// For conditions of distribution and use, see copyright notice in license.txt

#include "pch.h"
#include "Database.h"
#include "Statement.h"


namespace SQLiteMetro
{


//////////////////////////////////////////////////////////////////////////
Database::Database(Platform::String^ dbPath)
{
	int ret = sqlite3_open16(dbPath->Data(), &m_SQLite);
	IsReady = (ret == SQLITE_OK);
}

//////////////////////////////////////////////////////////////////////////
Database::~Database()
{
	if (m_SQLite) sqlite3_close(m_SQLite);
}

//////////////////////////////////////////////////////////////////////////
Statement^ Database::PrepareStatement(Platform::String^ statement)
{
	return ref new Statement(this, statement);
}


} // namespace SQLiteMetro
