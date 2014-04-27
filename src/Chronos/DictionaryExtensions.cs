using System.Collections.Generic;
using System.Reflection;

namespace Chronos
{
    public static class DictionaryExtensions
    {

        /// <summary>
        /// Adds the key value pair if the key doesn't exist in the dictionary, otherwise does nothing
        /// </summary>
        /// <returns>true if the key was added, false if the key already existed</returns>
        public static bool AddOnlyIfDoesntExist<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            TValue value)
        {
            if (dictionary.ContainsKey(key))
                return false;

            dictionary.Add(key,value);
            return true;
        }

        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
            {
                dictionary.Add(key,value);
            }
            return dictionary;
        }

       
         
    }
}