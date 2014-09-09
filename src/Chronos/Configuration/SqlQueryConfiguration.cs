using System.Reflection;

namespace Chronos.Configuration
{
    public class SqlQueryConfiguration
    {
        public string StartDelimiter { get; set; }
        public string QueriesNamespace { get; set; }
        public Assembly Assembly { get; set; }
    }
}