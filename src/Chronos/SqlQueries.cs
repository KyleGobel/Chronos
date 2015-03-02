using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Chronos.Dapper.Chronos.Dapper;
using ServiceStack;
using ServiceStack.Text;

namespace Chronos
{
    public static class SqlQueryExtensions
    {

        public static List<T> QueryWithMap<T>(this IDbConnection connection, string sql, Mappings<T> mappings, object param = null, IDbTransaction transaction = null, bool buffered = true,int? commandTimeout = null, CommandType? commandType = null)
            where T : class, new()
        {
            var dyn = connection.Query<dynamic>(sql,param,transaction,buffered,commandTimeout,commandType).ToList();
            var maps = mappings.GetReverseMappings();
            var dictList = dyn.Select(x =>
                ((IDictionary<string, object>) x)
                    .Where(d => maps.ContainsKey(d.Key))
                    .ToDictionary(d => maps[d.Key], d => d.Value))
                .Select(x => x.To<T>())
                .ToList();
            

            return dictList;           
        }

        public static string QueryToJson(this IDbConnection connection, string sql, object param = null,
            IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null,
            CommandType? commandType = null)
        {
            using (JsConfig.With(dateHandler: DateHandler.ISO8601, includeNullValues:true))
            {
                var dyn =connection.Query<dynamic>(sql, 
                    param, transaction, buffered, commandTimeout, commandType)
                    .ToList();
                return dyn.ToJson();
            }

        }
        public static List<T> QueryWithMap<T>(this IDbConnection connection, string sql, Func<string, string> mapFunc, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null) where T : new()
        {
            var dyn = connection.Query<dynamic>(sql,param, transaction, buffered, commandTimeout, commandType).ToList();
            var dictList = dyn.Select(x =>
                ((IDictionary<string, object>) x)
                    .Where(d => mapFunc(d.Key) != null)
                    .ToDictionary(d => mapFunc(d.Key), d => d.Value))
                .Select(x => x.To<T>())
                .ToList();
            
            return dictList;
        }
    }
}