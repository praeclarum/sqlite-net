using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

#if NETFX_CORE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SetUp = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestFixture = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using Test = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
#else
using NUnit.Framework;
#endif


namespace SQLite.Tests {
    [TestFixture]
    public class CustomTypesTests {
        public class CustomTypeTestObj {
            [PrimaryKey]
            public Guid Id { get; set; }
            public JsonDocument JsonColumn { get; set; }

            public override string ToString() {
                return string.Format("[TestObj: Id={0}, Json={1}]", Id, JsonColumn?.ToString ());
            }
        }

        public class TestObjQueryResult
        {
	        public JsonDocument JsonColumn { get; set; }
        }
        
        public class JsonDocumentTypeHandler : CustomTypeHandler<JsonDocument>
        {   
	        public int InitializeCalled { get; set; }
	        public int TableCreatedCalled { get; set; }
	        
	        public override void Initialize (SQLiteConnection connection)
	        {
		        InitializeCalled++;
	        }

	        public override string GetSqlType (CustomTypeMetadata metadata)
	        {
		        return "TEXT";
	        }

	        public override object ConvertToBindableValue (JsonDocument value, CustomTypeMetadata metadata)
	        {
		        if (value is null)
			        return null;

		        using var stream = new MemoryStream();
		        Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
		        value.WriteTo(writer);
		        writer.Flush();
		        string json = Encoding.UTF8.GetString(stream.ToArray());
		        return json;
	        }

	        public override JsonDocument ConvertFromDatabaseValue (object value, CustomTypeMetadata metadata)
	        {
		        if (value is string { } s)
			        return JsonDocument.Parse (s);

		        if (value is null)
			        return null;

		        throw new NotSupportedException (
			        $"Cannot convert '{value}' ({value.GetType ().FullName}) to JsonDocument");
	        }

	        public override void OnTableCreated (SQLiteConnection connection, string tableName, CustomTypeMetadata metadata)
	        {
		        TableCreatedCalled++;
		        base.OnTableCreated(connection, tableName, metadata);
	        }
        }

        [SetUp]
        public void SetUp ()
        {
	        CustomTypeRegistry.Reset ();
        }
        
        [Test]
        public void WhenRegistered_ShouldCallInitializeOnCustomTypeHandler ()
        {
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new JsonDocumentTypeHandler ();
	        db.DefineCustomType (h);

	        Assert.AreEqual (1, h.InitializeCalled);
        }
        
        [Test]
        public void EachNewConnection_ShouldCallInitializeOnCustomTypeHandler ()
        {
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new JsonDocumentTypeHandler ();
	        db.DefineCustomType (h); //1

	        // Act
	        var db2 = new SQLiteConnection (db.DatabasePath); // 2
	        var db3 = new SQLiteConnection (db.DatabasePath); // 3
	        
	        // Assert
	        Assert.AreEqual (3, h.InitializeCalled);
	        
	        db.Close();
	        db2.Close();
	        db3.Close();
        }
        
        [Test]
        public void ShouldCallInitializeOnCustomTypeHandler_JustOnce ()
        {
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new JsonDocumentTypeHandler ();
	        
	        // Act
	        db.DefineCustomType (h);
	        db.DefineCustomType (h);

	        // Assert
	        Assert.AreEqual (1, h.InitializeCalled);
	        
	        db.Close();
        }

        [Test]
        public void CanCreateTable_WithColumnForCustomType ()
        {
	        // Arrange
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new JsonDocumentTypeHandler ();
	        db.DefineCustomType (h);
	           
	        // Act
	        db.CreateTable<CustomTypeTestObj>();
	        
	        // Assert
	        var jsonColumn = db.TableMappings.SingleOrDefault (t => t.TableName == "CustomTypeTestObj").Columns.Last ();
	        Assert.AreEqual ("JsonColumn", jsonColumn.Name);
	        Assert.AreEqual(typeof(JsonDocument), jsonColumn.ColumnType);
	        Assert.AreEqual (1, h.TableCreatedCalled); // important for cases like spatialite where we want to create a geometryconstraint
	        db.Close();
        }

