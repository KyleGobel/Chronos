using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Chronos.Configuration;
using Chronos.Dapper.Chronos.Dapper;
using Npgsql;
using ServiceStack.Logging;

namespace Chronos.AWS
{
    public class S3
    {
        private readonly S3ConnectionString _connectionInfo;
        private static readonly ILog Log = LogManager.GetLogger(typeof(S3));
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

        public void CompressAndUploadStringData(string data, string savePath)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException("data");
            }
            if (string.IsNullOrEmpty(savePath))
            {
                throw new ArgumentException("savePath");
            }

            try
            {
                var bytes = Compression.GZipString(data);
                UploadBytesToS3(bytes, savePath);
            }
            catch (Exception x)
            {
                Log.Error("Error uploading to s3", x);
                throw;
            }
        }
        /// <summary>
        /// Uploads bytes to s3 to the bucket of the connection string 
        /// </summary>
        /// <param name="data">the bytes to upload</param>
        /// <param name="savePath">the place to save the file on s3, will be appened with .gz</param>
        public void UploadBytesToS3(byte[] data, string savePath)
        {
            var req = new PutObjectRequest
            {
                BucketName = _connectionInfo.BucketName,
                Key = savePath + ".gz",
                InputStream = new MemoryStream(data)
            };
            using (var client = new AmazonS3Client(_connectionInfo.AccessKey, _connectionInfo.SecretKey,
                    new AmazonS3Config {ServiceURL = "http://s3.amazonaws.com"}))
            {
                client.PutObject(req);
            }
        }
        public static string GetFilename(string path)
        {
            const string pattern = @"(?<filename>[^\/]+)$";
            return Regex.Match(path, pattern).Groups["filename"].Value;
        }

        public static string Combine(params string[] paths)
        {
            var path = string.Empty;
            if (paths != null && paths.Length > 0)
            {
                path = paths[0];
                for (var i = 1; i < paths.Length; i++)
                {
                    if (!string.IsNullOrEmpty(paths[i]))
                    {
                        path = string.Format("{0}/{1}", path.TrimEnd('/'), paths[i]).TrimStart('/');
                    }
                }
            }
            return path;
        }


        /// <summary>
        /// Copy from s3 to redshift, performing either a merge or a copy depending on the config object
        /// </summary>
        /// <param name="config">The configuration for the copy, the only option property is the primary key columns if you are copying instead of merging</param>
        public void LoadRedshift(LoadRedshiftConfig config)
        {
            if (config.PerformMerge)
            {
                 MergeToRedshift(config.DataDirectoryPrefix, config.DataFileExtension, config.ProcessingDirectoryPrefix,
                    config.CompletedDirectoryPrefix, config.ErrorDirectoryPrefix, config.Delimeter, config.TableName, config.TableColumns,
                    config.PrimaryKeyColumns, config.ConnectionStringName,config.HeaderRowCount, config.ErrorLog, config.DebugLog);               
            }
            else
            {
                CopyToRedshift(config.DataDirectoryPrefix, config.DataFileExtension, config.ProcessingDirectoryPrefix,
                    config.CompletedDirectoryPrefix, config.ErrorDirectoryPrefix, config.Delimeter, config.TableName, config.TableColumns,
                    config.ConnectionStringName, config.HeaderRowCount, config.ErrorLog, config.DebugLog);
            }
        }

        /// <summary>
        /// Copy from s3 to redshift
        /// </summary>
        /// <param name="dataPrefix">the s3 location where the data files are located</param>
        /// <param name="dataFileExtension">the extension of the files to insert</param>
        /// <param name="processingPrefix">the s3 location where the data files are moved during processing</param>
        /// <param name="completedPrefix">the s3 location where the data files are moved after copy success</param>
        /// <param name="errorPrefix">the s3 location where the data files are moved if there is an error</param>
        /// <param name="delimeter"></param>
        /// <param name="tableName">the redshift table to copy to</param>
        /// <param name="columnList">the columns of the table to copy into, same order as the data files</param>
        /// <param name="primaryKeyColumns">the columns that are primary keys and should be used to figure out how to merge</param>
        /// <param name="connectionStringName">the connection string or connection string name of the redshift database</param>
        /// <param name="headerRowCount">how many of the rows in the text file are header rows</param>
        /// <param name="errorLog">function to log errors</param>
        /// <param name="debugLog">function for debug logging</param>
        public void MergeToRedshift(string dataPrefix, string dataFileExtension, 
            string processingPrefix, string completedPrefix, string errorPrefix, string delimeter,string tableName, string[] columnList, string[] primaryKeyColumns, string connectionStringName,
            int headerRowCount,Action<Exception,string> errorLog, Action<string> debugLog)
        {
            debugLog(string.Format("Getting files with extension {0} from {1}", dataFileExtension, dataPrefix));
            var files = ListFiles(dataPrefix)
                    .Where(x => x.EndsWith(dataFileExtension))
                    .Take(100)
                    .ToList();

            if (files.Count == 0)
            {
                debugLog("No Files to copy");
                return;
            }
            try
            {
                debugLog(string.Format("Moving {0} files to {1}", files.Count, processingPrefix));
                foreach (var file in files)
                {
                    var pFile = file.Replace(dataPrefix, processingPrefix);
                    MoveFile(file, _connectionInfo.BucketName, pFile, _connectionInfo.BucketName);
                }

                debugLog("Building sql query and primary key check");
                var sql = EmbeddedResource.Get("mergeToRedshift.sql")
                    .Replace("$COLUMNLIST$", string.Join(",", columnList))
                    .Replace("$ACCESSKEY$", _connectionInfo.AccessKey)
                    .Replace("$SECRETKEY$", _connectionInfo.SecretKey)
                    .Replace("$BUCKET$", _connectionInfo.BucketName)
                    .Replace("$TABLENAME$", tableName)
                    .Replace("$DELIMETER$", delimeter)
                    .Replace("$HEADERROWS$", headerRowCount.ToString(CultureInfo.InvariantCulture))
                    .Replace("$COLUMNLIST$", string.Join(",", columnList))
                    .Replace("$PATH$", processingPrefix);

                var primaryKeyCheck = new List<string>();
                var stagingTableName = tableName + "_staging";
                foreach (var pkey in primaryKeyColumns)
                {
                    var tableKey = tableName + ".[" + pkey + "]";
                    var stageKey = stagingTableName + ".[" + pkey + "]";
                    var input = "{tableKey} = {stageKey} or ({tableKey} is null and {stageKey} is null)";
                    input = Regex.Replace(input, "{tableKey}", tableKey);
                    input = Regex.Replace(input, "{stageKey}", stageKey);
                    primaryKeyCheck.Add(input);
                }

                var primaryKeySql = string.Join(" and ", primaryKeyCheck);
                sql = sql.Replace("$PRIMARYKEYCHECK$", primaryKeySql);

                debugLog("Copying files to redshift");
                using (var connection = new NpgsqlConnection(ConfigUtilities.GetConnectionString(connectionStringName)))
                {
                    debugLog("Running SQL: " + sql);
                    connection.Execute(sql, commandTimeout: 1000*60*15);
                }

                debugLog("Moving files to completed directory");
                foreach (var file in files)
                {
                    var processedFile = file.Replace(dataPrefix, processingPrefix);
                    var completeFile = file.Replace(dataPrefix, completedPrefix);
                    MoveFile(processedFile, _connectionInfo.BucketName, completeFile, _connectionInfo.BucketName);
                }
            }
            catch (Exception exception)
            {
                errorLog(exception, "Error in copy to redshift");
                debugLog("Moving files to error directory");
                var filesInProcessing = ListFiles(processingPrefix)
                    .Where(x => x.EndsWith(dataFileExtension))
                    .ToList();

                foreach (var file in filesInProcessing.Select(S3.GetFilename))
                {
                    var processedFile = Combine(processingPrefix, file);
                    var errorFile = Combine(errorPrefix, file);
                    MoveFile(processedFile, _connectionInfo.BucketName, errorFile, _connectionInfo.BucketName);
                }
            }
        }

        public void CopyToRedshift(string dataPrefix, string dataFileExtension, 
            string processingPrefix, string completedPrefix, string errorPrefix,string delimiter, string tableName, string[] columnList, string connectionStringName,
            int headerRowCount,Action<Exception,string> errorLog, Action<string> debugLog)
        {
            debugLog(string.Format("Getting files with extension {0} from {1}", dataFileExtension, dataPrefix));
            var files = ListFiles(dataPrefix)
                    .Where(x => x.EndsWith(dataFileExtension))
                    .Take(100)
                    .ToList();

            if (files.Count == 0)
            {
                debugLog("No Files to copy");
                return;
            }
            try
            {
                debugLog(string.Format("Moving {0} files to {1}", files.Count, processingPrefix));
                foreach (var file in files)
                {
                    var pFile = Combine(processingPrefix, GetFilename(file));
                    MoveFile(file, _connectionInfo.BucketName, pFile, _connectionInfo.BucketName);
                }

                debugLog("Building sql query and primary key check");
                var sql = EmbeddedResource.Get("copyToRedshift.sql")
                    .Replace("$COLUMNLIST$", string.Join(",", columnList))
                    .Replace("$ACCESSKEY$", _connectionInfo.AccessKey)
                    .Replace("$SECRETKEY$", _connectionInfo.SecretKey)
                    .Replace("$BUCKET$", _connectionInfo.BucketName)
                    .Replace("$TABLENAME$", tableName)
                    .Replace("$DELIMETER$", delimiter)
                    .Replace("$HEADERROWS$", headerRowCount.ToString(CultureInfo.InvariantCulture))
                    .Replace("$COLUMNLIST$", string.Join(",", columnList))
                    .Replace("$PATH$", processingPrefix);

                debugLog("Copying files to redshift");
                using (var connection = new NpgsqlConnection(ConfigUtilities.GetConnectionString(connectionStringName)))
                {
                    debugLog("Running SQL: " + sql);
                    connection.Execute(sql, commandTimeout: 1000*60*15);
                }

                debugLog("Moving files to completed directory");
                foreach (var file in files.Select(S3.GetFilename))
                {
                    var processedFile = Combine(processingPrefix, file);
                    var completeFile = Combine(completedPrefix, file);
                    MoveFile(processedFile, _connectionInfo.BucketName, completeFile, _connectionInfo.BucketName);
                }
            }
            catch (Exception exception)
            {
                errorLog(exception, "Error in copy to redshift");
                debugLog("Moving files to error directory");
                var filesInProcessing = ListFiles(processingPrefix)
                    .Where(x => x.EndsWith(dataFileExtension))
                    .ToList();

                foreach (var file in filesInProcessing.Select(S3.GetFilename))
                {
                    var processedFile = Combine(processingPrefix, file);
                    var errorFile = Combine(errorPrefix, file);
                    MoveFile(processedFile, _connectionInfo.BucketName, errorFile, _connectionInfo.BucketName);
                }
            }
        }
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

        public bool FileExists(string path)
        {
            return FileExists(path, _connectionInfo.BucketName);
        }
        public bool FileExists(string path, string bucket)
        {
            using (var s3 = new AmazonS3Client(_connectionInfo.AccessKey, _connectionInfo.SecretKey,
                new AmazonS3Config {ServiceURL = "http://s3.amazonaws.com"}))
            {
                return s3.ListObjects(new ListObjectsRequest
                {
                    BucketName = bucket,
                    Prefix = path,
                    MaxKeys = 1
                })
                    .MaxKeys > 0;
            }
        }
        public bool MoveFile(string sourcePath, string sourceBucket, string destinationPath, string destinationBucket)
        {
            try
            {
                var request = new CopyObjectRequest
                {
                    DestinationBucket = destinationBucket,
                    DestinationKey = destinationPath,
                    SourceKey = sourcePath,
                    SourceBucket = sourceBucket
                };

                using (var s3 = new AmazonS3Client(_connectionInfo.AccessKey, _connectionInfo.SecretKey,
                        new AmazonS3Config {ServiceURL = "http://s3.amazonaws.com"}))
                {
                    var response = s3.CopyObject(request);
                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        s3.DeleteObject(new DeleteObjectRequest() {BucketName = sourceBucket, Key = sourcePath});
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Error Moving file", ex);
                return false;
            }
        }

        public Stream GetFileStream(string keyName)
        {
            using (var s3 = new AmazonS3Client(_connectionInfo.AccessKey, _connectionInfo.SecretKey,
                new AmazonS3Config {ServiceURL = "http://s3.amazonaws.com"}))
            {
                 var req = new GetObjectRequest
                    {
                        BucketName = _connectionInfo.BucketName,
                        Key = keyName
                    };
                var response = s3.GetObject(req);
                return response.ResponseStream;
            }
        }

        public long GetTotalFileCount()
        {
            return GetTotalFileCount(_connectionInfo);
        }

        public long GetTotalFileCount(string prefixOrFolderName)
        {
            return GetTotalFileCount(new S3ConnectionString
            {
                AccessKey = _connectionInfo.AccessKey,
                SecretKey = _connectionInfo.SecretKey,
                BucketName = _connectionInfo.BucketName,
                FolderName = prefixOrFolderName
            });
        }

        public long GetTotalFileCount(string bucketName, string prefixOrFolderName)
        {
            return GetTotalFileCount(new S3ConnectionString
            {
                AccessKey = _connectionInfo.AccessKey,
                SecretKey = _connectionInfo.SecretKey,
                BucketName = bucketName,
                FolderName = prefixOrFolderName
            });
        }
        public static long GetTotalFileCount(string accessKey, string secretKey, string bucketName, string prefixOrFolder)
        {
            return GetTotalFileCount(new S3ConnectionString
            {
                AccessKey = accessKey,
                BucketName = bucketName,
                SecretKey = secretKey,
                FolderName = prefixOrFolder
            });
        }
        public static long GetTotalFileCount(S3ConnectionString connectionInfo)
        {
            using (var client = new AmazonS3Client(connectionInfo.AccessKey, connectionInfo.SecretKey,
                    new AmazonS3Config {ServiceURL = "http://s3.amazonaws.com"}))
            {
                var listObjectsRequest = new ListObjectsRequest
                {
                    BucketName = connectionInfo.BucketName,
                    Prefix = connectionInfo.FolderName
                };

                var marker = default(string);
                var totalCount = 0;

                var resp = default(ListObjectsResponse);
                do
                {
                    listObjectsRequest.Marker = marker;
                    resp = client.ListObjects(listObjectsRequest);
                    totalCount += resp.S3Objects.Count;

                    marker = resp.NextMarker;
                } while (resp.IsTruncated);

                return totalCount;
            }
        }
           
        public void DownloadFiles(string s3FolderName, string saveFolder)
        {
            DownloadFiles(_connectionInfo.AccessKey, _connectionInfo.SecretKey, _connectionInfo.BucketName, s3FolderName, saveFolder, false);
        }
        public static void DownloadFiles(string accessKey, string secretKey, string bucketName, string s3FolderName, string saveFolder, bool removeFromS3AfterDownload, Action<GetObjectResponse> onFileDownload = null, Action<DeleteObjectResponse> onFileDelete = null)
        {
            Log.DebugFormat("Starting download of files like '{0}' to '{1}'", bucketName + "/" + s3FolderName, saveFolder);
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
            Log.Debug("Finished downloading files from s3");
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