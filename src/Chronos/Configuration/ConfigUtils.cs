using System;
using System.Configuration;
using System.Reflection;

namespace Chronos.Configuration
{
    /// <summary>
    /// Mostly just blatenly copied from ServiceStacks's ConfigUtils
    /// https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack/Configuration/ConfigUtils.cs
    /// </summary>
    public class ConfigUtils
    {
        private const string ErrorAppSettingNotFound = "Unable to find App Setting: {0}";
        private const string ErrorConnectionStringNotFound = "Unable to find Connection String: {0}";
        private const string ErrorCreatingType = "Error creating type {0} from text '{1}";
        private const string ConfigNullValue = "{null}";
        public static string GetAppSetting(string key)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (value == null)
                throw new ConfigurationErrorsException(string.Format(ErrorAppSettingNotFound, key));

            return value;
        }

        public static string GetConnectionString(string key)
        {
            var value = ConfigurationManager.ConnectionStrings[key];
            if (value == null)
                throw new ConfigurationErrorsException(string.Format(ErrorConnectionStringNotFound, key));

            return value.ConnectionString;
        }



        public static T GetAppSetting<T>(string key, T defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (value == null) return defaultValue;

            return ConfigNullValue.EndsWith(value) ? default(T) : ParseTextValue<T>(value);
        }

        /// <summary>
        /// Returns AppSetting[key] if exists otherwise defaultValue
        /// </summary>
        public static string GetAppSetting(string key, string defaultValue)
        {
            return ConfigurationManager.AppSettings[key] ?? defaultValue;
        }

        /// <summary>
        /// Adds or Updates the current exe config file key with the value
        /// uses the values ToString method to save the value
        /// </summary>
        public static void UpdateExeAppSetting<T>(string key, T value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = config.AppSettings.Settings;
            if (appSettings[key] == null)
            {
                appSettings.Add(key, value.ToString());
            }
            else
            {
                appSettings[key].Value = value.ToString();
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }


        /// <summary>
        /// Gets the constructor info for T(string) if exists.
        /// </summary>
        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            foreach (var ci in type.GetConstructors())
            {
                var ciTypes = ci.GetGenericArguments();
                var matchFound = (ciTypes.Length == 1 && ciTypes[0] == typeof(string)); //e.g. T(string)
                if (matchFound)
                {
                    return ci;
                }
            }
            return null;
        }


        /// <summary>
        /// Get the static Parse(string) method on the type supplied
        /// </summary>
        private static MethodInfo GetParseMethod(Type type)
        {
            const string parseMethod = "Parse";
            if (type == typeof(string))
            {
                return typeof(ConfigUtils).GetMethod(parseMethod, BindingFlags.Public | BindingFlags.Static);
            }
            var parseMethodInfo = type.GetMethod(parseMethod,
                                                 BindingFlags.Public | BindingFlags.Static, null,
                                                 new Type[] { typeof(string) }, null);

            return parseMethodInfo;
        }

        /// <summary>
        /// Returns the value returned by the 'T.Parse(string)' method if exists otherwise 'new T(string)'. 
        /// e.g. if T was a TimeSpan it will return TimeSpan.Parse(textValue).
        /// If there is no Parse Method it will attempt to create a new instance of the destined type
        /// </summary>
        private static T ParseTextValue<T>(string textValue)
        {
            var parseMethod = GetParseMethod(typeof(T));
            if (parseMethod == null)
            {
                var ci = GetConstructorInfo(typeof(T));
                if (ci == null)
                {
                    throw new TypeLoadException(string.Format(ErrorCreatingType, typeof(T).Name, textValue));
                }
                var newT = ci.Invoke(null, new object[] { textValue });
                return (T)newT;
            }
            var value = parseMethod.Invoke(null, new object[] { textValue });
            return (T)value;
        }

    }
}