using System.Reflection;
using Chronos;
using Xunit;

namespace Test
{
    public class EmbeddedResourcesTests
    {
        [Fact]
        public void CanGetResourceName()
        {
            Assembly asm;
            var result = EmbeddedResource.FindFullyQualifiedName("TestQuery.sql", out asm);

            Assert.NotNull(result);
            Assert.NotNull(asm);
        }
    }
}