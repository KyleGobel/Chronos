using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using ServiceStack;

namespace Chronos
{
    /// <summary>
    /// Custom Order Attribute to specify we are serializing this, and what order it should be in
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class OrderAttribute : Attribute
    {
        private readonly int _order; 
        public int Order { get { return _order; }}
        public OrderAttribute(int order)
        {
            _order = order;
        }
    }


    /// <summary>
    /// Static config class containing info about how we should serialize
    /// </summary>
    public static class TsvConfig
    {
        static TsvConfig()
        {
            Delimiter = "\t";
            LineEnding = Environment.NewLine;
            ReplaceDelimiterInValuesWith = " ";
            SerializeFnDictionary = new Dictionary<Type, Func<object, string>>();
            SerializeFnDictionary[typeof (DateTime)] = obj => ((DateTime) obj).ToUniversalTime().ToString("O");
            DeserialzeFnDictionary = new Dictionary<Type, Func<string, object>>();
        }
        public static Dictionary<Type, Func<object, string>> SerializeFnDictionary { get; set; } 
        public static Dictionary<Type, Func<string, object>> DeserialzeFnDictionary { get; set; } 
        public static string Delimiter { get; set; }
        public static string LineEnding { get; set; }
        public static string ReplaceDelimiterInValuesWith { get; set; }
        //public static Func<DateTime, string> DateTimeSerializeFn { get; set; }   
    }
    public class TsvFormatter
    {

        /// <summary>
        /// Standard issue memoize function
        /// </summary>
        private static Func<TIn, TOut> Memoize<TIn, TOut>(Func<TIn, TOut> func)
        {
            var cache = new Dictionary<TIn, TOut>();
            return (input =>
            {
                TOut result;
                if (cache.TryGetValue(input, out result)) return result;
                return cache[input] = func(input);
            });
        }


        public static readonly Func<Type, List<string>> GetSerializablePropertiesInOrder = Memoize<Type, List<string>>(
            t => t.GetProperties()
                .Where(x => HasOrderAttribute(x) || HasDataMemberAttribute(x))
                .OrderBy(x => GetOrder(x))
                .Select(x => x.Name)
                .ToList()),
            GetHeaderColumns = Memoize<Type, List<string>>(GetTsvHeaderValues);

        public static readonly Func<Type, string> GetHeaderRow = Memoize<Type, string>(x => MakeTsvRow(GetTsvHeaderValues(x)));

        private static readonly Func<PropertyInfo, int> GetOrder =
            Memoize<PropertyInfo, int>(t =>
            {
                if (HasOrderAttribute(t))
                {
                    var orderAttr =
                        t.GetCustomAttributes(false).Single(x => x.GetType().Name == "OrderAttribute") as OrderAttribute;

                    return orderAttr.Order;
                }

                if (HasDataMemberAttribute(t))
                {
                    var orderAttr =
                        t.GetCustomAttributes(false).Single(x => x.GetType().Name == "DataMemberAttribute") as
                            DataMemberAttribute;

                    return orderAttr.Order;
                }
                return -1;
            });

        private static readonly Func<PropertyInfo, bool> HasOrderAttribute =
            Memoize<PropertyInfo, bool>(t => t.GetCustomAttributes(false).Any(a => a.GetType().Name == "OrderAttribute"));

        private static readonly Func<PropertyInfo, bool> HasDataMemberAttribute =
            Memoize<PropertyInfo, bool>(
                t => t.GetCustomAttributes(false).Any(a => a.GetType().Name == "DataMemberAttribute"));
        public static string MakeTsvRow(IEnumerable<string> values)
        {
            return String.Concat(
                String.Join(TsvConfig.Delimiter, values), TsvConfig.LineEnding);
        }
        private static List<string> GetTsvHeaderValues(Type t)
        {
            var names = new List<string>();
          
            foreach(var prop in t.GetProperties().Where(p => HasOrderAttribute(p) || HasDataMemberAttribute(p)).OrderBy(x => GetOrder(x)))
            {
                var dispNameAttr = prop.GetCustomAttributes(false)
                    .FirstOrDefault(x => x.GetType().Name == "DisplayNameAttribute");

                if (dispNameAttr is DisplayNameAttribute)
                    names.Add((dispNameAttr as DisplayNameAttribute).DisplayName);
                else
                {
                    names.Add(prop.Name);
                }
            }
            return names;
        }
        public static string GetTsvRow<T>(List<string> properties, T obj)
        {
            var type = typeof (T);
            var dataCols = properties.Select(x => SerializeToString(type.GetProperty(x).GetValue(obj)));

            var tsvRow = String.Join(TsvConfig.Delimiter,dataCols);
            return String.Concat(tsvRow, TsvConfig.LineEnding);
        }

