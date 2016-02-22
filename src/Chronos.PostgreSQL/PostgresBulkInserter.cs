using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Chronos.Configuration;
using Chronos.Interfaces;
using FastMember;
using Npgsql;
using ServiceStack.Logging;
using static System.String;

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

        public void Insert(IEnumerable items, string tableName, Action<long> notifyRowsCopied = null,
            Action<Exception> onError = null)
        {
            var targetMethod = typeof (ObjectReader).GetMethod("Create", BindingFlags.Static | BindingFlags.Public);
            var targetGenericMethod = targetMethod.MakeGenericMethod(new Type[] {_type});

            var castMethod = typeof (Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public);
            var castGenericMethod = castMethod.MakeGenericMethod(new Type[] {_type});

            var destinationColumnsString =
                ColumnMappings.GetSqlBulkInsertMappings()
                    .Select(x => x.DestinationColumn)
                    .Aggregate((a, b) => a + "," + b);
            var sourceColumnsArray = ColumnMappings.GetSqlBulkInsertMappings().Select(x => x.SourceColumn).ToArray();

            var objectReader = (ObjectReader) targetGenericMethod.Invoke(null,
                new[] {castGenericMethod.Invoke(null, new[] {items}), sourceColumnsArray});

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

            var copyCommand = Format(CultureInfo.InvariantCulture, "COPY {0}({1}) FROM STDIN WITH CSV", tableName,
                destinationColumnsString);

            using (var writer = connection.BeginTextImport(copyCommand))
            {

                Log.DebugFormat("Starting bulk insert into '{0}'", tableName);
                try
                {
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
                        if (row%NotifyAfter == 0)
                        {
                            if (notifyRowsCopied != null)
                            {
                                notifyRowsCopied(row);
                            }
                        }
                    }
                    writer.Flush();
                }
                catch (Exception x)
                {
                    Log.ErrorFormat("Error bulk inserting", x);
                    if (onError == null) throw;

                    onError(x);
                }
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
            using (var connection = new NpgsqlConnection(_connectionString))
            {
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
                var copyCommand = string.Format(CultureInfo.InvariantCulture, "COPY {0}({1}) FROM STDIN WITH CSV",
                    tableName,
                    destinationColumns);
                using (var w = connection.BeginTextImport(copyCommand))
                {
                    try
                    {
                        RunInsert(w, tableName, items, mappingsDictionary.Select(x => x.Key).ToArray(), notifyRowsCopied);
                    }
                    catch (Exception x)
                    {
                        Log.ErrorFormat("Error bulk inserting", x);
                        if (onError == null) throw;

                        onError(x);
                    }
                }
            }

        }

        private void RunInsert(TextWriter writer, string tableName, IEnumerable<T> items, string[] sourceColumns, Action<long> notifyRowsCopied = null)
        {
            Log.DebugFormat("Starting bulk insert into '{0}'", tableName);



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
                        if (value != null && value.GetType().Name != "DBNull")
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
        }
    }
}
