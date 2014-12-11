using System;
using Amazon;
using Amazon.Runtime;
using Chronos.AWS;
using Xunit;

namespace Test
{
    public class DynamoTests
    {
        private string accessKey = "You Access Key";
        private string secretKey = "Your Secret Key";
        [Fact (Skip = "ughhh")]
        public void CanGetJsonFromSecondaryIndex()
        {
            var awsCreds = new BasicAWSCredentials(accessKey,secretKey);
            var client = new Dynamo(awsCreds,RegionEndpoint.USWest2);

            var json = client.GetSingle("dev_conversions", "clickId-index", "clickId",
                new Guid("0886fc2b-9080-463b-801e-9fe17c33539f").ToString());

            Assert.NotNull(json);
        }
    }
}