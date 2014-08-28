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

```
public class Person
{
  public string FullName {get; set;}
  public int Age {get; set;}
}

public void Main() 
{
  var bcp = new BulkInserter("connectionStringOrName");
  
  //maps all properties to lowercase_underscore format column names 
  //FullName - full_name
  //Age - age
  bcp.ColumnMappings.MapColumnsAsLowercaseUnderscore();
  
  List<Person> data = /* some data */;
  
  //will bulk copy all the Persons in data to the db table dbo.people
  bcp.Insert(data, "dbo.people");
}
```

Enumerable Extensions
===========================

**ForEach**

Used to just peform an Action on every item in an enumerable

**DistinctBy**

Used to ``Distinct`` on a certain property, rather than having to group by and selecting first,
this implementation uses a HashSet


App.Config or Web.Config helper methods
==========================
View source, will show examples soon

Console Application Helper Templates
==========================
View source, will show example soon

