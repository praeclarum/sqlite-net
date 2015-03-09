using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class IoCUnitTests
    {
        [SetUp]
        public void Initialize()
        {
            _container = new SimpleIoC();

            _container.Register<IProduct, Product>();
            _container.Register<IOrder, Order>();
            _container.Register<IOrderHistory, OrderHistory>();
            _container.Register<IOrderLine, OrderLine>();
        }

        private SimpleIoC _container;

        private static void VerifyCreations(TestDb db)
        {
            var orderLine = db.GetMapping(typeof (OrderLine));
            Assert.AreEqual(6, orderLine.Columns.Length);

            var l = new OrderLine
            {
                Status = OrderLineStatus.Shipped
            };
            db.Insert(l);
            var lo = db.Table<OrderLine>().First(x => x.Status == OrderLineStatus.Shipped);
            Assert.AreEqual(lo.Id, l.Id);
        }

        private class SimpleIoC
        {
            private readonly Dictionary<Type, Type> _typeMap = new Dictionary<Type, Type>();

            public void Register<T, T1>()
            {
                _typeMap[typeof (T)] = typeof (T1);
            }

            public bool CanResolve(Type type)
            {
                return _typeMap.ContainsKey(type) || IsClass(type);
            }

            private bool IsClass(Type type)
            {
                return type.GetTypeInfo().IsClass;
            }

            public T Resolve<T>()
            {
                return (T) Resolve(typeof (T));
            }

            public object Resolve(Type type)
            {
                if (_typeMap.ContainsKey(type))
                {
                    var mappedType = _typeMap[type];
                    return Activator.CreateInstance(mappedType);
                }
                if (IsClass(type))
                {
                    return Activator.CreateInstance(type);
                }
                throw new NotSupportedException();
            }
        }

        [Test]
        [Category("IoC.TinyIoC")]
        public void BulkInsertAndSelect()
        {
            var db = new TestDb(false, new ContractResolver(t => _container.CanResolve(t), (t, op) => _container.Resolve(t)));

            db.CreateTable<IProduct>();
            db.CreateTable<IOrder>();
            db.CreateTable<IOrderLine>();
            db.CreateTable<IOrderHistory>();

            var rnd = new Random();
            var data = new List<IOrderLine>();
            for (var i = 0; i < 100; i++)
            {
                var l = _container.Resolve<IOrderLine>();

                l.OrderId = rnd.Next(1, 100);
                l.ProductId = rnd.Next(1, 100);
                l.Quantity = rnd.Next(1, 25);
                l.Status = OrderLineStatus.Shipped;
                l.UnitPrice = rnd.Next(0, 100);

                data.Add(l);
            }

            db.InsertAll(data.ToArray());

            var results = db.Table<IOrderLine>();

            Assert.AreEqual(data.Count, results.Count());
            Assert.AreEqual(data.First().UnitPrice, results.First().UnitPrice);
            Assert.AreEqual(data.Last().UnitPrice, results.Last().UnitPrice);
        }

        [Test]
        public void CreateThemByInterface()
        {
            var db = new TestDb();

            db.CreateTable<IProduct>();
            db.CreateTable<IOrder>();
            db.CreateTable<IOrderLine>();
            db.CreateTable<IOrderHistory>();

            VerifyCreations(db);
        }
    }
}