        [Test]
        public void CanMigrateTable_WithColumnForCustomType ()
        {
	        // Arrange
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new JsonDocumentTypeHandler ();
	        db.DefineCustomType (h);

	        db.Execute ("""
	                    CREATE TABLE TestObj (
	                        Id         VARCHAR (36) PRIMARY KEY
	                                                NOT NULL
	                    );
	                    """);
	        
	        // Act
	        db.CreateTable<CustomTypeTestObj>();
	        
	        
	        // Assert
	        var jsonColumn = db.TableMappings.SingleOrDefault (t => t.TableName == "CustomTypeTestObj").Columns.Last ();
	        Assert.AreEqual ("JsonColumn", jsonColumn.Name);
	        Assert.AreEqual(typeof(JsonDocument), jsonColumn.ColumnType);
	        Assert.AreEqual (1, h.TableCreatedCalled); // important for cases like spatialite where we want to create a geometryconstraint
	        db.Close();
        }

        [Test]
        public void ShouldPersistAndReadCustomType()
        {
	        var db = new SQLiteConnection(TestPath.GetTempFileName());
	        db.DefineCustomType ( new JsonDocumentTypeHandler ());
            db.CreateTable<CustomTypeTestObj>();

            var obj1 = new CustomTypeTestObj() { Id=new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"),
	            JsonColumn = JsonDocument.Parse ("""
                                                 {
                                                     "name": "Joe",
                                                     "age": 16,
                                                     "canDrive": false
                                                 }
                                                 """)};
            var obj2 = new CustomTypeTestObj() {  Id=new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"),
	            JsonColumn = JsonDocument.Parse ("""
	                                             {
	                                                 "name": "Mary",
	                                                 "age": 25,
	                                                 "canDrive": true
	                                             }
	                                             """)};

            var numIn1 = db.Insert(obj1);
            var numIn2 = db.Insert(obj2);
            Assert.AreEqual(1, numIn1);
            Assert.AreEqual(1, numIn2);

            var result = db.Query<TestObjQueryResult>("select JsonColumn from CustomTypeTestObj").ToList();
            Assert.AreEqual(2, result.Count);
            
            Assert.IsNotNull (result[0].JsonColumn);
            Assert.IsNotNull (result[1].JsonColumn);

            Assert.AreEqual (obj1.JsonColumn.ToString (), result[0].JsonColumn.ToString ());
            Assert.AreEqual (obj2.JsonColumn.ToString (), result[1].JsonColumn.ToString ());
            
            db.Close();
        }

        [Test]
        public void ShouldSupportCustomType_InCustomQueryType ()
        {
	        var db = new SQLiteConnection(TestPath.GetTempFileName());
	        db.DefineCustomType ( new JsonDocumentTypeHandler ());
	        db.CreateTable<CustomTypeTestObj>();

	        var obj1 = new CustomTypeTestObj() { Id=new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"),
		        JsonColumn = JsonDocument.Parse ("""
		                                         {
		                                             "name": "Joe",
		                                             "age": 16,
		                                             "canDrive": false
		                                         }
		                                         """)};
	        var obj2 = new CustomTypeTestObj() {  Id=new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"),
		        JsonColumn = JsonDocument.Parse ("""
		                                         {
		                                             "name": "Mary",
		                                             "age": 25,
		                                             "canDrive": true
		                                         }
		                                         """)};

	        var numIn1 = db.Insert(obj1);
	        var numIn2 = db.Insert(obj2);
	        Assert.AreEqual(1, numIn1);
	        Assert.AreEqual(1, numIn2);

	        var result = db.Query<CustomTypeTestObj>("select * from CustomTypeTestObj").ToList();
	        Assert.AreEqual(2, result.Count);
            
	        Assert.AreEqual(obj1.Id, result[0].Id);
	        Assert.AreEqual(obj2.Id, result[1].Id);
            
	        Assert.IsNotNull (result[0].JsonColumn);
	        Assert.IsNotNull (result[1].JsonColumn);

	        Assert.AreEqual (obj1.JsonColumn.ToString (), result[0].JsonColumn.ToString ());
	        Assert.AreEqual (obj2.JsonColumn.ToString (), result[1].JsonColumn.ToString ());
            
	        db.Close();
        }
        
