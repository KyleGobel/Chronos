Chronos
=======

Chronos is a time saving library with extension methods I use often or find my self re-writing often.

Only dependant on the .net framework.


DateTime Releated Extensions 
===========================

StartOfDay / EndOfDay
```cs
//2014-1-1
var testDate = new DateTime(2014, 1, 1);

//2014-1-1 0:0:0
testDate.StartOfDay();

//2014-1-1 23:59:59.9999999
testDate.EndOfDay();
```

ToUnixTimestamp / FromUnixTimestap

```cs
//2014-1-1 @ 0:0:0 UTC
var testDate = new DateTime(2014, 1, 1);

//1388534400
testDate.ToUnixTimestamp();

const long timeStamp = 1388534400;

//2014-1-1 @ 0:0:0 UTC
DateTime dateTime = timeStamp.FromUnixTimeStamp();
```

more descriptions/docs when i get more time

