using System.IO;
using System.IO.Compression;

namespace Chronos
{
    public class Compression
    {
        public static string GZipStringToFile(string filename, string value)
        {
            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, value);
            return GZipFile(tmp, filename);
        }

        public static string GZipFile(string src, string dest)
        {
            using (Stream fs = File.OpenRead(src))
            using (Stream fd = File.Create(dest))
            using (Stream csStream = new GZipStream(fd, CompressionMode.Compress))
            {
                fs.CopyTo(csStream);
            }
            return dest;
        }

        public static void UnGZipFileToFile(string compressedfile, string destinationFilename)
        {
            using (Stream fd = File.Create(destinationFilename))
            using (Stream fs = File.OpenRead(compressedfile))
            using (Stream csStream = new GZipStream(fs, CompressionMode.Decompress))
            {
                csStream.CopyTo(fd);
            }
        }
    }
}