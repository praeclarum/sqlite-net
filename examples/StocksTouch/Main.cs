
using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Path = System.IO.Path;
using SQLite;

namespace Stocks.Touch
{
	public class Application
	{
		static void Main (string[] args)
		{
			UIApplication.Main (args);
		}
	}

	public partial class AppDelegate : UIApplicationDelegate
	{
		Database _db;
		UINavigationController _navigationController;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{			
			_db = new Database (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments), "stocks.db"));
			_db.Trace = true;
			
			var stocksView = new StocksView (_db);
			
			_navigationController = new UINavigationController (stocksView);
			
			window.AddSubview (_navigationController.View);
			
			window.MakeKeyAndVisible ();
			
			return true;
		}

		// This method is required in iPhoneOS 3.0
		public override void OnActivated (UIApplication application)
		{
		}
	}
}
