
using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Net.Attributes;
using NUnit.Framework;
using Newtonsoft.Json;
using System.Text;

namespace SQLite.Net.Tests
{
    //[TestFixture]
    public abstract class BlobSerializationTest
    {   
        protected abstract IBlobSerializer Serializer { get; }

        public class BlobDatabase : SQLiteConnection
        {
            public BlobDatabase(IBlobSerializer serializer) :
                base(new SQLitePlatformTest(), TestPath.CreateTemporaryDatabase(), false, serializer)
            {
                DropTable<ComplexOrder>();
            }
        }

        public class ComplexOrder : IEquatable<ComplexOrder>
        {
            public ComplexOrder()
            {
                PlacedTime = DateTime.UtcNow;
                History = new List<ComplexHistory>();
                Lines = new List<ComplexLine>();
            }

            [AutoIncrement, PrimaryKey]
            public int Id { get; set; }

            public DateTime PlacedTime { get; set; }

            public List<ComplexHistory> History { get; set; }

            public List<ComplexLine> Lines { get; set; }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as ComplexOrder);
            }

            public override int GetHashCode()
            {
                return this.Id.GetHashCode() ^
                    //this.PlacedTime.GetHashCode() ^
                    this.History.GetHashCode() ^
                    this.Lines.GetHashCode();
            }

