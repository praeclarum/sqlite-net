using System;
using NUnit.Framework;
using SQLite.Net.Attributes;

namespace SQLite.Net.Tests
{
    [TestFixture]
    public class SerializableTests
    {
        private TestDb _db;

        [SetUp]
        public void SetUp()
        {
            _db = new TestDb();
            _db.CreateTable<ComplexType>();
        }

        [Test]
        public void SupportsSerializableInt64()
        {
            Int64 value = Int64.MaxValue;
            var model = new ComplexType { Int64Value = new SerializableInt64(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.Int64Value.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableInt32()
        {
            Int32 value = Int32.MaxValue;
            var model = new ComplexType { Int32Value = new SerializableInt32(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.Int32Value.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableInt16()
        {
            Int16 value = Int16.MaxValue;
            var model = new ComplexType { Int16Value = new SerializableInt16(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.Int16Value.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableString()
        {
            string value = "foo";
            var model = new ComplexType { StringValue = new SerializableString(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.StringValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableBoolean()
        {
            bool value = true;
            var model = new ComplexType { BooleanValue = new SerializableBoolean(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.BooleanValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableByte()
        {
            byte value = 10;
            var model = new ComplexType { ByteValue = new SerializableByte(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.ByteValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableSingle()
        {
            Single value = 0.0000001f;
            var model = new ComplexType { SingleValue = new SerializableSingle(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.SingleValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableDouble()
        {
            Double value = 0.0000001;
            var model = new ComplexType { DoubleValue = new SerializableDouble(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.DoubleValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableDecimal()
        {
            Decimal value = 0.0000001m;
            var model = new ComplexType { DecimalValue = new SerializableDecimal(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.DecimalValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableTimeSpan()
        {
            TimeSpan value = TimeSpan.MaxValue;
            var model = new ComplexType { TimeSpanValue = new SerializableTimeSpan(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.TimeSpanValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableDateTime()
        {
            DateTime value1 = DateTime.UtcNow;
            DateTime value2 = DateTime.Now;
            var model = new ComplexType
            {
                DateTimeValue = new SerializableDateTime(value1),
                DateTimeValue2 = new SerializableDateTime(value2)
            };
            _db.Insert(model);
            ComplexType found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.DateTimeValue.InnerValue.ToUniversalTime(), Is.EqualTo(value1.ToUniversalTime()));
            Assert.That(found.DateTimeValue2.InnerValue.ToUniversalTime(), Is.EqualTo(value2.ToUniversalTime()));

            Assert.That(found.DateTimeValue.InnerValue.ToLocalTime(), Is.EqualTo(value1.ToLocalTime()));
            Assert.That(found.DateTimeValue2.InnerValue.ToLocalTime(), Is.EqualTo(value2.ToLocalTime()));
        }

        [Test]
        public void SupportsSerializableByteArray()
        {
            byte[] value = new byte[] { 1, 2, 3, 4, 5 };
            var model = new ComplexType { ByteArrayValue = new SerializableByteArray(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.ByteArrayValue.InnerValue, Is.EqualTo(value));
        }

        [Test]
        public void SupportsSerializableGuid()
        {
            Guid value = Guid.NewGuid();
            var model = new ComplexType { GuidValue = new SerializableGuid(value) };
            _db.Insert(model);
            var found = _db.Get<ComplexType>(m => m.ID == model.ID);
            Assert.That(found.GuidValue.InnerValue, Is.EqualTo(value));
        }

        private class ComplexType
        {
            [AutoIncrement, PrimaryKey]
            public int ID { get; set; }

            public SerializableInt64 Int64Value { get; set; }
            public SerializableInt32 Int32Value { get; set; }
            public SerializableInt16 Int16Value { get; set; }
            public SerializableString StringValue { get; set; }
            public SerializableBoolean BooleanValue { get; set; }
            public SerializableByte ByteValue { get; set; }
            public SerializableSingle SingleValue { get; set; }
            public SerializableDouble DoubleValue { get; set; }
            public SerializableDecimal DecimalValue { get; set; }
            public SerializableTimeSpan TimeSpanValue { get; set; }
            public SerializableDateTime DateTimeValue { get; set; }
            public SerializableDateTime DateTimeValue2 { get; set; }
            public SerializableByteArray ByteArrayValue { get; set; }
            public SerializableGuid GuidValue { get; set; }
        }

        private class SerializableInt64 : Wrapper<Int64>
        {
            public SerializableInt64(Int64 value) : base(value)
            {
            }
        }

        private class SerializableInt32 : Wrapper<Int32>
        {
            public SerializableInt32(Int32 value) : base(value)
            {
            }
        }

        private class SerializableInt16 : Wrapper<Int16>
        {
            public SerializableInt16(Int16 value) : base(value)
            {
            }
        }

        private class SerializableString : Wrapper<string>
        {
            public SerializableString(string value) : base(value)
            {
            }
        }

        private class SerializableBoolean : Wrapper<Boolean>
        {
            public SerializableBoolean(Boolean value) : base(value)
            {
            }
        }

        private class SerializableByte : Wrapper<Byte>
        {
            public SerializableByte(Byte value) : base(value)
            {
            }
        }

        private class SerializableSingle : Wrapper<Single>
        {
            public SerializableSingle(Single value) : base(value)
            {
            }
        }

        private class SerializableDouble : Wrapper<Double>
        {
            public SerializableDouble(Double value) : base(value)
            {
            }
        }

        private class SerializableDecimal : Wrapper<Decimal>
        {
            public SerializableDecimal(Decimal value) : base(value)
            {
            }
        }

        private class SerializableTimeSpan : Wrapper<TimeSpan>
        {
            public SerializableTimeSpan(TimeSpan value) : base(value)
            {
            }
        }

        private class SerializableDateTime : Wrapper<DateTime>
        {
            public SerializableDateTime(DateTime value) : base(value)
            {
            }
        }

        private class SerializableByteArray : Wrapper<byte[]>
        {
            public SerializableByteArray(byte[] value) : base(value)
            {
            }
        }

        private class SerializableGuid : Wrapper<Guid>
        {
            public SerializableGuid(Guid value) : base(value)
            {
            }
        }

        private abstract class Wrapper<T> : ISerializable<T>
        {
            public T InnerValue;

            public Wrapper(T value)
            {
                InnerValue = value;
            }

            public T Serialize()
            {
                return InnerValue;
            }
        }
    }
}

