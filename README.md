Chronos
=======

Chronos is a time saving library with extension methods I use often or find my self re-writing often.

1/31/2015

Protocol Buffers
=============================
Added new project Chronos.ProtoBuffers that adds some extensions and things for using the proto-buf format from google.  This takes a dependency on protobuf-net.

Same as with the TSV serializer, you'll need the order attributes on your pocos.

```csharp
public class MyObj
{
	[Order(1)]
	public string FirstName {get; set;}

	[Order(2)]
	public string LastName {get; set;}

	[Order(3)]
	public int Age {get; set;}
}

var objToSerialize = new MyObj 
{
	FirstName = "Kyle",
	LastName = "Gobel",
	Age = 29
};
```

Then a couple ways to serialize
```csharp
var protoBufByteArray = objToSerialize.ToProtoBufByteArray();
objToSerialize.ToProtoBufFile(@"C:\mySavedObj.bin");
```

Deserialization is what you would expect
```csharp
var myObj = protoBufByteArray.FromProtoBufByteArray();
var myObjFromFile = @"C:\mySavedObj.bin".FromProtoBufFile();
```

Added a nice profiling method taken from Sam Saffron and a stack overflow wiki
http://stackoverflow.com/questions/1047218/benchmarking-small-code-samples-in-c-can-this-implementation-be-improved

changed the ``Console.Write`` to instead use an ``Action<string>`` (and defaults to Console.Write if nothing is supplied) for easier writing to a file or piping the output somewhere.

```csharp
Profiling.Profile("Quick and dirty profile of some code", 10, () => { /*profiling code */ });
```

Added overloads for Sha1, Sha256, and Md5 to accept byte arrays


Dynamo
=====================
DynamoDb from AWS is pretty cool, except it's the hardest thing ever to use or get data out of (look at the implementation to get a single item).  

Will continue to add methods as I need them myself.

Currently there is just a single method to get a json object document from a table using an index.

