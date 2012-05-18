// This file is part of SQLiteMetro
// For conditions of distribution and use, see copyright notice in license.txt

#pragma once

#include "sqlite3.h"

namespace SQLiteMetro
{
	ref class Database;

	public ref class Statement sealed
	{
	public:
		Statement(Database^ database, Platform::String^ statement);
		~Statement();

		property bool IsValid;

		bool Execute();
		bool GetNextRow();

		Platform::String^ GetTextAt(int index);
		int GetIntAt(int index);
		double GetDoubleAt(int index);
		Platform::Array<uint8>^ GetBlobAt(int index);

		bool BindParamText(int index, Platform::String^ val);
		bool BindParamInt(int index, int val);
		bool BindParamDouble(int index, double val);
		bool BindParamBlob(int index, Platform::Array<uint8>^ val);

	private:
		sqlite3_stmt* m_Statement;
	};
}

