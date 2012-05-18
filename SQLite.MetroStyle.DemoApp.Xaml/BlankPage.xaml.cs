using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SQLite.MetroStyle.DemoApp.Xaml
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BlankPage : Page
    {
        public BlankPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void ShowAlertAsync(string message)
        {
            if (!(this.Dispatcher.HasThreadAccess))
            {
                Dispatcher.Invoke(Windows.UI.Core.CoreDispatcherPriority.Normal, (sender, e) =>
                {
                    ShowAlertAsync(message);
                }, this, null);
            }
            else
            {
                MessageDialog dialog = new MessageDialog(message);
                await dialog.ShowAsync();
            }
        }

        private SQLiteAsyncConnection GetConnection()
        {
            return new SQLiteAsyncConnection(SQLiteConnectionSpecification.CreateForAsync("foobar.db"));
        }

        private void HandleCreateTable(object sender, RoutedEventArgs e)
        {
            var conn = GetConnection();
            conn.CreateTableAsync<Customer>().ContinueWith((t) =>
            {
                this.ShowAlertAsync("Table created OK.");

            });
        }

        private void HandleInsertCustomer(object sender, RoutedEventArgs e)
        {
            var conn = GetConnection();

            // create...
            Customer customer = new Customer()
            {
                FirstName = "foo",
                LastName = "bar",
                Email = Guid.NewGuid().ToString()
            };

            // insert...
            conn.InsertAsync(customer).ContinueWith((t) =>
            {
                this.ShowAlertAsync(string.Format("Customer created OK - ID: {0}.", customer.Id));

            });
        }
    }
}