        [Test]
        public void ShouldThrow_IfCustomTypeIsUsedInQuery()
        {
	        var db = new SQLiteConnection(TestPath.GetTempFileName());
	        db.DefineCustomType ( new JsonDocumentTypeHandler ());
	        db.CreateTable<CustomTypeTestObj>();

	        var obj1 = new CustomTypeTestObj() { Id=new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"),
		        JsonColumn = JsonDocument.Parse ("""
		                                         {
		                                             "name": "Joe",
		                                             "age": 16,
		                                             "canDrive": false
		                                         }
		                                         """)};
	        var obj2 = new CustomTypeTestObj() {  Id=new Guid("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"),
		        JsonColumn = JsonDocument.Parse ("""
		                                         {
		                                             "name": "Mary",
		                                             "age": 25,
		                                             "canDrive": true
		                                         }
		                                         """)};

	        var numIn1 = db.Insert(obj1);
	        var numIn2 = db.Insert(obj2);
	        Assert.AreEqual(1, numIn1);
	        Assert.AreEqual(1, numIn2);

	        NotSupportedException exception = null;

	        try {
		        // Act
		        db.Table<CustomTypeTestObj> ().Where (o => o.JsonColumn.RootElement.ValueKind == JsonValueKind.Null).ToList ();
	        }
	        catch (NotSupportedException ex) {
		        exception = ex;
	        }

	        // Assert
	        Assert.IsNotNull (exception);
	        Assert.AreEqual ("Custom type JsonDocument cannot be used in LINQ queries. Column 'JsonColumn' of type JsonDocument is not supported in WHERE clauses or other LINQ operations.", exception.Message);
	        
	        db.Close();
        }
        
        [Test]
        public void ShouldSupportBaseTypeColumnType ()
        {
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        db.DefineCustomType (new BaseClassTypeHandler ());
	        db.CreateTable<BaseClassTestObj>();

	        var obj1 = new BaseClassTestObj() {
		        Id=new Guid("36473164-C9E4-4CDF-B266-A0B287C85623"),
		        CustomColumn = new BaseClass {Name="base"}
	        };
	        var obj2 = new BaseClassTestObj () {
		        Id = new Guid ("BC5C4C4A-CA57-4B61-8B53-9FD4673528B6"),
		        CustomColumn = new DerivedClass () { Name = "derived" }
	        };

	        var numIn1 = db.Insert(obj1);
	        var numIn2 = db.Insert(obj2);
	        Assert.AreEqual(1, numIn1);
	        Assert.AreEqual(1, numIn2);

	        // Act
	        var result = db.Query<BaseClassTestObj>("select * from BaseClassTestObj").ToList();
	        
	        // Assert
	        Assert.AreEqual(2, result.Count);
            
	        Assert.AreEqual(obj1.Id, result[0].Id);
	        Assert.AreEqual(obj2.Id, result[1].Id);
            
	        Assert.IsNotNull (result[0].CustomColumn);
	        Assert.IsNotNull (result[1].CustomColumn);

	        Assert.IsInstanceOf<BaseClass>(result[0].CustomColumn);
	        Assert.IsInstanceOf<DerivedClass>(result[1].CustomColumn);
	        
	        Assert.AreEqual("base", result[0].CustomColumn.Name);
	        Assert.AreEqual("derived", result[1].CustomColumn.Name);
            
	        db.Close();
        }
        
        public class BaseClassTestObj {
	        [PrimaryKey]
	        public Guid Id { get; set; }
	        public BaseClass CustomColumn { get; set; }

	        public override string ToString() {
		        return string.Format("[TestObj: Id={0}, CustomColumn={1}:{2}]", Id, CustomColumn?.GetType ()?.Name, CustomColumn?.Name);
	        }

        }
        public class BaseClassTypeHandler : CustomTypeHandler<BaseClass>
        {
	        public override void Initialize (SQLiteConnection connection)
	        {
	        }

	        public override string GetSqlType (CustomTypeMetadata metadata)
	        {
		        return "TEXT";
	        }

	        public override object ConvertToBindableValue (BaseClass value, CustomTypeMetadata metadata)
	        {
		        if (value is null)
			        return null;
		        
		        if (value is DerivedClass d)
			        return $"D:{d.Name}";
		        return $"B:{value.Name}";
	        }

	        public override BaseClass ConvertFromDatabaseValue (object value, CustomTypeMetadata metadata)
	        {
		        if (value is null)
			        return null;

		        if (value is string s) {
			        if (s.StartsWith ("D"))
				        return new DerivedClass { Name = s.Split (':')[1] };

			        return new BaseClass () { Name = s.Split (':')[1] };
		        }

		        throw new NotSupportedException ($"Value {value} not supported");
	        }
        }
        public class BaseClass
        {
	        public string Name { get; set; }       
        }
        public class DerivedClass : BaseClass
        {
	        
        }
        
