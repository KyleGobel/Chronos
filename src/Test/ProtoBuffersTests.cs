using System;
using System.Security;
using System.Text;
using Chronos;
using Chronos.ProtoBuffers;
using Xunit;

namespace Test
{
    public class ProtoBuffersTests
    {
        [Fact(DisplayName = "Can serialize/deserialize to proto buf using chronos order attributes")]
        public void CanSerializeAndDeserializeObjWithOrderAttributes()
        {
            var obj = new MyObj
            {
                PropA = 8,
                PropB = "some value"
            };

            var bytes = obj.ToProtoBufByteArray();

            Assert.NotNull(bytes);

            var newObj = bytes.FromProtoBufByteArray<MyObj>();

            Assert.Equal(obj.PropA, newObj.PropA);
            Assert.Equal(obj.PropB, newObj.PropB);
        }

        [Fact(DisplayName = "Can read/write proto buff to a file")]
        public void CanReadWriteProtoBufToFile()
        {
            var obj = new MyObj
            {
                PropA = 8,
                PropB = "some value"
            };

            obj.ToProtoBufFile("test.bin");

            var newObj = "test.bin".FromProtoBufFile<MyObj>();

            Assert.Equal(obj.PropA, newObj.PropA);
            Assert.Equal(obj.PropB, newObj.PropB);
        }

        [Fact(DisplayName = "If item isn't a cache obj null is returned")]
        public void ReturnsNullForNonExistantItems()
        {
            var bytes = new byte[] {};
            Assert.Null(bytes.FromCache<MyObj>());

            var nullObj = default(byte[]);
            Assert.Null(nullObj.FromCache<MyObj>());

            var nonEmptyBytes = Encoding.UTF8.GetBytes("blah blah blah");
            Assert.Null(nonEmptyBytes.FromCache<MyObj>());
        }
    }

    public class MyObj
    {
        [Order(10)]
        public int PropA { get; set; }
        [Order(3)]
        public string PropB { get; set; }
    }
}