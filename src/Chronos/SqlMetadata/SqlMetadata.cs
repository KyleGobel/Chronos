using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Chronos.Dapper.Chronos.Dapper;
using ServiceStack;

namespace Chronos.SqlMetadata
{
    public class TableMetadata
    {
        public string Database { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
    }

    public class ColumnMetadata
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public DbType DbType { get; set; }
        public int Length { get; set; }
    }

    public static class SqlMetadata
    {
        public static List<TableMetadata> GetTables(this IDbConnection connection)
        {
            const string sql =
                @"SELECT TABLE_CATALOG as Database, TABLE_SCHEMA as Schema, TABLE_NAME as Table FROM information_schema.tables";
            return connection.Query<TableMetadata>(sql).ToList();
        }

        public static List<IDbDataParameter> GetStoredProcedureParams(this IDbConnection connection,
            string storedProcName)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = storedProcName;
            cmd.CommandType = CommandType.StoredProcedure;
            var sqlCommand = cmd as SqlCommand;
            if (sqlCommand != null) SqlCommandBuilder.DeriveParameters(sqlCommand);
            return cmd.Parameters.Cast<IDbDataParameter>().ToList();
        }

        public static bool? IsColumnNullable(this SqlConnection connection, string table, string columnName)
        {
            const string sql =
                "select is_nullable from sys.columns where object_id = object_id(@table) and name = @column";
            return connection.Query<bool?>(sql, new {table, column = columnName}).FirstOrDefault();
        }

        public static List<ColumnMetadata> GetColumnMetadata(this SqlConnection connection, string schema,
            string tableName)
        {
            const string sql =
                @"select column_name, is_nullable, data_type, character_maximum_length from INFORMATION_SCHEMA.Columns where TABLE_SCHEMA = @schema and TABLE_NAME = @table";
            IEnumerable<dynamic> columns = connection.Query<dynamic>(sql, new {schema = schema, table = tableName});

            var metadata = new List<ColumnMetadata>();
            foreach (var col in columns)
            {
                DbType t;
                DbType.TryParse(col.data_type, true, out t);
                var m = new ColumnMetadata {Name = col.column_name, DbType = t};
                if (col.is_nullable)
                {
                    m.Type = DbTypeTypeMap[m.DbType].GetNullableType();
                }
                else
                {
                    m.Type = DbTypeTypeMap[m.DbType];
                }
                m.Length = col.character_maximum_length ?? 0;
                metadata.Add(col);
            }
            return metadata;
        }

        public static Dictionary<string, Type> GetColumnTypesFromQuery(this SqlConnection connection, string sql)
        {
            const string wrapperSql = "select top 0 * from ({0}) ____________________";
            using (var cmd = new SqlCommand(wrapperSql.Fmt(sql), connection))
            {
                var reader = cmd.ExecuteReader();

                var typeNameDictionary = new Dictionary<string, Type>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    typeNameDictionary.Add(reader.GetName(i), reader.GetFieldType(i));
                }
                return typeNameDictionary;
            }
        }
        public static Dictionary<DbType, Type> DbTypeTypeMap  = new Dictionary<DbType, Type>
        {
            {DbType.Int64, typeof(Int64)},
            {DbType.Binary, typeof(byte[])},
            {DbType.Boolean, typeof(bool)},
            {DbType.AnsiStringFixedLength, typeof(string)},
            {DbType.String, typeof(string)},
            {DbType.Date, typeof(DateTime)},
            {DbType.DateTime, typeof(DateTime)},
            {DbType.DateTime2, typeof(DateTime)},
            {DbType.DateTimeOffset, typeof(DateTimeOffset)},
            {DbType.Decimal, typeof(decimal)},
            {DbType.Double, typeof(double)},
            {DbType.Int32, typeof(int)},
            {DbType.StringFixedLength, typeof(string)},
            {DbType.Single, typeof(Single)},
            {DbType.Int16, typeof(Int16)},
            {DbType.Time, typeof(TimeSpan)},
            {DbType.Byte, typeof(byte)},
            {DbType.Guid, typeof(Guid)},
            {DbType.AnsiString, typeof(string)},
            {DbType.Xml, typeof(string)}
        };
    }

}