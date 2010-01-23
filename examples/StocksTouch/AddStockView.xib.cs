
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace Stocks.Touch
{
	public partial class AddStockView : UIViewController
	{
		#region Constructors

		// The IntPtr and NSCoder constructors are required for controllers that need 
		// to be able to be created from a xib rather than from managed code

		public AddStockView (IntPtr handle) : base(handle)
		{
			Initialize ();
		}

		[Export("initWithCoder:")]
		public AddStockView (NSCoder coder) : base(coder)
		{
			Initialize ();
		}
		
		Database Db;

		public AddStockView (Database db) : base("AddStockView", null)
		{
			Db = db;
			Initialize ();
		}

		void Initialize ()
		{
		}
		
		#endregion
		
		public event Action Finished;
		
		public override void ViewDidLoad ()
		{
			addBtn.TouchUpInside += delegate {
				Db.UpdateStock(symbolName.Text);
				DismissModalViewControllerAnimated(true);
				if (Finished != null) {
					Finished();
				}
			};
		}

		
		
	}
}
