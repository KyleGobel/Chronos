using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Chronos.Configuration;
using ServiceStack.Logging;

namespace Chronos.AWS
{
    public class S3
    {
        private readonly S3ConnectionString _connectionInfo;
        private static readonly ServiceStack.Logging.ILog Log = LogManager.GetLogger(typeof(S3));
        public bool LoggingEnabled = true;
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

        private const string FilepathPattern =
            @"^[\/\\]*(?<folder>.+[\/\\])*(?<filename_without_extension>.+)[\.](?<extension>.+)$";


        public void DownloadFiles(string s3FolderName, string saveFolder, bool deleteFromS3AfterDownload)
        {
            DownloadFiles(_connectionInfo.AccessKey, _connectionInfo.SecretKey, _connectionInfo.BucketName, s3FolderName, saveFolder, deleteFromS3AfterDownload);
        }

        public static void DownloadFiles(S3ConnectionString connectionInfo, string savePath, bool remveFromS3AfterDownload,
            Action<GetObjectResponse> onFileDownloaded = null, Action<DeleteObjectResponse> onFileDeleted = null)
        {
            DownloadFiles(connectionInfo.AccessKey, connectionInfo.SecretKey, connectionInfo.BucketName,
                connectionInfo.FolderName, savePath, remveFromS3AfterDownload, onFileDownloaded, onFileDeleted);
        }

        public void DownloadFiles(string s3FolderName, string saveFolder)
        {
            DownloadFiles(_connectionInfo.AccessKey, _connectionInfo.SecretKey, _connectionInfo.BucketName, s3FolderName, saveFolder, false);
        }
        public static void DownloadFiles(string accessKey, string secretKey, string bucketName, string s3FolderName, string saveFolder, bool removeFromS3AfterDownload, Action<GetObjectResponse> onFileDownload = null, Action<DeleteObjectResponse> onFileDelete = null)
        {
            if (!Directory.Exists(saveFolder))
            {
                Log.ErrorFormat("Couldn't find folder {0}", saveFolder);
                throw new ArgumentException(string.Format("Could not find folder {0}", saveFolder));
            }

            using (var client = new AmazonS3Client(accessKey, secretKey,
                new AmazonS3Config { ServiceURL = "http://s3.amazonaws.com" }))
            {
                var marker = default(string);
                do
                {
                    var listResponse = ListFiles(accessKey, secretKey, bucketName, s3FolderName, marker);
                    foreach (var f in listResponse.S3Objects.Select(x => x.Key))
                    {
                        var req = new GetObjectRequest
                        {
                            BucketName = bucketName,
                            Key = f
                        };

                        var match = Regex.Match(f, FilepathPattern);

                        var filename = match.Groups["filename_without_extension"].Value;
                        var extension = match.Groups["extension"].Value;
                        var savePath = Path.Combine(saveFolder, filename + "." + extension);
                        var transferPath = savePath + ".tran";
                        Log.DebugFormat("Downloading '{0}' to '{1}'", bucketName + "/" + f, transferPath);
                        var res = client.GetObject(req);

                        if (onFileDownload != null)
                        {
                            Log.Debug("Running onFileDownload filter");
                            onFileDownload(res);
                        }

                        res.WriteResponseStreamToFile(transferPath, false);

                        if (removeFromS3AfterDownload)
                        {
                            var deleteRequest = new DeleteObjectRequest
                            {
                                BucketName = bucketName,
                                Key = f
                            };
                            Log.DebugFormat("Deleting '{0}' from S3", bucketName + "/" + f);
                            var deleteResponse = client.DeleteObject(deleteRequest);
                            if (onFileDelete != null)
                            {
                                Log.Debug("Running onFileDelete filter");
                                onFileDelete(deleteResponse);
                            }
                        }

                        //try to move the file to it's original save spot
                        Log.DebugFormat("Moving file '{0}' to '{1}'", transferPath, savePath);

                        for (var retryCount = 0; retryCount < 3; retryCount++)
                        {
                            try
                            {
                                File.Move(transferPath, savePath);
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (retryCount == 2)
                                {
                                    Log.Error("Failed to move file.  Exceeded retry count", ex);
                                    throw;
                                }

                                Log.ErrorFormat("Failed to move file from '{0}', to '{1}'.  Retry: {2}",
                                    transferPath, savePath, retryCount);

                                Thread.Sleep(1000);
                            }
                        }
                    }
                    marker = listResponse.IsTruncated ? listResponse.NextMarker : default(string);
                } while (marker != default(string));
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
        public static ListObjectsResponse ListFiles(string accessKey, string secretKey, string bucketName, string folderName, string marker = null)
        {
            var client = AWSClientFactory.CreateAmazonS3Client(accessKey, secretKey,
                new AmazonS3Config { ServiceURL = "http://s3.amazonaws.com" });

            var request = new ListObjectsRequest
            {
                BucketName = bucketName,
                Prefix = folderName,
                Marker = marker
            };

            return client.ListObjects(request);
        }
    }
}