using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Chronos.Configuration;

namespace Chronos.AWS
{
    public class S3
    {
        private readonly S3ConnectionString _connectionInfo;
        public S3(S3ConnectionString connectionString)
        {
            _connectionInfo = connectionString;
        }

        public S3(string connectionStringOrAppSettingName)
        {
            S3ConnectionString connStr;
            if (S3ConnectionString.TryParse(connectionStringOrAppSettingName, out connStr))
            {
                _connectionInfo = connStr;
            }
            else
            {
                _connectionInfo = ConfigUtilities.GetS3ConnectionString(connectionStringOrAppSettingName);
            }
        }

        public string PutFile(string filepath, string saveAs)
        {
            return PutFile(filepath, saveAs, S3CannedACL.PublicRead);
        }
        public string PutFile(string filepath, string saveAs, S3CannedACL acl)
        {
            using (var client = new AmazonS3Client(_connectionInfo.AccessKey, _connectionInfo.SecretKey, new AmazonS3Config { ServiceURL = "http://s3.amazonaws.com" }))
            {
                var request = new PutObjectRequest
                {
                    BucketName = _connectionInfo.BucketName,
                    FilePath = filepath,
                    CannedACL = acl,
                    Key = _connectionInfo.FolderName + "/" + saveAs
                };

                client.PutObject(request);
                return string.Format("http://s3.amazonaws.com/{0}/{1}/{2}", _connectionInfo.BucketName,
                    _connectionInfo.FolderName, saveAs);
            }
        }


        public string[] ListFiles()
        {
            var response = ListFiles(_connectionInfo);

            if (response != null && response.S3Objects != null)
            {
                return response.S3Objects.Select(x => x.Key).ToArray();
            }
            return null;
        }

        public string[] ListFiles(string folder)
        {
            var response = ListFiles(_connectionInfo.AccessKey, _connectionInfo.SecretKey, _connectionInfo.BucketName, folder);
            if (response != null && response.S3Objects != null)
            {
                return response.S3Objects.Select(x => x.Key).ToArray();
            }
            return null;
        }

        public string[] ListFiles(string bucket, string folder)
        {
             var response = ListFiles(_connectionInfo.AccessKey, _connectionInfo.SecretKey, bucket, folder);
            if (response != null && response.S3Objects != null)
            {
                return response.S3Objects.Select(x => x.Key).ToArray();
            }
            return null;
        }

        public static ListObjectsResponse ListFiles(S3ConnectionString connectionString)
        {
            return ListFiles(connectionString.AccessKey, connectionString.SecretKey, connectionString.BucketName,
                connectionString.FolderName);
        }
        public static ListObjectsResponse ListFiles(string accessKey, string secretKey, string bucketName, string folderName)
        {
            var client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey,
                new AmazonS3Config {ServiceURL = "http://s3.amazonaws.com"});

            var request = new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = folderName
            };

            return client.ListObjects(request);
        }
    }
}