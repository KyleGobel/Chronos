using System;
using System.IO;
using System.Reflection;
using Chronos.Configuration;

namespace Chronos
{
    public static class SqlQueryExtensions
    {
        public static string GetSqlQuery<T>(this T requestDto, string filename = null, Action<string> queryFilter = null, SqlQueryConfiguration config = null) where T : class
        {
            var sqlConfig = config ?? new SqlQueryConfiguration();
            var namespacePart = sqlConfig.QueriesNamespace;

            var sqlStatement = string.Empty;

            //name is the same as the requestDto, but with a .sql extension
            var resourceName = filename ?? (namespacePart + "." + typeof(T).Name + ".sql");

            using (var stm = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stm != null)
                {
                    sqlStatement = new StreamReader(stm).ReadToEnd();
                }
            }
            var startIndex = sqlStatement.IndexOf(sqlConfig.StartDelimiter, StringComparison.CurrentCultureIgnoreCase);


            if (startIndex > 0)
            {
                return sqlStatement.Substring(startIndex + sqlConfig.StartDelimiter.Length);
            }

            if (queryFilter != null)
            {
                queryFilter(sqlStatement);
            }
            return sqlStatement;
        }
    }
}