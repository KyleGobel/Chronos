Chronos
=======

Chronos is a time saving library with extension methods I use often or find my self re-writing often.

Only dependant on the .net framework.


DateTime Releated Extensions 
===========================

**StartOfDay / EndOfDay**
```cs
//2014-1-1
var testDate = new DateTime(2014, 1, 1);

//2014-1-1 0:0:0
testDate.StartOfDay();

//2014-1-1 23:59:59.9999999
testDate.EndOfDay();
```

**StartOfWeek / EndOfWeek**
```cs
//2014-1-1
var testDate = new DateTime(2014, 1, 1);

//2013-12-29 0:0:0
testDate.StartOfWeek();

//2014-1-4 23:59:59.9999999
testDate.EndOfWeek();
```

**ToUnixTimestamp / FromUnixTimestap**

```cs
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
```cs
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

```cs
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
   
    ```cs
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

```cs

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

```cs
var rabbitConnDetails = ConfigUtilities.GetRabbitMqConnectionString("rabbitConnStringName");
```

Chronos.RabbitMq
====================

Rabbit isn't all that easy to work with, lots of code needed to do simple things.  This package allows for easy queuing.  This method uses no extra exchanges, no bindings,and very few options available.  Uses the default rabbit direct exchange, and decent defaults for your standard worker queue pattern.

```cs
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

```cs

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

```cs
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

```cs
string compressedFileName = Compression.GZipFileToFile("someFile.txt", "someFile.txt.gz"); //compressed
Compression.UnGZipFileToFile(compressedFileName, "someFile.txt");
```


Sql Metadata
==================
Several extensions were added to help me out in some other recent projects

``List<TableMetadata> IDbConnection.GetTables`` - this returns a list of ``TableMetadata`` of all the tables.

``TableMetadata`` looks like this

```cs
public class TableMetadata
{
  public string Database { get; set; }
  public string Schema { get; set; }
  public string Table { get; set; }
}
```

``List<IDbDataParameter>IDbConnection.GetStoredProcedureParams(string storedProcName)`` - much like this one sounds, it will return a list of parameters that this stored procedure accepts

``bool? SqlConnection.IsColumnNullable(string table, string columnName)`` - returns whether or not the column is nullable

``List<ColumnMetadata> SqlConnection.GetColumnMetadata(string schema, string tableName)`` - returns a list of ``ColumnMetadata`` about a table

``ColumnMetadata`` looks like this

```cs
public class ColumnMetadata
{
  public string Name { get; set; }
  public Type Type { get; set; }
  public DbType DbType { get; set; }
  public int Length { get; set; }
}
```

``Dictionary<string, Type> SqlConnection.GetColumnTypesFromQuery(string sql)`` - this will take a query and return a <columnName>, <C# Type> dictionary..i.e

```cs
using (var connection = new SqlConnection(connStr))
{
  connection.Open();
  var typeDictionary = connection.GetColumnTypesFromQuery("select id, name from products");
  
  //typeDictionary contains::
  // { "id", typeof(Int32) }
  // { "name", typeof(String)}
}
```

