﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Chronos
{
    public static class DictionaryExtensions
    {
        public static DataTable ToDataTable(this IReadOnlyCollection<Dictionary<string, string>> list)
        {
            var result = new DataTable();
            if (list.Count == 0)
                return result;

            var columnNames = list.SelectMany(dict => dict.Keys).Distinct();
            result.Columns.AddRange(columnNames.Select(c => new DataColumn(c)).ToArray());
            foreach (var item in list)
            {
                var row = result.NewRow();
                foreach (var key in item.Keys)
                {
                    row[key] = item[key];
                }

                result.Rows.Add(row);
            }

            return result;
        }


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