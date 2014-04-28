using Chronos.Configuration;
using Xunit;

namespace Test
{
    public class ConfigurationTests
    {
        [Fact]
        public void Test()
        {
            var result = ConfigUtils.GetAppSetting("test", "");
        }
    }
}