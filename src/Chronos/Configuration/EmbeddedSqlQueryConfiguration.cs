using System.Reflection;

namespace Chronos.Configuration
{
    public class EmbeddedSqlQueryConfiguration
    {
        public EmbeddedSqlQueryConfiguration()
        {
            StartDelimiter = "--start";
            Assembly = null; 
        }

        /// <summary>
        /// The point at which you want the query to start, it's useful to sometimes
        /// have stuff like variables defined that you only want at design time.
        /// 
        /// The default for this delimiter is --start (meaning the GetSqlQuery function will only return what
        /// comes after the '--start' text)
        /// </summary>
        public string StartDelimiter { get; set; }

        /// <summary>
        /// The Assembly to look for the embedded resource, default is the request dto's assembly
        /// or the calling assembly if it's not available
        /// </summary>
        public Assembly Assembly { get; set; }
    }
}