using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chronos.Configuration
{
    public class S3ConnectionString
    {
        private const string Pattern =
            @"^s3://(?<AccessKey>[^:]+):(?<SecretKey>[^@]+)@(?<BucketName>[^/]*)(/(?<FolderName>.*))?$";
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public string FolderName { get; set; }

        public static S3ConnectionString Parse(string s)
        {
            var match = Regex.Match(s, Pattern);
            return new S3ConnectionString
            {
                AccessKey = match.Groups["AccessKey"].Value,
                SecretKey = match.Groups["SecretKey"].Value,
                BucketName = match.Groups["BucketName"].Value,
                FolderName = match.Groups["FolderName"].Value
            };
        }

        public static S3ConnectionString Empty = new S3ConnectionString
        {
            AccessKey = "",
            FolderName = "",
            BucketName = "",
            SecretKey = ""
        };

        public static bool TryParse(string s, out S3ConnectionString connStr)
        {
            var match = Regex.Match(s, Pattern);

            if (match.Success)
            {
                connStr = Parse(s);
                return true;
            }
            connStr = S3ConnectionString.Empty;
            return false;
        }
    }
}
