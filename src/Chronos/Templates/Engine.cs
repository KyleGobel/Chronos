using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Chronos.Interfaces;
using Chronos.SqlMetadata;

namespace Chronos.Templates
{
    public class Engine
    {
        private readonly ITemplateEngine _templateEngine;
        public Dictionary<DbType, string> DbTypeDictionary;
        public Dictionary<Type, string> TypeTemplateDictionary;
        private static MethodInfo _humanizeMethod;
        public Func<string,string> ParamNameFilter ;

        public Engine(ITemplateEngine templateEngine)
        {
            _humanizeMethod = typeof (string).GetExtensionMethod("Humanize");
            ParamNameFilter = HumanizeIfAvail;
            _templateEngine = templateEngine;
            DbTypeDictionary = new Dictionary<DbType, string>
            {
                {DbType.DateTime, EmbeddedResource.Get("DateTime.html")}
            };

            TypeTemplateDictionary = new Dictionary<Type, string>
            {
                {typeof(string), EmbeddedResource.Get("String.html")},
                {typeof(DateTime), EmbeddedResource.Get("DateTime.html")}
            };
        }

        private string HumanizeIfAvail(string str)
        {
            if (_humanizeMethod == null)
                return str;

            try
            {
                return (string) _humanizeMethod.Invoke(null, new object[] {str});
            }
            catch (Exception)
            {
                return str;
            }
        }

        public string GetTemplateForStoredProcedure(IDbConnection connection ,string sprocName)
        {
            var html = string.Empty;
            var p = connection.GetStoredProcedureParams(sprocName);
            html = p.Aggregate(html, (current, param) =>
            {
                if (DbTypeDictionary.ContainsKey(param.DbType))
                {
                    var paramName = default(string);
                    paramName = ParamNameFilter(param.ParameterName.Replace("@", ""));
                    return current + _templateEngine.RenderToString(DbTypeDictionary[param.DbType], new { DbType = param.DbType, UglyName = param.ParameterName.Replace("@", ""), PrettyName = paramName});
                }
                else
                    return current;
            });
            return html;
        }
    }
}