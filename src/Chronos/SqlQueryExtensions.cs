using System;
using System.IO;
using System.Reflection;
using Chronos.Configuration;

namespace Chronos
{
    public static class SqlQueryExtensions
    {
        public static string GetSqlQuery<T>(this T requestDto, string fullyQualifiedName = null, Action<string> queryFilter = null, SqlQueryConfiguration config = null) where T : class
        {
            var sqlConfig = config ?? new SqlQueryConfiguration();
            var namespacePart = sqlConfig.QueriesNamespace ?? typeof(T).Namespace;

            var sqlStatement = string.Empty;

            //name is the same as the requestDto, but with a .sql extension
            var resourceName = fullyQualifiedName ?? (namespacePart + "." + typeof(T).Name + ".sql");

            try
            {
                using (var stm = sqlConfig.Assembly.GetManifestResourceStream(resourceName))
                {
                    if (stm != null)
                    {
                        sqlStatement = new StreamReader(stm).ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Couldn't load query from manifest " + resourceName + " check inner exception for specific exception", ex);
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