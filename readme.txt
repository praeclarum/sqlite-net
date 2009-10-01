sqlite-net

sqlite-net is a minimal library to allow .NET applications to store data in [http://www.sqlite.org SQLite 3 databases]. It is written in C# 3.0 but can be used by any .NET language. It was first designed to work with [http://monotouch.net/ MonoTouch] on the iPhone, but should work in any other CLI environment.

sqlite-net was designed as a quick and convenient database layer. Its design follows from these *goals*:

* It is a thin wrapper over SQLite and should therefore be fast and efficient. That is, the library should not be the performance bottleneck of your queries.

* It provides very simple methods for executing queries safely (using parameters) and for retrieving the results of those query.

* It works with your data model without forcing you to change your classes. (Contains a small reflection-driven ORM layer.)

* It has 0 dependencies aside from a [http://www.sqlite.org/download.html compiled form of the sqlite3 library].

*Non-goals* include:

* No `IQueryable` support for constructing queries. You can of course use LINQ on the results of queries, but you cannot use it to construct the queries.

* Not an implementation of `IDbConnection` and its family. This is not a full SQLite driver. If you need that, go get [http://sqlite.phxsoftware.com/ System.Data.SQLite] or [http://code.google.com/p/csharp-sqlite/ csharp-sqlite].

The design is similar to that used by Demis Bellot in the [http://code.google.com/p/servicestack/source/browse/#svn/trunk/Common/ServiceStack.Common/ServiceStack.OrmLite OrmLite sub project of ServiceStack].