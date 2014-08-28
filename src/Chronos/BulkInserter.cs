using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FastMember;
using ServiceStack;

namespace Chronos
{
    public class BulkInserter<T> where T : class
    {
        private readonly string _connectionString;
        public SqlBulkCopyOptions Options { get; set; }
        public BulkInserter(string nameOrConnectionString, BulkInsertColumnMappings<T> columnMappings = null)
        {
            try
            {
                _connectionString = Configuration.ConfigUtilities.GetConnectionString(_connectionString);
            }
            catch (ConfigurationErrorsException configException)
            {
                _connectionString = nameOrConnectionString;
            }
            ColumnMappings = columnMappings ?? new BulkInsertColumnMappings<T>();
            Options = SqlBulkCopyOptions.FireTriggers;
        }

        public BulkInsertColumnMappings<T> ColumnMappings { get; set; }

        public void Insert(List<T> items, string tableName, Action<long> notifyRowsCopied =null, Action<Exception> onError = null)
        {
            try
            {
                using (var bcp = new SqlBulkCopy(_connectionString, Options))
                using (var reader = ObjectReader.Create(items))
                {
                    ColumnMappings.GetMappings().ForEach(x => bcp.ColumnMappings.Add(x));
                    bcp.DestinationTableName = tableName;
                    bcp.SqlRowsCopied += (sender, args) => { if (notifyRowsCopied != null) notifyRowsCopied(args.RowsCopied); };
                    bcp.NotifyAfter = 100;
                    bcp.BatchSize = 5000;
                    bcp.WriteToServer(reader);
                }

            }
            catch (Exception x)
            {
                if (onError == null) throw;

                onError(x);
            }
        }
    }

    public class BulkInsertColumnMappings<T> where T : class
    {
        private readonly Dictionary<string, string> _mappings;

        public BulkInsertColumnMappings()
        {
            _mappings = new Dictionary<string, string>();
        }

        public List<SqlBulkCopyColumnMapping> GetMappings()
        {
            return _mappings.Select(x => new SqlBulkCopyColumnMapping(x.Key, x.Value)).ToList();
        }

        public BulkInsertColumnMappings<T> MapLowercaseAsLowercaseUnderscore()
        {
            var publicProps = typeof (T).GetProperties(BindingFlags.Public); 
            publicProps.ForEach(x => MapColumn(x.Name, x.Name.ToLowercaseUnderscore()));
            return this;
        }

        public BulkInsertColumnMappings<T> MapColumnsAsCamelCase()
        {
            var publicProps = typeof (T).GetProperties(BindingFlags.Public); 
            publicProps.ForEach(x => MapColumn(x.Name, x.Name.ToCamelCase()));
            return this;
        }

        public BulkInsertColumnMappings<T> MapColumn(string sourceProperty,
            string destinationColumn)
        {
            _mappings.AddOrUpdate(sourceProperty, destinationColumn);
            return this;
        }

        public BulkInsertColumnMappings<T> Except(string property)
        {
            _mappings.Remove(property);
            return this;
        }
 
        public BulkInsertColumnMappings<T> MapColumn<TProperty>(Expression<Func<T, TProperty>> sourceProperty,
            string destinationColumn) 
        {
            var name = sourceProperty.Body.ToString();
            var propertyName = name.Substring(name.LastIndexOf('.') + 1);

            _mappings.AddOrUpdate(propertyName, destinationColumn);
            return this;
        }
    }
}