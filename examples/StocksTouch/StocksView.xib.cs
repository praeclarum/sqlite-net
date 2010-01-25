
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
		
		public Database Db { get; private set; }
		
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public StocksView (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public StocksView (NSCoder coder) : base(coder)
		{
			Initialize ();
		}

		public StocksView (Database db)
		{
			Db = db;
			Initialize ();
		}

		#endregion
		
		void Initialize ()
		{
			Title = "Stocks";
		}
		
		public override void ViewDidLoad ()
		{
			var ds = new TickersSource (Db);
			
			NavigationItem.BackBarButtonItem = new UIBarButtonItem ("Stocks", UIBarButtonItemStyle.Plain, (s, e) => { });
			NavigationItem.RightBarButtonItem = new UIBarButtonItem ("Add", UIBarButtonItemStyle.Plain, (s, e) => { 
				var c = new AddStockView(Db);
				c.Finished += delegate() {
					ds.Refresh(table);
				};
				NavigationController.PresentModalViewController(c, true);
				table.ReloadData();
			});
			NavigationItem.LeftBarButtonItem = new UIBarButtonItem ("Admin", UIBarButtonItemStyle.Plain, (s, e) => { 
				var c = new SQLiteAdmin(Db);
				NavigationController.PushViewController(c.NewTablesViewController(), true);
			});

			table.DataSource = ds;
			table.SetEditing (true, true);
		}

		
		#region Table Controller

		public class TickersSource : UITableViewDataSource
		{			
			List<Stock> rows;
			Database _db;
			
			public TickersSource(Database db) {
				_db = db;
				rows = _db.QueryAllStocks().ToList();
			}
			
			public void Refresh(UITableView table) {
				rows = _db.QueryAllStocks().ToList();
				table.ReloadData();
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
				cell.TextLabel.Text = rows[indexPath.Row].ToString();
				cell.DetailTextLabel.Text = ("row " + indexPath.Row);
				return cell;
			}
		}
#endregion



	}
}
