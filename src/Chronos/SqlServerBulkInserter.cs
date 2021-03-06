﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Chronos.Interfaces;
using FastMember;
using ServiceStack;
using ServiceStack.Logging;

namespace Chronos
{
    public class SqlServerBulkInserter<T> : IBulkInserter<T> where T : class
    {
        private readonly string _connectionString;
        private static readonly ILog Log = LogManager.GetLogger(typeof (SqlServerBulkInserter<T>));
        public SqlBulkCopyOptions Options { get; set; }
        public int BulkInsertTimeout { get; set; }
        public SqlServerBulkInserter(string nameOrConnectionString, Mappings<T> columnMappings = null)
        {
            try
            {
                _connectionString = Configuration.ConfigUtilities.GetConnectionString(nameOrConnectionString);
            }
            catch (ConfigurationErrorsException configException)
            {
                _connectionString = nameOrConnectionString;
            }
            ColumnMappings = columnMappings ?? new Mappings<T>().MapDirectly();
            Options = SqlBulkCopyOptions.FireTriggers;
            BulkInsertTimeout = 0;
        }

        public Mappings<T> ColumnMappings { get; set; }

        public void Insert(List<T> items, string tableName, Action<long> notifyRowsCopied =null, Action<Exception> onError = null)
        {
            try
            {
                Log.DebugFormat("Starting bulk insert into '{0}'",tableName);
                using (var bcp = new SqlBulkCopy(_connectionString, Options))
                using (var reader = ObjectReader.Create(items))
                {
                    ColumnMappings.GetSqlBulkInsertMappings().ForEach(x => bcp.ColumnMappings.Add(x));
                    bcp.DestinationTableName = tableName;
                    bcp.SqlRowsCopied += (sender, args) => { if (notifyRowsCopied != null) notifyRowsCopied(args.RowsCopied); };
                    bcp.NotifyAfter = 100;
                    bcp.BatchSize = 5000;
                    bcp.BulkCopyTimeout = BulkInsertTimeout;
                    bcp.WriteToServer(reader);
                }

            }
            catch (Exception x)
            {
                Log.ErrorFormat("Error bulk inserting", x);
                if (onError == null) throw;

                onError(x);
            }
        }
    }

