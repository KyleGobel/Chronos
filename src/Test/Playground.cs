using Chronos.Configuration;
using Chronos.Interfaces;
using Chronos.RabbitMq;
using ServiceStack;
using Xunit;

namespace Test
{
    public class Playground
    {
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