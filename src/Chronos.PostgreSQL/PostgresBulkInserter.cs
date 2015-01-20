using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Chronos.Configuration;
using Chronos.Interfaces;
using FastMember;
using Npgsql;
using ServiceStack.Logging;

namespace Chronos.PostgreSQL
{
    public class PostgresBulkInserter : IBulkInserter
    {
        private readonly Type _type;
        private readonly string _connectionString;
        private static readonly ILog Log = LogManager.GetLogger(typeof (PostgresBulkInserter));
        private const char CsvDelimiter = ',';
        private const char CsvQuote = '"';
        private const string CsvReplacement = "$+$+";
        private static Regex _csvRegex = new Regex(@"("")", RegexOptions.Compiled);
        private const int NotifyAfter = 100;
        public PostgresBulkInserter(string nameOrConnectionString, Type type, Mappings columnMappings = null)
        {
            _type = type;
            try
            {
                _connectionString = ConfigUtilities.GetConnectionString(nameOrConnectionString);
            }
            catch (ConfigurationErrorsException configException)
            {
                _connectionString = nameOrConnectionString;
            }
            ColumnMappings = columnMappings ?? new Mappings(type).MapDirectly();
        }
        public Mappings ColumnMappings { get; set; }
        public void Insert(IEnumerable items, string tableName, Action<long> notifyRowsCopied = null, Action<Exception> onError = null)
        {
            var targetMethod = typeof(ObjectReader).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            var targetGenericMethod = targetMethod.MakeGenericMethod(new Type[] { _type });

            var castMethod = typeof(Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public);
            var castGenericMethod = castMethod.MakeGenericMethod(new Type[] { _type });

            var destinationColumnsString =  ColumnMappings.GetSqlBulkInsertMappings().Select(x => x.DestinationColumn).Aggregate((a, b) => a + "," + b);
            var sourceColumnsArray = ColumnMappings.GetSqlBulkInsertMappings().Select(x => x.SourceColumn).ToArray();

            var objectReader = (ObjectReader)targetGenericMethod.Invoke(null,
                    new[] { castGenericMethod.Invoke(null, new[] { items }), sourceColumnsArray });

            var connection = new NpgsqlConnection(_connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception x)
            {
                Log.Error(x);
                if (onError == null) throw;
                onError(x);
            };

            var bulk = new NpgsqlCopyIn(
                String.Format(CultureInfo.InvariantCulture, "COPY {0}({1}) FROM STDIN WITH CSV", tableName,
                    destinationColumnsString), connection);

            Log.DebugFormat("Starting bulk insert into '{0}'", tableName);
            try
            {
                bulk.Start();

                var stream = bulk.CopyStream;
                var writer = new StreamWriter(stream);

                var row = 0;
                IDataReader reader = objectReader as IDataReader;
                while (reader.Read())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (i > 0)
                            writer.Write(CsvDelimiter);
                        object value = reader.GetValue(i);
                        if (value != null)
                        {
                            writer.Write(CsvQuote);
                            writer.Write(_csvRegex.Replace(value.ToString(), CsvReplacement));
                            writer.Write(CsvQuote);
                        }
                    }
                    writer.WriteLine();

                    row++;
                    if (row % NotifyAfter == 0)
                    {
                        if (notifyRowsCopied != null)
                        {
                            notifyRowsCopied(row);
                        }
                    }
                }
                writer.Flush();
                bulk.End();
            }
            catch (Exception x)
            {
                Log.ErrorFormat("Error bulk inserting", x);
                bulk.Cancel(x.Message);
                if (onError == null) throw;

                onError(x);
            }
          
        }
    }
    public class PostgresBulkInserter<T> : IBulkInserter<T> where T : class
    {
        public Mappings<T> ColumnMappings { get; set; }

        private readonly string _connectionString;
        private static readonly ILog Log = LogManager.GetLogger(typeof (PostgresBulkInserter<T>));

        private const char CsvDelimiter = ',';
        private const char CsvQuote = '"';
        private const string CsvReplacement = "$+$+";
        private static Regex _csvRegex = new Regex(@"("")", RegexOptions.Compiled);
        private const int NotifyAfter = 100;

        public PostgresBulkInserter(string nameOrConnectionString, Mappings<T> columnMappings = null)
        {
            _connectionString = ConfigUtilities.GetConnectionString(nameOrConnectionString);

            ColumnMappings = columnMappings ?? new Mappings<T>().MapDirectly();
        }

        public void Insert(List<T> items,string tableName,Action<long> notifyRowsCopied = null, Action<Exception> onError = null)
        {
            var mappingsDictionary = ColumnMappings.GetSqlBulkInsertMappings().ToDictionary(x => x.SourceColumn, x => x.DestinationColumn);
            var destinationColumns = mappingsDictionary.Select(x => x.Value).Aggregate((a, b) => a + "," + b);
            var connection = new NpgsqlConnection(_connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception x)
            {
                Log.Error(x);
                if (onError == null) throw;
                onError(x);
            }
            var bulk =new NpgsqlCopyIn(
                    String.Format(CultureInfo.InvariantCulture, "COPY {0}({1}) FROM STDIN WITH CSV", tableName, destinationColumns), connection);

            try
            {
                RunInsert(bulk, tableName,items,mappingsDictionary.Select(x => x.Key).ToArray(), notifyRowsCopied); 
            }
            catch (Exception x)
            {
                Log.ErrorFormat("Error bulk inserting", x);
                bulk.Cancel(x.Message);
                if (onError == null) throw;

                onError(x);
            }

        }

        private void RunInsert(NpgsqlCopyIn bulk, string tableName, IEnumerable<T> items, string[] sourceColumns, Action<long> notifyRowsCopied = null)
        {
            Log.DebugFormat("Starting bulk insert into '{0}'", tableName);

            bulk.Start();

            var stream = bulk.CopyStream;
            var writer = new StreamWriter(stream);

            var row = 0;
            using (IDataReader reader = ObjectReader.Create(items,sourceColumns ))
            {
                while (reader.Read())
                {
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        if (i > 0)
                            writer.Write(CsvDelimiter);
                        object value = reader.GetValue(i);
                        if (value != null)
                        {
                            writer.Write(CsvQuote);
                            writer.Write(_csvRegex.Replace(value.ToString(), CsvReplacement));
                            writer.Write(CsvQuote);
                        }
                    }
                    writer.WriteLine();

                    row++;
                    if (row % NotifyAfter == 0)
                    {
                        if (notifyRowsCopied != null)
                        {
                            notifyRowsCopied(row);
                        }
                    }
                }
            }
            writer.Flush();
            bulk.End();
        }
    }
}