    public class SqlServerBulkInserter : IBulkInserter
    {
        private readonly Type _type;
        private readonly string _connectionString;
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerBulkInserter));
        public SqlBulkCopyOptions Options { get; set; }
        public int BulkInsertTimeout { get; set; }

        public SqlServerBulkInserter(string nameOrConnectionString)
        {
            try
            {
                _connectionString = Configuration.ConfigUtilities.GetConnectionString(nameOrConnectionString);
            }
            catch (ConfigurationErrorsException configException)
            {
                _connectionString = nameOrConnectionString;
            }
            Options = SqlBulkCopyOptions.FireTriggers;
            BulkInsertTimeout = 0;

            ColumnMappings = new Mappings();
        }

        public SqlServerBulkInserter(string nameOrConnectionString,Type type, Mappings columnMappings = null)
        {
            _type = type;
            try
            {
                _connectionString = Configuration.ConfigUtilities.GetConnectionString(nameOrConnectionString);
            }
            catch (ConfigurationErrorsException configException)
            {
                _connectionString = nameOrConnectionString;
            }
            ColumnMappings = columnMappings ?? new Mappings(type).MapDirectly();
            Options = SqlBulkCopyOptions.FireTriggers;
            BulkInsertTimeout = 0;
        }

        public Mappings ColumnMappings { get; set; }

        /// <summary>
        /// Insert json into a table
        /// </summary>
        /// <param name="serializer">The serializer to use, must be able to deserialize json to a string dictionary</param>
        /// <param name="json">Json to insert</param>
        /// <param name="tableName">The table to insert to</param>
        /// <param name="colMappingFunc">The mapping function to use, takes the json key -> and returns the table column name to map to, if none supplied it will use the existing mappings (you have to add them or this will error)</param>
        /// <param name="notifyRowsCopied">optional action to run on x number of rows inserted</param>
        /// <param name="onError">optional action to run on an exception, will throw the error instead of this isn't supplied</param>
        public void Insert(ISerializer serializer, string json, string tableName, Func<string,string> colMappingFunc = null, Action<long> notifyRowsCopied = null,
            Action<Exception> onError = null)
        {
            var dictionary = serializer.Deserialize<List<Dictionary<string, string>>>(json);

            if (colMappingFunc != null)
            {
                var allKeys = dictionary.SelectMany(x => x.Select(y => y.Key)).Distinct();

                allKeys.ForEach(x => ColumnMappings.Map(x, colMappingFunc(x)));
            }


            var dt = dictionary.ToDataTable();
            Insert(dt, tableName, notifyRowsCopied, onError);
        }
        public void Insert(DataTable data, string tableName, Action<long> notifyRowsCopied = null,
            Action<Exception> onError = null)
        {
            try
            {
                using (var bcp = new SqlBulkCopy(_connectionString, Options))
                {
                    ColumnMappings.GetSqlBulkInsertMappings().ForEach(x => bcp.ColumnMappings.Add(x));
                    bcp.DestinationTableName = tableName;
                    bcp.SqlRowsCopied +=
                        (sender, args) => { if (notifyRowsCopied != null) notifyRowsCopied(args.RowsCopied); };
                    bcp.NotifyAfter = 100;
                    bcp.BatchSize = 5000;
                    bcp.BulkCopyTimeout = BulkInsertTimeout;
                    bcp.WriteToServer(data);
                }
            }
            catch (Exception x)
            {
                Log.ErrorFormat("Error bulk inserting", x);
                if (onError == null) throw;

                onError(x);
            }
        }
        public void Insert(IEnumerable items, string tableName, Action<long> notifyRowsCopied = null, Action<Exception> onError = null)
        {

            var targetMethod = typeof (ObjectReader).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            var targetGenericMethod = targetMethod.MakeGenericMethod(new Type[] {_type});

            var castMethod = typeof (Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public);
            var castGenericMethod = castMethod.MakeGenericMethod(new Type[] {_type});
            try
            {
                var objectReader = (ObjectReader)targetGenericMethod.Invoke(null,
                    new[] {castGenericMethod.Invoke(null, new [] {items }), null});
                Log.DebugFormat("Starting bulk insert into '{0}'", tableName);
                using (var bcp = new SqlBulkCopy(_connectionString, Options))
                using (var reader = objectReader)
                {
                    ColumnMappings.GetSqlBulkInsertMappings().ForEach(x => bcp.ColumnMappings.Add(x));
                    bcp.DestinationTableName = tableName;
                    bcp.SqlRowsCopied += (sender, args) => { if (notifyRowsCopied != null) notifyRowsCopied(args.RowsCopied); };
                    bcp.NotifyAfter = 100;
                    bcp.BatchSize = 5000;
                    bcp.BulkCopyTimeout = BulkInsertTimeout;
                    bcp.WriteToServer(reader);
                }

            }
            catch (Exception x)
            {
                Log.ErrorFormat("Error bulk inserting", x);
                if (onError == null) throw;

                onError(x);
            }
        }
    }

    public class Mappings
    {
        private readonly Type _type;
        private readonly Dictionary<string, string> _mappings;
        public Mappings(Type type)
        {
            _type = type;
            _mappings = new Dictionary<string, string>();
        }

        public Mappings()
        {
            _mappings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds a dictionary of sourceColumn -> destinationColumn mappings to the existing mappings
        /// </summary>
        public Mappings AddStringDictionary(Dictionary<string, string> colMappingsToAdd)
        {
            foreach (var kvp in colMappingsToAdd)
            {
                _mappings.AddOrUpdate(kvp.Key, kvp.Value);
            }

            return this;
        }

        public Mappings ClearMappings()
        {
            _mappings.Clear();
            return this;
        }
        public List<SqlBulkCopyColumnMapping> GetSqlBulkInsertMappings()
        {
            return _mappings.Select(x => new SqlBulkCopyColumnMapping(x.Key, x.Value)).ToList();
        }

        public Dictionary<string, string> GetMappingsAsDictionary()
        {
            return _mappings;
        }
        public Mappings MapAsLowercaseUnderscore()
        {
            var publicProps = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            publicProps.ForEach(x => Map(x.Name, x.Name.ToLowercaseUnderscore()));
            return this;
        }
        public Mappings Map(string sourceProperty,
          string destinationColumn)
        {
            _mappings.AddOrUpdate(sourceProperty, destinationColumn);
            return this;
        }

        public Mappings MapAsCamelCase()
        {
            var publicProps = _type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            publicProps.ForEach(x => Map(x.Name, x.Name.ToCamelCase()));
            return this;
        }

        public Mappings MapDirectly()
        {
            var publicProps = _type.GetProperties(BindingFlags.Public |BindingFlags.Instance);
            publicProps.ForEach(x => Map(x.Name, x.Name));
            return this;
        }

        public Mappings Except(string property)
        {
            _mappings.Remove(property);
            return this;
        }

    }
    public class Mappings<T> where T : class
    {
        private readonly Dictionary<string, string> _mappings;

        public Mappings()
        {
            _mappings = new Dictionary<string, string>();
        }

        public List<SqlBulkCopyColumnMapping> GetSqlBulkInsertMappings()
        {
            return _mappings.Select(x => new SqlBulkCopyColumnMapping(x.Key, x.Value)).ToList();
        }

        public Dictionary<string, string> GetMappingsAsDictionary()
        {
            return _mappings;
        }

        /// <summary>
        /// Will only work if there is 1 to 1 mappings
        /// </summary>
        public Dictionary<string, string> GetReverseMappings()
        {
            return _mappings.ToDictionary(x => x.Value, x => x.Key);
        }
        public Mappings<T> MapAsLowercaseUnderscore()
        {
            var publicProps = typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance); 
            publicProps.ForEach(x => Map(x.Name, x.Name.ToLowercaseUnderscore()));
            return this;
        }

        public Mappings<T> MapAsCamelCase()
        {
            var publicProps = typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance); 
            publicProps.ForEach(x => Map(x.Name, x.Name.ToCamelCase()));
            return this;
        }

        public Mappings<T> Map(string sourceProperty,
            string destinationColumn)
        {
            _mappings.AddOrUpdate(sourceProperty, destinationColumn);
            return this;
        }

        public Mappings<T> Except(string property)
        {
            _mappings.Remove(property);
            return this;
        }
        public Mappings<T> MapDirectly()
        {
            var publicProps = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            publicProps.ForEach(x => Map(x.Name, x.Name));
            return this;
        }
 
        public Mappings<T> Map<TProperty>(Expression<Func<T, TProperty>> sourceProperty,
            string destinationColumn) 
        {
            var name = sourceProperty.Body.ToString();
            var propertyName = name.Substring(name.LastIndexOf('.') + 1);

            _mappings.AddOrUpdate(propertyName, destinationColumn);
            return this;
        }
    }
}