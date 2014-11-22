using System.Collections.Generic;
using Chronos.Configuration;
using Chronos.RabbitMq;
using Xunit;

namespace Test
{
    public class RabbitMqTests
    {
        [Fact(DisplayName="Can publish list of messages to rabbitmq", Skip="Build server has no queue server")]
        public void CanPublishMessages()
        {
            var mq = new RabbitQueue("a valid connection string here");
            var testItems = new List<TestItems>
            {
                {new TestItems { Id =1, Url = "http://www.google.com"}},
                {new TestItems { Id=2, Url = "http://www.yahoo.com"}}
            };
            Assert.DoesNotThrow(() => mq.PublishMessages(testItems));
        }
    }
    public class TestItems
    {
        public long Id { get; set; }
        public string Url { get; set; }
    }
}