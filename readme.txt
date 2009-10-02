
sqlite-net is an open source, minimal library to allow .NET and Mono applications to store data in [http://www.sqlite.org SQLite 3 databases]. It is written in C# 3.0 and is meant to be simply compiled in with your projects. It was first designed to work with [http://monotouch.net/ MonoTouch] on the iPhone, but should work in any other CLI environment.

sqlite-net was designed as a quick and convenient database layer. Its design follows from these *goals*:

  * It should be very easy to integrate with existing projects and with MonoTouch projects.
  
  * It is a thin wrapper over SQLite and should be fast and efficient. (The library should not be the performance bottleneck of your queries.)
  
  * It provides very simple methods for executing queries safely (using parameters) and for retrieving the results of those query in a strongly typed fashion.
  
  * It works with your data model without forcing you to change your classes. (Contains a small reflection-driven ORM layer.)
  
  * It has 0 dependencies aside from a [http://www.sqlite.org/download.html compiled form of the sqlite3 library].

*Non-goals* include:

  * No `IQueryable` support for constructing queries. You can of course use LINQ on the results of queries, but you cannot use it to construct the queries.
  
  * Not an implementation of `IDbConnection` and its family. This is not a full SQLite driver. If you need that, go get [http://sqlite.phxsoftware.com/ System.Data.SQLite] or [http://code.google.com/p/csharp-sqlite/ csharp-sqlite].

The design is similar to that used by Demis Bellot in the [http://code.google.com/p/servicestack/source/browse/#svn/trunk/Common/ServiceStack.Common/ServiceStack.OrmLite OrmLite sub project of ServiceStack].

----

The library contains simple attributes that you can use to control the construction of tables. In a simple stock program, you might use:

{{{
public class Stock
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	[MaxLength(8)]
	public string Symbol { get; set; }
}

public class Valuation
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }
	[Indexed]
	public int StockId { get; set; }
	public DateTime Time { get; set; }
	public decimal Price { get; set; }
}
}}}

With these, you can automatically generate tables in your database by calling `CreateTable`:

{{{
var db = new SQLiteConnection("foofoo");
db.CreateTable<Stock>();
db.CreateTable<Valuation>();
}}}

You can insert rows in the database using `Insert`. If the table contains an auto-incremented primary key, then the value for that key will be available to you after the insert:

{{{
public static void AddStock(SQLiteConnection db, string symbol) {
	var s = db.Insert(new Stock() {
		Symbol = symbol
	});
	Console.WriteLine("{0} == {1}", s.Symbol, s.Id);
}
}}}

You can query the database using the `Query` method of `SQLiteConnection`:

{{{
public static IEnumerable<Valuation> QueryValuations (SQLiteConnection db, Stock stock)
{
	return db.Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
}
}}}

The generic parameter to the `Query` method specifies the type of object to create for each row. It can be one of your table classes, or anyother class whose public properties match the column returned by the query. For instance, we could rewrite the above query as:

{{{
public class Val {
	public decimal Money { get; set; }
	public DateTime Date { get; set; }
}
public static IEnumerable<Val> QueryVals (SQLiteConnection db, Stock stock)
{
	return db.Query<Val> ("select 'Price' as 'Money', 'Time' as 'Date' from Valuation where StockId = ?", stock.Id);
}
}}}

Updates must be performed manually using the `Execute` method of `SQLiteConnection`.

----

This is an open source project that welcomes contributions/suggestions/bug reports from those who use it. If you have any ideas on how to improve the library, please contact [mailto:fak@praeclarum.org Frank Krueger].

 
