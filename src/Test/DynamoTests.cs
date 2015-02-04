using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Runtime;
using Chronos.AWS;
using Xunit;

namespace Test
{
    public class DynamoTests
    {
        private string accessKey = "";
        private string secretKey = "/h";
        [Fact (Skip = "ughhh")]
        public void CanGetJsonFromSecondaryIndex()
        {
            var awsCreds = new BasicAWSCredentials(accessKey,secretKey);
            var client = new Dynamo(awsCreds,RegionEndpoint.USWest2);

            var json = client.Get("dev_conversions", "clickId-index", "clickId",
                new Guid("0886fc2b-9080-463b-801e-9fe17c33539f").ToString());

            Assert.NotNull(json);
        }

        [Fact (DisplayName = "Can write a dictionary to dynamo", Skip="no keys")]
        public void CanWriteDictionary()
        {
            var awsCreds = new BasicAWSCredentials(accessKey,secretKey);
            var client = new Dynamo(awsCreds,RegionEndpoint.USWest2);

            var dict = new Dictionary<string, object>
            {
                {"keyword", "test+keyword"},
                {"ppc", 1.75},
                {"last_modified", DateTime.UtcNow.ToString("O")}
            };

            client.WriteSingle("dev_yahoo_ppc", dict);
        }

        [Fact(DisplayName = "Can get a number from dynamo")]
        public void CanGetNumberANumber()
        {
            var awsCreds = new BasicAWSCredentials(accessKey, secretKey);
            var client = new Dynamo(awsCreds, RegionEndpoint.USWest2);

            var json = client.Get("dev_yahoo_ppc", "keyword", "test+keyword");

            Assert.NotNull(json);
        }

        [Fact(DisplayName = "Can write many objects to dynamo")]
        public void CanWriteManyItems()
        {
            var awsCreds = new BasicAWSCredentials(accessKey, secretKey);
            var client = new Dynamo(awsCreds, RegionEndpoint.USWest2);

            var firstItem = new Dictionary<string, object>
            {
                {"last_modified", DateTime.UtcNow.ToString("O")},
                {"keyword", "test+item"},
                {"ppc", 123.33}
            };

            var secondItem = new Dictionary<string, object>
            {
                {"last_modified", DateTime.UtcNow.AddDays(-2).ToString("O")},
                {"keyword", "test+item+number+2"},
                {"ppc", 13.43}
            };

            var list = new List<Dictionary<string, object>>
            {
                firstItem,
                secondItem
            };
            client.WriteMany("dev_yahoo_ppc",list);
        }
    }
}