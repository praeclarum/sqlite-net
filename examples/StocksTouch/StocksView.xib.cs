
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using SQLite.MonoTouchAdmin;


namespace Stocks.Touch
{
	public partial class StocksView : UIViewController
	{
		Database _db;
		
		public StocksView (Database db)
		{
			_db = db;
			
			Title = "Symbols";
		}

		public override void ViewDidLoad ()
		{
			var ds = new SymbolsData (_db);
			
			table.DataSource = ds;
			table.SetEditing (true, false);
			
			NavigationItem.RightBarButtonItem = new UIBarButtonItem (UIBarButtonSystemItem.Add, delegate { 
				var c = new AddStockView (_db);
				c.Finished += delegate {
					ds.Refresh ();
					table.ReloadData ();
				};
				var n = new UINavigationController (c);
				NavigationController.PresentModalViewController(n, true);
			});
			NavigationItem.LeftBarButtonItem = new UIBarButtonItem ("Admin", UIBarButtonItemStyle.Plain, delegate { 
				var c = new SQLiteAdmin(_db);
				NavigationController.PushViewController(c.NewTablesViewController(), true);
			});
		}
		
		public class SymbolsData : UITableViewDataSource
		{			
			List<Stock> rows;
			Database _db;
			
			public SymbolsData(Database db) {
				_db = db;
				rows = _db.QueryAllStocks().ToList();
			}
			
			public void Refresh () {
				rows = _db.QueryAllStocks().ToList();
			}
			
			public override void MoveRow (UITableView tableView, NSIndexPath sourceIndexPath, NSIndexPath destinationIndexPath)				
			{
				var item = rows[sourceIndexPath.Row];
				rows.RemoveAt(sourceIndexPath.Row);
				rows.Insert(destinationIndexPath.Row, item);
			}

			public override bool CanMoveRow (UITableView tableView, NSIndexPath indexPath)
			{
				return true;
			}
			
			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath)
			{
				switch (editingStyle) {
				case UITableViewCellEditingStyle.Delete:
					_db.Delete(rows[indexPath.Row]);
					rows.RemoveAt(indexPath.Row);
					tableView.DeleteRows(new[]{indexPath}, UITableViewRowAnimation.Fade);
					break;
				}
			}

			public override int NumberOfSections (UITableView tableView)
			{
				return 1;
			}

			public override int RowsInSection (UITableView tableview, int section)
			{
				return rows.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
			{
				var cell = tableView.DequeueReusableCell ("cell");
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Subtitle, "cell");
				}
				cell.ShowsReorderControl = true;
				var stock = rows[indexPath.Row];
				var val = _db.QueryLatestValuation (stock);
				cell.TextLabel.Text = stock.Symbol;
				cell.DetailTextLabel.Text = val != null ? val.Price.ToString () : "?";
				return cell;
			}
		}
	}
}
