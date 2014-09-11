using System.IO;
using Chronos;
using Xunit;

namespace Test
{
    public class CompressionTests
    {
        [Fact(DisplayName = "Gzip compressess and decompresses correctly")]
        public void CompDecomp()
        {
            const string testString = "two things are infinite:";

            //dumb way to test this but who cares
            var tmpFile = Path.GetTempFileName();
            Compression.GZipStringToFile(tmpFile, testString);

            Compression.UnGZipFileToFile(tmpFile, tmpFile+".unzipped");

            var result = File.ReadAllText(tmpFile + ".unzipped");
            Assert.Equal(testString, result);
        }
    }
}