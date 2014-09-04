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