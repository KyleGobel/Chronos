using Chronos.Configuration;
using Xunit;

namespace Test
{
    public class ConfigurationTests
    {
        [Fact]
        public void Test()
        {
            var result = ConfigUtilities.GetAppSetting("test", "");
        }

        [Fact (DisplayName="RabbitMqConnectionString.Parse method parses a rabbit connection string")]
        public void RabbitMqConnectionStringTest()
        {
            var obj = RabbitMqConnectionString.Parse("rabbitMq://host.com:1234:testUn@testPw");

            Assert.Equal("host.com", obj.Host);
            Assert.Equal("testPw", obj.Password);
            Assert.Equal(1234, obj.Port);
            Assert.Equal("testUn", obj.Username);

        }

        [Fact(DisplayName = "S3ConnectionString.Parse method parses an s3 connection string")]
        public void ParseS3ConnString()
        {
             var obj = S3ConnectionString.Parse("s3://accessKey:secretKey@bucket/path/to/folder");

            Assert.Equal("accessKey", obj.AccessKey);
            Assert.Equal("secretKey", obj.SecretKey);
            Assert.Equal("bucket", obj.BucketName);
            Assert.Equal("path/to/folder", obj.FolderName);

        }
    }
}