```csharp
var client = new Dyanmo(new BasicAWSCredentials(_accessKey, _secretKey), RegionEndpoint.USWest2);

//this will lookup the record in tableName where the column id equals 7
var json = client.GetSingle("tableName", "indexName", "id", "7");
```
(more dynamo stuff coming soon..it's in the Chronos.AWS nuget package)



DateTime Releated Extensions 
===========================

**StartOfDay / EndOfDay**
```csharp
//2014-1-1
var testDate = new DateTime(2014, 1, 1);

//2014-1-1 0:0:0
testDate.StartOfDay();

//2014-1-1 23:59:59.9999999
testDate.EndOfDay();
```

**StartOfWeek / EndOfWeek**
```csharp
//2014-1-1
var testDate = new DateTime(2014, 1, 1);

//2013-12-29 0:0:0
testDate.StartOfWeek();

//2014-1-4 23:59:59.9999999
testDate.EndOfWeek();
```
**Type Extensions**
```csharp
//Nullable<DateTime>
typeof(DateTime).GetNullableType();
```

**ToUnixTimestamp / FromUnixTimestap**

```csharp
//2014-1-1 @ 0:0:0 UTC
var testDate = new DateTime(2014, 1, 1);

//1388534400
testDate.ToUnixTimestamp();

const long timeStamp = 1388534400;

//2014-1-1 @ 0:0:0 UTC
DateTime dateTime = timeStamp.FromUnixTimeStamp();
```

**IsBetween**

The IsBetween method will tell you if a date is between two other dates, if you'd like it to also compare the times, then you can set the optional third parameter ``compareTime`` to true
```csharp
var startDate = DateTime.Parse("2010-1-1");

var endDate = DateTime.Parse("2010-1-3");

var jan2nd = DateTime.Parse("2010-1-2");
var jan4th = DateTime.Parse("2010-1-4");

//true
jan2nd.IsBetween(startDate, endDate);

//false
jan4th.IsBetween(startdate, endDate);


startDate = DateTime.Parse("2010-1-1 1:30 AM");

endDate = DateTime.Parse("2010-1-1 2:00 AM");

//2010-1-1 1:45 AM
var testDate = DateTime.Parse("2010-1-1 1:45 AM");

//true
testDate.IsBetween(startDate, endDate, true);


testDate = DateTime.Parse("2010-1-1 3:00 AM");

//notice when we don't specify the compareTime boolean
testDate.IsBetween(startDate, endDate); //true
testDate.IsBetween(startDate, endDate, true); //false
```

Bulk Inserter
===========================
Makes it easy and fast to do bulk inserts

```csharp
public class Person
{
  public string FullName {get; set;}
  public int Age {get; set;}
}

public void Main() 
{
  var bcp = new BulkInserter<Person>("connectionStringOrName");                   //1.
  bcp.ColumnMappings.MapColumnsAsLowercaseUnderscore();                           //2.
  List<Person> data = /* some data */;                                            //3.
  bcp.Insert(data, "people");                                                     //4.
}
```
- **Line 1** Creates a new BulkInsertObject of type ``Person``, passing in either a connection string name, or the connection string itself
- **Line 2** Maps all properties to lowercase_underscore format column names 
    ```
       -- Source Property = ``FullName`` 
       -- Destination Column =  ``full_name``

       -- Source Property = ``Age`` 
       -- Destination Column =  ``age``
    ```

       Other options are
       ``bcp.ColumnMappings.MapColumnsAsCamelCase();``
   
       Also you can map single columns fluently if that's your thing
   
    ```csharp
       bcp.ColumnMappings
            .MapColumn(col => col.FullName, "Name")
            .MapColumn(col => col.Age, "YearsAlive");
    ```

- **Line 3** Any list of ``Person`` to insert
- **Line 4** Run the bulk copy, inserting all data in the ``data`` object, into the table named ``people``

Enumerable Extensions
===========================

**ForEach**

Used to just peform an Action on every item in an enumerable

**DistinctBy**

Used to ``Distinct`` on a certain property, rather than having to group by and selecting first,
this implementation uses a HashSet

S3 Connection String
===========================
There is no such thing as an Amazon s3 connection string, but there should be.
Instead of storing like 900 values in app/web.config files for s3 connections I prefer to store them in a single value

```xml
<add key="s3ConnectionStringName" value="s3://myAccessKey:mySecretKey@MyBucket/And/Some/Folder" />
```

```csharp

var s3Connection = ConfigUtils.GetS3ConnectionString("s3ConnectionStringName");

/*
s3Connection.AccessKey = "myAccessKey";
s3Connection.SecretKey = "mySecretKey";
s3Connection.BucketName = "MyBucket";
s3Connection.FolderName = "And/Some/Folder";
*/
```

RabbitMqConnection String
===========================
Rabbit does have the ampq connection strings, but i wanted something easier for myself...works the same as the s3 connection

```xml
<add key="rabbitConnStringName" value="rabbitMq://host:port:username@password"/>
```

```csharp
var rabbitConnDetails = ConfigUtilities.GetRabbitMqConnectionString("rabbitConnStringName");
```

Chronos.RabbitMq
====================

Rabbit isn't all that easy to work with, lots of code needed to do simple things.  This package allows for easy queuing.  This method uses no extra exchanges, no bindings,and very few options available.  Uses the default rabbit direct exchange, and decent defaults for your standard worker queue pattern.

```csharp
public class ExampleMessage
{
    public string Message {get; set;}
    public DateTime Timestamp {get; set;}
}


//create the mq object
IMessageQueue mq = new RabbitQueue(_rabbitConnectionString);

//publish a message
mq.PublishMessage(new ExampleMessage { Message = "test message", Timestamp = DateTime.UtcNow });

//handle a message (will block for the set timeout waiting to receive a message: default 3 seconds)
bool receivedMessage = false;
mq.HandleMessage<ExampleMessage>(msg => 
{
    //do something here to handle the message
    if (msg.Message == "test message")
    {
         //returning true will Ack the message
         return true;
    }
    else
    {
        //returning false will Nack the message
        return false;
    }
}, out receivedMessage);

//receivedMessage will be true if a message was actually handled
//will be false if the timeout expired with no message
```

The last method of the simple interface will continually handle messages (infinite loop), there is no timeout, as it will wait forever, and the method will never return, you would probably want to start this in a background thread.

```csharp

mq.HandleMessages<ExampleMessage>(msg => 
{
    //handle message here
    return true;
});

```

Hashing
=================
Other small stuff that is annoying to look up and write

MD5, SHA1, SHA256

```csharp
string text = "I need to encode this!";

text.ToMd5(); //3e7a1b12be59eb75f386eca14ba73b15
text.ToSha1(); //36a771801c21e634cd780ce8b262bc748911eba9
text.ToSha256(); //ce5676bba712e862a111083c07e51256cc4be1fbff0fe4ad657f143f9c626e01
```

Compressing
=================
A couple additions for compressing files using gzip.
Considering adding 7z support prolly via the command line utility, but would then have to include 7za.exe prolly as embedded resource or something, which would increase the size of the library by alot...still thinking about solutions for this

here's an example using gzip

```csharp
string compressedFileName = Compression.GZipFileToFile("someFile.txt", "someFile.txt.gz"); //compressed
Compression.UnGZipFileToFile(compressedFileName, "someFile.txt");
```


Sql Metadata
==================
Several extensions were added to help me out in some other recent projects

```csharp
List<TableMetadata> IDbConnection.GetTables()
``` 
this returns a list of ``TableMetadata`` of all the tables.


``TableMetadata`` looks like this

```csharp
public class TableMetadata
{
  public string Database { get; set; }
  public string Schema { get; set; }
  public string Table { get; set; }
}
```

```csharp
List<IDbDataParameter>IDbConnection.GetStoredProcedureParams(string storedProcName)
``` 
much like this one sounds, it will return a list of parameters that this stored procedure accepts


```csharp
bool? SqlConnection.IsColumnNullable(string table, string columnName)
```
returns whether or not the column is nullable

```csharp
List<ColumnMetadata> SqlConnection.GetColumnMetadata(string schema, string tableName)
```
returns a list of ``ColumnMetadata`` about a table

``ColumnMetadata`` looks like this

```csharp
public class ColumnMetadata
{
  public string Name { get; set; }
  public Type Type { get; set; }
  public DbType DbType { get; set; }
  public int Length { get; set; }
}
```

```csharp
Dictionary<string, Type> SqlConnection.GetColumnTypesFromQuery(string sql)
``` 
this will take a query and return a <columnName>, <C# Type> dictionary..i.e

```csharp
using (var connection = new SqlConnection(connStr))
{
  connection.Open();
  var typeDictionary = connection.GetColumnTypesFromQuery("select id, name from products");
  
  //typeDictionary contains::
  // { "id", typeof(Int32) }
  // { "name", typeof(String)}
}
```

