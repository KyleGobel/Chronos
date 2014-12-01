using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Chronos.Configuration;

namespace Chronos
{
    public static class EmbeddedSql
    {
        public static string GetSqlQuery<T>(this T requestDto, Action<string> queryFilter = null, EmbeddedSqlQueryConfiguration config = null) where T : class
        {
            return GetSqlQuery(typeof (T).Name, queryFilter,config);
        }

        public static string GetSqlQuery(string queryName,  Action<string> queryFilter = null, EmbeddedSqlQueryConfiguration config = null)
        {
            queryName = queryName.ToLower().Replace(".sql", "");
            config = config ?? new EmbeddedSqlQueryConfiguration();
            var assembly = default(Assembly);
            var fqName = EmbeddedResource.FindFullyQualifiedName(queryName, out assembly, config.Assembly);
            var text = EmbeddedResource.GetTextFromResource(assembly, fqName);

            var startIndex = text.IndexOf(config.StartDelimiter, StringComparison.CurrentCultureIgnoreCase);


            if (startIndex > 0)
            {
                return text.Substring(startIndex + config.StartDelimiter.Length);
            }

            if (queryFilter != null)
            {
                queryFilter(text);
            }
            return text;
        }
    }
}