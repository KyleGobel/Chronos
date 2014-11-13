using System.IO;
using System.IO.Compression;
using System.Runtime.Remoting.Messaging;
using System.Text;

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

        public static byte[] GZipString(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var ms = new MemoryStream();
            var gzip = new GZipStream(ms, CompressionMode.Compress);
            gzip.Write(bytes, 0, bytes.Length);
            gzip.Close();
            return ms.ToArray();
        }

        public static string UnGZipStream(Stream dataStream)
        {
            var gzip = new GZipStream(dataStream, CompressionMode.Decompress);
            var reader = new StreamReader(gzip, Encoding.UTF8);
            var result = reader.ReadToEnd();
            return result;
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