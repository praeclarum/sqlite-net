// This file is part of SQLiteMetro
// For conditions of distribution and use, see copyright notice in license.txt

#include "pch.h"
#include "Statement.h"
#include "Database.h"


namespace SQLiteMetro
{


//////////////////////////////////////////////////////////////////////////
Statement::Statement(Database^ database, Platform::String^ statement)
{
	int ret = sqlite3_prepare16(database->m_SQLite, statement->Data(), -1, &m_Statement, 0);
	IsValid = (ret == SQLITE_OK);
}

//////////////////////////////////////////////////////////////////////////
Statement::~Statement()
{
	if (m_Statement) sqlite3_finalize(m_Statement);
}

//////////////////////////////////////////////////////////////////////////
bool Statement::Execute()
{
	if (!IsValid) return false;

	int ret = sqlite3_step(m_Statement);
	return (ret == SQLITE_DONE);
}

//////////////////////////////////////////////////////////////////////////
bool Statement::GetNextRow()
{
	if (!IsValid) return false;

	int ret = sqlite3_step(m_Statement);
	return (ret == SQLITE_ROW);
}

//////////////////////////////////////////////////////////////////////////
Platform::String^ Statement::GetTextAt(int index)
{
	if (!IsValid) return nullptr;

	return ref new Platform::String(static_cast<const wchar_t*>(sqlite3_column_text16(m_Statement, index)));
}

//////////////////////////////////////////////////////////////////////////
int Statement::GetIntAt(int index)
{
	if (!IsValid) return 0;

	return sqlite3_column_int(m_Statement, index);
}

//////////////////////////////////////////////////////////////////////////
double Statement::GetDoubleAt(int index)
{
	if (!IsValid) return 0.0;

	return sqlite3_column_double(m_Statement, index);
}

//////////////////////////////////////////////////////////////////////////
Platform::Array<uint8>^ Statement::GetBlobAt(int index)
{
	if (!IsValid) return nullptr;

	int numBytes = sqlite3_column_bytes(m_Statement, index);
	if (numBytes > 0)
	{
		uint8* data = (uint8*)sqlite3_column_blob(m_Statement, index);
		return ref new Platform::Array<uint8>(data, numBytes);
	}
	else return nullptr;
}

//////////////////////////////////////////////////////////////////////////
bool Statement::BindParamText(int index, Platform::String^ val)
{
	if (!IsValid) return false;

	int ret = sqlite3_bind_text16(m_Statement, index, val->Data(), -1, SQLITE_STATIC);
	return (ret == SQLITE_OK);
}

//////////////////////////////////////////////////////////////////////////
bool Statement::BindParamInt(int index, int val)
{
	if (!IsValid) return false;

	int ret = sqlite3_bind_int(m_Statement, index, val);
	return (ret == SQLITE_OK);
}

//////////////////////////////////////////////////////////////////////////
bool Statement::BindParamDouble(int index, double val)
{
	if (!IsValid) return false;

	int ret = sqlite3_bind_double(m_Statement, index, val);
	return (ret == SQLITE_OK);
}

//////////////////////////////////////////////////////////////////////////
bool Statement::BindParamBlob(int index, Platform::Array<uint8>^ val)
{
	if (!IsValid) return false;

	int ret = sqlite3_bind_blob(m_Statement, index, val->Data, val->Length, SQLITE_STATIC);
	return (ret == SQLITE_OK);
}


} // namespace SQLiteMetro