using System.Reflection;

namespace Chronos.Configuration
{
    public class SqlQueryConfiguration
    {
        public SqlQueryConfiguration()
        {
            StartDelimiter = "--start";
            Assembly = Assembly.GetExecutingAssembly();
            QueriesNamespace = default(string);
        }

        /// <summary>
        /// The point at which you want the query to start, it's useful to sometimes
        /// have stuff like variables defined that you only want at design time.
        /// 
        /// The default for this delimiter is --start (meaning the GetSqlQuery function will only return what
        /// comes after the '--start' text)
        /// </summary>
        public string StartDelimiter { get; set; }
        public string QueriesNamespace { get; set; }

        /// <summary>
        /// The Assembly to look for the embedded resource, default is the executing assembly
        /// </summary>
        public Assembly Assembly { get; set; }
    }
}