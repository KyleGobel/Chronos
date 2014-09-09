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
            var fqName = FindFullyQualifiedName(queryName, out assembly, config.Assembly);
            var text = GetTextFromResource(assembly, fqName);

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

        private static string FindFullyQualifiedName(string queryName,out Assembly assemblyFoundIn, Assembly assembly = null)
        {
            var pattern = string.Format(@"(?:(?!{0}))\.{0}\.sql", queryName);
            if (assembly != null)
            {
                assemblyFoundIn = assembly;
                return assembly
                    .GetManifestResourceNames()
                    .FirstOrDefault(m => Regex.IsMatch(m, pattern, RegexOptions.IgnoreCase));
            }
            else
            {
                var firstMatch = "";
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    firstMatch = asm.GetManifestResourceNames()
                        .FirstOrDefault(m => Regex.IsMatch(m, pattern, RegexOptions.IgnoreCase));
                    if (firstMatch != null)
                    {
                        assemblyFoundIn = asm;
                        return firstMatch;
                    }
                }
                assemblyFoundIn = null;
                return firstMatch;
            }
        }

        private static string GetTextFromResource(Assembly assembly, string fullyQualifiedName)
        {
            var text = "";
            try
            {
                using (var stm = assembly.GetManifestResourceStream(fullyQualifiedName))
                {
                    if (stm != null)
                    {
                        text = new StreamReader(stm).ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Couldn't load query from manifest " + text + " check inner exception for specific exception", ex);
            }
            return text;
        }

    }
}