        private static string SerializeToString(object item)
        {
            if (item == null)
                return "";
            var type = item.GetType();
            Func<object, string> serializeFn;
            if (TsvConfig.SerializeFnDictionary.TryGetValue(type, out serializeFn))
            {
                return serializeFn(item);
            }
            return item.ToString().Replace(TsvConfig.Delimiter, TsvConfig.ReplaceDelimiterInValuesWith);
        }

        public static Func<Type, SortedDictionary<int, KeyValuePair<string, Type>>> PropertyNamesDictionary =
            Memoize<Type, SortedDictionary<int, KeyValuePair<string, Type>>>(type =>
            {
                var sd = new SortedDictionary<int, KeyValuePair<string, Type>>();
                var index = 0;
                //ugh, this should be a reduce here..breaking my functionalness
                GetSerializablePropertiesInOrder(type).ForEach(x =>
                {
                    sd.Add(index, new KeyValuePair<string, Type>(x, type.GetProperty(x).PropertyType));
                    index++;
                });

                return sd;
            });

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        public static object GetValue(object obj, Type type)
        {
            return IsNullableType(type) ? GetNullableValue(obj, type) : GetNonNullableValue(obj, type);
        }

        private static object GetNullableValue(object obj, Type nullableType)
        {
            var underlyingType = Nullable.GetUnderlyingType(nullableType);

            if (string.IsNullOrEmpty(obj.ToString()))
                return null;
            else
                return GetValue(obj, underlyingType);
        }
        private static object GetNonNullableValue(object obj, Type type)
        {
            if (obj != null)
            {
                var nullable = type.IsNullableType();
                if (type == typeof(string))
                    return obj;
                var value = GetDefault(type);
                var methodInfo = (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                  where m.Name == "TryParse"
                                  select m).FirstOrDefault();

                if (methodInfo == null)
                    throw new ApplicationException("Unable to find TryParse method!");

                object result = methodInfo.Invoke(null, new object[] { obj, value });
                if ((result != null) && ((bool)result))
                {
                    methodInfo = (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                  where m.Name == "Parse"
                                  select m).FirstOrDefault();
                    if (methodInfo == null)
                        throw new ApplicationException("Unable to find Parse method!");
                    value = methodInfo.Invoke(null, new object[] { obj });
                    return value;
                }
            }

            return GetDefault(type);
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
    }

    public static class TsvExtensions
    {
        public static string ToTsvDataRow<T>(this T objToSerialize) where T : class
        {
            var properties = TsvFormatter.GetSerializablePropertiesInOrder(typeof (T));

            return TsvFormatter.GetTsvRow(properties, objToSerialize);
        }

        public static string ToTsvHeaderRow<T>(this T objToSerialize) where T : class
        {
            return TsvFormatter.GetHeaderRow(typeof (T));
        }

        public static string ToTsv<T>(this IEnumerable<T> collectionToSerialize, bool includeHeaders = true) where T : class
        {
            var data = string.Join("", collectionToSerialize.Select(x => x.ToTsvDataRow()));
            return includeHeaders ? string.Concat(TsvFormatter.GetHeaderRow(typeof (T)), data) : data;
        }
    
     
        public static T FromTsvRow<T>(this string tsvString) where T : new()
        {
            if (string.IsNullOrEmpty(tsvString))
                return default(T);

            var tsvValues = tsvString.Split(new [] {TsvConfig.Delimiter}, StringSplitOptions.None);
            var propsDic = TsvFormatter.PropertyNamesDictionary(typeof (T));

            var obj = new T();
            var type = typeof (T);
            var index = 0;
            foreach (var p in propsDic.OrderBy(x => x.Key))
            {
                var value = TsvConfig.DeserialzeFnDictionary.ContainsKey(p.Value.Value)
                    ? TsvConfig.DeserialzeFnDictionary[p.Value.Value](tsvValues[index])
                    : TsvFormatter.GetValue(tsvValues[index],p.Value.Value);

                type.GetProperty(p.Value.Key).SetValue(obj, value);
                index++;
            }

            return obj;
        }


        public static List<T> FromTsv<T>(this string tsvString, bool stringIncludesHeader = true) where T : new()
        {
            if (stringIncludesHeader)
            {
                return tsvString.RemoveTsvHeaderRow().Split(new[] { TsvConfig.LineEnding }, StringSplitOptions.None)
                    .Select(x => x.FromTsvRow<T>())
                    .ToList();
            }

            return tsvString.Split(new[] {TsvConfig.LineEnding}, StringSplitOptions.None)
                .Select(x => x.FromTsvRow<T>())
                .ToList();

        }

        public static string RemoveTsvHeaderRow(this string tsvStringWithHeader)
        {
            if (tsvStringWithHeader.Contains(TsvConfig.LineEnding))
            {
                 return tsvStringWithHeader.Substring(
                        tsvStringWithHeader.IndexOf(TsvConfig.LineEnding + TsvConfig.LineEnding.Length, System.StringComparison.Ordinal));

            }
            return tsvStringWithHeader;
        }
    }
}