            public bool Equals(ComplexOrder other)
            {
                if (other == null)
                {
                    return false;
                }

                return this.Id.Equals(other.Id) &&
                    Math.Abs((this.PlacedTime - other.PlacedTime).TotalMilliseconds) < 100 &&
                    this.History.SequenceEqual(other.History) &&
                    this.Lines.SequenceEqual(other.Lines);
            }
        }

        public class ComplexHistory : IEquatable<ComplexHistory>
        {
            public DateTime Time { get; set; }
            public string Comment { get; set; }

            public override int GetHashCode()
            {
                return this.Comment.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as ComplexHistory);
            }

            public bool Equals(ComplexHistory other)
            {
                if (other == null)
                {
                    return false;
                }

                return this.Comment.Equals(other.Comment);
            }
        }

        public class ComplexLine : IEquatable<ComplexLine>
        {
            [Indexed("IX_OrderProduct", 2)]
            public int ProductId { get; set; }

            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public OrderLineStatus Status { get; set; }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as ComplexLine);
            }

            public override int GetHashCode()
            {
                return
                    this.ProductId.GetHashCode() ^
                    this.Quantity.GetHashCode() ^
                    this.Status.GetHashCode() ^
                    this.UnitPrice.GetHashCode();
            }

            public bool Equals(ComplexLine other)
            {
                if (other == null)
                {
                    return false;
                }

                return
                    this.ProductId.Equals(other.ProductId) &&
                    this.Quantity.Equals(other.Quantity) &&
                    this.Status.Equals(other.Status) &&
                    this.UnitPrice.Equals(other.UnitPrice);
            }
        }

        [Test]
        public void VerifyTableCreationFailsWithNoSerializer()
        {
            NotSupportedException ex = null;
            using (var db = new BlobDatabase(null))
            {
                try
                {
                    var count = db.CreateTable<ComplexOrder>();
                    Assert.IsTrue(count == 0);
                    Assert.IsNull(db.GetMapping<ComplexOrder>());
                    return;
                }
                catch (NotSupportedException e)
                {
                    ex = e;
                }
            }

            Assert.IsNotNull(ex);
        }

        [Test]
        public void VerifyTableCreationSucceedsWithSupportedSerializer()
        {
            if (!this.Serializer.CanDeserialize(typeof(ComplexOrder)))
            {
                Assert.Ignore("Serialize does not support this data type");
            }

            using (var db = new BlobDatabase(this.Serializer))
            {
                db.CreateTable<ComplexOrder>();
                var mapping = db.GetMapping<ComplexOrder>();
                Assert.IsNotNull(mapping);
                Assert.AreEqual(4, mapping.Columns.Length);
            }
        }

        //[Test]
        public void TestListOfObjects()
        {
            if (!this.Serializer.CanDeserialize(typeof(ComplexOrder)))
            {
                Assert.Ignore("Serialize does not support this data type");
            }

            using (var db = new BlobDatabase(this.Serializer))
            {
                db.CreateTable<ComplexOrder>();
                var order = new ComplexOrder();

                var count = db.Insert(order);
                Assert.AreEqual(count, 1);
                var orderCopy = db.Find<ComplexOrder>(order.Id);
                Assert.AreEqual(order, orderCopy);

                for (var n = 0; n < 10; )
                {
                    order.Lines.Add(new ComplexLine() { ProductId = 1, Quantity = ++n, Status = OrderLineStatus.Placed, UnitPrice = (n / 10m) });
                    db.Update(order);
                    orderCopy = db.Find<ComplexOrder>(order.Id);
                    Assert.AreEqual(order, orderCopy);
                    order.History.Add(new ComplexHistory() { Time = DateTime.UtcNow, Comment = string.Format("New history {0}", n) });
                    db.Update(order);
                    orderCopy = db.Find<ComplexOrder>(order.Id);
                    Assert.AreEqual(order, orderCopy);
                }
            }
        }
    }

    [TestFixture]
    public class BlobDelegateTest : BlobSerializationTest
    {
        public enum Bool { True, False }

        public class SupportedTypes
        {
            public Boolean Boolean { get; set; }
            public Byte Byte { get; set; }
            public UInt16 UInt16 { get; set; }
            public SByte SByte { get; set; }
            public Int16 Int16 { get; set; }
            public Int32 Int32 { get; set; }
            public UInt32 UInt32{ get; set; }
            public Int64 Int64{ get; set; }
            public Single Single { get; set; }
            public Double Double { get; set; }
            public Decimal Decimal { get; set; }
            public String String{ get; set; }
            public DateTime DateTime{ get; set; }
            public Bool EnumBool{ get; set; }
            public Guid Guid{ get; set; }
            public byte[] Bytes{ get; set; }
            public TimeSpan Timespan { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
    }

        public class UnsupportedTypes
        {
            [PrimaryKey]
            public Guid Id { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public DivideByZeroException DivideByZeroException { get; set; }
        }

        protected override IBlobSerializer Serializer
        {
            get
            {
                return new BlobSerializerDelegate(
                    obj => new byte[0],
                    (data, type) => null,
                    type => false);
            }
        }

        [Test]
        public void CanDeserializeIsRequested()
        {
            var types = new List<Type>();

            var serializer = new BlobSerializerDelegate(obj => null, (d, t) => null, t =>
                {
                    types.Add(t);
                    return true;
                });

            using (var db = new BlobDatabase(serializer))
            {
                db.CreateTable<ComplexOrder>();
            }

            Assert.That(types, Has.Member(typeof (List<ComplexHistory>)));
            Assert.That(types, Has.Member(typeof (List<ComplexLine>)));

            Assert.AreEqual(2, types.Count, "Too many types requested by serializer");
        }

        [Test]
        public void DoesNotCallOnSupportedTypes()
        {
            var types = new List<Type>();

            var serializer = new BlobSerializerDelegate(obj => null, (d, t) => null, t =>
            {
                throw new InvalidOperationException(string.Format("Type {0} should not be requested.", t));
            });

            using (var db = new BlobDatabase(serializer))
            {
                db.CreateTable<SupportedTypes>();
            }

            Assert.AreEqual(0, types.Count, "Types requested from serializer");
        }

        [Test]
        public void CallsOnUnsupportedTypes()
        {
            var types = new List<Type>();

            var serializer = new BlobSerializerDelegate(obj => null, (d, t) => null, t =>
            {
                types.Add(t);
                return true;
            });

            using (var db = new BlobDatabase(serializer))
            {
                db.CreateTable<UnsupportedTypes>();
            }

            Assert.That(types, Has.Member(typeof (DivideByZeroException)));

            Assert.AreEqual(1, types.Count, "Too many types requested by serializer");
        }

        [Test]
        public void SavesUnsupportedTypes()
        {
            UnsupportedTypes item = null;

            var serializer = new BlobSerializerDelegate(
                obj =>
                {
                    if (obj is DivideByZeroException)
                    {
                        var e = (DivideByZeroException)obj;
                        var json = JsonConvert.SerializeObject(e);        // subst your own serializer
                        return Encoding.UTF8.GetBytes(json);
                    }

                    throw new InvalidOperationException(string.Format("Type {0} should not be requested.", obj.GetType()));
                },
                (d, t) =>
                {
                    if (t == typeof(DivideByZeroException))
                    {
#if __WINRT__ || WINDOWS_PHONE
                        var json = Encoding.UTF8.GetString(d, 0, d.Length);
#else
                        var json = Encoding.UTF8.GetString(d);
#endif
                        var result = JsonConvert.DeserializeObject<DivideByZeroException>(json);
                        return result;
                    }

                    throw new InvalidOperationException(string.Format("Type {0} should not be requested.", t));
                },
                t => true);

            using (var db = new BlobDatabase(serializer))
            {
                db.CreateTable<UnsupportedTypes>();
                item = new UnsupportedTypes()
                {
                    Id = Guid.NewGuid(),
                    DateTimeOffset = DateTime.Now,
                    DivideByZeroException = new DivideByZeroException("a message")
                };

                db.Insert(item);
                var dbItem = db.Find<UnsupportedTypes>(item.Id);

                Assert.AreEqual(item.Id, dbItem.Id);
                Assert.AreEqual(item.DateTimeOffset, dbItem.DateTimeOffset);
                Assert.AreEqual(item.DivideByZeroException.Message, dbItem.DivideByZeroException.Message);
            }
        }
    }
}
