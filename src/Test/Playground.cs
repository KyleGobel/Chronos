using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Chronos;
using Chronos.Configuration;
using Chronos.Interfaces;
using Chronos.RabbitMq;
using ServiceStack;
using Xunit;

namespace Test
{
    public class TestModel
    {
        public int Id { get; set; }
    }
    public class Playground
    {
        [Fact(Skip="Internal Only (buld server doesn't have database configured")]
        public void CanBulkInsertNonGenerically()
        {
            var tableName = "TestMe";

            IEnumerable nonGeneric = new List<TestModel> {new TestModel {Id = 1}, new TestModel {Id = 2}};

            var bcp = new SqlServerBulkInserter("Server=localhost;Database=Main;Trusted_Connection=True;", typeof (TestModel));
            bcp.Insert(nonGeneric, tableName);


            //old fashionly open SSMS and check to see if items were inserted :)
        }
        [Fact(Skip="Internal Only (build server doesn't have rabbit installed)")]
        public void RabbitPublishTest()
        {
            IMessageQueue mq = new RabbitQueue(RabbitMqConnectionString.Parse("rabbitMq://localhost:5672:guest@guest"), new ServiceStackJsonSerializer(), true);
            mq.PublishMessage(new TestMessage { Message = "testing"});
        }

        [Fact(Skip="Internal Only (build server doesn't have rabbit installed)")]
        public void HandleTest()
        {
            IMessageQueue mq = new RabbitQueue(RabbitMqConnectionString.Parse("rabbitMq://localhost:5672:guest@guest"), new ServiceStackJsonSerializer(), true);

            bool receivedMessage = false;
            mq.HandleMessage<TestMessage>(x =>
            {
                Assert.Equal("testing", x.Message);
                return true;
            },out receivedMessage);

            Assert.True(receivedMessage);
        }
    }

    public class TestMessage
    {
        public string Message { get; set; }
    }
    public class ServiceStackJsonSerializer : ISerializer
    {
        public T Deserialize<T>(string s)
        {
            return s.FromJson<T>();
        }

        public string Serialize<T>(T obj)
        {
            return obj.ToJson();
        }
    }
}