using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Expr = System.Linq.Expressions.Expression;

namespace QueryCompiler
{
	public class Table<T> : IQueryable<T>
	{

		#region IEnumerable<T> implementation
		public IEnumerator<T> GetEnumerator ()
		{
			throw new System.NotImplementedException();
		}

		#endregion

		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			throw new System.NotImplementedException();
		}

		#endregion

		#region IQueryable implementation
		public Type ElementType {
			get {
				return typeof(T);
			}
		}
		
		public System.Linq.Expressions.Expression Expression {
			get {
				return Expr.Constant(this);
			}
		}
		
		public IQueryProvider Provider {
			get {
				return new TableProvider();
			}
		}
		#endregion
	}
	
	public class TableProvider : IQueryProvider {

		#region IQueryProvider implementation
		public IQueryable CreateQuery (System.Linq.Expressions.Expression expression)
		{
			Console.WriteLine(expression);
			throw new System.NotImplementedException();
		}
		
		public object Execute (System.Linq.Expressions.Expression expression)
		{
			throw new System.NotImplementedException();
		}
		
		public IQueryable<TElement> CreateQuery<TElement> (System.Linq.Expressions.Expression expression)
			
		{
			throw new System.NotImplementedException();
		}
		
		public TResult Execute<TResult> (System.Linq.Expressions.Expression expression)
			
		{
			throw new System.NotImplementedException();
		}
		#endregion
		
	}

	public class Database
	{
		public IQueryable<T> All<T>() {
			return new Table<T>();
		}
	}
	
	public class TestA {
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime CreatedTime { get; set; }
	}
	
	public static class Tests {
		
		public static void TestDateTime() {
			var db = new Database();
			
			var lastTime = DateTime.UtcNow - TimeSpan.FromDays(30);
			var lates = from a in db.All<TestA>()
				        where a.CreatedTime > lastTime
					    select a;
			Output(lates.ToArray());
		}
		
		public static void TestSelectAnon() {
			var db = new Database();
			
			var lates = from a in db.All<TestA>()
					    select new { a.Id, a.Name };
			Output(lates.ToArray());
		}
		
		static void Output(object o) {
			Console.WriteLine(o);
		}
		
	}
}
