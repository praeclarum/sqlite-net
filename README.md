
# sqlite-net

sqlite-net is an open source, minimal library to allow .NET and Mono applications to store data in
[SQLite 3 databases](http://www.sqlite.org). It was first designed to work with [Xamarin.iOS](http://xamarin.com),
but has since grown up to work on all the platforms (Xamarin.*, .NET, UWP, Azure, etc.).

sqlite-net was designed as a quick and convenient database layer. Its design follows from these *goals*:

* Very easy to integrate with existing projects and runs on all the .NET platforms.
  
* Thin wrapper over SQLite that is fast and efficient. (This library should not be the performance bottleneck of your queries.)
  
* Very simple methods for executing CRUD operations and queries safely (using parameters) and for retrieving the results of those query in a strongly typed fashion.
  
* Works with your data model without forcing you to change your classes. (Contains a small reflection-driven ORM layer.)
  
* 0 dependencies aside from a [compiled form of the sqlite2 library](http://www.sqlite.org/download.html).

## Installation

Install [SQLite-net PCL](https://www.nuget.org/packages/sqlite-net-pcl) from nuget.

## Please Contribute!

This is an open source project that welcomes contributions/suggestions/bug reports from those who use it. If you have any ideas on how to improve the library, please [post an issue here on github](https://github.com/praeclarum/sqlite-net/issues). Please check out the [How to Contribute](https://github.com/praeclarum/sqlite-net/wiki/How-to-Contribute).


# Example Time!

Please consult the Wiki for, ahem, [complete documentation](https://github.com/praeclarum/sqlite-net/wiki).

The library contains simple attributes that you can use to control the construction of tables. In a simple stock program, you might use:

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

Once you've defined the objects in your model you have a choice of APIs. You can use the "synchronous API" where calls
block one at a time, or you can use the "asynchronous API" where calls do not block. You may care to use the asynchronous
API for mobile applications in order to increase reponsiveness.

Both APIs are explained in the two sections below.

## Synchronous API

Once you have defined your entity, you can automatically generate tables in your database by calling `CreateTable`:

    var db = new SQLiteConnection("foofoo");
    db.CreateTable<Stock>();
    db.CreateTable<Valuation>();

You can insert rows in the database using `Insert`. If the table contains an auto-incremented primary key, then the value for that key will be available to you after the insert:

    public static void AddStock(SQLiteConnection db, string symbol) {
    	var s = db.Insert(new Stock() {
    		Symbol = symbol
    	});
    	Console.WriteLine("{0} == {1}", s.Symbol, s.Id);
    }

Similar methods exist for `Update` and `Delete`.

The most straightforward way to query for data is using the `Table` method. This can take predicates for constraining via WHERE clauses and/or adding ORDER BY clauses:

		var conn = new SQLiteConnection("foofoo");
		var query = conn.Table<Stock>().Where(v => v.Symbol.StartsWith("A"));

		foreach (var stock in query)
			Debug.WriteLine("Stock: " + stock.Symbol);

You can also query the database at a low-level using the `Query` method:

    public static IEnumerable<Valuation> QueryValuations (SQLiteConnection db, Stock stock)
    {
    	return db.Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
    }

The generic parameter to the `Query` method specifies the type of object to create for each row. It can be one of your table classes, or any other class whose public properties match the column returned by the query. For instance, we could rewrite the above query as:

    public class Val {
    	public decimal Money { get; set; }
    	public DateTime Date { get; set; }
    }
    public static IEnumerable<Val> QueryVals (SQLiteConnection db, Stock stock)
    {
    	return db.Query<Val> ("select 'Price' as 'Money', 'Time' as 'Date' from Valuation where StockId = ?", stock.Id);
    }

You can perform low-level updates of the database using the `Execute` method.

## Asynchronous API

The asynchronous library uses the Task Parallel Library (TPL). As such, normal use of `Task` objects, and the `async` and `await` keywords 
will work for you.

Once you have defined your entity, you can automatically generate tables by calling `CreateTableAsync`:

	var conn = new SQLiteAsyncConnection("foofoo");
	conn.CreateTableAsync<Stock>().ContinueWith((results) =>
	{
		Debug.WriteLine("Table created!");
	});

You can insert rows in the database using `Insert`. If the table contains an auto-incremented primary key, then the value for that key will be available to you after the insert:

		Stock stock = new Stock()
		{
			Symbol = "AAPL"
		};

		var conn = new SQLiteAsyncConnection("foofoo");
		conn.InsertAsync(stock).ContinueWith((t) =>
		{
			Debug.WriteLine("New customer ID: {0}", stock.Id);
		});

Similar methods exist for `UpdateAsync` and `DeleteAsync`.

Querying for data is most straightforwardly done using the `Table` method. This will return an `AsyncTableQuery` instance back, whereupon
you can add predictates for constraining via WHERE clauses and/or adding ORDER BY. The database is not physically touched until one of the special 
retrieval methods - `ToListAsync`, `FirstAsync`, or `FirstOrDefaultAsync` - is called.

		var conn = new SQLiteAsyncConnection("foofoo");
		var query = conn.Table<Stock>().Where(v => v.Symbol.StartsWith("A"));
			
		query.ToListAsync().ContinueWith((t) =>
		{
			foreach (var stock in t.Result)
				Debug.WriteLine("Stock: " + stock.Symbol);
		});

There are a number of low-level methods available. You can also query the database directly via the `QueryAsync` method. Over and above the change 
operations provided by `InsertAsync` etc you can issue `ExecuteAsync` methods to change sets of data directly within the database.

Another helpful method is `ExecuteScalarAsync`. This allows you to return a scalar value from the database easily:

		var conn = new SQLiteAsyncConnection("foofoo");
		conn.ExecuteScalarAsync<int>("select count(*) from Stock").ContinueWith((t) =>
		{
			Debug.WriteLine(string.Format("Found '{0}' stock items.", t.Result));
		});

## Thank you!

Thank you to the .NET community for embracing this project, and thank you to all the contributors who have helped to make this great.