        [Test]
        public void IfNoCustomIndexSqlProvided_CreatesDefaultIndex ()
        {
	        // Arrange
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new CustomNumberTypeHandler ();
	        db.DefineCustomType (h);

	        // Act
	        db.CreateTable<TestObj2> ();

	        // Assert
	        var def = db.ExecuteScalar<string> ("""
	                                            SELECT sql 
	                                            FROM sqlite_master 
	                                            WHERE type = 'index' 
	                                              AND name = 'TestObj2_Amount';
	                                            """);
	        Assert.AreEqual ("CREATE INDEX \"TestObj2_Amount\" on \"TestObj2\"(\"Amount\")", def);
        }
        
        [Test]
        public void IfCustomIndexSqlProvided_CreatesCustomIndex ()
        {
	        // Arrange
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new CustomNumberTypeHandler ();
	        db.DefineCustomType (h);

	        h.CustomIndexSql = "CREATE INDEX acctchng_magnitude ON testobj2(abs(amount));";
	        
	        // Act
	        db.CreateTable<TestObj2> ();

	        // Assert
	        var def = db.ExecuteScalar<string> ("""
	                                  SELECT sql 
	                                  FROM sqlite_master 
	                                  WHERE type = 'index' 
	                                    AND name = 'acctchng_magnitude';
	                                  """);
	        Assert.AreEqual ("CREATE INDEX acctchng_magnitude ON testobj2(abs(amount))", def);
        }

        [Test]
        public void CanUseCustomSelectExpression ()
        {
	        // Arrange
	        var db = new SQLiteConnection (TestPath.GetTempFileName ());
	        var h = new CustomNumberTypeHandler ();
	        db.DefineCustomType (h);
	        db.CreateTable<TestObj2> ();

	        // Note: this is a very contrived example.
	        // this feature is, however, very important when working with e.g. Spatialite
	        // as you can use 'AsText(columnName)' to get the geometry as string, which is serializable
	        h.CustomSelectExpression = "{0}+10";
	        
	        db.Insert (new TestObj2 { Amount = new CustomNumber {Value=1} });
	        db.Insert (new TestObj2 { Amount = new CustomNumber {Value=-100} });
	        
	        // Act
	        var results = db.Table<TestObj2> ().ToList ();
	        
	        // Assert
	        Assert.AreEqual (results[0].Amount.Value, 11);
	        Assert.AreEqual (results[1].Amount.Value, -90);
        }
        
        public class TestObj2
        {
	        [PrimaryKey]
	        [AutoIncrement]
	        public int Id { get; set; }

	        [Indexed]
	        public CustomNumber Amount { get; set; }
        }

        public class CustomNumber
        {
	        public long Value { get; set; }
        }

        public class CustomNumberTypeHandler : CustomTypeHandler<CustomNumber>
        {
	        public override void Initialize (SQLiteConnection connection)
	        {
	        }

	        public override string GetSqlType (CustomTypeMetadata metadata)
	        {
		        return "INTEGER";
	        }

	        public override object ConvertToBindableValue (CustomNumber value, CustomTypeMetadata metadata)
	        {
		        return value?.Value ?? 0;
	        }

	        public override CustomNumber ConvertFromDatabaseValue (object value, CustomTypeMetadata metadata)
	        {
		        if (value is null)
			        return new CustomNumber { Value = 0 };
		        
		        return new CustomNumber { Value = (long)value };
	        }

	        public string CustomIndexSql { get; set; }
	        
	        public override (string sql, CommandType commandType) GetCreateIndexSql (string indexName, string tableName, string columnName,
		        bool isUnique, CustomTypeMetadata metadata)
	        {
		        if (CustomIndexSql != null)
			        return (CustomIndexSql, CommandType.Execute);
		        
		        return base.GetCreateIndexSql(indexName, tableName, columnName, isUnique, metadata);
	        }

	        public string CustomSelectExpression { get; set; }

	        public override string GetSelectExpression (string columnName, CustomTypeMetadata metadata)
	        {
		        if (CustomSelectExpression != null)
			        return string.Format (CustomSelectExpression, columnName);
		        
		        return base.GetSelectExpression(columnName, metadata);
	        }
        }
    }
}
