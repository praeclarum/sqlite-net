//
// Copyright (c) 2009-2010 Krueger Systems, Inc.
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;


namespace SQLite.MonoTouchAdmin
{

	public class SQLiteAdmin
	{
		public SQLiteConnection Connection { get; private set; }

		public SQLiteAdmin (SQLiteConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			Connection = connection;
		}

		public UIViewController NewTablesViewController ()
		{
			var c = new TablesViewController (Connection);
			return c;
		}
		
		public static RectangleF GetTableRect() {
			return new RectangleF (0, 0, 320, 416);
		}
	}

	public class TablesViewController : UIViewController
	{
		public SQLiteConnection Connection { get; private set; }

		public TableMapping[] TableMappings { get; private set; }
		public UITableView UITable { get; private set; }
		
		public Data DataSource { get; private set; }

		public TablesViewController (SQLiteConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");
			Connection = connection;
			
			TableMappings = Connection.TableMappings.ToArray ();
			
			UITable = new UITableView (SQLiteAdmin.GetTableRect(), UITableViewStyle.Plain);
			DataSource = new Data (this);
			UITable.DataSource = DataSource;
			UITable.Delegate = new Del (this);
		}

		public override void ViewDidLoad ()
		{
			View.AddSubview (UITable);
			if (NavigationItem != null) {
				NavigationItem.Title = Connection.GetType().Name;
			}
		}
		
		public class Del : UITableViewDelegate
		{
			TablesViewController _c;
			public Del (TablesViewController c)
			{
				_c = c;
			}
			public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
			{
				var table = _c.DataSource.GetValue(indexPath);
				if (_c.NavigationController != null) {
					var c = new TableViewController(table, _c.Connection);
					_c.NavigationController.PushViewController(c, true);
				}
			}
		}

		public class Data : UITableViewDataSource
		{
			TablesViewController _c;
			public Data (TablesViewController c)
			{
				_c = c;
			}
			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}
			public override int RowsInSection (UITableView tableview, int section)
			{
				return _c.TableMappings.Length;
			}
			public TableMapping GetValue (NSIndexPath indexPath) {
				return _c.TableMappings[indexPath.Row];
			}
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell ("table");
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Default, "table");
				}
				var table = GetValue(indexPath);
				cell.TextLabel.Text = table.TableName;
				return cell;
			}
		}
	}
	
	public class TableViewController : UIViewController
	{
		public SQLiteConnection Connection { get; private set; }

		public TableMapping Table { get; private set; }
		public List<object> Rows { get; private set; }
		public UITableView UITable { get; private set; }
		
		int PageSize = 100;

		public TableViewController (TableMapping table, SQLiteConnection connection)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			if (connection == null)
				throw new ArgumentNullException ("connection");
			Table = table;
			Connection = connection;
			
			Rows = new List<object>();
			
			UITable = new UITableView (SQLiteAdmin.GetTableRect(), UITableViewStyle.Plain);
			UITable.DataSource = new Data (this);
			
			GetMoreData();
			UITable.ReloadData();
		}
		
		void GetMoreData() {
			var pk = Table.PK;
			if (pk == null) {
				Rows.AddRange(Connection.Query(Table, 
				                               "select * from \"" + Table.TableName + "\""));
			}
			else {
				var lastId = Rows.Count > 0 ? pk.GetValue(Rows[Rows.Count-1]) : 0;
				Rows.AddRange(Connection.Query(Table, 
				                               "select * from \"" + Table.TableName + "\"" +
				                               " where \"" + pk.Name + "\" > ? " +
				                               " order by \"" + pk.Name + "\"" +
				                               " limit " + PageSize, lastId));
			}
		}
		
		public override void ViewDidLoad ()
		{
			if (NavigationItem != null) {
				NavigationItem.Title = Table.TableName;
			}
			View.AddSubview(UITable);
		}
		
		public class Data : UITableViewDataSource {
			TableViewController _c;
			public Data (TableViewController c)
			{
				_c = c;
			}
			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}
			public override int RowsInSection (UITableView tableview, int section)
			{
				return _c.Rows.Count;
			}
			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell ("row");
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Default, "row");
				}
				var row = _c.Rows[indexPath.Row];
				cell.TextLabel.Text = row.ToString();
				return cell;
			}
		}
	}

}
