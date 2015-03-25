using System;

namespace Chronos.AWS
{
    public class LoadRedshiftConfig
    {
        public bool PerformMerge { get; set; }
        public string DataDirectoryPrefix { get; set; } 
        public string DataFileExtension { get; set; }
        public string ProcessingDirectoryPrefix { get; set; }
        public string CompletedDirectoryPrefix { get; set; }
        public string ErrorDirectoryPrefix { get; set; }
        public string TableName { get; set; }
        public string[] TableColumns { get; set; }
        public int HeaderRowCount { get; set; }
        public string[] PrimaryKeyColumns { get; set; }
        public string ConnectionStringName { get; set; }
        public Action<Exception, string> ErrorLog { get; set; }
        public Action<string> DebugLog { get; set; }
    }
}