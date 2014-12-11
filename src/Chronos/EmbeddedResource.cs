using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Chronos
{
    public class EmbeddedResource
    {
        public static string Get(string resourceName)
        {
            var asm = default(Assembly);
            var fullname = FindFullyQualifiedName(resourceName, out asm);

            return GetTextFromResource(asm, fullname);
        }
        public static string GetTextFromResource(Assembly assembly, string fullyQualifiedName)
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
        public static string FindFullyQualifiedName(string resourceName, out Assembly assemblyFoundIn, Assembly assembly = null)
        {
            var pattern = string.Format(@"(?:(?!{0}))\.{0}", resourceName);
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
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic);
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
    }
}