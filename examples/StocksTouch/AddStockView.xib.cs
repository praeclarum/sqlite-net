
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Stocks.Touch
{
	public partial class AddStockView : UIViewController
	{
		Database _db;
		
		public event EventHandler Finished = delegate {};
		
		public AddStockView (Database db)
			: base("AddStockView", null)
		{
			_db = db;
			
			Title = "New Symbol";
			NavigationItem.LeftBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Cancel, Cancel);
			NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Done, AddSymbol);
		}
		
		UITextField _symbolName;
		
		public override void ViewDidLoad ()
		{
			_symbolName = symbolName;
			_symbolName.ShouldReturn = delegate {
				AddSymbol (symbolName, EventArgs.Empty);
				return true;
			};
		}
		
		void Cancel (object sender, EventArgs e)
		{
			DismissModalViewControllerAnimated(true);
		}
		
		void AddSymbol (object sender, EventArgs e)
		{
			_db.UpdateStock (symbolName.Text);
			DismissModalViewControllerAnimated (true);
			Finished (this, EventArgs.Empty);
		}
	}
}
