using System;
using System.Linq;
using System.Runtime.Serialization;
using Chronos;
using Xunit;

namespace Test
{
    public class TsvFormatterTests
    {

        [Fact(DisplayName = "Can Deserialize to obj with primitive types")]
        public void CanDeserializeToObjectWithPrimitives()
        {
            const string tsv = "Kyle\t1\t8.6\tanother string";

            var obj = tsv.FromTsv<TestTsvObject>(stringIncludesHeader: false).Single();

            Assert.Equal("Kyle", obj.Name);
            Assert.Equal(1, obj.TestDataA);
            Assert.Equal(8.6m, obj.TestDataB);
            Assert.Equal("another string", obj.TestDataC);
        }

        [Fact(DisplayName = "Can Deserialize to obj when using a different delimiter")]
        public void DifferentDelimiter()
        {
            const string tsv = "Kyle\t|\t1\t|\t8.6\t|\tanother string";

            TsvConfig.Delimiter = "\t|\t";
            var obj = tsv.FromTsv<TestTsvObject>(stringIncludesHeader: false).Single();

            Assert.Equal("Kyle", obj.Name);
            Assert.Equal(1, obj.TestDataA);
            Assert.Equal(8.6m, obj.TestDataB);
            Assert.Equal("another string", obj.TestDataC);
        }


        [Fact(DisplayName = "Can deserialize with null or blank values")]
        public void CanDeserializeWithNullValues()
        {
            TsvConfig.Delimiter = "\t|\t";
            var tsv =
                @"0ec419f3-ce19-434a-8e52-568dbf290184	|	9/2/2014 5:33:21 PM	|	00000000-0000-0000-0000-000000000000	|	b10dc4a4-d477-4fa6-a1d9-940d956071e8	|	8a450df6-9884-0f51-9642-56ba8fc4f211	|	20140902	|	1	|	masp	|	355213042050385	|	6f0cf4fb87dc9cdd	|	US	|	71.47.242.109	|		|	
73d86193-e58e-4a6d-982c-fa8599ec25e1	|	9/2/2014 5:33:22 PM	|	00000000-0000-0000-0000-000000000000	|	06c70439-63cf-43c2-8e1e-8d41bd96b70d	|	ccceb993-952c-6669-7d0c-e68fcffce1d7	|	20140902	|	1	|	masp	|	355213042050385	|	6f0cf4fb87dc9cdd	|	US	|	71.47.242.109	|		|	
30884161-86d8-4977-a6e5-b817a8c64bd5	|	9/2/2014 5:33:29 PM	|	00000000-0000-0000-0000-000000000000	|	c819d237-d412-40e1-b236-1ae01a7561e0	|	0b28f203-0ba8-e4c0-9cfd-ced469a0ff0e	|	20140902	|	1	|	masp	|		|	1a1abd94c819a1a3	|	US	|	66.86.60.10	|		|	";

            var objs = tsv.FromTsv<ExampleObject2>(false);

            Assert.NotNull(objs);
            Assert.Equal(3, objs.Count);
        }

    }
    public class ExampleObject2
    {
        [DataMember(Order = 1)]
        public Guid Id { get; set; }

        [DataMember(Order = 2)]
        public DateTime Timestamp { get; set; }

        [DataMember(Order = 3)]
        public Guid TrackingId { get; set; }

        [DataMember(Order = 4)]
        public Guid UserId { get; set; }

        [DataMember(Order = 5)]
        public Guid Hash { get; set; }

        [DataMember(Order = 6)]
        public string UserClass { get; set; }

        [DataMember(Order = 7)]
        public string Version { get; set; }

        [DataMember(Order = 8)]
        public string Source { get; set; }

        [DataMember(Order = 9)]
        public string DeviceId { get; set; }

        [DataMember(Order = 10)]
        public string AndroidId { get; set; }

        [DataMember(Order = 11)]
        public string Country { get; set; }

        [DataMember(Order = 12)]
        public string Ip { get; set; }

        [DataMember(Order = 13)]
        public Guid? AndroidAdvertisingId { get; set; }
        [DataMember(Order = 14)]
        public string DeviceInfo { get; set; }
    }
    public class TestTsvObject
    {
        [DataMember(Order=1)]
        public string Name { get; set; }
        [DataMember(Order=2)]
        public int TestDataA { get; set; } 

        [DataMember(Order=3)]
        public decimal TestDataB { get; set; }

        [DataMember(Order=4)]
        public string TestDataC { get; set; }
    }
}