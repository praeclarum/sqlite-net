using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite.MetroStyle.DemoApp.Xaml
{
    public class Customer
    {
        [AutoIncrement, PrimaryKey]
        public int Id { get; set; }

        [MaxLength(64)]
        public string FirstName { get; set; }

        [MaxLength(64)]
        public string LastName { get; set; }

        [MaxLength(64)]
        public string Email { get; set; }
    }
}
